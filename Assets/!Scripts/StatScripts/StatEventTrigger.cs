using UnityEngine;
using UnityEngine.Events;

public class StatEventTrigger : MonoBehaviour
{
    public enum ComparisonType { Equal, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, NotEqual }

    [Header("Stat Settings")]
    public Character character;
    public string statName = "Intelligence";
    public ComparisonType comparison = ComparisonType.GreaterOrEqual;
    public float compareValue = 5f;

    [Header("Event Settings")]
    public bool continuousCheck = true;      //  New: track both gain and loss
    public bool onlyTriggerOnce = false;     // Optional: keep this false for jobs
    public UnityEvent onConditionMet;        // Fired when requirement met
    public UnityEvent onConditionLost;       //  New: Fired when requirement lost

    private bool conditionCurrentlyMet = false;

    private void OnEnable()
    {
        if (character != null)
            character.OnStatChanged += OnStatChanged;

        CheckStat(character?.GetStatValue(statName) ?? 0);
    }

    private void OnDisable()
    {
        if (character != null)
            character.OnStatChanged -= OnStatChanged;
    }

    private void OnStatChanged(string changedStat, float delta)
    {
        if (changedStat == statName)
        {
            float totalValue = character.GetStatValue(statName);
            CheckStat(totalValue);
        }
    }

    private void CheckStat(float current)
    {
        bool conditionMet = comparison switch
        {
            ComparisonType.Equal => Mathf.Approximately(current, compareValue),
            ComparisonType.GreaterThan => current > compareValue,
            ComparisonType.LessThan => current < compareValue,
            ComparisonType.GreaterOrEqual => current >= compareValue,
            ComparisonType.LessOrEqual => current <= compareValue,
            ComparisonType.NotEqual => !Mathf.Approximately(current, compareValue),
            _ => false
        };

        //  React both ways
        if (conditionMet && !conditionCurrentlyMet)
        {
            onConditionMet.Invoke();
            conditionCurrentlyMet = true;
        }
        else if (!conditionMet && conditionCurrentlyMet && continuousCheck)
        {
            onConditionLost.Invoke();
            conditionCurrentlyMet = false;
        }
    }
}
