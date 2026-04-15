using UnityEngine;
using TMPro;
using System.Collections;

public class StatJuiceUI : MonoBehaviour
{
    [Header("References")]
    public Character character;
    public string statName = "Money";
    public TextMeshProUGUI statText;

    [Header("Enable Juice")]
    public bool enableJuice = true;

    [Header("Tick Settings")]
    public bool smoothTick = true;
    public float tickSpeed = 8f;

    [Header("Pop Animation")]
    public bool enablePop = true;
    public float popScale = 1.2f;
    public float popDuration = 0.15f;

    [Header("Color Feedback")]
    public bool enableColorFlash = true;
    public Color normalColor = Color.white;
    public Color increaseColor = Color.green;
    public Color decreaseColor = Color.red;

    [Header("Zero State")]
    public bool turnRedAtZero = true;
    public Color zeroColor = Color.red;

    private float displayedValue;
    private float targetValue;

    private Vector3 originalScale;

    void OnEnable()
    {
        if (character != null)
            character.OnStatChanged += OnStatChanged;

        if (character != null)
        {
            targetValue = character.GetStatValue(statName);
            displayedValue = targetValue;
        }

        if (statText != null)
            originalScale = statText.transform.localScale;
    }

    void OnDisable()
    {
        if (character != null)
            character.OnStatChanged -= OnStatChanged;
    }

    void Update()
    {
        if (!enableJuice || character == null || statText == null) return;

        targetValue = character.GetStatValue(statName);

        if (smoothTick)
        {
            displayedValue = Mathf.Lerp(displayedValue, targetValue, Time.deltaTime * tickSpeed);

            if (Mathf.Abs(displayedValue - targetValue) < 0.01f)
                displayedValue = targetValue;

            // overwrite ONLY the number part
            UpdateDisplayedNumber(displayedValue);
        }

        if (turnRedAtZero)
        {
            if (Mathf.Approximately(targetValue, 0f))
                statText.color = zeroColor;
        }
    }

    // -----------------------------
    // EVENT REACTION
    // -----------------------------
    void OnStatChanged(string changedStat, float delta)
    {
        if (!enableJuice) return;
        if (changedStat != statName) return;

        if (enablePop)
        {
            StopCoroutine("PopAnimation");
            StartCoroutine(PopAnimation());
        }

        if (enableColorFlash)
        {
            if (delta > 0)
                StartCoroutine(ColorFlash(increaseColor));
            else if (delta < 0)
                StartCoroutine(ColorFlash(decreaseColor));
        }
    }

    // -----------------------------
    // NUMBER OVERRIDE (SAFE)
    // -----------------------------
    void UpdateDisplayedNumber(float value)
    {
        string current = statText.text;

        int intVal = Mathf.FloorToInt(value);

        var match = System.Text.RegularExpressions.Regex.Match(current, @"\d+");

        if (match.Success)
        {
            string newText =
                current.Substring(0, match.Index) +
                intVal.ToString() +
                current.Substring(match.Index + match.Length);

            statText.text = newText;
        }
    }

    // -----------------------------
    // POP
    // -----------------------------
    IEnumerator PopAnimation()
    {
        statText.transform.localScale = originalScale * popScale;

        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;

            statText.transform.localScale = Vector3.Lerp(
                originalScale * popScale,
                originalScale,
                t / popDuration
            );

            yield return null;
        }

        statText.transform.localScale = originalScale;
    }

    // -----------------------------
    // COLOR FLASH
    // -----------------------------
    IEnumerator ColorFlash(Color flash)
    {
        statText.color = flash;

        float t = 0f;
        float duration = 0.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            statText.color = Color.Lerp(flash, normalColor, t / duration);
            yield return null;
        }

        statText.color = normalColor;
    }
}