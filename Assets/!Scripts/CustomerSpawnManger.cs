using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CustomerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject customerPrefab;
    public Transform spawnPoint; // Restaurant door entry point
    public TextMeshProUGUI timeDisplay; // Reference to TMP UI showing time (e.g., "Day 1, 3:45 PM")

    [Header("Operating Hours")]
    public int startHour = 18; // 6 PM (24-hour format)
    public int endHour = 5; // 5 AM (next day)

    [System.Serializable]
    public class DaySpawnConfig
    {
        public int day;
        public int customersPerHour;
    }

    [Header("Day Configuration")]
    public List<DaySpawnConfig> dayConfigs = new List<DaySpawnConfig>();

    private Dictionary<int, int> dayCustomerCount = new Dictionary<int, int>();
    private int currentDay = 1;
    private int currentHour = 0;
    private int lastHour = -1;
    private float timeLoadedAtStartup = 0f;

    void Start()
    {
        ParseDayConfig();
        UpdateTimeFromWorldStat(); // Get initial time
        timeLoadedAtStartup = Time.time;
    }

    void Update()
    {
        UpdateTimeFromWorldStat();
        CheckForNewHour();
    }

    void ParseDayConfig()
    {
        dayCustomerCount.Clear();

        foreach (var config in dayConfigs)
        {
            dayCustomerCount[config.day] = config.customersPerHour;
        }
    }

    void UpdateTimeFromWorldStat()
    {
        if (timeDisplay == null) return;

        // Get the time string from TMP UI (format: "Day X, H:MM AM/PM")
        string timeString = timeDisplay.text;

        if (!string.IsNullOrEmpty(timeString))
        {
            ExtractDayAndHour(timeString, out int day, out int hour);
            currentDay = day;
            currentHour = hour;

      
        }
    }

    void ExtractDayAndHour(string timeString, out int day, out int hour)
    {
        day = 1;
        hour = 0;

        // Parse format: "Day X, H:MM AM/PM"
        // Example: "Day 1, 6:00 p.m."

        string[] parts = timeString.Split(',');

        if (parts.Length >= 1)
        {
            // Extract day from "Day X"
            string dayPart = parts[0].Trim();
            string[] dayTokens = dayPart.Split(' ');
            if (dayTokens.Length >= 2)
                int.TryParse(dayTokens[1], out day);
        }

        if (parts.Length >= 2)
        {
            // Extract hour from "H:MM AM/PM"
            string timePart = parts[1].Trim();
            string[] timeTokens = timePart.Split(':');
            if (timeTokens.Length >= 1)
                int.TryParse(timeTokens[0], out hour);

            // Convert to 24-hour format
            bool isPM = timePart.ToLower().Contains("p.m.") || timePart.ToLower().Contains("pm");

            if (isPM && hour != 12)
                hour += 12;
            else if (!isPM && hour == 12)
                hour = 0; // 12 AM is 0 in 24-hour format
        }
    }

    void CheckForNewHour()
    {
        // Ignore time changes for first 3 seconds after scene load
        if (Time.time - timeLoadedAtStartup < 3f)
            return;

        // Spawn if hour changed and we're within operating hours
        if (currentHour != lastHour && IsWithinOperatingHours(currentHour))
        {
            SpawnCustomersForCurrentHour();
        }
        lastHour = currentHour;
    }

    bool IsWithinOperatingHours(int hour)
    {
        if (startHour < endHour)
        {
            // Normal case: e.g., 9 AM to 5 PM
            return hour >= startHour && hour <= endHour;
        }
        else
        {
            // Overnight case: e.g., 6 PM to 5 AM
            return hour >= startHour || hour <= endHour;
        }
    }

    void SpawnCustomersForCurrentHour()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("CustomerSpawnManager: Missing customerPrefab or spawnPoint!");
            return;
        }

        int customersToSpawn = GetCustomerCountForDay(currentDay);

        for (int i = 0; i < customersToSpawn; i++)
        {
            Quaternion spawnRotation = Quaternion.Euler(-90, 0, 0);
            GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnRotation);
            Customer customer = customerObj.GetComponent<Customer>();

            if (customer != null)
            {
                customer.Initialize();
            }
        }

        Debug.Log($"Spawned {customersToSpawn} customers on Day {currentDay} Hour {currentHour}");
    }

    int GetCustomerCountForDay(int day)
    {
        if (dayCustomerCount.ContainsKey(day))
            return dayCustomerCount[day];

        // If day not found, return the closest lower day's count, or 1 as default
        int closestDay = 1;
        foreach (int d in dayCustomerCount.Keys)
        {
            if (d <= day && d > closestDay)
                closestDay = d;
        }

        return dayCustomerCount.ContainsKey(closestDay) ? dayCustomerCount[closestDay] : 1;
    }
}