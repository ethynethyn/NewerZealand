using UnityEngine;
using TMPro;
using System.Collections;

public class TMPFadeIn : MonoBehaviour
{
    public TMP_Text text;     // Assign your TextMeshPro object
    public float duration = 2f;

    void OnEnable()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float time = 0f;

        // Get current color and force alpha to 0
        Color c = text.color;
        c.a = 0f;
        text.color = c;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            c.a = Mathf.Lerp(0f, 1f, t);
            text.color = c;

            yield return null;
        }

        // Ensure fully visible at end
        c.a = 1f;
        text.color = c;
    }
}