using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// When activated: kills the animator, spends a fixed amount of time turning to
/// face the target, then charges off in that direction until it reaches it.
/// After that it can settle into actively shadowing the target, holding a set gap.
/// </summary>
public class ApproachOnActivate : MonoBehaviour
{
    private enum State { Idle, Delaying, Turning, Waiting, Charging, Arrived, Following }

    public enum ArrivalBehaviour
    {
        StopForGood,          // freezes once it reaches you
        ChargeAgainIfYouMove, // waits, then does a fresh turn-and-charge when you leave range
        KeepFollowing         // shadows you continuously, holding followDistance
    }

    [Header("Target")]
    [Tooltip("Leave empty to grab Camera.main at activation time.")]
    [SerializeField] private Transform target;

    [Header("Activation")]
    [Tooltip("Start on the first enable. Later re-enables resume where it left off instead of starting over.")]
    [SerializeField] private bool activateOnEnable = false;

    [Tooltip("Seconds to hold still after Activate() before it starts turning. The animator is already off, so it sits frozen mid-pose for this whole window.")]
    [SerializeField] private float activationDelay = 0f;

    [Tooltip("Switched off the instant Activate() runs, before the delay even starts counting. Leave empty to auto-find on this object or its children.")]
    [SerializeField] private Animator animatorToDisable;

    [Header("Turning")]
    [Tooltip("Seconds the turn takes, no matter how far it has to rotate.")]
    [SerializeField] private float turnDuration = 0.8f;

    [Tooltip("Shape of the turn over that duration. Straight line = constant speed. Dip below 0 or above 1 for windup and overshoot.")]
    [SerializeField] private AnimationCurve turnEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Keep re-aiming while turning. Off = locks onto where the target was when the turn started.")]
    [SerializeField] private bool trackTargetWhileTurning = false;

    [Tooltip("Stay upright instead of tilting up at the camera.")]
    [SerializeField] private bool yAxisOnly = true;

    [Tooltip("Beat to hold after the turn finishes, before it moves.")]
    [SerializeField] private float pauseAfterTurn = 0f;

    [Header("Charging")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("How close the charge gets before it counts as having reached you.")]
    [SerializeField] private float stopDistance = 1.5f;

    [Tooltip("Run in a straight line along the locked heading. Off = homes in and never needs to re-turn.")]
    [SerializeField] private bool lockDirection = true;

    [Tooltip("If the target drifts more than this many degrees off the heading, stop and turn again. 0 = never re-turn.")]
    [SerializeField] private float retargetAngle = 25f;

    [Header("Following")]
    [Tooltip("What it does once the charge lands.")]
    [SerializeField] private ArrivalBehaviour onArrival = ArrivalBehaviour.KeepFollowing;

    [Tooltip("The gap it holds while shadowing you. Usually the same as Stop Distance, but set it larger to have it back off after the charge.")]
    [SerializeField] private float followDistance = 2f;

    [Tooltip("On = sits directly behind the target's back and swings around as they turn. Off = just holds the gap from wherever it already is.")]
    [SerializeField] private bool holdBehindPlayer = true;

    [Tooltip("How fast it shadows you. Set this above your walk speed or it'll fall behind.")]
    [SerializeField] private float followSpeed = 3.5f;

    [Tooltip("Slack around the hold spot so it doesn't twitch while you stand still.")]
    [SerializeField] private float repositionDeadzone = 0.15f;

    [Tooltip("How fast it swivels to keep facing you while shadowing, in degrees per second. 0 or less snaps.")]
    [SerializeField] private float followTurnSpeed = 220f;

    [Tooltip("If you get further than this it winds up and charges again. 0 = never, it just walks after you.")]
    [SerializeField] private float reChargeDistance = 8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTurnStarted;
    [SerializeField] private UnityEvent onChargeStarted;
    [SerializeField] private UnityEvent onArrived;

