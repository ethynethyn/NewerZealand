using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using DialogueEditor;

/// <summary>
/// "Granny"-style stalker AI (works with Cinemachine / Starter Assets First Person Controller).
///
/// DETECTION: a GRID of rays is cast across a patch of her body. You need enough of that patch clear
/// (Required Visible Fraction) for long enough (Required Look Time) before she triggers.
///
/// FACING: Chase Facing controls how she carries herself. Walk Naturally = she faces the way she's
/// actually walking, like a person. Walk Then Stare = natural until she's close, then she locks onto
/// you. Rotation happens in Update, NOT LateUpdate, so SpriteBillboardTwoSided always gets the last word.
///
/// SPRITES: during the spot reaction and the catch she calls SetFocus(camera) on any
/// SpriteBillboardTwoSided in her children, forcing the FRONT sprite. ClearFocus() when it's over.
///
/// CHASE TARGET: the target is dropped to the floor beneath you and then snapped onto the NavMesh, so
/// jumping doesn't move her destination around or stall her pathing.
/// </summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(NavMeshAgent))]
public class GrannyAI : MonoBehaviour
{
    public enum State { Idle, Roaming, Spotted, Chasing, Watching, Caught }
    public enum FacingMode { WalkNaturally, WalkThenStareWhenClose, AlwaysFacePlayer }
    enum CatchPose { None, Lunge, Hold }

    [Header("References")]
    [Tooltip("The player capsule (the object with the CharacterController). Granny navigates toward this, and it's rotated to keep you looking at her.")]
    public Transform player;
    [Tooltip("Your MAIN CAMERA (the one with the CinemachineBrain). Auto-found if left empty or wrong.")]
    public Camera playerCamera;
    [Tooltip("Your movement + look script(s). Starter Assets: FirstPersonController. REQUIRED for the freeze to work.")]
    [UnityEngine.Serialization.FormerlySerializedAs("disableOnCatch")]
    public MonoBehaviour[] playerControlScripts;

    [Header("Sprite Billboard")]
    [Tooltip("Her SpriteBillboardTwoSided components. Auto-found in her children if left empty.")]
    public SpriteBillboardTwoSided[] billboards;
    [Tooltip("Fill in each billboard's empty Camera Transform slot with the player camera on startup.")]
    public bool autoAssignBillboardCamera = true;

    [Header("Granny POV Camera (corner view)")]
    [Tooltip("A Camera parented to Granny at head height, looking down her forward axis. IMPORTANT: delete/disable its AudioListener.")]
    public Camera povCamera;
    [Tooltip("OPTIONAL: a UI object (frame, border, 'REC' label) switched on and off along with the POV camera.")]
    public GameObject povUiRoot;
    [Tooltip("Show the POV view when Granny activates.")]
    public bool povOnWhileActive = true;
    [Tooltip("Kill the POV view the moment she spots you and your camera snaps to her.")]
    public bool disablePovWhenSpotted = true;
    [Tooltip("If she loses you and goes back to roaming, bring the POV view back. OFF = once it's gone for this encounter, it's gone.")]
    public bool povReturnsAfterLosingYou = false;
    [Tooltip("Set the POV camera's viewport rect for you. Untick if you're using a RenderTexture / RawImage instead.")]
    public bool applyPovViewport = true;
    [Tooltip("Corner and size, in 0-1 screen fractions. x/y is the BOTTOM-LEFT of the box, so the default is top-right.")]
    public Rect povViewport = new Rect(0.72f, 0.72f, 0.26f, 0.26f);
    [Tooltip("Must be HIGHER than your main camera's Depth or it renders underneath.")]
    public int povCameraDepth = 10;
    [Tooltip("The POV camera looks where she's WALKING, independently of which way her body is turned. Without this it's welded to her body — and since she always faces you, the feed looks like she's walking backwards round every corner.")]
    public bool povFacesMovement = true;
    [Tooltip("How fast the POV view swings round to follow her path, in degrees per second. 180-260 reads like a head turning. Higher snaps.")]
    public float povTurnSpeed = 220f;
    [Tooltip("When she's stopped and hunting you, the POV turns to look at you instead of holding whatever direction she last walked in.")]
    public bool povLooksAtPlayerWhenStopped = true;
    [Tooltip("Hide her own sprite while the POV camera renders, so her back isn't floating in her own feed. Works in the built-in pipeline and in URP/HDRP.")]
    public bool hideSelfFromPov = true;

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
    [Tooltip("Horizontal distance at which she grabs you. Height is ignored, so jumping never saves you.")]
    public float catchDistance = 1.8f;
    [Tooltip("Only allow catching while actively chasing. If OFF, simply touching her grabs you.")]
    public bool onlyCatchWhileChasing = false;
    [Tooltip("How far OUTSIDE the classroom boundary she stops while waiting at the rim.")]
    public float rimStandoff = 0.3f;
    [Tooltip("How often she's allowed to REPLAN her route, in seconds. Recomputing a path every single frame toward a moving target is what makes her weave and double back for no reason. 0.15-0.3 is smooth.")]
    public float repathInterval = 0.2f;
    [Tooltip("She only replans if you've moved at least this far since her last route. Stops her re-planning over tiny shuffles.")]
    public float repathMoveThreshold = 0.5f;
    [Tooltip("Agent avoidance. NONE gives the straightest, most direct approach. Only raise it if she has to share corridors with other NavMeshAgents.")]
    public ObstacleAvoidanceType obstacleAvoidance = ObstacleAvoidanceType.NoObstacleAvoidance;
    [Tooltip("DIAGNOSTIC: logs her state, speeds and the target she's actually heading for, once a second.")]
    public bool logChaseSpeed = false;

