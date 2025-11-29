using UnityEngine;

[DisallowMultipleComponent]
public class Value : MonoBehaviour
{
    [Header("Item Value Settings")]
    public string itemName = "Unnamed Item";
    public float value = 10f;  // Base currency worth

    [Tooltip("Optional multiplier for item condition, rarity, etc.")]
    public float valueMultiplier = 1f;

    public float GetTotalValue()
    {
        return value * valueMultiplier;
    }
}
