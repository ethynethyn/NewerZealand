using UnityEngine;
using System;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    [Header("Character Identity")]
    public string characterName = "Unnamed";

    [Header("Character Stats")]
    public List<CharacterStat> stats = new List<CharacterStat>();

    // Global event for when any stat changes
    // The float now represents the **delta**, not the total
    public event Action<string, float> OnStatChanged;

    // Track previous values internally for delta calculation
    private Dictionary<string, float> previousValues = new Dictionary<string, float>();

    private void Awake()
    {
        // Initialize previous values
        foreach (var stat in stats)
        {
            if (stat.definition != null)
                previousValues[stat.definition.statName] = stat.currentValue;
        }
    }

    void Update()
    {
        // Auto-change stats and fire delta events
        foreach (var stat in stats)
        {
            float previousValue = stat.currentValue;
            stat.UpdateStat(Time.deltaTime);

            if (!Mathf.Approximately(previousValue, stat.currentValue))
            {
                float delta = stat.currentValue - previousValue;
                OnStatChanged?.Invoke(stat.definition.statName, delta);

                // Update stored previous value
                previousValues[stat.definition.statName] = stat.currentValue;
            }
        }
    }

    public float GetStatValue(string statName)
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null && stat.definition.statName == statName)
            {
                return Mathf.Clamp(stat.currentValue, stat.minValue, stat.maxValue);
            }
        }

        Debug.LogWarning($"Stat '{statName}' not found on {characterName}.");
        return 0;
    }

    public void ModifyStat(string statName, float amount)
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null && stat.definition.statName == statName)
            {
                float previousValue = stat.currentValue;

                stat.currentValue = Mathf.Clamp(stat.currentValue + amount, stat.minValue, stat.maxValue);

                if (!Mathf.Approximately(previousValue, stat.currentValue))
                {
                    float delta = stat.currentValue - previousValue;
                    OnStatChanged?.Invoke(statName, delta);

                    // Update previous value for delta tracking
                    previousValues[statName] = stat.currentValue;
                }

                Debug.Log($"{characterName}'s {statName} changed by {amount}. New value: {stat.currentValue}");
                return;
            }
        }

        Debug.LogWarning($"Stat '{statName}' not found on {characterName}.");
    }

    public void SaveStats()
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null)
            {
                string key = characterName + "_" + stat.definition.statName;
                PlayerPrefs.SetFloat(key, stat.currentValue);
            }
        }

        PlayerPrefs.Save();
        Debug.Log($"Saved stats for {characterName}");
    }

    public void LoadStats()
    {
        foreach (var stat in stats)
        {
            if (stat.definition != null)
            {
                string key = characterName + "_" + stat.definition.statName;
                if (PlayerPrefs.HasKey(key))
                {
                    stat.currentValue = PlayerPrefs.GetFloat(key);
                    stat.currentValue = Mathf.Clamp(stat.currentValue, stat.minValue, stat.maxValue);

                    // Fire delta event based on previous stored value
                    float delta = stat.currentValue - (previousValues.ContainsKey(stat.definition.statName)
                        ? previousValues[stat.definition.statName] : 0f);

                    OnStatChanged?.Invoke(stat.definition.statName, delta);

                    // Update previous value
                    previousValues[stat.definition.statName] = stat.currentValue;
                }
            }
        }

        Debug.Log($"Loaded stats for {characterName}");
    }
}
