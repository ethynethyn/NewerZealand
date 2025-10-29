using UnityEngine;

[System.Serializable]
public class CharacterStat
{
    [Header("Base Settings")]
    public StatDefinition definition;
    public float currentValue = 0;

    [Header("Value Limits")]
    public float minValue = 0;
    public float maxValue = 100;

    [Header("Auto Change Settings")]
    public bool autoChangeEnabled = false;
    public float changeAmount = 1f;       // Positive = increase, Negative = decrease
    public float changeInterval = 1f;     // Seconds between changes

    private float timer = 0f;

    // Call this from Update() in another script that manages the character’s stats.
    public void UpdateStat(float deltaTime)
    {
        if (!autoChangeEnabled) return;

        timer += deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0f;
            currentValue += changeAmount;
            currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        }
    }
}
