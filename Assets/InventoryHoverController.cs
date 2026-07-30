using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// While the inventory (and/or a loot box) is open, this shows the hovered item's
/// name + description in two TMP fields on the bag UI, and moves the highlight onto
/// the hovered slot instead of the toolbar's selected slot.
///
/// Works for hotbar, backpack, and container slots automatically — they're all
/// SlotUI, found by the same raycast the clicks use. Put ONE in the scene.
/// </summary>
public class InventoryHoverController : MonoBehaviour
{
    [Header("Tooltip (place these on your bag UI)")]
    [Tooltip("Optional root shown only while hovering an item (e.g. a tooltip background).")]
    public GameObject tooltipRoot;
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Hover Highlight")]
    public bool highlightHoveredSlot = true;
    [Tooltip("Highlight empty slots you hover too (like the toolbar highlight). " +
             "Off = only highlight slots that contain an item.")]
    public bool highlightEmptySlots = true;

    SlotUI hovered;
    readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();

    void Awake() => HideTooltip();

    void Update()
    {
        if (!InventoryPanelUI.IsOpen)
        {
            ClearHover();
            return;
        }

        SlotUI s = SlotUnderPointer();
        if (s != hovered)
        {
            // Clearing the previous slot is always safe while open: HotbarUI doesn't
            // touch highlights until the bag closes, at which point it re-applies them.
            if (hovered != null) hovered.SetSelected(false);
            hovered = s;
            UpdateTooltip(hovered);
        }

        // Re-assert every frame so nothing (e.g. HotbarUI on the open frame) can
        // leave the hovered slot's highlight stuck off.
        if (hovered != null && ShouldHighlight(hovered))
            hovered.SetSelected(true);
    }

    void ClearHover()
    {
        // Only clear non-hotbar highlights here; HotbarUI restores hotbar highlights
        // itself when the bag closes (avoids clobbering the selected-slot highlight).
        if (hovered != null && hovered.area != InventoryArea.Hotbar)
            hovered.SetSelected(false);
        hovered = null;
        HideTooltip();
    }

    bool ShouldHighlight(SlotUI s)
    {
        if (!highlightHoveredSlot || s == null || !s.interactable) return false;
        if (highlightEmptySlots) return true;
        var slot = InventoryManager.Instance != null ? InventoryManager.Instance.GetSlot(s.Location) : null;
        return slot != null && !slot.IsEmpty;
    }

    void UpdateTooltip(SlotUI s)
    {
        ItemData item = null;
        if (s != null && InventoryManager.Instance != null)
        {
            var slot = InventoryManager.Instance.GetSlot(s.Location);
            if (slot != null && !slot.IsEmpty && !slot.item.isEmptyHand)
                item = slot.item;
        }

        if (item == null) { HideTooltip(); return; }

        if (tooltipRoot != null) tooltipRoot.SetActive(true);
        if (nameText != null) nameText.text = item.itemName;
        if (descriptionText != null) descriptionText.text = item.description;
    }

    void HideTooltip()
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        if (nameText != null) nameText.text = "";
        if (descriptionText != null) descriptionText.text = "";
    }

    // Same "slot under the cursor" raycast the click system uses.
    SlotUI SlotUnderPointer()
    {
        if (EventSystem.current == null) return null;
        Vector2 pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        var ped = new PointerEventData(EventSystem.current) { position = pos };
        raycastBuffer.Clear();
        EventSystem.current.RaycastAll(ped, raycastBuffer);
        for (int i = 0; i < raycastBuffer.Count; i++)
        {
            var s = raycastBuffer[i].gameObject.GetComponentInParent<SlotUI>();
            if (s != null) return s;
        }
        return null;
    }
}
