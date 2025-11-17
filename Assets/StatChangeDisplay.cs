using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    public float lineSpacing = 2f;

    private List<TextMeshProUGUI> activePopups = new List<TextMeshProUGUI>();
    private float clearTime = 0.1f;
    private float lastPopupTime;

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
        if (Mathf.Approximately(delta, 0f)) return;

        float now = Time.time;
        if (now - lastPopupTime > clearTime)
            activePopups.Clear(); // new burst  reset stacking

        lastPopupTime = now;

        var tmpInstance = Instantiate(statTextPrefab, canvas.transform);
        tmpInstance.gameObject.SetActive(true);

        // Stack vertically for this batch
        int index = activePopups.Count;
        tmpInstance.rectTransform.anchoredPosition =
            spawnOffset + Vector2.down * (index * lineSpacing);

        activePopups.Add(tmpInstance);

        string prefix = delta > 0 ? "+" : "";
        string colorHex = delta > 0 ? "#00FF00" : "#FF0000";
        tmpInstance.text = $"<color={colorHex}>{statName}: {prefix}{delta:0}</color>";

        StartCoroutine(FadeAndRise(tmpInstance, index));
    }

    private IEnumerator FadeAndRise(TextMeshProUGUI tmp, int index)
    {
        float elapsed = 0f;
        Vector3 startPos = tmp.rectTransform.anchoredPosition;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, Mathf.Lerp(1f, 0f, t));
            tmp.rectTransform.anchoredPosition = startPos + Vector3.up * (riseSpeed * t);

            yield return null;
        }

        activePopups.Remove(tmp);
        Destroy(tmp.gameObject);
    }
}