    [Header("Chase Target (jump handling)")]
    [Tooltip("Raycast down from you to find the FLOOR before picking her target. This is what stops your jumps from dragging her destination up and around. Leave ON.")]
    public bool groundTheChaseTarget = true;
    [Tooltip("How far down to look for that floor. Should comfortably exceed your jump height.")]
    public float groundRayLength = 8f;
    [Tooltip("Layers that count as floor for that raycast. Exclude your Player layer if she starts behaving oddly.")]
    public LayerMask groundMask = ~0;
    [Tooltip("How far the target may be snapped onto the NavMesh. Keep it small — a big radius lets the target jump to a ledge or the far side of a wall, which is what made her distance to you feel random.")]
    public float navSampleRadius = 1.5f;
    [Tooltip("If the snapped point ends up further than this HORIZONTALLY from you, it's rejected and she keeps heading for the last good spot instead of veering off.")]
    public float maxTargetSnapDistance = 2f;

    [Header("Facing")]
    [Tooltip("WALK NATURALLY: she faces the way she's walking, like a person — best for the POV corner view.\nWALK THEN STARE: natural until she's within Stare Distance, then she locks onto you.\nALWAYS FACE PLAYER: she stares the whole time and strafes/back-pedals.")]
    public FacingMode chaseFacing = FacingMode.WalkNaturally;
    [Tooltip("How close she has to be before she stops walking naturally and locks onto you. Only used by Walk Then Stare.")]
    public float stareDistance = 4f;
    [Tooltip("How fast she swings her body around, in degrees per second. 300-400 reads as a person turning. 720+ is a snap.")]
    [UnityEngine.Serialization.FormerlySerializedAs("facePlayerTurnSpeed")]
    public float turnSpeed = 360f;
    [Tooltip("Below this speed she's considered stopped and just keeps her current facing instead of spinning on the spot.")]
    public float facingMoveThreshold = 0.2f;
    [Tooltip("Degrees to spin her if her model's face doesn't point down the transform's blue Z arrow. Applies everywhere.")]
    public float modelForwardYawOffset = 0f;

    [Header("Movement Feel")]
    [Tooltip("How quickly she reaches her speed WHILE ROAMING. Low (8-15) gives her weight and an unhurried amble.")]
    public float acceleration = 12f;
    [Tooltip("How quickly she reaches her speed WHILE CHASING. This is the SLIPPERY knob — low values make her glide and overshoot corners. 30-45 keeps her natural but planted. Raise it if she slides past you.")]
    public float chaseAcceleration = 35f;
    [Tooltip("The AGENT's own turn rate. We drive her facing ourselves, so this mostly affects how tightly she corners.")]
    public float angularSpeed = 300f;

    [Header("Vision (line-of-sight)")]
    [Tooltip("Height of her FACE above her transform origin.")]
    public float eyeHeight = 1.6f;
    [Tooltip("Solid layers that BLOCK the sightline: walls, doors, props (triggers ignored).")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("OPTIONAL extra: a layer for detection-blocker volumes.")]
    public LayerMask detectionBlockerMask = 0;

    [Header("Vision — How Good A Look You Need")]
    [Tooltip("WIDTH of the sight-tested patch, in metres. Basically the MINIMUM GAP WIDTH you need to see her through.")]
    public float sightPatchWidth = 0.9f;
    [Tooltip("HEIGHT of the sight-tested patch, in metres. Centred on her face.")]
    public float sightPatchHeight = 1.0f;
    [Range(1, 9)] public int sightSampleColumns = 5;
    [Range(1, 9)] public int sightSampleRows = 3;
    [Tooltip("How much of the patch must be clear before it counts as seeing her. 1 = the WHOLE patch.")]
    [Range(0.05f, 1f)] public float requiredVisibleFraction = 0.75f;
    [Tooltip("How many CONTINUOUS seconds you need that clear view before she reacts.")]
    public float requiredLookTime = 0.4f;
    [Tooltip("Grace period — if the view breaks for less than this, your look progress is kept.")]
    public float lookMemory = 0.25f;
    [Tooltip("She only counts if she's actually ON SCREEN.")]
    public bool requireOnScreen = true;
    [Range(5f, 180f)] public float maxViewConeAngle = 70f;
    [Tooltip("Beyond this distance she can't be spotted at all. 0 = unlimited.")]
    public float maxDetectionDistance = 0f;
    [Tooltip("Seconds between sight checks. 0 = every frame.")]
    public float visionCheckInterval = 0.05f;

