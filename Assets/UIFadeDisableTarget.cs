using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using StarterAssets;

[RequireComponent(typeof(Image))]
public class UIFadeDisableTarget : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float disableMovementTime = 1f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Player Controller")]
    [SerializeField] private FirstPersonController playerController;

    private Image img;
    private Coroutine routine;

    void Awake()
    {
        img = GetComponent<Image>();
    }

    void OnEnable()
    {
        // Reset opacity
        Color c = img.color;
        c.a = 1f;
        img.color = c;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        // Disable player movement
        if (playerController != null)
            playerController.enabled = false;

        yield return new WaitForSeconds(disableMovementTime);

        // Re-enable movement
        if (playerController != null)
            playerController.enabled = true;

        // Fade UI
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

        c.a = 0f;
        img.color = c;

        gameObject.SetActive(false);
    }
}