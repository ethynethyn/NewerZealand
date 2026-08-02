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

    // Just makes the existing private method public
    public bool IsInPeriodPublic(float hour, SchoolPeriod p)
    {
        return IsInPeriod(hour, p);
    }

    // ── A/B DAY HELPERS ───────────────────────────────────────────────
    // Day index uses the RAW (un-modded) time stat. Day 1 = index 0.
    //   index 0 (Day 1) → A day
    //   index 1 (Day 2) → B day
    //   index 2 (Day 3) → A day ... and so on.
    public int GetDayIndex()
    {
        if (character == null) return 0;
        float rawTime = character.GetStatValue(timeStatName);
        return Mathf.FloorToInt(rawTime / 24f);
    }

    // A days are the ODD calendar days (1, 3, 5...), which are EVEN indices (0, 2, 4...).
    public bool IsADay()
    {
        return (GetDayIndex() % 2) == 0;
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