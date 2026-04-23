using UnityEngine;
using System;

public class SchoolTimeController : MonoBehaviour
{
    public static Action<SchoolState, int> OnStateChanged;

    [Header("Time Source")]
    public Character character;
    public string timeStatName = "Time";

    [Header("Periods")]
    public SchoolPeriod[] periods;

    private SchoolPeriod lastPeriod;

    // Add this to SchoolTimeController — just makes the existing private method public
    public bool IsInPeriodPublic(float hour, SchoolPeriod p)
    {
        return IsInPeriod(hour, p);
    }

    void Update()
    {
        if (character == null || periods.Length == 0) return;

        float hour = character.GetStatValue(timeStatName) % 24f;

        SchoolPeriod active = periods[0];

        for (int i = 0; i < periods.Length; i++)
        {
            if (IsInPeriod(hour, periods[i]))
            {
                active = periods[i];
                break;
            }
        }

        // 🔥 PERIOD CHANGE DETECTED
        if (active.startHour != lastPeriod.startHour)
        {
            Debug.Log($"🕒 STATE CHANGE → {active.name}");

            // 🔥 THIS FIXES YOUR BUG
            if (ClassroomRegistry.Instance != null)
            {
                ClassroomRegistry.Instance.ResetAllClassrooms();
            }

            OnStateChanged?.Invoke(active.state, active.periodIndex);

            lastPeriod = active;
        }
    }

    bool IsInPeriod(float hour, SchoolPeriod p)
    {
        if (p.startHour < p.endHour)
            return hour >= p.startHour && hour < p.endHour;
        else
            return hour >= p.startHour || hour < p.endHour;
    }
}