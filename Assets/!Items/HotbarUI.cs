using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the hotbar row from a slot prefab and drives selection with the scroll
/// wheel and number keys (1 = hand, 2 = slot 2, ...).
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [Header("Slot Building")]
    public SlotUI slotPrefab;
    public Transform slotContainer;   // e.g. a horizontal LayoutGroup

    [Header("Selection Input")]
    public bool enableScroll = true;
    [Tooltip("Flip if scrolling feels backwards. Default: scroll up = previous slot.")]
    public bool invertScroll = false;
    public bool enableNumberKeys = true;

    readonly List<SlotUI> slots = new List<SlotUI>();

    // 1..9 -> hotbar indices 0..8
    static readonly Key[] NumberKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    void Start()
    {
        BuildSlots();

        var mgr = InventoryManager.Instance;
        mgr.OnChanged += RefreshAll;
        mgr.OnSelectionChanged += RefreshSelection;

        RefreshAll();
        RefreshSelection();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance == null) return;
        InventoryManager.Instance.OnChanged -= RefreshAll;
        InventoryManager.Instance.OnSelectionChanged -= RefreshSelection;
    }

    void BuildSlots()
    {
        var mgr = InventoryManager.Instance;
        for (int i = 0; i < mgr.hotbar.Length; i++)
        {
            var s = Instantiate(slotPrefab, slotContainer);
            s.Init(InventoryArea.Hotbar, i, interactable: i != 0); // slot 0 (hand) is locked
            slots.Add(s);
        }
    }

    void RefreshAll()
    {
        foreach (var s in slots) s.Refresh();
    }

    void RefreshSelection()
    {
        // While the bag is open, the hovered slot is the highlighted one instead,
        // so the toolbar selection highlight steps aside.
        bool show = !InventoryPanelUI.IsOpen;
        int sel = InventoryManager.Instance.SelectedHotbarIndex;
        for (int i = 0; i < slots.Count; i++)
            slots[i].SetSelected(show && i == sel);
    }

    bool prevOpen;

    void Update()
    {
        // Re-apply the selection highlight when the bag opens or closes.
        if (InventoryPanelUI.IsOpen != prevOpen)
        {
            prevOpen = InventoryPanelUI.IsOpen;
            RefreshSelection();
        }

        if (InventoryPanelUI.IsOpen) return; // don't switch slots while the bag is open

        var mgr = InventoryManager.Instance;

        if (enableScroll && Mouse.current != null)
        {
            float y = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(y) > 0.01f)
            {
                int dir = (y > 0f) ? -1 : 1;   // scroll up -> previous
                if (invertScroll) dir = -dir;
                mgr.ScrollSelection(dir);
            }
        }

        if (enableNumberKeys && Keyboard.current != null)
        {
            int max = Mathf.Min(NumberKeys.Length, mgr.hotbar.Length);
            for (int i = 0; i < max; i++)
            {
                if (Keyboard.current[NumberKeys[i]].wasPressedThisFrame)
                {
                    mgr.SetSelectedIndex(i);
                    break;
                }
            }
        }
    }
}