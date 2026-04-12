using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class TimeEventTrigger : MonoBehaviour
{
    public enum ComparisonType
    {
        Equal, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, NotEqual
    }

    [Header("Time Source")]
    public TextMeshProUGUI timeDisplay;

    [Header("Condition")]
    public int targetDay = 1;
    public int targetHour = 9;
    public ComparisonType comparison = ComparisonType.GreaterOrEqual;

    [Header("Options")]
    public bool checkDay = false;
    public bool onlyTriggerOnce = false;

    [Header("Events")]
    public UnityEvent onTimeMet;
    public UnityEvent onTimeLost;

    private int lastHour = -1;
    private int lastDay = -1;

    private bool conditionCurrentlyMet = false;

    void Update()
    {
        if (timeDisplay == null) return;

        ExtractDayAndHour(timeDisplay.text, out int currentDay, out int currentHour);

        //  Only run when time actually changes
        if (currentHour != lastHour || currentDay != lastDay)
        {
            EvaluateTime(currentDay, currentHour);

            lastHour = currentHour;
            lastDay = currentDay;
        }
    }

    void EvaluateTime(int day, int hour)
    {
        bool result = Compare(hour, targetHour);

        if (checkDay)
            result = result && (day == targetDay);

        if (result && !conditionCurrentlyMet)
        {
            onTimeMet.Invoke();
            conditionCurrentlyMet = true;

            if (onlyTriggerOnce)
                enabled = false;
        }
        else if (!result && conditionCurrentlyMet)
        {
            onTimeLost.Invoke();
            conditionCurrentlyMet = false;
        }
    }

    bool Compare(int current, int target)
    {
        return comparison switch
        {
            ComparisonType.Equal => current == target,
            ComparisonType.GreaterThan => current > target,
            ComparisonType.LessThan => current < target,
            ComparisonType.GreaterOrEqual => current >= target,
            ComparisonType.LessOrEqual => current <= target,
            ComparisonType.NotEqual => current != target,
            _ => false
        };
    }

    void ExtractDayAndHour(string timeString, out int day, out int hour)
    {
        day = 1;
        hour = 0;

        string[] parts = timeString.Split(',');

        // Day
        if (parts.Length >= 1)
        {
            string[] dayTokens = parts[0].Trim().Split(' ');
            if (dayTokens.Length >= 2)
                int.TryParse(dayTokens[1], out day);
        }

        // Hour
        if (parts.Length >= 2)
        {
            string timePart = parts[1].Trim().ToLower();

            string[] timeTokens = timePart.Split(':');
            if (timeTokens.Length >= 1)
                int.TryParse(timeTokens[0], out hour);

            bool isPM = timePart.Contains("pm") || timePart.Contains("p.m.");

            if (isPM && hour != 12)
                hour += 12;
            else if (!isPM && hour == 12)
                hour = 0;
        }
    }
}