using UnityEngine;
using TMPro;
using System;

public class StatDisplayUI : MonoBehaviour
{
    [Header("Stat Source")]
    public Character character;
    public string statName = "Health";
    public TextMeshProUGUI statText;

    [Header("Formatting Options")]
    public bool showAsComplete = false;
    public bool showAsBooleanText = false;
    public bool showAsFraction = false;
    public bool showOnlyNumber = false;
    public bool showAsCurrency = false;
    public bool showAsPercent = false;
    public bool showAs12HourTime = false; // Display as 12-hour time with days

    [Tooltip("Max value for fraction or percent")]
    public float maxValue = 100f;

    [Header("Advanced Formatting")]
    public string customCurrencySymbol = "$";

    void Update()
    {
        if (character == null || statText == null) return;

        float value = character.GetStatValue(statName);
        string output = "";

        // 🆕 12-hour time with day count
        if (showAs12HourTime)
        {
            int totalHours = Mathf.FloorToInt(value);
            int days = totalHours / 24 + 1;                  // Day count starting at 1
            int hourOfDay = totalHours % 24;                // Hour within current day
            int minutes = Mathf.FloorToInt((value - totalHours) * 60);

            DateTime time = new DateTime(1, 1, 1, hourOfDay, minutes, 0);
            output = $"Day {days}, {time.ToString("h:mm tt")}";
        }
        else if (showAsComplete)
        {
            output = value >= 1 ? "Complete" : "Incomplete";
        }
        else if (showAsBooleanText)
        {
            output = value >= 1 ? "YES" : "NO";
        }
        else if (showAsFraction)
        {
            output = $"{Mathf.FloorToInt(value)}/{Mathf.FloorToInt(maxValue)}";
        }
        else if (showOnlyNumber)
        {
            output = Mathf.FloorToInt(value).ToString();
        }
        else
        {
            output = $"{statName}: {Mathf.FloorToInt(value)}";
        }

        bool stripStatName = (showAsCurrency || showAsPercent)
                             && !showAsFraction && !showAsComplete && !showAsBooleanText && !showOnlyNumber && !showAs12HourTime;

        if (showAsCurrency)
        {
            string prefix = string.IsNullOrEmpty(customCurrencySymbol) ? "$" : customCurrencySymbol;
            output = prefix + output;
        }

        if (showAsPercent)
        {
            output += "%";
        }

        if (stripStatName)
        {
            output = output.Replace(statName + ": ", "");
        }

        statText.text = output;
    }
}
