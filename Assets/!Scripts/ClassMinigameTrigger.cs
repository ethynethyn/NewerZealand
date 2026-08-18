using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on a GameObject that has a Collider with "Is Trigger" ticked.
/// When the player stands inside the trigger and presses the interact key, it runs
/// the "class minigame" session.
///
/// SINGLE ROUND (default): sitting down runs ONE minigame round, then returns you to the
/// player automatically. When the round is finished and you're back in control, the
/// On Work Completed Object is switched ON (e.g. an object that triggers the period end).
///
/// LOOP MODE (Single Round OFF): after each round you get the choice again —
///   LEFT CLICK  = keep working (another round)
///   RIGHT CLICK = get out of the chair and end the session.
///
/// A minigame round is:
///   1. Lerp the player camera to the minigame view (Target Camera).
///   2. Load the minigame scene additively and pause the main scene.
///   3. Wait until the minigame reports it is finished.
///   4. Resume the main scene and unload the minigame.
///   5. Switch the "time passing" object ON (you switch it OFF yourself).
///
/// BELL INTERRUPT: if the Bell Camera becomes active at any point during the session, the
/// session is abandoned instantly, control returns to the player, any half-finished minigame
/// is cleaned up, and the completion object is NOT fired (the work wasn't finished).
///
/// "PLAYER INSIDE" is decided from ACTUAL geometry every frame, not from OnTriggerExit,
/// because teleporting the player out of the trigger doesn't reliably fire OnTriggerExit.
/// </summary>
public class ClassMinigameTrigger : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("The trigger collider on this object. Leave empty to auto-find the Collider on this GameObject.")]
    [SerializeField] private Collider triggerArea;

    [Header("Cameras")]
    [Tooltip("The player's live camera (the one that actually moves).")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("The minigame view. The player camera lerps here to start a round. Can be a disabled camera used as a 'view marker'.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("The choice view. The player camera lerps here whenever a choice is shown. Can be a disabled camera used as a 'view marker'.")]
    [SerializeField] private Camera choiceCamera;
    [Tooltip("Your 'bell ringing' camera. If this becomes active during a session, the session is abandoned immediately and control returns to the player so your bell/teleport can run.")]
    [SerializeField] private Camera bellCamera;
    [SerializeField] private float cameraLerpDuration = 1f;
    [Tooltip("Optional. A camera-follow / camera-control script to switch off while the camera is being moved, so it doesn't fight the lerp.")]
    [SerializeField] private MonoBehaviour cameraControllerToDisable;

    [Header("Objects")]
    [Tooltip("Object that disables player input. Turned ON at the start, OFF only when the player gets out of the chair (or when the bell interrupts).")]
    [SerializeField] private GameObject inputBlocker;
    [Tooltip("Your 'time passing' object. Switched ON after each completed round. You manage switching it OFF yourself.")]
    [SerializeField] private GameObject timePassingObject;

    [Header("Round / Completion")]
    [Tooltip("ON (default) = sitting down runs ONE round, then returns you to the player automatically (no 'keep working?' loop).\n" +
             "OFF = loop mode: after each round the choice is shown again.")]
    [SerializeField] private bool singleRound = true;
    [Tooltip("ON = skip the sit-down choice and go straight into the round.\n" +
             "OFF (default) = show the choice first (left = work, right = get up).")]
    [SerializeField] private bool skipInitialChoice = false;
    [Tooltip("Activated once the player has FINISHED a round and is back in control in the main scene " +
             "(e.g. an object that triggers the period end). Not fired if they get up without working, " +
             "or if the bell interrupts. Leave it inactive in the scene.")]
    [SerializeField] private GameObject onWorkCompletedObject;

    [Header("Choice")]
    [Tooltip("UI objects shown whenever the player is choosing.\n" +
             "LEFT CLICK = keep working, RIGHT CLICK = get out of the chair.\n" +
             "All switched ON/OFF together, automatically. Drag your two objects in here.")]
    [SerializeField] private GameObject[] choiceUIs;

    [Header("Scene")]
    [Tooltip("Exact name of the minigame scene. It MUST be added to File > Build Settings.")]
    [SerializeField] private string minigameSceneName;

    private bool playerInside;
    private Collider playerCollider;   // the player's collider, remembered once we've seen it
    private bool sequenceRunning;
    private Coroutine sessionRoutine;

    // Only ONE class minigame trigger may be enabled at a time (enforced in OnEnable/OnDisable).
    private static ClassMinigameTrigger activeInstance;

    // Saved player-camera state so we can return to where we started.
    private Vector3 savedCamPos;
    private Quaternion savedCamRot;
    private float savedCamFov;
    private Vector3 savedCamLocalPos;
    private Quaternion savedCamLocalRot;

    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;

    private readonly List<GameObject> pausedRoots = new List<GameObject>();

    private void Awake()
    {
        if (triggerArea == null)
        {
            triggerArea = GetComponent<Collider>();
            if (triggerArea == null)
                Debug.LogWarning("[ClassMinigameTrigger] No trigger Collider found on this object. Add one (Is Trigger ticked) or assign Trigger Area.", this);
        }

        if (bellCamera != null && playerCamera != null && bellCamera.depth <= playerCamera.depth)
            bellCamera.depth = playerCamera.depth + 1;
    }

    private void OnEnable()
    {
        if (activeInstance != null && activeInstance != this) activeInstance.enabled = false;
        activeInstance = this;
    }

    private void OnDisable()
    {
        if (activeInstance == this) activeInstance = null;
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

        if (playerInside && !BellRinging() && Input.GetKeyDown(interactKey))
            sessionRoutine = StartCoroutine(RunMinigame());
    }

    private bool BellRinging()
    {
        return bellCamera != null && bellCamera.isActiveAndEnabled;
    }

    private IEnumerator RunMinigame()
    {
        if (!IsSetupValid()) yield break;

        sequenceRunning = true;

        inputBlocker.SetActive(true);

        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        savedCamPos = playerCamera.transform.position;
        savedCamRot = playerCamera.transform.rotation;
        savedCamFov = playerCamera.fieldOfView;
        savedCamLocalPos = playerCamera.transform.localPosition;
        savedCamLocalRot = playerCamera.transform.localRotation;

        if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = false;

        bool firstIteration = true;
        bool completedARound = false;

        while (true)
        {
            // ---------- CHOICE ----------
            // Shown at the start (unless Skip Initial Choice) and, in loop mode, again after
            // every round. LEFT = keep working (run a round), RIGHT = get out of the chair.
            bool showChoice = !(firstIteration && skipInitialChoice);
            if (showChoice)
            {
                yield return LerpCamera(choiceCamera.transform.position,
                                        choiceCamera.transform.rotation,
                                        choiceCamera.fieldOfView);
                SetChoiceUI(true);

                bool keepWorking = false;
                bool chosen = false;
                while (!chosen)
                {
                    if (Input.GetMouseButtonDown(0)) { keepWorking = true; chosen = true; }      // left
                    else if (Input.GetMouseButtonDown(1)) { keepWorking = false; chosen = true; } // right
                    yield return null;
                }

                SetChoiceUI(false);
                if (!keepWorking) break; // right click -> leave the chair
            }
            firstIteration = false;

            // ---------- MINIGAME ROUND ----------
            // 1. Lerp the player camera onto the minigame view.
            yield return LerpCamera(targetCamera.transform.position,
                                    targetCamera.transform.rotation,
                                    targetCamera.fieldOfView);

            // 2. Load the minigame on top, THEN pause the main scene.
            Scene mainScene = SceneManager.GetActiveScene();

            ClassMinigameBridge.Reset();
            yield return SceneManager.LoadSceneAsync(minigameSceneName, LoadSceneMode.Additive);

            PauseScene(mainScene);

            // 3. Wait until the minigame says it's finished.
            yield return new WaitUntil(() => ClassMinigameBridge.IsFinished);

            // 4. Resume the main scene FIRST (camera still parked at the minigame view).
            ResumeScene();

            // 5. Remove the minigame scene.
            if (SceneManager.GetSceneByName(minigameSceneName).isLoaded)
                yield return SceneManager.UnloadSceneAsync(minigameSceneName);

            // 6. Advance time for the round just completed.
            if (timePassingObject != null) timePassingObject.SetActive(true);

            completedARound = true;

            // Single-round mode: don't loop back to the choice, just leave the chair.
            if (singleRound) break;

            // (loop mode) ...loop back to the choice (now an "after a round" choice).
        }

        // Lerp back to the player camera and return input.
        yield return LerpCamera(savedCamPos, savedCamRot, savedCamFov);
        EndSession();

        // Now that the player has finished the work and is back in control, switch on the
        // completion object (e.g. the thing that triggers the period end). Only if they
        // actually completed a round — not if they just got out of the chair.
        if (completedARound && onWorkCompletedObject != null)
            onWorkCompletedObject.SetActive(true);
    }

    // Called from Update the moment the bell camera goes live during a session.
    private void AbortForBell()
    {
        if (sessionRoutine != null) StopCoroutine(sessionRoutine);
        sessionRoutine = null;

        SetChoiceUI(false);
        ResumeScene(); // harmless if the main scene wasn't paused
        Scene mg = SceneManager.GetSceneByName(minigameSceneName);
        if (mg.IsValid() && mg.isLoaded) SceneManager.UnloadSceneAsync(minigameSceneName);

        playerCamera.transform.localPosition = savedCamLocalPos;
        playerCamera.transform.localRotation = savedCamLocalRot;
        playerCamera.fieldOfView = savedCamFov;

        EndSession();

        enabled = false;
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

    private IEnumerator LerpCamera(Vector3 toPos, Quaternion toRot, float toFov)
    {
        Vector3 fromPos = playerCamera.transform.position;
        Quaternion fromRot = playerCamera.transform.rotation;
        float fromFov = playerCamera.fieldOfView;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, cameraLerpDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.transform.position = Vector3.Lerp(fromPos, toPos, s);
            playerCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, s);
            playerCamera.fieldOfView = Mathf.Lerp(fromFov, toFov, s);
            yield return null;
        }

        playerCamera.transform.position = toPos;
        playerCamera.transform.rotation = toRot;
        playerCamera.fieldOfView = toFov;
    }

    private void PauseScene(Scene scene)
    {
        pausedRoots.Clear();
        GameObject keepAlive = transform.root.gameObject; // never switch off ourselves
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == keepAlive) continue;
            if (root.activeSelf)
            {
                root.SetActive(false);
                pausedRoots.Add(root);
            }
        }
    }

    private void ResumeScene()
    {
        foreach (GameObject root in pausedRoots)
            if (root != null) root.SetActive(true);
        pausedRoots.Clear();
    }

    private bool IsSetupValid()
    {
        if (playerCamera == null) { Debug.LogError("[ClassMinigameTrigger] Player Camera is not assigned.", this); return false; }
        if (targetCamera == null) { Debug.LogError("[ClassMinigameTrigger] Target Camera is not assigned.", this); return false; }
        if (choiceCamera == null && !skipInitialChoice) { Debug.LogError("[ClassMinigameTrigger] Choice Camera is not assigned.", this); return false; }
        if (inputBlocker == null) { Debug.LogError("[ClassMinigameTrigger] Input Blocker is not assigned.", this); return false; }
        if (string.IsNullOrEmpty(minigameSceneName)) { Debug.LogError("[ClassMinigameTrigger] Minigame Scene Name is empty.", this); return false; }
        return true;
    }
}

/// <summary>
/// Tiny shared "mailbox" so the minigame scene can tell the main scene it's done.
/// Because it's static, the value survives across scenes.
/// From anywhere in your minigame scene, just call:  ClassMinigameBridge.Finish();
/// </summary>
public static class ClassMinigameBridge
{
    public static bool IsFinished { get; private set; }
    public static void Reset() => IsFinished = false;
    public static void Finish() => IsFinished = true;
}
