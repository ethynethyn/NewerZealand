using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// The "cursor stack" for the inventory (Minecraft-style). All slot interaction is
/// driven from here by polling the mouse and raycasting for the slot under the
/// cursor — this deliberately looks THROUGH the cursor ghost, so the ghost can
/// never block a click (that was the old bug).
///
///   - Left-click a slot: pick it up (follows cursor). Left-click / release on
///     another slot: drop all / merge / swap.
///   - Right-click a slot while carrying: drop ONE.
///   - Right-click a slot while empty-handed: pick up half (toggleable).
///   - Shift + left-click: quick-move the stack to the other container.
///   - Or drag a stack in one motion (press, move, release).
///
/// Put ONE in the scene. Requires an EventSystem and a GraphicRaycaster on each
/// canvas (Unity adds these by default). The ghost Image should have Raycast
/// Target OFF and sit on a top Screen Space - Overlay canvas.
/// </summary>
public class DragAndDropController : MonoBehaviour
{
    public static DragAndDropController Instance { get; private set; }

    [Header("Cursor Ghost")]
    [Tooltip("Image that follows the cursor while carrying. Raycast Target OFF.")]
    public Image ghostImage;
    [Tooltip("Optional count text on the ghost (Raycast Target OFF).")]
    public TMP_Text ghostCountText;

    [Header("Behaviour")]
    [Tooltip("Right-clicking a slot while NOT carrying picks up half the stack. " +
             "Turn off if you only ever want right-click to place one.")]
    public bool rightClickPicksUpHalf = true;

    // The carried stack.
    ItemData carriedItem;
    int carriedCount;

    // Press bookkeeping (to tell a pick-up click from a place click / drag).
    SlotLocation pressSlot;
    bool pressSlotValid;
    bool pickedUpThisPress;

    readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();

    public bool IsCarrying => carriedItem != null && carriedCount > 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        HideGhost();
    }

    void Update()
    {
        if (IsCarrying && ghostImage != null)
            ghostImage.transform.position = MousePos();   // ghost follows the cursor

        // Slots only respond while the bag is open (cursor is free).
        if (!InventoryPanelUI.IsOpen || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) HandleLeftDown();
        if (Mouse.current.leftButton.wasReleasedThisFrame) HandleLeftUp();
        if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();
    }

    // ---- Left button (pick up / place / drag) ----------------------------

    void HandleLeftDown()
    {
        pickedUpThisPress = false;
        pressSlotValid = false;

        SlotUI s = SlotUnderPointer();
        if (s == null || !s.interactable) return;

        pressSlot = s.Location;
        pressSlotValid = true;

        if (IsCarrying) return;                 // placement happens on release / next click

        if (ShiftHeld())
        {
            InventoryManager.Instance.QuickMove(pressSlot);
        }
        else if (InventoryManager.Instance.TakeStack(pressSlot, out carriedItem, out carriedCount))
        {
            pickedUpThisPress = true;
            ShowGhost();
        }
    }

    void HandleLeftUp()
    {
        if (!IsCarrying) return;

        SlotUI s = SlotUnderPointer();
        if (s == null || !s.interactable) return;   // released over nothing -> keep carrying

        var loc = s.Location;

        // Releasing on the very slot we just picked up from = a plain pick-up click:
        // keep the stack on the cursor instead of dropping it straight back.
        bool sameAsPickup = pressSlotValid && pickedUpThisPress && loc.Equals(pressSlot);
        if (sameAsPickup) return;

        InventoryManager.Instance.PlaceStack(loc, ref carriedItem, ref carriedCount);
        RefreshGhost();
    }

    // ---- Right button ----------------------------------------------------

    void HandleRightClick()
    {
        SlotUI s = SlotUnderPointer();
        if (s == null || !s.interactable) return;
        var loc = s.Location;

        if (IsCarrying)
        {
            InventoryManager.Instance.PlaceOne(loc, carriedItem, ref carriedCount);
            if (carriedCount <= 0) carriedItem = null;
            RefreshGhost();
        }
        else if (rightClickPicksUpHalf)
        {
            if (InventoryManager.Instance.TakeHalf(loc, out carriedItem, out carriedCount))
                ShowGhost();
        }
    }

    // ---- Cleanup ----------------------------------------------------------

    /// <summary>Call when the bag closes so a carried stack is never lost.</summary>
    public void ReturnCarriedToInventory()
    {
        if (IsCarrying)
        {
            InventoryManager.Instance.AddItem(carriedItem, carriedCount);
            carriedItem = null; carriedCount = 0;
        }
        pressSlotValid = false;
        pickedUpThisPress = false;
        HideGhost();
    }

    // ---- Ghost helpers ----------------------------------------------------

    void ShowGhost()
    {
        if (ghostImage != null)
        {
            ghostImage.sprite = carriedItem != null ? carriedItem.icon : null;
            ghostImage.enabled = true;
            ghostImage.gameObject.SetActive(true);
            ghostImage.transform.position = MousePos();
        }
        RefreshGhostCount();
    }

    void RefreshGhost()
    {
        if (!IsCarrying) { HideGhost(); return; }
        ShowGhost();
    }

    void RefreshGhostCount()
    {
        if (ghostCountText != null)
            ghostCountText.text = carriedCount > 1 ? carriedCount.ToString() : "";
    }

    void HideGhost()
    {
        if (ghostImage != null) ghostImage.gameObject.SetActive(false);
        if (ghostCountText != null) ghostCountText.text = "";
    }

    // ---- Pointer utilities ------------------------------------------------

    static bool ShiftHeld()
        => Keyboard.current != null &&
           (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    static Vector2 MousePos()
        => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

    SlotUI SlotUnderPointer()
    {
        if (EventSystem.current == null) return null;
        var ped = new PointerEventData(EventSystem.current) { position = MousePos() };
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