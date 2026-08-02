using System.Collections;
using UnityEngine;

/// <summary>
/// Recess "increase a stat" activity. Plays an Animator state (a rep / reading / a half-pipe run)
/// and bumps a stat, all in the main scene (no scene loading, no pausing).
///
/// CAMERAS - the enable-a-camera approach (like your bell cam), so the player's mouselook never
/// fights it. The player camera is left completely alone. The Choice and Activity cameras are
/// separate marker cameras that start DISABLED. To show one, we snap it to the current view and
/// ENABLE it, then lerp it to its real scene position:
///   - Start / after each rep: Choice cam appears at the current view and slides to its home.
///   - Do the activity: Activity cam appears where the Choice cam is and slides to its home.
///   - Get up: Choice cam slides to the player camera's view, then we cut back to the player cam.
///
/// Flow:
///   Press E in the trigger -> input blocked, cursor freed -> Choice cam in.
///   Choice UI: LEFT CLICK = do the activity, RIGHT CLICK = get up.
///   Activity: show "mash E", play the Animator state; MASH E to speed it up (GTA-sprint style).
///   When it finishes: stat up + time object ON, back to the choice.
///
/// SETUP for the marker cameras (Choice + Activity):
///   - Give them a HIGHER Depth than the player camera (the script bumps them up if needed).
///   - Set Clear Flags to Skybox or Solid Color so they fully cover the player cam underneath.
///   - Remove their AudioListener (only your main camera should have one).
/// </summary>
public class StatIncreaseTrigger : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("The trigger collider on this object. Leave empty to auto-find the Collider on this GameObject.")]
    [SerializeField] private Collider triggerArea;
    [Tooltip("Optional 'Press E' prompt shown while the player is in range (before they start).")]
    [SerializeField] private GameObject promptUI;

    [Header("Cameras")]
    [Tooltip("The player's live camera. LEFT ALONE - never moved or disabled by this script.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Marker camera for the 'choose' view. Starts disabled; enabled + lerped to its scene position.")]
    [SerializeField] private Camera choiceCamera;
    [Tooltip("Marker camera for the activity view. Starts disabled; enabled + lerped to its scene position.")]
    [SerializeField] private Camera activityCamera;
    [Tooltip("Optional 'bell ringing' camera. If it goes live mid-session, control returns to the player.")]
    [SerializeField] private Camera bellCamera;
    [SerializeField] private float cameraLerpDuration = 1f;
    [Tooltip("Optional mouselook / camera script to switch off during the session, so the player camera holds still and the get-up returns to the exact entry view.")]
    [SerializeField] private MonoBehaviour cameraControllerToDisable;

    [Header("Stat (same Character / ModifyStat setup as your other triggers)")]
    [SerializeField] private Character targetCharacter;
    [SerializeField] private string statToModify = "Strength";
    [SerializeField] private float statChangeAmount = 10f;

    [Header("Objects")]
    [Tooltip("Object that disables player input. ON for the whole session, OFF when the player gets up.")]
    [SerializeField] private GameObject inputBlocker;
    [Tooltip("Your 'time passing' object. Switched ON after each completed rep. You switch it OFF yourself.")]
    [SerializeField] private GameObject timePassingObject;

    [Header("Choice")]
    [Tooltip("UI shown while choosing (e.g. 'Increase Strength'). LEFT CLICK = do it, RIGHT CLICK = get up. All toggled together.")]
    [SerializeField] private GameObject[] choiceUIs;

    [Header("Activity (Animator + mashing)")]
    [Tooltip("The Animator that plays the activity animation (usually the player's Animator).")]
    [SerializeField] private Animator activityAnimator;
    [Tooltip("Exact name of the animation STATE to play, as it appears in the Animator Controller. Its clip should be NON-LOOPING.")]
    [SerializeField] private string activityStateName = "Activity";
    [Tooltip("Optional state to return to when the activity ends / is interrupted (e.g. 'Idle'). Leave empty to skip.")]
    [SerializeField] private string returnStateName = "";
    [Tooltip("Which Animator layer the states are on (usually 0).")]
    [SerializeField] private int activityLayer = 0;
    [Tooltip("UI shown during the activity telling the player to mash E. Toggled automatically; it pops on each press.")]
    [SerializeField] private GameObject ePrompt;
    [Tooltip("Key to mash to speed the activity up.")]
    [SerializeField] private KeyCode mashKey = KeyCode.E;
    [Tooltip("Baseline animation speed with NO mashing. Keep > 0 so the activity can't stall; lower toward 0 to force mashing.")]
    [SerializeField] private float minMashSpeed = 0.5f;
    [Tooltip("Fastest the animation can go, however hard they mash.")]
    [SerializeField] private float maxMashSpeed = 5f;
    [Tooltip("Speed added by each key press.")]
    [SerializeField] private float mashSpeedPerPress = 1.5f;
    [Tooltip("How fast the added speed bleeds off (per second).")]
    [SerializeField] private float mashSpeedDecay = 3f;
    [Tooltip("How much the E prompt grows on each press (0.3 = +30%). 0 = no pop.")]
    [SerializeField] private float mashPulseScale = 0.3f;

    private bool playerInside;
    private Collider playerCollider;
    private bool sequenceRunning;
    private Coroutine sessionRoutine;

    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;

    private Vector3 ePromptBaseScale = Vector3.one;
    private float savedAnimatorSpeed = 1f;

    // Home (scene) poses of the marker cameras, captured once so we always lerp back to them.
    private Vector3 choiceHomePos, activityHomePos;
    private Quaternion choiceHomeRot, activityHomeRot;
    private float choiceHomeFov, activityHomeFov;

    private void Awake()
    {
        if (triggerArea == null)
        {
            triggerArea = GetComponent<Collider>();
            if (triggerArea == null)
                Debug.LogWarning("[StatIncreaseTrigger] No trigger Collider found. Add one (Is Trigger ticked) or assign Trigger Area.", this);
        }

        if (ePrompt != null) ePromptBaseScale = ePrompt.transform.localScale;
        if (activityAnimator != null) savedAnimatorSpeed = activityAnimator.speed;

        // Remember where the marker cams live in the scene, then make sure they're OFF to start.
        if (choiceCamera != null)
        {
            choiceHomePos = choiceCamera.transform.position;
            choiceHomeRot = choiceCamera.transform.rotation;
            choiceHomeFov = choiceCamera.fieldOfView;
        }
        if (activityCamera != null)
        {
            activityHomePos = activityCamera.transform.position;
            activityHomeRot = activityCamera.transform.rotation;
            activityHomeFov = activityCamera.fieldOfView;
        }

        // Make sure the marker/bell cams render on top of the player cam (higher depth = on top).
        BumpDepthAbove(choiceCamera, 1);
        BumpDepthAbove(activityCamera, 1);
        BumpDepthAbove(bellCamera, 2); // bell should sit above the marker cams too

        if (choiceCamera != null) choiceCamera.gameObject.SetActive(false);
        if (activityCamera != null) activityCamera.gameObject.SetActive(false);
    }

    private void BumpDepthAbove(Camera cam, int by)
    {
        if (cam != null && playerCamera != null && cam.depth < playerCamera.depth + by)
            cam.depth = playerCamera.depth + by;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerCollider = other;
            playerInside = true;
        }
    }

    private void Update()
    {
        if (sequenceRunning)
        {
            if (BellRinging()) AbortForBell();
            return;
        }

        if (playerCollider == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) playerCollider = p.GetComponent<Collider>();
        }

        if (playerCollider != null && triggerArea != null)
            playerInside = triggerArea.bounds.Intersects(playerCollider.bounds);

        if (promptUI != null) promptUI.SetActive(playerInside);

        if (playerInside && !BellRinging() && Input.GetKeyDown(interactKey))
            sessionRoutine = StartCoroutine(RunActivity());
    }

    private bool BellRinging()
    {
        return bellCamera != null && bellCamera.isActiveAndEnabled;
    }

    private IEnumerator RunActivity()
    {
        if (!IsSetupValid()) yield break;

        sequenceRunning = true;

        if (promptUI != null) promptUI.SetActive(false);
        inputBlocker.SetActive(true);

        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hold the player camera still so the get-up returns to the exact entry view.
        if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = false;

        // ENTER: Choice cam appears at the player's current view, slides to its home. (Player cam
        // stays enabled underneath - the choice cam is on top, so it covers it.)
        yield return MoveCamera(choiceCamera, null,
            playerCamera.transform.position, playerCamera.transform.rotation, playerCamera.fieldOfView,
            choiceHomePos, choiceHomeRot, choiceHomeFov);

        while (true)
        {
            // ---------- CHOICE (Choice cam already at its home) ----------
            SetChoiceUI(true);

            bool doActivity = false;
            bool chosen = false;
            while (!chosen)
            {
                if (Input.GetMouseButtonDown(0)) { doActivity = true; chosen = true; } // left = do it
                else if (Input.GetMouseButtonDown(1)) { doActivity = false; chosen = true; } // right = get up
                yield return null;
            }

            SetChoiceUI(false);
            if (!doActivity) break;

            // CHOICE -> ACTIVITY: Activity cam appears where the Choice cam is, slides home;
            // the Choice cam switches off the same frame (same pose = seamless swap).
            yield return MoveCamera(activityCamera, choiceCamera,
                choiceCamera.transform.position, choiceCamera.transform.rotation, choiceCamera.fieldOfView,
                activityHomePos, activityHomeRot, activityHomeFov);

            // ---------- ACTIVITY ----------
            yield return PlayActivity();

            // Reward: bump the stat and advance time.
            if (targetCharacter != null) targetCharacter.ModifyStat(statToModify, statChangeAmount);
            if (timePassingObject != null) timePassingObject.SetActive(true);

            // ACTIVITY -> CHOICE: Choice cam appears where the Activity cam is, slides home;
            // the Activity cam switches off the same frame.
            yield return MoveCamera(choiceCamera, activityCamera,
                activityCamera.transform.position, activityCamera.transform.rotation, activityCamera.fieldOfView,
                choiceHomePos, choiceHomeRot, choiceHomeFov);

            // ...loop back to the choice.
        }

        // GET UP: slide the Choice cam to the player camera's view, then cut back to the player cam.
        yield return MoveCamera(choiceCamera, null,
            choiceCamera.transform.position, choiceCamera.transform.rotation, choiceCamera.fieldOfView,
            playerCamera.transform.position, playerCamera.transform.rotation, playerCamera.fieldOfView);
        choiceCamera.gameObject.SetActive(false);

        EndSession();
    }

    // Snaps 'cam' to the from-pose, ENABLES it (and disables 'disableAfter' the same frame, if
    // given, for a seamless swap), then lerps it to the to-pose.
    private IEnumerator MoveCamera(Camera cam, Camera disableAfter,
                                   Vector3 fromPos, Quaternion fromRot, float fromFov,
                                   Vector3 toPos, Quaternion toRot, float toFov)
    {
        cam.transform.SetPositionAndRotation(fromPos, fromRot);
        cam.fieldOfView = fromFov;
        cam.gameObject.SetActive(true);
        if (disableAfter != null) disableAfter.gameObject.SetActive(false);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, cameraLerpDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
            cam.fieldOfView = Mathf.Lerp(fromFov, toFov, s);
            yield return null;
        }

        cam.transform.SetPositionAndRotation(toPos, toRot);
        cam.fieldOfView = toFov;
    }

    // Plays the activity animation state; mashing the key drives Animator.speed.
    private IEnumerator PlayActivity()
    {
        if (ePrompt != null) ePrompt.SetActive(true);
        float pulse = 0f;

        if (activityAnimator != null)
        {
            float speed = minMashSpeed;
            activityAnimator.speed = speed;
            activityAnimator.Play(activityStateName, activityLayer, 0f);

            // Wait until we're actually in the state (usually next frame). Small timeout so a
            // wrong state name just skips instead of hanging.
            float enterTimeout = 0.5f;
            while (enterTimeout > 0f && !activityAnimator.GetCurrentAnimatorStateInfo(activityLayer).IsName(activityStateName))
            {
                enterTimeout -= Time.deltaTime;
                yield return null;
            }

            // Run until the (non-looping) clip finishes, or the controller moves us off the state.
            while (true)
            {
                AnimatorStateInfo info = activityAnimator.GetCurrentAnimatorStateInfo(activityLayer);
                if (!info.IsName(activityStateName) || info.normalizedTime >= 1f)
                    break;

                if (Input.GetKeyDown(mashKey)) { speed += mashSpeedPerPress; pulse = 1f; } // mash = faster
                speed = Mathf.Clamp(speed - mashSpeedDecay * Time.deltaTime, minMashSpeed, maxMashSpeed);
                activityAnimator.speed = speed;

                pulse = Mathf.Max(0f, pulse - Time.deltaTime / 0.12f);
                if (ePrompt != null) ePrompt.transform.localScale = ePromptBaseScale * (1f + pulse * mashPulseScale);

                yield return null;
            }

            RestoreAnimator();
        }

        if (ePrompt != null)
        {
            ePrompt.transform.localScale = ePromptBaseScale;
            ePrompt.SetActive(false);
        }
    }

    private void RestoreAnimator()
    {
        if (activityAnimator == null) return;
        activityAnimator.speed = savedAnimatorSpeed;
        if (!string.IsNullOrEmpty(returnStateName))
            activityAnimator.Play(returnStateName, activityLayer, 0f);
    }

    // Bell goes live mid-session: switch the marker cams off (player cam shows through), tidy up, hand control back.
    private void AbortForBell()
    {
        if (sessionRoutine != null) StopCoroutine(sessionRoutine);
        sessionRoutine = null;

        SetChoiceUI(false);
        RestoreAnimator();
        if (ePrompt != null)
        {
            ePrompt.transform.localScale = ePromptBaseScale;
            ePrompt.SetActive(false);
        }

        if (choiceCamera != null) choiceCamera.gameObject.SetActive(false);
        if (activityCamera != null) activityCamera.gameObject.SetActive(false);

        EndSession();
    }

    private void EndSession()
    {
        if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = true;
        if (inputBlocker != null) inputBlocker.SetActive(false);

        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;

        sequenceRunning = false;
    }

    private void SetChoiceUI(bool on)
    {
        if (choiceUIs == null) return;
        foreach (GameObject go in choiceUIs)
            if (go != null) go.SetActive(on);
    }

    private bool IsSetupValid()
    {
        if (playerCamera == null) { Debug.LogError("[StatIncreaseTrigger] Player Camera is not assigned.", this); return false; }
        if (choiceCamera == null) { Debug.LogError("[StatIncreaseTrigger] Choice Camera is not assigned.", this); return false; }
        if (activityCamera == null) { Debug.LogError("[StatIncreaseTrigger] Activity Camera is not assigned.", this); return false; }
        if (inputBlocker == null) { Debug.LogError("[StatIncreaseTrigger] Input Blocker is not assigned.", this); return false; }
        if (activityAnimator == null) { Debug.LogError("[StatIncreaseTrigger] Activity Animator is not assigned.", this); return false; }
        if (string.IsNullOrEmpty(activityStateName)) { Debug.LogError("[StatIncreaseTrigger] Activity State Name is empty.", this); return false; }
        return true;
    }
}