    private State state = State.Idle;
    private Quaternion turnStartRotation;
    private Quaternion turnEndRotation;
    private float turnTimer;
    private float waitTimer;
    private float delayTimer;
    private Vector3 chargeDirection;

    public bool IsActive => state != State.Idle && state != State.Arrived;
    public bool HasArrived => state == State.Arrived || state == State.Following;
    public bool IsFollowing => state == State.Following;

    private void Awake()
    {
        // true = include inactive children, so a disabled rig still gets found.
        if (animatorToDisable == null)
            animatorToDisable = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        // Already mid-run (or finished), so this is a re-enable rather than a first start.
        // Every timer and the current state survived being switched off, so just let Update pick back up.
        if (state != State.Idle)
        {
            // Re-assert in case something turned the animator back on while we were off.
            // Skipped once we've reached you, in case you hooked ReEnableAnimator to onArrived.
            if (state != State.Arrived && state != State.Following && animatorToDisable != null)
                animatorToDisable.enabled = false;

            return;
        }

        if (activateOnEnable) Activate();
    }

    // ---------- public API ----------

    /// <summary>Kills the animator right away, then waits out activationDelay before it starts turning.</summary>
    public void Activate()
    {
        // Immediate, before the delay even starts counting.
        if (animatorToDisable != null)
            animatorToDisable.enabled = false;

        if (target == null && Camera.main != null)
            target = Camera.main.transform;

        if (target == null)
        {
            Debug.LogWarning($"{name}: no target assigned and no Camera.main in the scene.", this);
            return;
        }

        if (activationDelay > 0f)
        {
            delayTimer = activationDelay;
            state = State.Delaying;
            return;
        }

        BeginTurn();
    }

    /// <summary>Cuts the remaining delay short and starts turning right now.</summary>
    public void SkipDelay()
    {
        if (state == State.Delaying) BeginTurn();
    }

    /// <summary>Stops everything where it stands. It will resume from here if re-enabled.</summary>
    public void Deactivate()
    {
        state = State.Idle;
    }

    /// <summary>Wipes the run so the next Activate() or enable starts from scratch.</summary>
    public void ResetSequence()
    {
        state = State.Idle;
        delayTimer = 0f;
        turnTimer = 0f;
        waitTimer = 0f;
    }

    /// <summary>Forces a clean run from the top, delay and all.</summary>
    public void Restart()
    {
        ResetSequence();
        Activate();
    }

    /// <summary>Drops out of shadowing and winds up a fresh charge.</summary>
    public void ChargeNow()
    {
        if (state == State.Following || state == State.Arrived) BeginTurn();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetFollowDistance(float distance)
    {
        followDistance = Mathf.Max(0f, distance);
    }

    /// <summary>Hook to onArrived if you want a walk cycle playing while it shadows you.</summary>
    public void ReEnableAnimator()
    {
        if (animatorToDisable != null)
            animatorToDisable.enabled = true;
    }

    // ---------- state machine ----------

    private void Update()
    {
        if (target == null) return;

        switch (state)
        {
            case State.Delaying:
                delayTimer -= Time.deltaTime;
                if (delayTimer <= 0f) BeginTurn();
                break;

            case State.Turning:
                TickTurn();
                break;

            case State.Waiting:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f) BeginCharge();
                break;

            case State.Charging:
                TickCharge();
                break;

            case State.Following:
                TickFollow();
                break;

            case State.Arrived:
                if (onArrival == ArrivalBehaviour.ChargeAgainIfYouMove && FlatOffset().magnitude > stopDistance)
                    BeginTurn();
                break;
        }
    }

    private void BeginTurn()
    {
        turnStartRotation = transform.rotation;
        turnEndRotation = LookRotationToTarget();
        turnTimer = 0f;
        state = State.Turning;
        onTurnStarted?.Invoke();

        if (turnDuration <= 0f)
        {
            transform.rotation = turnEndRotation;
            FinishTurn();
        }
    }

