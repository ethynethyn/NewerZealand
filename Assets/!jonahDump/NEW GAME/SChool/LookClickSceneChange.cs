using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on a GameObject with a (trigger) collider.
/// Look at it with the main camera + click LMB -> swaps to another camera,
/// waits, then loads a scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LookClickSceneChange : MonoBehaviour
{
    [Header("Look Detection")]
    [Tooltip("Max distance the camera can be from this object and still interact.")]
    [SerializeField] private float maxDistance = 5f;

    [Tooltip("Layers the look-ray can hit. Make sure this object's layer is included, " +
             "and uncheck layers you want the ray to pass through.")]
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Camera Swap")]
    [Tooltip("The camera that gets turned on. Leave empty if you just want the delay + scene change.")]
    [SerializeField] private Camera cameraToActivate;

    [Tooltip("Turns off the main camera's Camera + AudioListener components so the new one is the only view.")]
    [SerializeField] private bool disableMainCamera = true;

    [Tooltip("Starts the target camera disabled when the scene loads.")]
    [SerializeField] private bool startTargetCameraOff = true;

    [Header("Scene Change")]
    [Tooltip("Seconds to wait after the camera activates before loading the scene.")]
    [SerializeField] private float delayBeforeSceneChange = 3f;

    [Tooltip("Exact name of the scene to load. Must be in File > Build Settings.")]
    [SerializeField] private string sceneToLoad;

    [Tooltip("Ignores Time.timeScale, so the wait still works if you pause the game.")]
    [SerializeField] private bool useUnscaledTime = false;

    private Camera mainCam;
    private bool triggered;

    private void Awake()
    {
        mainCam = Camera.main;

        if (startTargetCameraOff && cameraToActivate != null)
            cameraToActivate.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (triggered) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // re-grab in case the main camera got swapped at runtime
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // ray straight out of the middle of the screen
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Collide))
            return;

        // hit us, or one of our child colliders?
        if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        triggered = true;

        if (cameraToActivate != null)
        {
            cameraToActivate.gameObject.SetActive(true);
            cameraToActivate.enabled = true;

            if (disableMainCamera && mainCam != null && mainCam != cameraToActivate)
            {
                // disabling components instead of the whole GameObject,
                // in case the camera is parented to your player
                mainCam.enabled = false;

                AudioListener listener = mainCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(delayBeforeSceneChange);
        else
            yield return new WaitForSeconds(delayBeforeSceneChange);

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"[{name}] No scene name set, nothing to load.", this);
            yield break;
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    // optional: shows the interact range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}