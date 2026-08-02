using UnityEngine;

public class StatEffectOnUse : MonoBehaviour
{
    [Header("Target")]
    public Character targetCharacter;

    [Header("Stat Effect")]
    public string statName = "Fun";
    public float amount = 1f;

    public void Apply()
    {
        if (targetCharacter == null) return;

        targetCharacter.ModifyStat(statName, amount);

        Debug.Log($"{gameObject.name} applied {amount} to {statName}");
    }
}