using UnityEngine;

public class SetStatChangeInterval : MonoBehaviour
{
    [Header("Target")]
    public Character targetCharacter;
    public string statName = "Time";

    [Header("New Interval")]
    public float newChangeInterval = 12f;

    [Header("Set Value (Optional)")]
    public bool setValueOnEnable = false;
    public float newValue = 0f;

    private bool hasTriggered = false;

    void OnEnable()
    {
        if (hasTriggered) return;
        if (targetCharacter == null) return;

        foreach (var stat in targetCharacter.stats)
        {
            if (stat.definition != null && stat.definition.statName == statName)
            {
                stat.changeInterval = newChangeInterval;

                if (setValueOnEnable)
                    stat.currentValue = Mathf.Clamp(newValue, stat.minValue, stat.maxValue);

                hasTriggered = true;
                Debug.Log($"[SetStatChangeInterval] '{statName}' interval set to {newChangeInterval}" +
                          (setValueOnEnable ? $", value set to {newValue}." : "."));
                return;
            }
        }

        Debug.LogWarning($"[SetStatChangeInterval] Stat '{statName}' not found on {targetCharacter.characterName}.");
    }
}