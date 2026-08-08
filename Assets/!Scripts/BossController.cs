using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// One state's visual: its own GameObject/SpriteRenderer (scaled by hand so every state
/// lines up) plus the frames to animate. Only one of these is active at a time.
/// </summary>
[System.Serializable]
public class BossStateVisual
{
    [Tooltip("This state's own SpriteRenderer object. Scale it independently so all states match.")]
    public SpriteRenderer renderer;
    [Tooltip("Frames cycled while this state is active. Assign at least one.")]
    public Sprite[] frames;
    [Tooltip("Animation speed for this state, in frames per second.")]
    public float fps = 8f;

    [System.NonSerialized] public Vector3 baseScale = Vector3.one;
    [System.NonSerialized] public Color baseColor = Color.white;

    public void Capture() { if (renderer) { baseScale = renderer.transform.localScale; baseColor = renderer.color; } }
    public void SetActive(bool on) { if (renderer) renderer.gameObject.SetActive(on); }
    public void Restore() { if (renderer) { renderer.transform.localScale = baseScale; renderer.color = baseColor; } }
}

/// <summary>
/// The boss "brain". Three modes, each backed by its own SpriteRenderer object:
///   ATTACKING   – fires volleys of twin-eye lasers, frozen in place
///   KICKING     – if you crowd him he hops, kicks you away, then restarts the volley
///   VULNERABLE  – physics-driven; push him into the hole. Don't push and he hops home.
///
/// He HOPS on transitions: into attack (from a break) and into a kick. A kick can't fire
/// until the attack hop finishes, so it never interrupts mid-transition.
///
/// Requires a Rigidbody. Put the Rigidbody + a Box Collider on this root object and make
/// the visuals (and the two Eye objects) children of it. The collider gets a frictionless
/// material so he slides down the hole instead of clinging to the walls.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BossController : MonoBehaviour
{
    public enum State { Attacking, Kicking, Vulnerable, Defeated }

    [Header("References")]
    public Transform player;
    public LaserTelegraph telegraphPrefab;
    [Tooltip("Empty child objects placed on his eyes. Beams fire from these two points.")]
    public Transform leftEye;
    public Transform rightEye;

    [Header("State visuals (separate objects – only one shows at a time)")]
    public BossStateVisual attackingVisual;
    public BossStateVisual vulnerableVisual;
    public BossStateVisual kickingVisual;

    [Header("Attack settings")]
    public float windupTime = 3f;
    public int lasersPerVolley = 3;
    public float delayBetweenLasers = 0.35f;
    public float hitRadius = 1.5f;
    public float groundY = 0f;
    [Tooltip("Fallback eye positions used only if the Eye objects above are not assigned.")]
    public Vector3 laserOriginOffset = new Vector3(0f, 1.4f, 0f);
    public float eyeSpacing = 0.6f;

    [Header("Kick (when the player gets too close)")]
    public float kickRange = 2f;
    public float kickForce = 12f;
    [Tooltip("Small upward pop added to the kick. Set 0 for a flat shove.")]
    public float kickUp = 1.5f;
    [Tooltip("How long he STAYS in kick mode after kicking, so the player can register it.")]
    public float kickHoldTime = 0.6f;
    [Tooltip("Minimum attacking time before he's allowed to kick again (starts when a kick ends).")]
    public float kickCooldown = 1f;

    [Header("Hops (state transitions)")]
    public float hopHeight = 1f;
    public float hopDuration = 0.35f;

    [Header("Boss juice")]
    public Color chargeTint = new Color(1f, 0.5f, 0.45f, 1f);

    [Header("Break / vulnerable settings")]
    public float breakDuration = 4f;
    [Tooltip("If the player pushes him less than this during a break, he hops back to start.")]
    public float pushThreshold = 0.75f;

    [Header("Scene reset")]
    public float resetDelay = 0.2f;

    [Header("Events")]
    public UnityEvent onDefeated;

    Rigidbody rb;
    State state;
    bool isResetting;
    Vector3 startPosition;
    Coroutine recoilCo;
    float lastKickTime = -999f;
    bool returnToStartNextAttack;
    bool firstAttackDone;

    BossStateVisual currentVisual;
    float animTimer;
    int animIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        attackingVisual.Capture();
        vulnerableVisual.Capture();
        kickingVisual.Capture();
        attackingVisual.SetActive(false);
        vulnerableVisual.SetActive(false);
        kickingVisual.SetActive(false);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        // Frictionless so he slides down the narrow gap instead of sticking to walls.
        // Unity 6+: rename PhysicMaterial -> PhysicsMaterial and PhysicMaterialCombine -> PhysicsMaterialCombine.
        var col = GetComponent<Collider>();
        if (col != null)
        {
            var slick = new PhysicsMaterial("BossSlick")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            col.material = slick;
        }
    }

    void Start() => StartCoroutine(BattleLoop());

    void Update() => AdvanceAnimation();

    IEnumerator BattleLoop()
    {
        while (state != State.Defeated)
        {
            yield return StartCoroutine(AttackPhase());
            if (state == State.Defeated) yield break;
            yield return StartCoroutine(VulnerablePhase());
        }
    }

    IEnumerator AttackPhase()
    {
        state = State.Attacking;
        rb.isKinematic = true;
        SetActiveVisual(attackingVisual);

        // Hop INTO attack mode (also returns to start if he wasn't pushed last break).
        // Skipped on the very first volley so the fight doesn't open with a random bounce.
        if (firstAttackDone)
        {
            Vector3 dest = returnToStartNextAttack ? startPosition : transform.position;
            yield return StartCoroutine(HopTo(dest));
        }
        firstAttackDone = true;
        returnToStartNextAttack = false;

        int shot = 0;
        while (shot < lasersPerVolley && state != State.Defeated)
        {
            // Lock the strike point at the START of the wind-up. The whole point of the
            // telegraph is that the player can move OFF this spot before it fires.
            Vector3 target = GroundTarget();
            LaserTelegraph marker = Instantiate(telegraphPrefab, target, Quaternion.identity);
            marker.Begin(windupTime, EyeOrigins());

            // charge (bail to a kick if the player crowds him)
            bool kicked = false;
            float t = 0f;
            while (t < windupTime)
            {
                if (CanKick()) { kicked = true; break; }
                t += Time.deltaTime;
                AnimateBossCharge(Mathf.Clamp01(t / windupTime));
                yield return null;
            }

            if (kicked)
            {
                if (marker != null) Destroy(marker.gameObject);
                yield return StartCoroutine(DoKick());
                shot = 0;                        // must do all 3 again
                continue;
            }

            // FIRE — target was locked when the marker spawned; do NOT recompute it here,
            // or the shot re-aims onto the player and becomes impossible to dodge.
            if (marker != null) marker.Fire();
            BossFireRecoil();

            Vector3 flatPlayer = new Vector3(player.position.x, 0f, player.position.z);
            if (HorizontalDistance(flatPlayer, target) <= hitRadius)
            {
                Juice.Shake(1f);
                Juice.Flash(new Color(1f, 0f, 0f, 0.8f), 0.3f);
                StartCoroutine(ResetScene());
                yield break;
            }

            shot++;

            if (shot < lasersPerVolley)
            {
                float d = 0f;
                while (d < delayBetweenLasers)
                {
                    if (CanKick()) { kicked = true; break; }
                    d += Time.deltaTime;
                    yield return null;
                }
                if (kicked)
                {
                    yield return StartCoroutine(DoKick());
                    shot = 0;
                    continue;
                }
            }
        }
    }

    IEnumerator VulnerablePhase()
    {
        state = State.Vulnerable;
        rb.isKinematic = false;
        SetActiveVisual(vulnerableVisual);

        Vector3 posAtStart = transform.position;
        yield return new WaitForSeconds(breakDuration);
        if (state == State.Defeated) yield break;

        // Not shoved? Flag a hop-home; the hop itself happens at the start of the next AttackPhase.
        returnToStartNextAttack = HorizontalDistance(transform.position, posAtStart) < pushThreshold;
    }

    // -------- laser geometry --------
    Vector3 GroundTarget() => new Vector3(player.position.x, groundY, player.position.z);

    Vector3[] EyeOrigins()
    {
        if (leftEye != null && rightEye != null)
            return new Vector3[] { leftEye.position, rightEye.position };

        Vector3 c = transform.position + laserOriginOffset;
        Vector3 half = Vector3.right * (eyeSpacing * 0.5f);
        return new Vector3[] { c - half, c + half };
    }

    // -------- kicking --------
    bool CanKick()
    {
        if (Time.time - lastKickTime < kickCooldown) return false;
        return HorizontalDistance(player.position, transform.position) <= kickRange;
    }

    IEnumerator DoKick()
    {
        state = State.Kicking;
        SetActiveVisual(kickingVisual);

        // Shove the player right away so the hit feels connected.
        Vector3 away = player.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
        away.Normalize();
        Vector3 knock = away * kickForce + Vector3.up * kickUp;

        var receiver = player.GetComponent<IKnockbackReceiver>();
        if (receiver != null) receiver.ApplyKnockback(knock);
        else
        {
            var prb = player.GetComponent<Rigidbody>();
            if (prb != null && !prb.isKinematic) prb.linearVelocity = knock;   // Unity 6+: linearVelocity
            else Debug.LogWarning("BossController: the player needs a PlayerKnockback (or SimplePlayerController) component so the kick can push it — and it must be on the object in the Player field.");
        }

        Juice.Shake(0.5f);

        yield return StartCoroutine(HopTo(transform.position));   // hop INTO kick mode
        if (kickHoldTime > 0f) yield return new WaitForSeconds(kickHoldTime);

        lastKickTime = Time.time;   // cooldown starts once the kick fully ends
        state = State.Attacking;
        SetActiveVisual(attackingVisual);
    }

    // -------- hop (used for both transitions) --------
    IEnumerator HopTo(Vector3 dest)
    {
        rb.isKinematic = true;
        Vector3 from = transform.position;
        float t = 0f;
        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float f = hopDuration > 0f ? t / hopDuration : 1f;
            Vector3 p = Vector3.Lerp(from, dest, f);
            p.y += Mathf.Sin(f * Mathf.PI) * hopHeight;
            transform.position = p;
            yield return null;
        }
        transform.position = dest;
    }

    // -------- visual swapping --------
    void SetActiveVisual(BossStateVisual v)
    {
        if (currentVisual == v) return;
        if (recoilCo != null) { StopCoroutine(recoilCo); recoilCo = null; }

        attackingVisual.SetActive(attackingVisual == v);
        vulnerableVisual.SetActive(vulnerableVisual == v);
        kickingVisual.SetActive(kickingVisual == v);

        currentVisual = v;
        currentVisual?.Restore();
        animIndex = 0;
        animTimer = 0f;
    }

    // -------- boss juice (on the active visual) --------
    void AnimateBossCharge(float p)
    {
        if (currentVisual == null || currentVisual.renderer == null) return;
        var sr = currentVisual.renderer;
        Vector3 b = currentVisual.baseScale;
        float pulse = Mathf.Sin(Time.time * Mathf.Lerp(6f, 30f, p)) * 0.5f + 0.5f;
        float stretch = 1f + 0.08f * p * pulse;
        sr.transform.localScale = new Vector3(b.x * (1f - 0.04f * p * pulse), b.y * stretch, b.z);
        sr.color = Color.Lerp(currentVisual.baseColor, chargeTint, p * (0.5f + 0.5f * pulse));
    }

    void BossFireRecoil()
    {
        if (currentVisual == null || currentVisual.renderer == null) return;
        if (recoilCo != null) StopCoroutine(recoilCo);
        recoilCo = StartCoroutine(RecoilRoutine(currentVisual));
    }

    IEnumerator RecoilRoutine(BossStateVisual v)
    {
        if (v == null || v.renderer == null) yield break;
        var sr = v.renderer;
        float d = 0.18f, t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float f = t / d;
            float s = Mathf.Sin(f * Mathf.PI);
            sr.transform.localScale = new Vector3(v.baseScale.x * (1f - 0.15f * s), v.baseScale.y * (1f + 0.25f * s), v.baseScale.z);
            sr.color = Color.Lerp(Color.white, v.baseColor, f);
            yield return null;
        }
        v.Restore();
    }

    void RestoreBossVisual() => currentVisual?.Restore();

    // -------- sprite animation on the active visual --------
    void AdvanceAnimation()
    {
        if (state == State.Defeated) return;
        var v = currentVisual;
        if (v == null || v.renderer == null || v.frames == null || v.frames.Length == 0) return;

        animTimer += Time.deltaTime;
        float frameDur = 1f / Mathf.Max(0.01f, v.fps);
        if (animTimer >= frameDur) { animTimer -= frameDur; animIndex = (animIndex + 1) % v.frames.Length; }
        v.renderer.sprite = v.frames[Mathf.Clamp(animIndex, 0, v.frames.Length - 1)];
    }

    // -------- defeat / reset --------
    public void Defeat()
    {
        if (state == State.Defeated) return;
        state = State.Defeated;
        StopAllCoroutines();
        RestoreBossVisual();
        Juice.Shake(0.6f);
        onDefeated?.Invoke();
    }

    IEnumerator ResetScene()
    {
        if (isResetting) yield break;
        isResetting = true;
        yield return new WaitForSecondsRealtime(resetDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
