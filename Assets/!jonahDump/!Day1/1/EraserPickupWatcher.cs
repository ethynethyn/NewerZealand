using UnityEngine;

public class EraserPickupWatcher : MonoBehaviour
{
    [Tooltip("Drag the Eraser ItemData asset here.")]
    public ItemData eraserItem;

    [Tooltip("Turn on to print what it's finding each frame.")]
    public bool debugLogs = false;

    void Update()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            if (debugLogs) Debug.Log("[EraserWatcher] No InventoryManager.Instance yet.");
            return;
        }

        if (eraserItem == null)
        {
            Debug.LogError("[EraserWatcher] eraserItem is not assigned in the inspector!", this);
            enabled = false;
            return;
        }

        if (HasItem(inv.hotbar) || HasItem(inv.backpack))
        {
            JonahStaticManager.PickedUpEraser = true;
            Debug.Log("[EraserWatcher] Eraser found — PickedUpEraser = true", this);
            enabled = false; // job done, stop checking
        }
    }

    bool HasItem(InventorySlot[] slots)
    {
        if (slots == null) return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty) continue;
            if (debugLogs) Debug.Log($"[EraserWatcher] slot {i}: {slots[i].item.name}");
            if (slots[i].item == eraserItem) return true;
        }
        return false;
    }
}