    [Header("Keep Looking At Granny")]
    public Transform cameraPitchPivot;
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
    [Tooltip("THE THING THAT GETS PINNED — normally her SPRITE object. Auto-found if empty (first billboard, else first Renderer). Whatever is in here ends up dead centre in front of the camera at the exact same distance every single time, no offset maths involved.")]
    public Transform catchAnchor;
    [Tooltip("AUTO-FRAMING. How many SCREEN HEIGHTS she fills. 1 = she exactly fits on screen. ABOVE 1 = she overflows and gets cropped, which is what 'up in your face' actually looks like — try 2.5 to 4. Set to 0 to ignore this and use Face Distance manually instead.")]
    [Range(0f, 6f)] public float fitToScreenFraction = 3f;
    [Tooltip("WHERE on her sprite to aim, bottom to top. 0 = her feet, 0.5 = her middle, 0.85 = her face. This is the point that ends up dead centre on your screen, so set it to her face when she's cropped.")]
    [Range(0f, 1f)] public float pinPointHeight01 = 0.85f;
    [Tooltip("Manual distance in front of the camera. ONLY used when Fit To Screen Fraction is 0.")]
    public float faceDistance = 0.6f;
    [Tooltip("Up/down nudge in SCREEN space. 0 = dead centre.")]
    public float faceHeightOffset = 0f;
    [Tooltip("LEGACY: only used as a last-resort anchor if Catch Anchor is empty and she has no Renderer.")]
    public Transform headPoint;
    public float lungeSpeed = 14f;
    [Tooltip("Parents her to the camera for the cutscene. Untick only if her model comes out warped.")]
    public bool parentToCameraOnCatch = true;
    [Tooltip("Makes her Rigidbodies kinematic and switches her colliders off during the cutscene.")]
    public bool freezePhysicsOnCatch = true;
    [Tooltip("OFF = she stays upright. Leave OFF for sprites — the billboard handles facing anyway.")]
    public bool faceCameraWithPitch = false;
    [Tooltip("How violently her head shakes, in metres. This jitter runs ACROSS the screen only — her distance from the camera stays locked.")]
    public float shakeMagnitude = 0.08f;
    [Tooltip("How much of the shake is allowed to move her TOWARD/AWAY from the camera, as a fraction of Shake Magnitude. 0 = her distance never changes (recommended). 1 = the old free-for-all jitter.")]
    [Range(0f, 1f)] public float shakeDepthAmount = 0f;
    [Tooltip("How long she shakes in your face BEFORE the dialogue starts. She keeps shaking after this.")]
    public float shakeDuration = 1.5f;
    [Tooltip("OPTIONAL: your timer GameObject. Switched OFF the instant she catches you.")]
    public GameObject timerObject;
    [Tooltip("Scene to load at the end. MUST be in File > Build Settings. Empty = don't load.")]
    public string catchSceneName = "";
    public UnityEvent onPlayerCaught;

    [Header("Catch Dialogue (after the shake)")]
    public NPCConversation catchConversation;
    public bool unlockCursorDuringDialogue = true;
    [Tooltip("Safety net: give up waiting after this many seconds. 0 = wait forever.")]
    public float dialogueTimeout = 0f;
    public bool loadSceneAfterDialogue = true;
    public UnityEvent onCatchDialogueStart;
    public UnityEvent onCatchDialogueFinished;

    [Header("Anti-Stuck")]
    public bool antiStuck = true;
    public float stuckCheckInterval = 0.5f;
    public float minProgressPerInterval = 0.15f;
    [Tooltip("STAIRS FIX. After this many failed repath attempts she gets warped onto the nearest NavMesh point, which pops her free of a step edge she's wedged on. 0 = never warp.")]
    public int repathAttemptsBeforeWarp = 3;
    [Tooltip("How far to look for a NavMesh point when warping her free.")]
    public float warpSearchRadius = 2f;

    [Header("Startup / Debug")]
    public bool activeOnStart = true;
    public bool debug = true;

    // ---------- internals ----------
    NavMeshAgent agent;
    Behaviour cinemachineBrain;
    State state = State.Idle;
    CatchPose catchPose = CatchPose.None;
    int patrolIndex = -1;
    Coroutine roamRoutine;

    float currentChaseSpeed;
    float stuckClock;
    int stuckStrikes;
    Vector3 stuckCheckpoint;
    readonly RaycastHit[] losBuffer = new RaycastHit[32];

    float lookProgress;
    float lostSightClock;
    float visionClock;
    float cachedVisibleFraction;
    bool conversationFinished;
    float chaseLogClock;

    Vector3 currentChaseTarget;
    float repathClock;
    Renderer[] selfRenderers;
    readonly List<Renderer> hiddenForPov = new List<Renderer>();

    Transform originalParent;
    Transform activeCatchAnchor;
    Renderer activeCatchRenderer;
    float catchPinDistance;
    float catchLogClock;
    bool reparented;
    bool povSuppressed;
    Vector3 povLookDir;

    struct MatColor { public Material mat; public Color original; }
    readonly List<MatColor> cachedColors = new List<MatColor>();

    Vector3 EyePos => transform.position + Vector3.up * eyeHeight;
    Transform ViewTarget => playerCamera != null ? playerCamera.transform : player;
    public State CurrentState => state;

    public float LookProgress01 => requiredLookTime <= 0f ? 0f : Mathf.Clamp01(lookProgress / requiredLookTime);
    public float VisiblePatchFraction => cachedVisibleFraction;

