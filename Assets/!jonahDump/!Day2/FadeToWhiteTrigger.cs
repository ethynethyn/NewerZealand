using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on a GameObject with a Collider that has "Is Trigger" checked.
/// When the Player walks into it, the screen fades to white, waits, then loads a scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FadeToWhiteTrigger : MonoBehaviour
{
    [Header("Fade Settings")]
    public Color fadeColor = Color.white;
    [Min(0.01f)] public float fadeDuration = 1f;
    [Tooltip("Wait this long after the fade finishes before loading the scene.")]
    public float holdAfterFade = 1f;

    [Header("Scene To Load")]
    [Tooltip("Exact scene name as it appears in Build Settings. Leave empty to not load anything.")]
    public string sceneToLoad;

    [Header("Behaviour")]
    public bool triggerOnce = true;
    [Tooltip("Block mouse clicks on UI behind the fade while it's up.")]
    public bool blockInput = true;

    [Header("On Fade Complete (fires before scene load)")]
    public UnityEvent onFadeComplete;

    bool hasTriggered;
    Image fadeImage;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        if (fadeImage == null) fadeImage = CreateOverlay();

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
            yield return null;
        }
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        onFadeComplete?.Invoke();

        if (holdAfterFade > 0f) yield return new WaitForSeconds(holdAfterFade);

        if (!string.IsNullOrWhiteSpace(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }

    Image CreateOverlay()
    {
        var canvasGO = new GameObject("FadeCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        var img = imgGO.AddComponent<Image>();
        img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        img.raycastTarget = blockInput;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return img;
    }
}