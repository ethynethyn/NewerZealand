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
/// SHOP integration: when a shop is open, shop gestures are routed to ShopController
/// instead of moving items freely:
///   - Shift-click a shop slot = buy; shift-click a player slot = sell.
///   - Drag/click a shop item onto the backpack = buy; drop it back = cancel.
///   - Drag/click a player item onto the shop = sell (if allowed); else it returns.
///
/// Put ONE in the scene. Requires an EventSystem and a GraphicRaycaster on each
/// canvas. The ghost Image should have Raycast Target OFF on a top overlay canvas.
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

    // True while the carried stack is a tentative shop purchase (a copy of a shop
    // item that hasn't been paid for yet). Dropping it on the inventory buys it;
    // dropping it anywhere else discards it (the shop keeps its item).
    bool carryingPurchase;

    // Press bookkeeping (to tell a pick-up click from a place click / drag).
    SlotLocation pressSlot;
    bool pressSlotValid;
    bool pickedUpThisPress;

    readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();

    public bool IsCarrying => carriedItem != null && carriedCount > 0;

    // Right-drag "paint one per slot" state.
    bool rightPainting;
    readonly List<SlotLocation> paintedSlots = new List<SlotLocation>();

    bool ShopOpen => ShopController.Instance != null && ShopController.Instance.IsShopOpen;
    static bool IsShopSlot(SlotLocation loc) => loc.area == InventoryArea.Shop;

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

        if (Mouse.current.leftButton.wasPressedThisFrame)  HandleLeftDown();
        if (Mouse.current.leftButton.wasReleasedThisFrame) HandleLeftUp();

        if (Mouse.current.rightButton.wasPressedThisFrame)      HandleRightDown();
        else if (Mouse.current.rightButton.isPressed)           HandleRightDrag();
        if (Mouse.current.rightButton.wasReleasedThisFrame)     HandleRightUp();
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

        bool shift = ShiftHeld();

        // ---- Shop gestures ----
        if (ShopOpen)
        {
            if (shift)
            {
                if (IsShopSlot(pressSlot)) ShopController.Instance.ShiftClickBuy(pressSlot);
                else                       ShopController.Instance.ShiftClickSell(pressSlot);
                return;
            }

            if (IsShopSlot(pressSlot))
            {
                // Pick up a purchase: carry a copy, leave the shop slot untouched.
                if (ShopController.Instance.TryBeginBuy(pressSlot, out carriedItem, out carriedCount))
                {
                    carryingPurchase = true;
                    pickedUpThisPress = true;
                    ShowGhost();
                }
                return;
            }
            // Otherwise it's a player slot: fall through and pick it up normally
            // (so it can be dragged onto the shop to sell).
        }

        // ---- Normal ----
        if (shift)
        {
            var srcSlot = InventoryManager.Instance.GetSlot(pressSlot);
            Sprite icon = (srcSlot != null && !srcSlot.IsEmpty) ? srcSlot.item.icon : null;

            var moves = InventoryManager.Instance.QuickMove(pressSlot);
            if (icon != null && InventoryFX.Instance != null)
                for (int i = 0; i < moves.Count; i++)
                    InventoryFX.Instance.FlyBetweenSlots(moves[i].from, moves[i].to, icon);
        }
        else if (InventoryManager.Instance.TakeStack(pressSlot, out carriedItem, out carriedCount))
        {
            carryingPurchase = false;
            pickedUpThisPress = true;
            ShowGhost();
        }
    }

    void HandleLeftUp()
    {
        if (!IsCarrying) return;

        SlotUI s = SlotUnderPointer();

        // Released over nothing.
        if (s == null || !s.interactable)
        {
            if (carryingPurchase) CancelPurchase();   // discard the unpaid copy
            return;                                    // (normal carry stays on the cursor)
        }

        var loc = s.Location;

        // Releasing on the very slot we picked up from = a plain pick-up click:
        // keep the stack on the cursor instead of dropping it straight back.
        bool sameAsPickup = pressSlotValid && pickedUpThisPress && loc.Equals(pressSlot);
        if (sameAsPickup) return;

        // ---- Carrying a purchase ----
        if (carryingPurchase)
        {
            if (IsShopSlot(loc))
            {
                CancelPurchase();                 // dropped back on the shop -> no buy
            }
            else
            {
                ShopController.Instance.CompleteBuy(loc);   // dropped on inventory -> lands in THIS slot
                carriedItem = null; carriedCount = 0; carryingPurchase = false;
                RefreshGhost();
            }
            return;
        }

        // ---- Carrying a normal stack; dropping on the shop = sell ----
        if (ShopOpen && IsShopSlot(loc))
        {
            bool sold = ShopController.Instance.CompleteSale(carriedItem, carriedCount);
            if (sold) { carriedItem = null; carriedCount = 0; }        // consumed by the sale
            else InventoryManager.Instance.PlaceStack(pressSlot, ref carriedItem, ref carriedCount); // return it
            RefreshGhost();
            return;
        }

        // ---- Normal placement ----
        InventoryManager.Instance.PlaceStack(loc, ref carriedItem, ref carriedCount);
        RefreshGhost();
    }

    // ---- Right button (place one / paint one-per-slot / take half) --------

    void HandleRightDown()
    {
        SlotUI s = SlotUnderPointer();
        if (s == null || !s.interactable) return;

        // Shop: right-click drops ONE from the carried stack into the sell area.
        if (ShopOpen && IsShopSlot(s.Location))
        {
            if (IsCarrying && !carryingPurchase && ShopController.Instance.SellOne(carriedItem))
            {
                carriedCount -= 1;
                if (carriedCount <= 0) carriedItem = null;
                RefreshGhost();
            }
            return;   // no paint sweep into the shop
        }

        if (carryingPurchase) return;   // don't sprinkle an unpaid purchase

        if (IsCarrying)
        {
            // Begin a paint gesture: drop one into each slot the cursor crosses while
            // right is held. A plain right-click is just a one-slot paint.
            rightPainting = true;
            paintedSlots.Clear();
            PaintOne(s.Location);
        }
        else if (rightClickPicksUpHalf)
        {
            if (InventoryManager.Instance.TakeHalf(s.Location, out carriedItem, out carriedCount))
            {
                carryingPurchase = false;
                ShowGhost();
            }
        }
    }

    void HandleRightDrag()
    {
        if (!rightPainting || !IsCarrying || carryingPurchase) return;

        SlotUI s = SlotUnderPointer();
        if (s == null || !s.interactable || IsShopSlot(s.Location)) return;  // never paint into the shop

        PaintOne(s.Location);
    }

    void HandleRightUp()
    {
        rightPainting = false;
        paintedSlots.Clear();
    }

    // Drops one carried item into a slot, at most once per slot per paint gesture.
    void PaintOne(SlotLocation loc)
    {
        if (!IsCarrying || AlreadyPainted(loc)) return;

        paintedSlots.Add(loc);
        InventoryManager.Instance.PlaceOne(loc, carriedItem, ref carriedCount);
        if (carriedCount <= 0) carriedItem = null;
        RefreshGhost();
    }

    bool AlreadyPainted(SlotLocation loc)
    {
        for (int i = 0; i < paintedSlots.Count; i++)
            if (paintedSlots[i].Equals(loc)) return true;
        return false;
    }

    // ---- Cleanup ----------------------------------------------------------

    void CancelPurchase()
    {
        carriedItem = null; carriedCount = 0; carryingPurchase = false;
        if (ShopController.Instance != null) ShopController.Instance.CancelBuy();
        RefreshGhost();
    }

    /// <summary>Call when the bag closes so a carried stack is never lost.</summary>
    public void ReturnCarriedToInventory()
    {
        if (IsCarrying)
        {
            // An unpaid purchase is discarded (the shop still has it); a real carried
            // stack (e.g. a sell in progress) goes back into the inventory.
            if (!carryingPurchase)
                InventoryManager.Instance.AddItem(carriedItem, carriedCount);
        }
        carriedItem = null; carriedCount = 0; carryingPurchase = false;
        pressSlotValid = false;
        pickedUpThisPress = false;
        rightPainting = false;
        paintedSlots.Clear();
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