    /// <summary>Right-click the component header in the Inspector to run this. Existing scene values
    /// don't pick up new script defaults, so this is how you get the natural-movement numbers.</summary>
    [ContextMenu("Apply Natural Movement Preset")]
    void ApplyNaturalMovementPreset()
    {
        chaseFacing = FacingMode.AlwaysFacePlayer;
        stareDistance = 4f;
        turnSpeed = 540f;
        facingMoveThreshold = 0.2f;
        acceleration = 12f;         // ambling while she roams
        chaseAcceleration = 35f;    // planted while she hunts — this is the anti-slip value
        angularSpeed = 400f;
        groundTheChaseTarget = true;
        navSampleRadius = 1.5f;
        maxTargetSnapDistance = 2f;
        povFacesMovement = true;
        povTurnSpeed = 220f;
        repathInterval = 0.2f;
        repathMoveThreshold = 0.5f;
        obstacleAvoidance = ObstacleAvoidanceType.NoObstacleAvoidance;
        fitToScreenFraction = 3f;
        pinPointHeight01 = 0.85f;
        repathAttemptsBeforeWarp = 3;
        warpSearchRadius = 2f;
        shakeDepthAmount = 0f;
        if (agent != null) ApplyAgentTuning();
        Debug.Log("[Granny] Natural movement preset applied.");
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentChaseSpeed = chaseSpeed;
        stuckCheckpoint = transform.position;
        originalParent = transform.parent;
        ApplyAgentTuning();
        CacheColors();
        ResolveCamera();

        if (billboards == null || billboards.Length == 0)
            billboards = GetComponentsInChildren<SpriteBillboardTwoSided>(true);

        if (autoAssignBillboardCamera && playerCamera != null)
        {
            foreach (var b in billboards)
                if (b != null && b.cameraTransform == null) b.cameraTransform = playerCamera.transform;
        }

        SetPovVisible(false);
    }

    void Start()
    {
        if (activeOnStart) ActivateGranny();
    }

    void ApplyAgentTuning()
    {
        bool hunting = state == State.Chasing || state == State.Watching;
        agent.acceleration = hunting ? chaseAcceleration : acceleration;
        agent.angularSpeed = angularSpeed;
        agent.obstacleAvoidanceType = obstacleAvoidance;
        agent.autoRepath = true;
        agent.updateRotation = false;   // we drive her facing ourselves, in every state
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
            if (povCamera != null && cam == povCamera) continue;
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

    // ---------------- POV CAMERA ----------------
    public void ShowPovCamera() { povSuppressed = false; SetPovVisible(true); }
    public void HidePovCamera() { SetPovVisible(false); }

    void SetPovVisible(bool on)
    {
        if (povCamera != null)
        {
            if (on && applyPovViewport)
            {
                povCamera.rect = povViewport;
                povCamera.depth = povCameraDepth;
            }
            povCamera.enabled = on;
        }
        if (povUiRoot != null) povUiRoot.SetActive(on);
    }

    // ---------------- BILLBOARD FOCUS ----------------
    void SetBillboardFocus(Transform focus)
    {
        if (billboards == null) return;
        foreach (var b in billboards)
        {
            if (b == null) continue;
            if (b.cameraTransform == null && playerCamera != null) b.cameraTransform = playerCamera.transform;
            b.SetFocus(focus);
        }
    }

    void ClearBillboardFocus()
    {
        if (billboards == null) return;
        foreach (var b in billboards)
            if (b != null) b.ClearFocus();
    }

    public void ActivateGranny()
    {
        if (state == State.Caught) return;
        povSuppressed = false;
        EnterRoaming();
        if (povOnWhileActive) SetPovVisible(true);
    }

    public void DeactivateGranny()
    {
        if (state == State.Caught) return;
        StopRoamRoutine();
        StopChaseLoop();
        RestoreColors();
        ClearBillboardFocus();
        SetPovVisible(false);
        state = State.Idle;
        ResetLookProgress();
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

    public void FinishCatchDialogue() { conversationFinished = true; }

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
            case State.Watching: UpdateWatching(); break;
        }

        // Facing goes HERE, in Update — the sprite billboard runs in LateUpdate and has to get the
        // last word, or the sprite ends up skewed by however far she turned this frame.
        if (catchPose == CatchPose.None) UpdateFacing();
    }

