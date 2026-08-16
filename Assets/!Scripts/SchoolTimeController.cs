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

    [Header("Mode")]
    [Tooltip("ON  = the old behaviour: watch the Time stat and fire period changes automatically (a world clock).\n" +
             "OFF = periods are driven externally (e.g. by ClassPeriodStarter). This controller then NEVER fires on " +
             "its own — it just answers day/period questions. Use OFF in your class-halls scenes.")]
    public bool drivePeriodsFromTime = true;

    public enum DayMode { FromTime, FromPlayerProgression, ForceADay, ForceBDay }
    [Tooltip("How A-day vs B-day is decided (this is what NPCs ask when choosing a class).\n" +
             "FromTime = derive it from the Time stat (needs a Character).\n" +
             "FromPlayerProgression = follow the SceneProgressionManager's day, which flips each time the period loop wraps. Use this in class scenes.\n" +
             "ForceADay = always an A day.  ForceBDay = always a B day.")]
    public DayMode dayMode = DayMode.FromTime;

    private SchoolPeriod lastPeriod;

    // Just makes the existing private method public
    public bool IsInPeriodPublic(float hour, SchoolPeriod p)
    {
        return IsInPeriod(hour, p);
    }

    // ── A/B DAY HELPERS ───────────────────────────────────────────────
    // Day index uses the RAW (un-modded) time stat. Day 1 = index 0.
    public int GetDayIndex()
    {
        if (character == null) return 0;
        float rawTime = character.GetStatValue(timeStatName);
        return Mathf.FloorToInt(rawTime / 24f);
    }

    // A days are the ODD calendar days (1, 3, 5...), which are EVEN indices (0, 2, 4...).
    // With dayMode = ForceADay / ForceBDay this ignores time entirely.
    public bool IsADay()
    {
        switch (dayMode)
        {
            case DayMode.ForceADay: return true;
            case DayMode.ForceBDay: return false;
            case DayMode.FromPlayerProgression:
                return SceneProgressionManager.Instance == null
                    || SceneProgressionManager.Instance.IsADay();
            default:                return (GetDayIndex() % 2) == 0;
        }
    }

    void Update()
    {
        // External control (class scenes): do nothing on our own — ClassPeriodStarter drives it.
        if (!drivePeriodsFromTime) return;

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
