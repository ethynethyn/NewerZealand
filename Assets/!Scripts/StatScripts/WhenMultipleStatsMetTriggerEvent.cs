using UnityEngine;
using UnityEngine.Events;

public class MultiStatEventTrigger : MonoBehaviour
{
    public enum ComparisonType { Equal, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, NotEqual }

    [System.Serializable]
    public class StatCondition
    {
        public string statName = "Health";
        public ComparisonType comparison = ComparisonType.Equal;
        public float compareValue = 1;
    }

    [Header("Stat Settings")]
    public Character character;
    public StatCondition[] conditions;

    [Header("Event Settings")]
    public bool checkEveryFrame = false;
    public bool onlyTriggerOnce = true;
    public UnityEvent onConditionMet;

    private bool hasTriggered = false;

    void Start()
    {
        if (!checkEveryFrame)
            CheckStats();
    }

    void Update()
    {
        if (checkEveryFrame)
            CheckStats();
    }

    void CheckStats()
    {
        if (character == null || conditions.Length == 0)
            return;

        foreach (var condition in conditions)
        {
            float statValue = character.GetStatValue(condition.statName);
            bool conditionMet = false;

            switch (condition.comparison)
            {
                case ComparisonType.Equal:
                    conditionMet = statValue == condition.compareValue;
                    break;
                case ComparisonType.GreaterThan:
                    conditionMet = statValue > condition.compareValue;
                    break;
                case ComparisonType.LessThan:
                    conditionMet = statValue < condition.compareValue;
                    break;
                case ComparisonType.GreaterOrEqual:
                    conditionMet = statValue >= condition.compareValue;
                    break;
                case ComparisonType.LessOrEqual:
                    conditionMet = statValue <= condition.compareValue;
                    break;
                case ComparisonType.NotEqual:
                    conditionMet = statValue != condition.compareValue;
                    break;
            }

            // If any condition is not met, stop the whole check
            if (!conditionMet)
                return;
        }

        // All conditions were met
        if (!onlyTriggerOnce || !hasTriggered)
        {
            onConditionMet.Invoke();
            hasTriggered = true;
        }
    }
}
