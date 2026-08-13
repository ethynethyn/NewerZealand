using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop this on any GameObject. Call LoadScene() from a UnityEvent, button,
/// dialogue callback, animation event, etc.
/// </summary>
public class DelayedSceneLoader : MonoBehaviour
{
    [Tooltip("Exact scene name as it appears in Build Settings. No path, no .unity extension.")]
    public string sceneToLoad;

    [Tooltip("Seconds to wait before the scene actually loads.")]
    [Min(0f)] public float delay = 1f;

    /// <summary>Call this to start the delayed load.</summary>
    public void LoadScene()
    {
        StopAllCoroutines();
        StartCoroutine(LoadRoutine());
    }

    IEnumerator LoadRoutine()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogError("[DelayedSceneLoader] sceneToLoad is empty!", this);
            yield break;
        }

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        SceneManager.LoadScene(sceneToLoad.Trim());
    }
}