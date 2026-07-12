using UnityEngine;

/// <summary>
/// Coordinates opening a loot container: it opens your normal inventory panel AND
/// the loot panel beside it, and registers the container with the InventoryManager
/// so its slots become addressable. Put ONE in the scene.
///
/// Closing is always routed through the inventory panel (so Tab, Esc, or pressing
/// E again all tear everything down consistently).
/// </summary>
public class LootController : MonoBehaviour
{
    public static LootController Instance { get; private set; }

    [Header("References")]
    public InventoryPanelUI inventoryPanel;
    public LootPanelUI lootPanel;

    public bool IsContainerOpen { get; private set; }
    LootContainer openContainer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Open a container: inventory + loot panel, and register its slots.</summary>
    public void OpenContainer(LootContainer container)
    {
        if (container == null || IsContainerOpen) return;

        openContainer = container;
        IsContainerOpen = true;

        if (InventoryManager.Instance != null) InventoryManager.Instance.SetOpenContainer(container);
        if (lootPanel != null) lootPanel.Open(container);
        if (inventoryPanel != null) inventoryPanel.Open();   // freezes time, frees cursor, etc.
    }

    /// <summary>Close everything (delegates to the inventory panel's close).</summary>
    public void Close()
    {
        if (!IsContainerOpen) return;
        if (inventoryPanel != null) inventoryPanel.Close();  // -> OnInventoryClosed -> Teardown
        else Teardown();
    }

    /// <summary>Called by InventoryPanelUI whenever it closes, so loot state stays in sync.</summary>
    public void OnInventoryClosed()
    {
        if (IsContainerOpen) Teardown();
    }

    void Teardown()
    {
        IsContainerOpen = false;
        openContainer = null;
        if (lootPanel != null) lootPanel.Close();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SetOpenContainer(null);
    }
}
