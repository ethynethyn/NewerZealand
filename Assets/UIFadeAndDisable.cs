using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UIFadeAndDisable : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;

    private Image img;
    private Coroutine fadeRoutine;

    void Awake()
    {
        img = GetComponent<Image>();
    }

    void OnEnable()
    {
        // Reset alpha every time object is enabled
        Color c = img.color;
        c.a = 0.8f;
        img.color = c;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float timer = 0f;
        Color c = img.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            c.a = alpha;
            img.color = c;
            yield return null;
        }

        // Ensure fully transparent
        c.a = 0f;
        img.color = c;

        // Disable object
        gameObject.SetActive(false);
    }
}