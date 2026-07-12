using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The loot panel shown beside the inventory while a container is open. Lives on
/// the inventory canvas next to the backpack panel, and uses the SAME slot prefab
/// so it looks identical. LootController drives Open/Close.
///
/// Because its slots are InventoryArea.Container, all the normal click / drag /
/// right-click / shift-click behaviour works through DragAndDropController with no
/// extra code.
/// </summary>
public class LootPanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root object of the loot panel (its own background). Enabled while open.")]
    public GameObject panelRoot;
    public SlotUI slotPrefab;
    public Transform slotContainer;    // e.g. a GridLayoutGroup
    public TMP_Text titleText;

    readonly List<SlotUI> slots = new List<SlotUI>();
    LootContainer current;
    int builtCount = -1;
    bool subscribedToManager;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open(LootContainer container)
    {
        current = container;
        if (titleText != null) titleText.text = container.DisplayName;

        BuildSlots(container.SlotCount);
        Subscribe();

        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        Unsubscribe();
        current = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void BuildSlots(int count)
    {
        if (builtCount == count) return;   // reuse existing slot objects

        foreach (var s in slots) if (s != null) Destroy(s.gameObject);
        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            var s = Instantiate(slotPrefab, slotContainer);
            s.Init(InventoryArea.Container, i, interactable: true);
            slots.Add(s);
        }
        builtCount = count;
    }

    void RefreshAll()
    {
        foreach (var s in slots) if (s != null) s.Refresh();
    }

    // Refresh on any inventory change (covers drag/drop) and on the container's own
    // change (covers re-rolled loot while you're looking inside).
    void Subscribe()
    {
        if (!subscribedToManager && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged += RefreshAll;
            subscribedToManager = true;
        }
        if (current != null) current.Changed += RefreshAll;
    }

    void Unsubscribe()
    {
        if (current != null) current.Changed -= RefreshAll;
        if (subscribedToManager && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged -= RefreshAll;
            subscribedToManager = false;
        }
    }

    void OnDestroy() => Unsubscribe();
}
