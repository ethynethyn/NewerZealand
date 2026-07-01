using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on a GameObject that has a Collider with "Is Trigger" ticked.
/// When the player stands inside the trigger and presses the interact key, it runs
/// the "class minigame" session:
///
///   A. Turns ON your input-blocker object and remembers the camera's start.
///   B. Lerps the player camera to the Choice Camera and shows the Choice UI(s).
///      This choice runs right at the START, and again AFTER every completed round:
///        LEFT CLICK  = keep working -> run a minigame round (below), then choose again.
///        RIGHT CLICK = get out of the chair -> lerp back to the player camera,
///                      return input, and end the session.
///
///   A minigame round is:
///        1. Lerp the player camera to the minigame view (Target Camera).
///        2. Load the minigame scene additively and pause the main scene.
///        3. Wait until the minigame reports it is finished.
///        4. Resume the main scene and unload the minigame.
///        5. Switch your "time passing" object ON (you switch it OFF yourself).
///
/// BELL INTERRUPT: if the Bell Camera becomes active at any point during the session,
/// the session is abandoned instantly, control returns to the player, and any half-finished
/// minigame is cleaned up - so your existing bell/teleport logic runs normally.
///
/// "PLAYER INSIDE" is decided from ACTUAL geometry every frame, not from OnTriggerExit,
/// because teleporting the player out of the trigger doesn't reliably fire OnTriggerExit.
///
/// Tip: keep this on a standalone (root) object so pausing the scene never
/// accidentally switches off the object running this coroutine.
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
    [Tooltip("The choice view. The player camera lerps here whenever a choice is shown (at the start and after each round). Can be a disabled camera used as a 'view marker'.")]
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

    [Header("Choice")]
    [Tooltip("UI objects shown whenever the player is choosing: once when they first sit down (before the minigame) and again after every completed round.\n" +
             "LEFT CLICK = keep working, RIGHT CLICK = get out of the chair.\n" +
             "All of these are switched ON/OFF together, automatically. Drag your two objects in here.")]
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
    private Vector3 savedCamPos;          // world-space (used for the smooth get-out-of-chair return)
    private Quaternion savedCamRot;
    private float savedCamFov;
    private Vector3 savedCamLocalPos;     // local-space (used for the instant bell snap, survives a body teleport)
    private Quaternion savedCamLocalRot;

    private CursorLockMode savedCursorLock;  // restore the player's cursor mode after the minigame
    private bool savedCursorVisible;

    // The roots we switched off when pausing, so we switch the exact same ones back on.
    private readonly List<GameObject> pausedRoots = new List<GameObject>();

    private void Awake()
    {
        // Grab the trigger collider if it wasn't assigned.
        if (triggerArea == null)
        {
            triggerArea = GetComponent<Collider>();
            if (triggerArea == null)
                Debug.LogWarning("[ClassMinigameTrigger] No trigger Collider found on this object. Add one (Is Trigger ticked) or assign Trigger Area.", this);
        }

        // Make sure the bell cutscene camera renders ON TOP of the player camera, so the
        // instant it switches on it covers the screen and you never glimpse the player's
        // view underneath it. (Higher 'depth' = drawn later = on top.)
        // Only bumps it when needed, so a depth you've set higher yourself is left alone.
        if (bellCamera != null && playerCamera != null && bellCamera.depth <= playerCamera.depth)
            bellCamera.depth = playerCamera.depth + 1;
    }

    private void OnEnable()
    {
        // Switching this trigger ON retires whichever one was on before (e.g. the previous
        // class's trigger). THIS is what reliably disables the old class's trigger - it does
        // not depend on the bell firing while the player happens to be mid-minigame.
        if (activeInstance != null && activeInstance != this) activeInstance.enabled = false;
        activeInstance = this;
    }

    private void OnDisable()
    {
        // If we were the active one, clear the slot so the next trigger to switch on starts clean.
        if (activeInstance == this) activeInstance = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        // We only use this to LEARN which collider is the player. From then on, whether the
        // player is inside is judged by geometry (see Update), so teleports are handled too.
        if (other.CompareTag(playerTag))
        {
            playerCollider = other;
            playerInside = true;
        }
    }

    private void Update()
    {
        // While a session is running, watch the bell camera. If it goes live, bail out.
        if (sequenceRunning)
        {
            if (BellRinging()) AbortForBell();
            return;
        }

        // If we don't have the player yet - e.g. they were already standing inside when this
        // trigger got enabled, so OnTriggerEnter never fired - find them by tag.
        if (playerCollider == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) playerCollider = p.GetComponent<Collider>();
        }

        // Decide "is the player in the trigger" from ACTUAL geometry every frame, rather
        // than relying on OnTriggerExit. Teleporting the player out doesn't reliably fire
        // OnTriggerExit, which is why a teleported player could otherwise still activate it.
        // Once we've seen the player, we just test whether their collider still overlaps.
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

        // Block player input for the whole session.
        inputBlocker.SetActive(true);

        // Free the mouse for the minigame (drawing needs it, and a locked cursor is pinned to
        // screen-centre so drags can't move). The player's mouselook is paused during the round,
        // so nothing fights this. Restored in EndSession.
        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Remember where the camera started so we can return to it later.
        savedCamPos = playerCamera.transform.position;
        savedCamRot = playerCamera.transform.rotation;
        savedCamFov = playerCamera.fieldOfView;
        savedCamLocalPos = playerCamera.transform.localPosition;
        savedCamLocalRot = playerCamera.transform.localRotation;

        // Stop any follow script from fighting the lerps.
        if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = false;

        while (true)
        {
            // ---------- CHOICE (runs at the start, and again after every round) ----------
            // Lerp to the choice camera and show the choice UI objects.
            yield return LerpCamera(choiceCamera.transform.position,
                                    choiceCamera.transform.rotation,
                                    choiceCamera.fieldOfView);
            SetChoiceUI(true);

            // Wait for the player's choice.
            //   LEFT CLICK  = keep working (run a round).
            //   RIGHT CLICK = get out of the chair (leave the session).
            bool keepWorking = false;
            bool chosen = false;
            while (!chosen)
            {
                if (Input.GetMouseButtonDown(0)) { keepWorking = true; chosen = true; } // left
                else if (Input.GetMouseButtonDown(1)) { keepWorking = false; chosen = true; } // right
                yield return null;
            }

            SetChoiceUI(false);
            if (!keepWorking) break; // right click -> leave the chair

            // ---------- MINIGAME ROUND ----------
            // 1. Lerp the player camera onto the minigame view.
            yield return LerpCamera(targetCamera.transform.position,
                                    targetCamera.transform.rotation,
                                    targetCamera.fieldOfView);

            // 2. Load the minigame on top, THEN pause the main scene.
            //    (Loading first means the minigame's camera is ready before we hide
            //     the main scene, so you never get a black flash.)
            Scene mainScene = SceneManager.GetActiveScene();

            ClassMinigameBridge.Reset();
            yield return SceneManager.LoadSceneAsync(minigameSceneName, LoadSceneMode.Additive);

            PauseScene(mainScene);

            // 3. Wait until the minigame says it's finished.
            yield return new WaitUntil(() => ClassMinigameBridge.IsFinished);

            // 4. Resume the main scene FIRST (camera is still parked at the minigame
            //    view), so there's always a camera on screen during the unload.
            ResumeScene();

            // 5. Remove the minigame scene.
            if (SceneManager.GetSceneByName(minigameSceneName).isLoaded)
                yield return SceneManager.UnloadSceneAsync(minigameSceneName);

            // 6. Advance time for the round just completed. We only switch it ON here so
            //    its OnEnable fires and any coroutine/animation on it can run. You switch
            //    it OFF yourself. NOTE: make sure it's OFF again before the next round, or
            //    enabling an already-active object won't re-fire OnEnable.
            if (timePassingObject != null) timePassingObject.SetActive(true);

            // ...loop back to the choice (now an "after a round" choice).
        }

        // Player chose to get out of the chair: lerp back to the player camera and
        // return input exactly like before.
        yield return LerpCamera(savedCamPos, savedCamRot, savedCamFov);
        EndSession();
    }

    // Called from Update the moment the bell camera goes live during a session.
    private void AbortForBell()
    {
        // Stop the session coroutine wherever it currently is.
        if (sessionRoutine != null) StopCoroutine(sessionRoutine);
        sessionRoutine = null;

        // Clean up whatever state the session was in.
        SetChoiceUI(false);
        ResumeScene(); // harmless if the main scene wasn't paused
        Scene mg = SceneManager.GetSceneByName(minigameSceneName);
        if (mg.IsValid() && mg.isLoaded) SceneManager.UnloadSceneAsync(minigameSceneName);

        // Snap the player camera straight back onto the body (no lerp). Using the LOCAL
        // transform means this stays correct even if the bell teleports the body.
        playerCamera.transform.localPosition = savedCamLocalPos;
        playerCamera.transform.localRotation = savedCamLocalRot;
        playerCamera.fieldOfView = savedCamFov;

        // Return control to the player (re-enables the follow script + input).
        EndSession();

        // The bell has rung (class over): NOW that the camera is back on the body and input
        // has been handed back, switch this trigger off so the minigame can't be started
        // again. This only disables the trigger component, not the GameObject, so any visuals
        // or the collider stay put. Re-enable it from your own code when the minigame should
        // be available again, e.g.:  myClassMinigameTrigger.enabled = true;
        enabled = false;
    }

    // Re-enables the follow script, returns input, and marks the session finished.
    private void EndSession()
    {
        if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = true;
        if (inputBlocker != null) inputBlocker.SetActive(false);

        // Put the cursor back the way the game had it before the minigame.
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
            // unscaledDeltaTime: the move still plays even if Time.timeScale is 0.
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, cameraLerpDuration);
            float s = Mathf.SmoothStep(0f, 1f, t); // eases in/out for a nicer feel

            playerCamera.transform.position = Vector3.Lerp(fromPos, toPos, s);
            playerCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, s);
            playerCamera.fieldOfView = Mathf.Lerp(fromFov, toFov, s);
            yield return null;
        }

        // Snap to the exact target values at the end.
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
        if (choiceCamera == null) { Debug.LogError("[ClassMinigameTrigger] Choice Camera is not assigned.", this); return false; }
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