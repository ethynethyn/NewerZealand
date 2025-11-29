using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Weight : MonoBehaviour
{
    [Header("Weight Settings")]
    public float weightKG = 1f;
    public float weightMultiplier = 1f;

    [Header("Strength Requirement")]
    public bool requiresStrengthCheck = true;
    public bool blockPickupIfTooHeavy = true;

    [Header("Player Reference")]
    public Character playerCharacter;   // Assign this in the Inspector

    [Header("Events")]
    public UnityEvent onTooHeavyToLift;

    public float GetTotalWeight()
    {
        return weightKG * weightMultiplier;
    }

    public bool TryPickupCheck()
    {
        if (!requiresStrengthCheck)
            return true;

        if (playerCharacter == null)
        {
            Debug.LogWarning("Player character not assigned in Weight: " + gameObject.name);
            return true; // fallback to allow pickup if player not assigned
        }

        float playerStrength = playerCharacter.GetStatValue("Strength");
        float required = GetTotalWeight();

        Debug.Log($"Trying to pick up {gameObject.name} | Required: {required} | Player Strength: {playerStrength}");

        if (blockPickupIfTooHeavy && playerStrength < required)
        {
            onTooHeavyToLift?.Invoke();
            return false;
        }

        return true;
    }
}
