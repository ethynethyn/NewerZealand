using UnityEngine;
using TMPro;
using System.Collections;

public class StatChangeDisplay : MonoBehaviour
{
    [Header("References")]
    public Character character;
    public Canvas canvas;
    public TextMeshProUGUI statTextPrefab;

    [Header("Settings")]
    public Vector2 spawnOffset = new Vector2(0, 50);
    public float fadeDuration = 1.5f;
    public float riseSpeed = 30f;

    private void OnEnable()
    {
        if (character != null)
            character.OnStatChanged += ShowStatChange;
    }

    private void OnDisable()
    {
        if (character != null)
            character.OnStatChanged -= ShowStatChange;
    }

    private void ShowStatChange(string statName, float delta)
    {
        if (Mathf.Approximately(delta, 0f))
            return; // No change to show

        // Instantiate TMP element
        var tmpInstance = Instantiate(statTextPrefab, canvas.transform);
        tmpInstance.gameObject.SetActive(true);

        // Position in center + offset
        tmpInstance.rectTransform.anchoredPosition = spawnOffset;

        // Display delta with +/– sign and color using Rich Text
        string prefix = delta > 0 ? "+" : "";
        string colorHex = delta > 0 ? "#00FF00" : "#FF0000"; // Green for +, Red for -
        tmpInstance.text = $"<color={colorHex}>{statName}: {prefix}{delta:0}</color>";

        // Start fade + rise coroutine
        StartCoroutine(FadeAndRise(tmpInstance));
    }

    private IEnumerator FadeAndRise(TextMeshProUGUI tmp)
    {
        float elapsed = 0f;
        Vector3 startPos = tmp.rectTransform.anchoredPosition;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Fade out (TMP respects alpha with Rich Text)
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, Mathf.Lerp(1f, 0f, t));

            // Move upward
            tmp.rectTransform.anchoredPosition = startPos + Vector3.up * riseSpeed * t;

            yield return null;
        }

        Destroy(tmp.gameObject);
    }
}
