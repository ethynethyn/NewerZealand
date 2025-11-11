using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class MultiStatEventTrigger : MonoBehaviour
{
    public enum ComparisonType { Equal, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, NotEqual }

    [System.Serializable]
    public class StatCondition
    {
        public string statName = "Health";
        public ComparisonType comparison = ComparisonType.Equal;
        public float compareValue = 1f;
    }

    [Header("Stat Settings")]
    public Character character;
    public StatCondition[] conditions;

    [Header("Event Settings")]
    public bool continuousCheck = true;      // React when condition is lost
    public bool onlyTriggerOnce = false;     // Prevent retriggering if true
    public UnityEvent onConditionMet;
    public UnityEvent onConditionLost;

    private bool conditionCurrentlyMet = false;

    private void OnEnable()
    {
        if (character != null)
            character.OnStatChanged += OnStatChanged;

        // Initial check
        EvaluateConditions();
    }

    private void OnDisable()
    {
        if (character != null)
            character.OnStatChanged -= OnStatChanged;
    }

    // FIXED: Ignore delta, just trigger evaluation
    private void OnStatChanged(string changedStat, float delta)
    {
        if (conditions.Any(c => c.statName == changedStat))
            EvaluateConditions();
    }

    private void EvaluateConditions()
    {
        if (character == null || conditions == null || conditions.Length == 0)
            return;

        // Check all conditions against total stat values
        bool allMet = conditions.All(c =>
            Compare(character.GetStatValue(c.statName), c.compareValue, c.comparison)
        );

        // Handle transitions
        if (allMet && !conditionCurrentlyMet)
        {
            onConditionMet.Invoke();
            conditionCurrentlyMet = true;

            if (onlyTriggerOnce)
            {
                character.OnStatChanged -= OnStatChanged;
            }
        }
        else if (!allMet && conditionCurrentlyMet && continuousCheck)
        {
            onConditionLost.Invoke();
            conditionCurrentlyMet = false;
        }
    }

    private bool Compare(float current, float target, ComparisonType type)
    {
        return type switch
        {
            ComparisonType.Equal => Mathf.Approximately(current, target),
            ComparisonType.GreaterThan => current > target,
            ComparisonType.LessThan => current < target,
            ComparisonType.GreaterOrEqual => current >= target,
            ComparisonType.LessOrEqual => current <= target,
            ComparisonType.NotEqual => !Mathf.Approximately(current, target),
            _ => false
        };
    }
}