    private void TickTurn()
    {
        turnTimer += Time.deltaTime;
        float t = Mathf.Clamp01(turnTimer / turnDuration);

        if (trackTargetWhileTurning)
            turnEndRotation = LookRotationToTarget();

        // Unclamped so curves that dip past 0 or overshoot 1 actually read as windup / whip.
        transform.rotation = Quaternion.SlerpUnclamped(turnStartRotation, turnEndRotation, turnEase.Evaluate(t));

        if (t >= 1f) FinishTurn();
    }

    private void FinishTurn()
    {
        transform.rotation = turnEndRotation;

        chargeDirection = transform.forward;
        if (yAxisOnly) chargeDirection.y = 0f;
        chargeDirection.Normalize();

        if (pauseAfterTurn > 0f)
        {
            waitTimer = pauseAfterTurn;
            state = State.Waiting;
        }
        else
        {
            BeginCharge();
        }
    }

    private void BeginCharge()
    {
        state = State.Charging;
        onChargeStarted?.Invoke();
    }

    private void TickCharge()
    {
        Vector3 toTarget = FlatOffset();
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
        {
            state = onArrival == ArrivalBehaviour.KeepFollowing ? State.Following : State.Arrived;
            onArrived?.Invoke();
            return;
        }

        Vector3 heading = lockDirection ? chargeDirection : toTarget / distance;

        // Target has slipped off the heading, so stop and line up again.
        if (lockDirection && retargetAngle > 0f && Vector3.Angle(heading, toTarget) > retargetAngle)
        {
            BeginTurn();
            return;
        }

        if (!lockDirection)
            transform.rotation = Quaternion.LookRotation(heading, Vector3.up);

        float step = moveSpeed * Time.deltaTime;
        if (!lockDirection) step = Mathf.Min(step, distance - stopDistance);

        transform.position += heading * step;
    }

    // ---------- following ----------

    private void TickFollow()
    {
        // Keep looking at you the whole time, at a plain degrees-per-second rate.
        Quaternion desired = LookRotationToTarget();
        transform.rotation = followTurnSpeed <= 0f
            ? desired
            : Quaternion.RotateTowards(transform.rotation, desired, followTurnSpeed * Time.deltaTime);

        // Big gap opened up, so wind up and charge instead of jogging after you.
        if (reChargeDistance > 0f && FlatOffset().magnitude > reChargeDistance)
        {
            BeginTurn();
            return;
        }

        Vector3 offset = GetHoldPoint() - transform.position;
        if (yAxisOnly) offset.y = 0f;

        float distance = offset.magnitude;
        if (distance <= repositionDeadzone) return;

        float step = Mathf.Min(followSpeed * Time.deltaTime, distance);
        transform.position += (offset / distance) * step;
    }

    /// <summary>The spot it wants to occupy: either the target's back, or just followDistance out along the current line.</summary>
    private Vector3 GetHoldPoint()
    {
        Vector3 outward;

        if (holdBehindPlayer)
        {
            outward = -target.forward;
        }
        else
        {
            outward = transform.position - target.position;
            if (yAxisOnly) outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f) outward = -target.forward;
        }

        if (yAxisOnly) outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f) return target.position;

        return target.position + outward.normalized * followDistance;
    }

    // ---------- helpers ----------

    private Vector3 FlatOffset()
    {
        Vector3 offset = target.position - transform.position;
        if (yAxisOnly) offset.y = 0f;
        return offset;
    }

    private Quaternion LookRotationToTarget()
    {
        Vector3 offset = FlatOffset();
        if (offset.sqrMagnitude < 0.0001f) return transform.rotation;
        return Quaternion.LookRotation(offset.normalized, Vector3.up);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        if (target == null) return;

        // The ring it tries to sit on, plus the exact spot on it.
        Gizmos.color = new Color(0.2f, 0.8f, 1f);
        Gizmos.DrawWireSphere(target.position, followDistance);

        if (Application.isPlaying)
            Gizmos.DrawLine(transform.position, GetHoldPoint());
    }
}