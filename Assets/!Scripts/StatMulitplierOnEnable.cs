using UnityEngine;

public class StatMultiplierOnEnable : MonoBehaviour
{
    [Header("Target")]
    public Character targetCharacter;

    [Header("Multiplier Settings")]
    public string statName = "Health";
    public float multiplier = 2f;

    [Tooltip("If true, only positive gains are multiplied")]
    public bool onlyAffectGains = true;

    private bool isApplyingBonus = false;

    void OnEnable()
    {
        if (targetCharacter != null)
            targetCharacter.OnStatChanged += OnStatChanged;
    }

    void OnDisable()
    {
        if (targetCharacter != null)
            targetCharacter.OnStatChanged -= OnStatChanged;
    }

    void OnStatChanged(string changedStat, float delta)
    {
        if (isApplyingBonus) return; // prevent recursion

        if (changedStat != statName) return;

        if (onlyAffectGains && delta <= 0f) return;

        if (Mathf.Approximately(multiplier, 1f)) return;

        float bonusAmount = delta * (multiplier - 1f);

        isApplyingBonus = true;
        targetCharacter.ModifyStat(statName, bonusAmount);
        isApplyingBonus = false;

        Debug.Log($"[StatMultiplier] Applied bonus {bonusAmount} to {statName}");
    }
}