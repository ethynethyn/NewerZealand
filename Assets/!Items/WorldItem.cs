using UnityEngine;

/// <summary>
/// A pickup sitting in the world (e.g. a can on a table). Put this on a GameObject
/// that has a Collider, and put that object on your dedicated "Item" layer so the
/// interactor can find it. Pressing E while looking at it adds it to the inventory.
/// </summary>
public class WorldItem : MonoBehaviour
{
    public ItemData item;
    [Min(1)] public int amount = 1;

    [Tooltip("Optional: auto-fills this SpriteRenderer with the item's icon (editor convenience).")]
    public SpriteRenderer spriteRenderer;

    public string DisplayName => item != null ? item.itemName : name;

    void OnValidate()
    {
        if (spriteRenderer != null && item != null && item.icon != null)
            spriteRenderer.sprite = item.icon;
    }

    /// <summary>Try to add to the inventory. Returns true if fully taken (destroy me).</summary>
    public bool TryPickup()
    {
        if (item == null || InventoryManager.Instance == null) return true;

        int leftover = InventoryManager.Instance.AddItem(item, amount);
        if (leftover <= 0) return true;

        amount = leftover; // inventory was full; keep the remainder in the world
        return false;
    }
}
