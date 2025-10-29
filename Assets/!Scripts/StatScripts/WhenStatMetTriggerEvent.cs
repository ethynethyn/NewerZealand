using UnityEngine;
using UnityEngine.Events;

public class StatEventTrigger : MonoBehaviour
{
    public enum ComparisonType { Equal, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, NotEqual }

    [Header("Stat Settings")]
    public Character character;         // Reference to the Character script
    public string statName = "Health";  // Name of the stat to check
    public ComparisonType comparison = ComparisonType.Equal;
    public float compareValue = 1;

    [Header("Event Settings")]
    public bool checkEveryFrame = false;     // Trigger once or check constantly
    public bool onlyTriggerOnce = true;      // Optional: prevent re-triggering
    public UnityEvent onConditionMet;        // Events to invoke when stat meets condition

    private bool hasTriggered = false;

    void Start()
    {
        if (!checkEveryFrame)
            CheckStat();
    }

    void Update()
    {
        if (checkEveryFrame)
            CheckStat();
    }

    void CheckStat()
    {
        if (character == null) return;

        float current = character.GetStatValue(statName);
        bool conditionMet = false;

        switch (comparison)
        {
            case ComparisonType.Equal:
                conditionMet = current == compareValue;
                break;
            case ComparisonType.GreaterThan:
                conditionMet = current > compareValue;
                break;
            case ComparisonType.LessThan:
                conditionMet = current < compareValue;
                break;
            case ComparisonType.GreaterOrEqual:
                conditionMet = current >= compareValue;
                break;
            case ComparisonType.LessOrEqual:
                conditionMet = current <= compareValue;
                break;
            case ComparisonType.NotEqual:
                conditionMet = current != compareValue;
                break;
        }

        if (conditionMet && (!onlyTriggerOnce || !hasTriggered))
        {
            onConditionMet.Invoke();
            hasTriggered = true;
        }
    }
}