    void UpdateFacing()
    {
        if (state != State.Roaming && state != State.Chasing && state != State.Watching) return;

        bool stare = false;
        if (state == State.Chasing || state == State.Watching)
        {
            if (chaseFacing == FacingMode.AlwaysFacePlayer) stare = true;
            else if (chaseFacing == FacingMode.WalkThenStareWhenClose && player != null)
            {
                Vector3 a = transform.position; a.y = 0f;
                Vector3 b = player.position; b.y = 0f;
                stare = Vector3.Distance(a, b) <= stareDistance;
            }
        }

        Vector3 desired;
        if (stare && player != null)
        {
            desired = player.position - transform.position;
        }
        else
        {
            // Face where she's STEERING, not where she's currently sliding. desiredVelocity points at
            // the next corner of her path; velocity lags behind it and gets shoved around by stairs
            // and avoidance, which is what made her turn the wrong way.
            desired = Vector3.zero;
            if (agent.enabled && agent.isOnNavMesh)
            {
                desired = agent.desiredVelocity;
                if (desired.sqrMagnitude < 0.01f) desired = agent.velocity;
            }
            if (desired.sqrMagnitude < facingMoveThreshold * facingMoveThreshold) return; // stopped: hold still
        }

        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return;

        Quaternion want = Quaternion.LookRotation(desired.normalized, Vector3.up)
                        * Quaternion.Euler(0f, modelForwardYawOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        UpdatePovFacing();

        if (catchPose == CatchPose.None) return;

        Transform cam = ViewTarget;
        if (cam == null) return;

        Vector3 headTarget = CatchHeadTarget(cam);

        FaceCameraFromHead(cam, headTarget);

        // Measure where her head ACTUALLY is this frame — after rotation, after billboarding — and
        // shift her by the difference. Self-correcting, so no stored offset can go stale and she
        // lands at exactly the same distance no matter which way she came at you from.
        Vector3 bodyTarget = transform.position + (headTarget - AnchorPos());

        if (catchPose == CatchPose.Lunge)
        {
            transform.position = Vector3.MoveTowards(transform.position, bodyTarget, lungeSpeed * Time.deltaTime);
        }
        else
        {
            // Shake ACROSS the screen, not through it. A random sphere jitters her depth too, which
            // is what kept making her distance look different from one grab to the next.
            Vector3 shake = cam.right * (Random.Range(-1f, 1f) * shakeMagnitude)
                          + cam.up * (Random.Range(-1f, 1f) * shakeMagnitude)
                          + cam.forward * (Random.Range(-1f, 1f) * shakeMagnitude * shakeDepthAmount);
            transform.position = bodyTarget + shake;
        }

        if (debug)
        {
            catchLogClock -= Time.deltaTime;
            if (catchLogClock <= 0f)
            {
                catchLogClock = 1f;
                Debug.Log(string.Format("[Granny] PINNED | anchor '{0}' is {1:0.000}m from camera (locked at {2:0.000})",
                    activeCatchAnchor != null ? activeCatchAnchor.name : "none",
                    Vector3.Distance(cam.position, AnchorPos()), catchPinDistance));
            }
        }
    }

    /// <summary>Aims the corner-view camera down her path instead of down her body's forward axis.
    /// Her body faces you; her POV shouldn't have to.</summary>
    void UpdatePovFacing()
    {
        if (!povFacesMovement || povCamera == null || !povCamera.enabled) return;

        Transform povT = povCamera.transform;

        Vector3 dir = Vector3.zero;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            dir = agent.desiredVelocity;                    // where she's steering, i.e. into the turn
            if (dir.sqrMagnitude < 0.01f) dir = agent.velocity;
        }
        dir.y = 0f;

        if (dir.sqrMagnitude >= facingMoveThreshold * facingMoveThreshold)
        {
            povLookDir = dir.normalized;
        }
        else if (povLooksAtPlayerWhenStopped && player != null &&
                 (state == State.Chasing || state == State.Watching))
        {
            Vector3 toPlayer = player.position - povT.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.0001f) povLookDir = toPlayer.normalized;
        }

        if (povLookDir.sqrMagnitude < 0.0001f) povLookDir = transform.forward;

