using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// "Granny"-style stalker AI (works with Cinemachine / Starter Assets First Person Controller).
///
/// DETECTION: clear line of sight from the camera's position to her face — any distance, any facing.
/// Blocked by solid walls (Obstacle Mask) or any GrannyDetectionBlocker the line passes through
/// (stops the INITIAL lock-on only; once chasing she ignores both and follows you).
///
/// States: Idle, Roaming, Spotted (freeze + camera snap + red/tilt), Chasing (relentless, ramps up),
///         Watching (jammed OUTSIDE the classroom rim), Caught (head snaps into your face + shake + load scene).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GrannyAI : MonoBehaviour
{
    public enum State { Idle, Roaming, Spotted, Chasing, Watching, Caught }

    [Header("References")]
    [Tooltip("The player capsule (the object with the CharacterController). Granny navigates toward this, and it's rotated to keep you looking at her.")]
    public Transform player;
    [Tooltip("Your MAIN CAMERA (the one with the CinemachineBrain). Auto-found if left empty or wrong.")]
    public Camera playerCamera;
    [Tooltip("Your movement + look script(s). Starter Assets: FirstPersonController. REQUIRED for the freeze to work.")]
    [UnityEngine.Serialization.FormerlySerializedAs("disableOnCatch")]
    public MonoBehaviour[] playerControlScripts;

    [Header("Roaming")]
    [Tooltip("Points Granny wanders between. Create empty GameObjects and drop them here. Keep them OUT of classrooms.")]
    public Transform[] patrolPoints;
    [Tooltip("OPTIONAL 'roaming spot' she returns to after giving up. Leave empty to just resume patrolling.")]
    public Transform roamReturnPoint;
    public float roamSpeed = 2f;
    public float minWaitAtPoint = 1f;
    public float maxWaitAtPoint = 3f;
    public bool randomPatrol = true;

    [Header("Chasing")]
    [Tooltip("Starting chase speed. Must beat the player's sprint or she can't catch you.")]
    public float chaseSpeed = 6.5f;
    [Tooltip("How much her chase speed increases every second while chasing (0 = constant).")]
    public float chaseSpeedRampPerSecond = 1.5f;
    [Tooltip("Fastest she can ramp up to.")]
    public float maxChaseSpeed = 12f;
    [Tooltip("Reset her speed to the starting Chase Speed each new chase. Off = she stays fast across classroom breaks.")]
    public bool resetSpeedEachChase = true;
    [Tooltip("Horizontal distance at which she grabs you.")]
    public float catchDistance = 1.8f;
    [Tooltip("Only allow catching while actively chasing. If OFF, simply touching her grabs you.")]
    public bool onlyCatchWhileChasing = false;
    [Tooltip("How far OUTSIDE the classroom boundary she stops while waiting at the rim.")]
    public float rimStandoff = 0.3f;

    [Header("Movement Feel")]
    [Tooltip("How quickly she reaches her speed. HIGH = rigid/snappy. LOW = slippery. Try 40-80.")]
    public float acceleration = 50f;
    [Tooltip("How fast she turns (deg/sec). HIGH = crisp cornering. Try 400-720.")]
    public float angularSpeed = 600f;

    [Header("Vision (line-of-sight)")]
    [Tooltip("Height of her FACE above her transform origin. The sightline is checked here, the camera snaps here, and it's the fallback head offset for the catch if Head Point is empty.")]
    public float eyeHeight = 1.6f;
    [Tooltip("Solid layers that BLOCK the sightline: walls, doors, props (triggers ignored).")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("OPTIONAL extra: a layer for detection-blocker volumes. You can ignore this and just use the GrannyDetectionBlocker component.")]
    public LayerMask detectionBlockerMask = 0;

    [Header("Keep Looking At Granny")]
    [Tooltip("OPTIONAL: the transform your look script pitches for vertical look (Starter Assets: PlayerCameraRoot). Empty = horizontal only.")]
    public Transform cameraPitchPivot;
    [Tooltip("OPTIONAL: the private pitch field in your look script (Starter Assets: _cinemachineTargetPitch).")]
    public string lookPitchFieldName = "_cinemachineTargetPitch";

    [Header("Spotted Reaction")]
    public float cameraSnapDuration = 0.16f;
    public float tiltBackAngle = 18f;
    public float reactionHold = 0.25f;

    [Header("Appearance")]
    public Renderer[] grannyRenderers;
    public Color chaseColor = new Color(0.4f, 0f, 0f, 1f);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip spotSound;
    public AudioClip chaseLoopSound;
    public AudioClip catchSound;

    [Header("Catch Cutscene")]
    [Tooltip("OPTIONAL: an empty GameObject (or head bone) placed at her HEAD. Her head is snapped to in front of the camera. Empty = use Eye Height as the head offset.")]
    public Transform headPoint;
    [Tooltip("How far in front of the camera her head sits.")]
    public float faceDistance = 0.6f;
    [Tooltip("Up/down nudge in SCREEN space (camera-relative). 0 = centered.")]
    public float faceHeightOffset = 0f;
    [Tooltip("How fast her head lunges into the normalized spot.")]
    public float lungeSpeed = 14f;
    [Tooltip("How violently her head shakes (world units of jitter).")]
    public float shakeMagnitude = 0.08f;
    [Tooltip("How long the head shake lasts before the scene switches (seconds).")]
    public float shakeDuration = 1.5f;
    [Tooltip("Scene to load at the end. MUST be in File > Build Settings. Empty = don't load.")]
    public string catchSceneName = "";
    public UnityEvent onPlayerCaught;

    [Header("Anti-Stuck")]
    public bool antiStuck = true;
    public float stuckCheckInterval = 0.5f;
    public float minProgressPerInterval = 0.15f;

    [Header("Startup / Debug")]
    public bool activeOnStart = true;
    public bool debug = true;

    // ---------- internals ----------
    NavMeshAgent agent;
    Behaviour cinemachineBrain;
    State state = State.Idle;
    int patrolIndex = -1;
    Coroutine roamRoutine;

    float currentChaseSpeed;
    float stuckClock;
    Vector3 stuckCheckpoint;
    readonly RaycastHit[] losBuffer = new RaycastHit[32];

    struct MatColor { public Material mat; public Color original; }
    readonly List<MatColor> cachedColors = new List<MatColor>();

    Vector3 EyePos => transform.position + Vector3.up * eyeHeight;
    Transform ViewTarget => playerCamera != null ? playerCamera.transform : player;
    public State CurrentState => state;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentChaseSpeed = chaseSpeed;
        stuckCheckpoint = transform.position;
        ApplyAgentTuning();
        CacheColors();
        ResolveCamera();
    }

    void Start()
    {
        if (activeOnStart) ActivateGranny();
    }

    void ApplyAgentTuning()
    {
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
    }

    void ResolveCamera()
    {
        if (playerCamera != null && (playerCamera.GetComponent("CinemachineBrain") as Behaviour) != null)
        {
            cinemachineBrain = playerCamera.GetComponent("CinemachineBrain") as Behaviour;
            return;
        }
        foreach (var cam in Camera.allCameras)
        {
            var b = cam.GetComponent("CinemachineBrain") as Behaviour;
            if (b != null)
            {
                playerCamera = cam;
                cinemachineBrain = b;
                if (debug) Debug.Log("[Granny] Using CinemachineBrain camera: " + cam.name);
                return;
            }
        }
        if (playerCamera == null) playerCamera = Camera.main;
        cinemachineBrain = null;
        if (debug && cinemachineBrain == null) Debug.Log("[Granny] No CinemachineBrain found — driving the camera transform directly.");
    }

    public void ActivateGranny()
    {
        if (state == State.Caught) return;
        EnterRoaming();
    }

    public void DeactivateGranny()
    {
        if (state == State.Caught) return;
        StopRoamRoutine();
        StopChaseLoop();
        RestoreColors();
        state = State.Idle;
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    public void ForceRetreat()
    {
        if (state == State.Chasing || state == State.Spotted)
        {
            if (PlayerIsSafe()) EnterWatching();
            else { StopChaseLoop(); RestoreColors(); EnterRoaming(); }
        }
    }

    void Update()
    {
        if (state == State.Roaming || state == State.Chasing)
        {
            bool canCatch = state == State.Chasing || !onlyCatchWhileChasing;
            if (canCatch && player != null && !PlayerIsSafe())
            {
                Vector3 a = transform.position; a.y = 0f;
                Vector3 b = player.position; b.y = 0f;
                if (Vector3.Distance(a, b) <= catchDistance) { CatchPlayer(); return; }
            }
        }

        switch (state)
        {
            case State.Roaming: UpdateRoaming(); AntiStuckUpdate(); break;
            case State.Chasing: UpdateChasing(); AntiStuckUpdate(); break;
            case State.Watching: UpdateWatching(); break; // jammed at the rim — no anti-stuck
        }
    }

    // ---------------- ANTI-STUCK ----------------
    void AntiStuckUpdate()
    {
        if (!antiStuck || !agent.enabled || !agent.isOnNavMesh || agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            stuckClock = 0f;
            stuckCheckpoint = transform.position;
            return;
        }

        stuckClock += Time.deltaTime;
        if (stuckClock < stuckCheckInterval) return;

        float progress = Vector3.Distance(transform.position, stuckCheckpoint);
        if (progress < minProgressPerInterval)
        {
            Vector3 dest = agent.destination;
            agent.ResetPath();
            agent.SetDestination(dest);
        }
        stuckClock = 0f;
        stuckCheckpoint = transform.position;
    }

    // ---------------- ROAMING ----------------
    void EnterRoaming()
    {
        state = State.Roaming;
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0f;
            agent.speed = roamSpeed;
            ApplyAgentTuning();
        }
        stuckCheckpoint = transform.position;
        StopChaseLoop();
        RestoreColors();
        StopRoamRoutine();
        roamRoutine = StartCoroutine(RoamLoop());
    }

    IEnumerator RoamLoop()
    {
        if (roamReturnPoint != null)
        {
            GoTo(roamReturnPoint.position);
            yield return WaitToArrive();
        }
        while (true)
        {
            GoTo(NextPatrolPoint());
            yield return WaitToArrive();
            yield return new WaitForSeconds(Random.Range(minWaitAtPoint, maxWaitAtPoint));
        }
    }

    void UpdateRoaming()
    {
        if (agent.enabled) agent.speed = roamSpeed;

        bool couldSee = PlayerCanSeeGranny();
        if (debug && playerCamera != null)
            Debug.DrawLine(playerCamera.transform.position, EyePos, couldSee ? Color.green : Color.red);

        if (couldSee && !PlayerIsSafe())
        {
            if (debug) Debug.Log("[Granny] Clear sightline to player — triggering!");
            StartCoroutine(SpotSequence());
        }
    }

    Vector3 NextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return transform.position;

        if (randomPatrol)
        {
            int i = Random.Range(0, patrolPoints.Length);
            if (patrolPoints.Length > 1 && i == patrolIndex) i = (i + 1) % patrolPoints.Length;
            patrolIndex = i;
        }
        else patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        Transform p = patrolPoints[patrolIndex];
        return p != null ? p.position : transform.position;
    }

    // ---------------- DETECTION ----------------
    bool PlayerCanSeeGranny()
    {
        if (playerCamera == null) return false;

        Vector3 from = playerCamera.transform.position;
        Vector3 toFace = EyePos - from;
        float dist = toFace.magnitude;
        if (dist < 0.01f) return true;
        Vector3 dir = toFace.normalized;
        float checkDist = dist - 0.1f;

        if (Physics.Raycast(from, dir, checkDist, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        if (detectionBlockerMask.value != 0 &&
            Physics.Raycast(from, dir, checkDist, detectionBlockerMask, QueryTriggerInteraction.Collide))
            return false;

        int n = Physics.RaycastNonAlloc(from, dir, losBuffer, checkDist, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (losBuffer[i].collider.GetComponent<GrannyDetectionBlocker>() != null)
                return false;

        return true;
    }

    // ---------------- SPOTTED ----------------
    IEnumerator SpotSequence()
    {
        state = State.Spotted;
        StopRoamRoutine();
        if (agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
        agent.updateRotation = false;

        SetPlayerControl(false);
        if (cinemachineBrain != null) cinemachineBrain.enabled = false;

        Transform cam = playerCamera != null ? playerCamera.transform : null;

        if (cam != null)
        {
            Quaternion startRot = cam.rotation;
            float t = 0f, dur = Mathf.Max(0.01f, cameraSnapDuration);
            while (t < dur)
            {
                t += Time.deltaTime;
                Vector3 dir = EyePos - cam.position;
                if (dir.sqrMagnitude > 0.0001f)
                    cam.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(dir), Mathf.Clamp01(t / dur));
                yield return null;
            }
        }

        ApplyColor(chaseColor);
        if (audioSource != null && spotSound != null) audioSource.PlayOneShot(spotSound);

        Vector3 toPlayer = player.position - transform.position; toPlayer.y = 0f;
        Quaternion faceYaw = toPlayer.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toPlayer) : transform.rotation;
        Quaternion tilted = faceYaw * Quaternion.Euler(-tiltBackAngle, 0f, 0f);

        float tt = 0f; const float tiltIn = 0.08f;
        while (tt < tiltIn)
        {
            tt += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(faceYaw, tilted, Mathf.Clamp01(tt / tiltIn));
            PinCameraTo(cam);
            yield return null;
        }
        float hold = reactionHold;
        while (hold > 0f) { hold -= Time.deltaTime; PinCameraTo(cam); yield return null; }

        AimPlayerAtGranny();
        if (cinemachineBrain != null) cinemachineBrain.enabled = true;
        SetPlayerControl(true);

        if (PlayerIsSafe()) EnterWatching();
        else EnterChasing();
    }

    void PinCameraTo(Transform cam)
    {
        if (cam == null) return;
        Vector3 dir = EyePos - cam.position;
        if (dir.sqrMagnitude > 0.0001f) cam.rotation = Quaternion.LookRotation(dir);
    }

    void AimPlayerAtGranny()
    {
        Vector3 target = EyePos;

        if (player != null)
        {
            Vector3 toG = target - player.position; toG.y = 0f;
            if (toG.sqrMagnitude > 0.0001f)
                player.rotation = Quaternion.LookRotation(toG);
        }

        Vector3 camPos = playerCamera != null ? playerCamera.transform.position
                       : (cameraPitchPivot != null ? cameraPitchPivot.position
                       : (player != null ? player.position : transform.position));
        Vector3 toFace = target - camPos;
        float horiz = new Vector2(toFace.x, toFace.z).magnitude;
        float pitchDeg = -Mathf.Atan2(toFace.y, horiz) * Mathf.Rad2Deg;

        if (cameraPitchPivot != null)
            cameraPitchPivot.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);

        if (!string.IsNullOrEmpty(lookPitchFieldName) && playerControlScripts != null)
        {
            foreach (var s in playerControlScripts)
            {
                if (s == null) continue;
                var f = s.GetType().GetField(lookPitchFieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (f != null && f.FieldType == typeof(float)) { f.SetValue(s, pitchDeg); break; }
            }
        }
    }

    // ---------------- CHASING ----------------
    void EnterChasing()
    {
        state = State.Chasing;
        if (resetSpeedEachChase) currentChaseSpeed = chaseSpeed;
        else currentChaseSpeed = Mathf.Max(currentChaseSpeed, chaseSpeed);

        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.autoBraking = false;
            agent.stoppingDistance = 0f;
            agent.speed = currentChaseSpeed;
            ApplyAgentTuning();
        }
        stuckCheckpoint = transform.position;
        ApplyColor(chaseColor);
        PlayChaseLoop();
        if (debug) Debug.Log("[Granny] Chasing!");
    }

    void UpdateChasing()
    {
        if (PlayerIsSafe()) { EnterWatching(); return; }

        currentChaseSpeed = Mathf.Min(currentChaseSpeed + chaseSpeedRampPerSecond * Time.deltaTime, maxChaseSpeed);
        if (agent.enabled) agent.speed = currentChaseSpeed;

        GoTo(player.position);
    }

    // ---------------- WATCHING (jammed OUTSIDE the classroom rim) ----------------
    void EnterWatching()
    {
        state = State.Watching;
        StopRoamRoutine();
        ApplyColor(chaseColor);  // stay red
        PlayChaseLoop();         // keep the music — she's still after you
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.updateRotation = false; // face you manually
            agent.autoBraking = true;     // stop cleanly at the rim, don't overshoot in
            agent.stoppingDistance = 0f;
            agent.speed = chaseSpeed;
            ApplyAgentTuning();
        }
        if (debug) Debug.Log("[Granny] Jammed at the classroom rim — can't get in.");
    }

    void UpdateWatching()
    {
        if (!PlayerIsSafe()) { EnterChasing(); return; } // the instant you step out

        if (player == null) return;

        Vector3 dir = player.position - transform.position; dir.y = 0f;
        Vector3 dirN = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        var zone = GrannySafeZone.CurrentZone;
        Vector3 target;
        if (zone != null && zone.TryGetEntryPoint(transform.position, player.position, out Vector3 entry))
            target = entry - dirN * rimStandoff;                 // just outside where the line crosses in
        else if (zone != null)
            target = zone.ClosestBoundary(transform.position) - dirN * rimStandoff; // fallback
        else
            target = player.position;

        GoTo(target);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirN), Time.deltaTime * 8f);
    }

    // ---------------- CATCH / CUTSCENE ----------------
    void CatchPlayer()
    {
        state = State.Caught;
        StopRoamRoutine();
        SetPlayerControl(false);

        StopChaseLoop();
        if (audioSource != null && catchSound != null) audioSource.PlayOneShot(catchSound);
        ApplyColor(chaseColor);

        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        agent.isStopped = true;
        agent.updateRotation = false;
        agent.enabled = false;

        if (debug) Debug.Log("[Granny] Caught the player!");
        onPlayerCaught?.Invoke();
        StartCoroutine(CatchCutscene());
    }

    // Where her PIVOT must be so her HEAD sits at the normalized camera-space spot.
    Vector3 CatchBodyTarget(Transform cam)
    {
        Vector3 headTarget = cam.position + cam.forward * faceDistance + cam.up * faceHeightOffset;
        Vector3 headOffset = headPoint != null ? (headPoint.position - transform.position) : (Vector3.up * eyeHeight);
        return headTarget - headOffset;
    }

    void FaceCamera(Transform cam)
    {
        Vector3 dir = cam.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    IEnumerator CatchCutscene()
    {
        Transform cam = ViewTarget;

        // 1) lunge so her HEAD reaches the normalized spot directly in front of the camera
        while (true)
        {
            FaceCamera(cam);
            Vector3 body = CatchBodyTarget(cam);
            transform.position = Vector3.MoveTowards(transform.position, body, lungeSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, body) < 0.02f) break;
            yield return null;
        }

        // 2) shake her head in your face
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            FaceCamera(cam);
            transform.position = CatchBodyTarget(cam) + Random.insideUnitSphere * shakeMagnitude;
            yield return null;
        }

        if (!string.IsNullOrEmpty(catchSceneName))
            SceneManager.LoadScene(catchSceneName);
    }

    // ---------------- HELPERS ----------------
    void GoTo(Vector3 pos)
    {
        if (agent.enabled && agent.isOnNavMesh) agent.SetDestination(pos);
    }

    IEnumerator WaitToArrive()
    {
        yield return null;
        float timeout = 10f;
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.15f)
        {
            if (state != State.Roaming) yield break;
            timeout -= Time.deltaTime;
            if (timeout <= 0f) yield break;
            yield return null;
        }
    }

    bool PlayerIsSafe() => GrannySafeZone.PlayerInSafeZone;

    void SetPlayerControl(bool on)
    {
        if (playerControlScripts == null) return;
        foreach (var c in playerControlScripts)
            if (c != null) c.enabled = on;
    }

    void StopRoamRoutine()
    {
        if (roamRoutine != null) { StopCoroutine(roamRoutine); roamRoutine = null; }
    }

    void CacheColors()
    {
        cachedColors.Clear();
        if (grannyRenderers == null) return;
        foreach (var r in grannyRenderers)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
                cachedColors.Add(new MatColor { mat = m, original = GetMatColor(m) });
        }
    }

    void ApplyColor(Color c) { foreach (var mc in cachedColors) SetMatColor(mc.mat, c); }
    void RestoreColors() { foreach (var mc in cachedColors) SetMatColor(mc.mat, mc.original); }

    static Color GetMatColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    static void SetMatColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    void PlayChaseLoop()
    {
        if (audioSource == null || chaseLoopSound == null) return;
        if (audioSource.clip != chaseLoopSound || !audioSource.isPlaying)
        {
            audioSource.clip = chaseLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void StopChaseLoop()
    {
        if (audioSource != null && audioSource.clip == chaseLoopSound && audioSource.isPlaying)
            audioSource.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eye, 0.25f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        if (patrolPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var p in patrolPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.4f);
        }

        if (roamReturnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(roamReturnPoint.position, Vector3.one * 0.6f);
        }
    }
}