        Quaternion want = Quaternion.LookRotation(povLookDir, Vector3.up);
        povT.rotation = Quaternion.RotateTowards(povT.rotation, want, povTurnSpeed * Time.deltaTime);
    }

    // ---------------- ANTI-STUCK ----------------
    void AntiStuckUpdate()
    {
        if (!antiStuck || !agent.enabled || !agent.isOnNavMesh || agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            stuckClock = 0f;
            stuckStrikes = 0;
            stuckCheckpoint = transform.position;
            return;
        }

        stuckClock += Time.deltaTime;
        if (stuckClock < stuckCheckInterval) return;
        stuckClock = 0f;

        // Judge her on GROUND COVERED, not on speed. On stairs she can be sliding along at a fair
        // clip while going nowhere, which a speed check would happily wave through.
        float progress = Vector3.Distance(transform.position, stuckCheckpoint);
        stuckCheckpoint = transform.position;

        if (progress >= minProgressPerInterval) { stuckStrikes = 0; return; }

        stuckStrikes++;
        Vector3 dest = agent.destination;
        agent.ResetPath();
        agent.SetDestination(dest);

        if (repathAttemptsBeforeWarp > 0 && stuckStrikes >= repathAttemptsBeforeWarp)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit h, warpSearchRadius, agent.areaMask))
            {
                if (debug) Debug.LogWarning("[Granny] Wedged for " + stuckStrikes + " checks — warping her free.");
                agent.Warp(h.position);
                agent.SetDestination(dest);
            }
            stuckStrikes = 0;
        }
    }

    // ---------------- ROAMING ----------------
    void EnterRoaming()
    {
        state = State.Roaming;
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.autoBraking = true;
            agent.stoppingDistance = 0f;
            agent.speed = roamSpeed;
            ApplyAgentTuning();
        }
        stuckCheckpoint = transform.position;
        ResetLookProgress();
        StopChaseLoop();
        RestoreColors();
        ClearBillboardFocus();
        StopRoamRoutine();
        roamRoutine = StartCoroutine(RoamLoop());

        if (povOnWhileActive && (!povSuppressed || povReturnsAfterLosingYou))
        {
            povSuppressed = false;
            SetPovVisible(true);
        }
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

        visionClock -= Time.deltaTime;
        if (visionClock <= 0f)
        {
            visionClock = Mathf.Max(0f, visionCheckInterval);
            cachedVisibleFraction = VisibleFraction();
        }

        bool goodLook = cachedVisibleFraction >= requiredVisibleFraction && !PlayerIsSafe();

        if (goodLook)
        {
            lostSightClock = 0f;
            lookProgress += Time.deltaTime;
            if (lookProgress >= requiredLookTime)
            {
                if (debug) Debug.Log("[Granny] Good enough look (" + Mathf.RoundToInt(cachedVisibleFraction * 100f) + "% of patch clear) — triggering!");
                ResetLookProgress();
                StartCoroutine(SpotSequence());
            }
        }
        else
        {
            lostSightClock += Time.deltaTime;
            if (lostSightClock >= lookMemory) lookProgress = 0f;
        }
    }

    void ResetLookProgress()
    {
        lookProgress = 0f;
        lostSightClock = 0f;
        visionClock = 0f;
        cachedVisibleFraction = 0f;
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
    float VisibleFraction()
    {
        if (playerCamera == null) return 0f;

        Transform cam = playerCamera.transform;
        Vector3 from = cam.position;
        Vector3 centre = EyePos;
        Vector3 toCentre = centre - from;
        float dist = toCentre.magnitude;
        if (dist < 0.01f) return 1f;

        if (maxDetectionDistance > 0f && dist > maxDetectionDistance) return 0f;
        if (requireOnScreen && Vector3.Angle(cam.forward, toCentre) > maxViewConeAngle * 0.5f) return 0f;

        Vector3 dirN = toCentre / dist;
        Vector3 right = Vector3.Cross(Vector3.up, dirN);
        right = right.sqrMagnitude > 0.0001f ? right.normalized : cam.right;
        Vector3 up = Vector3.up;

        int cols = Mathf.Max(1, sightSampleColumns);
        int rows = Mathf.Max(1, sightSampleRows);
        int visible = 0, total = 0;

        for (int r = 0; r < rows; r++)
        {
            float v = rows == 1 ? 0f : (r / (float)(rows - 1)) - 0.5f;
            for (int c = 0; c < cols; c++)
            {
                float h = cols == 1 ? 0f : (c / (float)(cols - 1)) - 0.5f;
                Vector3 p = centre + right * (h * sightPatchWidth) + up * (v * sightPatchHeight);

                total++;
                bool clear = SightlineClear(from, p);
                if (clear) visible++;

                if (debug)
                    Debug.DrawLine(from, p, clear ? Color.green : Color.red, Mathf.Max(visionCheckInterval, 0.01f));
            }
        }

        return total == 0 ? 0f : visible / (float)total;
    }

    bool SightlineClear(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.01f) return true;

        Vector3 dir = delta / dist;
        float checkDist = dist - 0.1f;
        if (checkDist <= 0f) return true;

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

        if (disablePovWhenSpotted)
        {
            povSuppressed = true;
            SetPovVisible(false);
        }

        if (agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }

        SetPlayerControl(false);
        if (cinemachineBrain != null) cinemachineBrain.enabled = false;

        Transform cam = playerCamera != null ? playerCamera.transform : null;

        SetBillboardFocus(cam);   // she's staring right at you — force the front sprite

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
        Quaternion faceYaw = toPlayer.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(toPlayer) * Quaternion.Euler(0f, modelForwardYawOffset, 0f)
            : transform.rotation;
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

        ClearBillboardFocus();

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
        ResetLookProgress();
        if (resetSpeedEachChase) currentChaseSpeed = chaseSpeed;
        else currentChaseSpeed = Mathf.Max(currentChaseSpeed, chaseSpeed);

        if (agent.enabled)
        {
            agent.isStopped = false;
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

        ChaseTo(player.position);

        if (logChaseSpeed)
        {
            chaseLogClock -= Time.deltaTime;
            if (chaseLogClock <= 0f)
            {
                chaseLogClock = 1f;
                Vector3 a = transform.position; a.y = 0f;
                Vector3 b = player.position; b.y = 0f;
                Debug.Log(string.Format(
                    "[Granny] CHASING | gap {0:0.00}m (catch at {1:0.00}) | target {2:0.0} | ACTUAL {3:0.0} | dest {4} | path {5}",
                    Vector3.Distance(a, b), catchDistance, agent.speed, agent.velocity.magnitude,
                    currentChaseTarget, agent.pathStatus));
            }
        }
    }

    /// <summary>Picks where she should actually walk to. Drops to the FLOOR under you first, then snaps
    /// onto the NavMesh with a tight radius, then rejects anything that landed too far off. Together
    /// that makes your jumps completely invisible to her pathing.</summary>
    void ChaseTo(Vector3 raw)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        Vector3 origin = raw;

        // 1) find the floor beneath you — a jump shouldn't move the target at all
        if (groundTheChaseTarget &&
            Physics.Raycast(raw + Vector3.up * 0.5f, Vector3.down, out RaycastHit gh,
                            groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            origin = gh.point;
        }

        // 2) snap onto the NavMesh, tightly
        Vector3 dest = origin;
        if (navSampleRadius > 0f)
        {
            if (!NavMesh.SamplePosition(origin, out NavMeshHit nh, navSampleRadius, agent.areaMask))
                return;   // nothing sensible nearby — let her finish her current route

            float horizOff = Vector2.Distance(new Vector2(nh.position.x, nh.position.z),
                                              new Vector2(raw.x, raw.z));
            if (horizOff > maxTargetSnapDistance)
                return;   // snapped somewhere silly — DON'T send her to a stale point, that's the
                          // doubling-back you were seeing. Just leave her path alone this frame.

            dest = nh.position;
        }

        currentChaseTarget = dest;

        // 3) replan sparingly. A path recomputed every frame never settles, so she spends the whole
        //    chase steering toward a corner that's already moved.
        if (agent.pathPending) return;

        repathClock -= Time.deltaTime;
        if (repathClock > 0f) return;
        if ((dest - agent.destination).sqrMagnitude < repathMoveThreshold * repathMoveThreshold) return;

        repathClock = repathInterval;
        agent.SetDestination(dest);
    }

    // ---------------- WATCHING ----------------
    void EnterWatching()
    {
        state = State.Watching;
        StopRoamRoutine();
        ApplyColor(chaseColor);
        PlayChaseLoop();
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.autoBraking = true;
            agent.stoppingDistance = 0f;
            agent.speed = chaseSpeed;
            ApplyAgentTuning();
        }
        if (debug) Debug.Log("[Granny] Jammed at the classroom rim — can't get in.");
    }

    void UpdateWatching()
    {
        if (!PlayerIsSafe()) { EnterChasing(); return; }
        if (player == null) return;

        var zone = GrannySafeZone.CurrentZone;

        if (zone == null)
        {
            if (debug) Debug.LogWarning("[Granny] Player flagged safe but there's no current zone — resuming the chase.");
            EnterChasing();
            return;
        }

        Vector3 dir = player.position - transform.position; dir.y = 0f;
        Vector3 dirN = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        Vector3 target;
        if (zone.TryGetEntryPoint(transform.position, player.position, out Vector3 entry))
            target = entry - dirN * rimStandoff;
        else
            target = zone.ClosestBoundary(transform.position) - dirN * rimStandoff;

        ChaseTo(target);
    }

    // ---------------- CATCH / CUTSCENE ----------------
    void CatchPlayer()
    {
        state = State.Caught;
        StopRoamRoutine();
        SetPlayerControl(false);

        if (playerCamera == null) ResolveCamera();

        SetPovVisible(false);

        activeCatchAnchor = ResolveCatchAnchor();
        activeCatchRenderer = activeCatchAnchor != null ? activeCatchAnchor.GetComponentInChildren<Renderer>() : null;
        if (activeCatchRenderer == null) activeCatchRenderer = GetComponentInChildren<Renderer>();
        catchPinDistance = ComputeCatchPinDistance();
        catchLogClock = 0f;

        if (timerObject != null) timerObject.SetActive(false);

        StopChaseLoop();
        if (audioSource != null && catchSound != null) audioSource.PlayOneShot(catchSound);
        ApplyColor(chaseColor);

        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        agent.isStopped = true;
        agent.enabled = false;

        if (freezePhysicsOnCatch)
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        Transform cam = ViewTarget;

        // FRONT SPRITE, always. Focusing on the camera makes the billboard's forward vector point
        // straight at you so the dot product can't flip, and it skips the agent.velocity branch,
        // which would otherwise be read off a disabled agent.
        SetBillboardFocus(cam);

        if (cam == null)
        {
            Debug.LogError("[Granny] No camera AND no player transform — the catch cutscene has nothing to aim at.");
        }
        else
        {
            if (parentToCameraOnCatch)
            {
                originalParent = transform.parent;
                transform.SetParent(cam, true);
                reparented = true;
            }

            if (debug)
                Debug.Log(string.Format(
                    "[Granny] Caught! camera='{0}' | anchor '{1}' | sprite height {2:0.00}m | pin distance {3:0.000}m | camera near clip {4:0.00}",
                    cam.name,
                    activeCatchAnchor != null ? activeCatchAnchor.name : "NONE",
                    activeCatchRenderer != null ? activeCatchRenderer.bounds.size.y : 0f,
                    catchPinDistance,
                    playerCamera != null ? playerCamera.nearClipPlane : -1f));
        }

        onPlayerCaught?.Invoke();
        StartCoroutine(CatchCutscene());
    }

    /// <summary>The spot in front of the camera the anchor gets pinned to. Distance is locked in once
    /// at catch time, so nothing can make it drift frame to frame.</summary>
    Vector3 CatchHeadTarget(Transform cam)
        => cam.position + cam.forward * catchPinDistance + cam.up * faceHeightOffset;

    /// <summary>Where the pinned point IS right now, read live. Uses the renderer's bounds rather than
    /// the transform origin, so it doesn't matter whether her sprite's pivot is at her feet or her waist.</summary>
    Vector3 AnchorPos()
    {
        if (activeCatchRenderer != null)
        {
            Bounds b = activeCatchRenderer.bounds;
            return new Vector3(b.center.x,
                               b.min.y + b.size.y * Mathf.Clamp01(pinPointHeight01),
                               b.center.z);
        }
        if (activeCatchAnchor != null) return activeCatchAnchor.position;
        return transform.position + Vector3.up * eyeHeight;
    }

    /// <summary>Whatever you actually SEE. Pinning this instead of an invisible head point is the
    /// whole trick — no offsets to go stale when she rotates or the billboard spins the sprite.</summary>
    Transform ResolveCatchAnchor()
    {
        if (catchAnchor != null) return catchAnchor;
        if (billboards != null)
            foreach (var b in billboards) if (b != null) return b.transform;
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null) return rend.transform;
        if (headPoint != null) return headPoint;
        return transform;
    }

    /// <summary>Distance that makes her fill Fit To Screen Fraction of the screen height. Never closer
    /// than the near clip plane, so she can't get sliced in half by it.</summary>
    float ComputeCatchPinDistance()
    {
        float d = faceDistance;

        if (fitToScreenFraction > 0f && playerCamera != null)
        {
            float h = activeCatchRenderer != null ? activeCatchRenderer.bounds.size.y : 0f;

            if (h > 0.01f)
            {
                float halfFov = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                d = (h * 0.5f) / (Mathf.Tan(halfFov) * Mathf.Max(fitToScreenFraction, 0.05f));
            }
        }

        if (playerCamera != null) d = Mathf.Max(d, playerCamera.nearClipPlane + 0.05f);
        return Mathf.Max(d, 0.05f);
    }

    void FaceCameraFromHead(Transform cam, Vector3 headPos)
    {
        if (cam == null) return;

        Vector3 dir = cam.position - headPos;
        if (!faceCameraWithPitch) dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = -cam.forward;
            if (!faceCameraWithPitch) dir.y = 0f;
        }
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = -cam.up;
            if (!faceCameraWithPitch) dir.y = 0f;
        }
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? cam.up : Vector3.up;

        transform.rotation = Quaternion.LookRotation(dir, up) * Quaternion.Euler(0f, modelForwardYawOffset, 0f);
    }

    IEnumerator CatchCutscene()
    {
        catchPose = CatchPose.Lunge;

        float safety = 5f;
        while (safety > 0f)
        {
            Transform c = ViewTarget;
            if (c != null && Vector3.Distance(AnchorPos(), CatchHeadTarget(c)) < 0.02f) break;
            safety -= Time.deltaTime;
            yield return null;
        }

        catchPose = CatchPose.Hold;

        float t = 0f;
        while (t < shakeDuration) { t += Time.deltaTime; yield return null; }

        if (catchConversation != null)
            yield return StartCoroutine(RunCatchConversation());

        catchPose = CatchPose.None;
        ClearBillboardFocus();

        if (reparented)
        {
            transform.SetParent(originalParent, true);
            reparented = false;
        }

        if (loadSceneAfterDialogue && !string.IsNullOrEmpty(catchSceneName))
            SceneManager.LoadScene(catchSceneName);
    }

    IEnumerator RunCatchConversation()
    {
        conversationFinished = false;

        CursorLockMode prevLock = Cursor.lockState;
        bool prevVisible = Cursor.visible;
        if (unlockCursorDuringDialogue)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ConversationManager.OnConversationEnded += HandleConversationEnded;
        onCatchDialogueStart?.Invoke();

        if (ConversationManager.Instance != null)
        {
            ConversationManager.Instance.StartConversation(catchConversation);
        }
        else
        {
            Debug.LogWarning("[Granny] No ConversationManager in the scene — drag in the DialogueEditor ConversationManager prefab. Skipping dialogue.");
            conversationFinished = true;
        }

        float t = 0f;
        while (!conversationFinished)
        {
            t += Time.deltaTime;
            if (dialogueTimeout > 0f && t >= dialogueTimeout)
            {
                if (debug) Debug.LogWarning("[Granny] Dialogue timed out — moving on.");
                break;
            }
            yield return null;
        }

        ConversationManager.OnConversationEnded -= HandleConversationEnded;

        if (unlockCursorDuringDialogue)
        {
            Cursor.lockState = prevLock;
            Cursor.visible = prevVisible;
        }

        onCatchDialogueFinished?.Invoke();
    }

    void HandleConversationEnded() { conversationFinished = true; }

    void OnEnable()
    {
        Camera.onPreRender += PovPreRender;
        Camera.onPostRender += PovPostRender;
        RenderPipelineManager.beginCameraRendering += SrpBegin;
        RenderPipelineManager.endCameraRendering += SrpEnd;
    }

    void OnDisable()
    {
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
        Camera.onPreRender -= PovPreRender;
        Camera.onPostRender -= PovPostRender;
        RenderPipelineManager.beginCameraRendering -= SrpBegin;
        RenderPipelineManager.endCameraRendering -= SrpEnd;
        ShowSelfAgain();
    }

    void SrpBegin(ScriptableRenderContext ctx, Camera c) { PovPreRender(c); }
    void SrpEnd(ScriptableRenderContext ctx, Camera c) { PovPostRender(c); }

    /// <summary>Switch her renderers off for the duration of the POV camera's pass only. It's her own
    /// eyes — she shouldn't be standing in the shot.</summary>
    void PovPreRender(Camera c)
    {
        if (!hideSelfFromPov || povCamera == null || c != povCamera) return;
        if (selfRenderers == null) return;

        hiddenForPov.Clear();
        foreach (var r in selfRenderers)
        {
            if (r == null || !r.enabled) continue;
            r.enabled = false;
            hiddenForPov.Add(r);
        }
    }

    void PovPostRender(Camera c)
    {
        if (povCamera == null || c != povCamera) return;
        ShowSelfAgain();
    }

    void ShowSelfAgain()
    {
        for (int i = 0; i < hiddenForPov.Count; i++)
            if (hiddenForPov[i] != null) hiddenForPov[i].enabled = true;
        hiddenForPov.Clear();
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

        Vector3 right = transform.right;
        Vector3 up = Vector3.up;
        Vector3 hw = right * (sightPatchWidth * 0.5f);
        Vector3 hh = up * (sightPatchHeight * 0.5f);
        Vector3 a = eye - hw - hh, b = eye + hw - hh, c = eye + hw + hh, d = eye - hw + hh;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        if (chaseFacing == FacingMode.WalkThenStareWhenClose)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, stareDistance);
        }

        if (maxDetectionDistance > 0f)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(eye, maxDetectionDistance);
        }

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