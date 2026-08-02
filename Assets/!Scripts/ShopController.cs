using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Runs a shop: opens the inventory + shop panel, handles buying and selling,
/// takes/gives money through your Character, plays sounds, and shows forced
/// messages. Put ONE in the scene. DragAndDropController calls into this for
/// shop gestures; everything money-related lives here.
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Instance { get; private set; }

    [Header("Panels")]
    public InventoryPanelUI inventoryPanel;
    public ShopPanelUI shopPanel;

    [Header("Money (uses your Character stat system)")]
    [Tooltip("The player's Character. If left empty, the first one found is used.")]
    public Character playerCharacter;
    public string moneyStat = "Money";
    [Tooltip("Also record purchases as expenses in NightRecapManager (like your trigger script).")]
    public bool trackExpenseInRecap = true;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip buySuccessSound;
    public AudioClip buyFailSound;
    public AudioClip sellSuccessSound;
    public AudioClip sellFailSound;

    [Header("Messages")]
    [Tooltip("How long a forced message (e.g. 'Item purchased') stays before hover text returns.")]
    public float forcedMessageDuration = 1f;

    public bool IsShopOpen { get; private set; }
    Shop openShop;

    // Pending drag/click purchase (item picked up off a shop slot).
    SlotLocation pendingBuySlot;
    bool pendingBuyValid;
    SlotLocation lastBuyLanded;   // where the most recent purchase landed (for the fly animation)

    float forcedUntil;
    NightRecapManager recap;
    readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsShopOpen) return;
        if (Time.unscaledTime < forcedUntil) return; // a forced message is showing
        UpdateHoverInfo();
    }

    // ---- Open / close ----------------------------------------------------

    public void OpenShop(Shop shop)
    {
        if (shop == null || IsShopOpen) return;

        openShop = shop;
        IsShopOpen = true;
        pendingBuyValid = false;
        forcedUntil = 0f;
        ResolveRefs();

        if (InventoryManager.Instance != null) InventoryManager.Instance.SetOpenShop(shop);
        if (shopPanel != null) shopPanel.Open(shop);
        if (inventoryPanel != null) inventoryPanel.Open(freezeOverride: false); // shops don't freeze time
    }

    public void Close()
    {
        if (!IsShopOpen) return;
        if (inventoryPanel != null) inventoryPanel.Close(); // -> OnInventoryClosed -> Teardown
        else Teardown();
    }

    public void OnInventoryClosed()
    {
        if (IsShopOpen) Teardown();
    }

    void Teardown()
    {
        IsShopOpen = false;
        openShop = null;
        pendingBuyValid = false;
        if (shopPanel != null) shopPanel.Close();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SetOpenShop(null);
    }

    // ---- Buying ----------------------------------------------------------

    /// <summary>Shift-click a shop slot to buy one unit (goes to the backpack, animated).</summary>
    public void ShiftClickBuy(SlotLocation shopSlot)
    {
        if (!IsShopOpen || openShop == null) return;
        var entry = openShop.EntryAt(shopSlot.index);
        var slot = InventoryManager.Instance.GetSlot(shopSlot);
        if (entry == null || slot == null || slot.IsEmpty) return;   // not a buyable slot

        Sprite icon = slot.item.icon;
        if (Buy(entry, shopSlot, null) && InventoryFX.Instance != null)
            InventoryFX.Instance.FlyBetweenSlots(shopSlot, lastBuyLanded, icon);
    }

    /// <summary>Begin a drag/click purchase: carry a copy of the shop item (shop unchanged).</summary>
    public bool TryBeginBuy(SlotLocation shopSlot, out ItemData item, out int count)
    {
        item = null; count = 0;
        if (!IsShopOpen || openShop == null) return false;
        var entry = openShop.EntryAt(shopSlot.index);
        var slot = InventoryManager.Instance.GetSlot(shopSlot);
        if (entry == null || slot == null || slot.IsEmpty) return false;

        pendingBuySlot = shopSlot;
        pendingBuyValid = true;
        item = slot.item;
        count = 1;
        return true;
    }

    /// <summary>Complete a carried purchase dropped on a specific player slot. The item
    /// lands in that slot when it can accept it, otherwise the first free backpack slot.</summary>
    public bool CompleteBuy(SlotLocation target)
    {
        if (!pendingBuyValid || !IsShopOpen || openShop == null) { pendingBuyValid = false; return false; }
        pendingBuyValid = false;

        var entry = openShop.EntryAt(pendingBuySlot.index);
        var slot = InventoryManager.Instance.GetSlot(pendingBuySlot);
        if (entry == null || slot == null || slot.IsEmpty) return false; // stock gone
        return Buy(entry, pendingBuySlot, target);                       // no anim (the drag was the anim)
    }

    public void CancelBuy() => pendingBuyValid = false;

    bool Buy(Shop.ShopEntry entry, SlotLocation shopSlot, SlotLocation? target)
    {
        var item = entry.item;
        int price = entry.price;

        bool roomTarget = target.HasValue && InventoryManager.Instance.CanAccept(target.Value, item);
        bool roomBackpack = InventoryManager.Instance.BackpackRoomFor(item) >= 1;

        if (!roomTarget && !roomBackpack) { Fail(buyFailSound, "Inventory full"); return false; }
        if (Money() < price)              { Fail(buyFailSound, "Not enough money"); return false; }

        Spend(price);

        if (roomTarget)
        {
            int one = 1;
            InventoryManager.Instance.PlaceOne(target.Value, item, ref one);   // exactly where they dropped it
            lastBuyLanded = target.Value;
        }
        else
        {
            InventoryManager.Instance.AddToBackpack(item, 1);
            lastBuyLanded = InventoryManager.Instance.FirstSlotWith(item) ?? shopSlot;
        }

        Success(buySuccessSound, "Item purchased");
        if (entry.buyOnce) openShop.RemoveSlot(shopSlot.index);            // removed + shop compacts
        return true;
    }

    // ---- Selling ---------------------------------------------------------

    /// <summary>Shift-click a player slot to sell the whole stack. The stack flies to a
    /// sell slot and the sale is announced on arrival.</summary>
    public void ShiftClickSell(SlotLocation playerSlot)
    {
        if (!IsShopOpen || openShop == null) return;
        var slot = InventoryManager.Instance.GetSlot(playerSlot);
        if (slot == null || slot.IsEmpty || slot.item.isEmptyHand) return;

        if (!openShop.CanSell || !slot.item.sellable)
        {
            Fail(sellFailSound, "ITEM NOT TRADABLE");
            return;
        }

        var item = slot.item;
        int total = item.sellValue * slot.count;

        InventoryManager.Instance.TakeStack(playerSlot, out _, out _);   // remove now (in flight)
        AddMoney(total);

        if (InventoryFX.Instance != null && openShop.TryGetSellSlot(out int sellIdx))
        {
            var sellLoc = new SlotLocation(InventoryArea.Shop, sellIdx);
            InventoryFX.Instance.FlyBetweenSlots(playerSlot, sellLoc, item.icon,
                () => Success(sellSuccessSound, $"+${total}  SALE COMPLETE"));
        }
        else
        {
            Success(sellSuccessSound, $"+${total}  SALE COMPLETE");
        }
    }

    /// <summary>Sell a single unit (right-click drop-one). Returns true if it sold.</summary>
    public bool SellOne(ItemData item)
    {
        if (item == null || item.isEmptyHand) return false;
        if (!IsShopOpen || openShop == null || !openShop.CanSell || !item.sellable)
        {
            Fail(sellFailSound, "ITEM NOT TRADABLE");
            return false;
        }

        AddMoney(item.sellValue);
        Success(sellSuccessSound, $"+${item.sellValue}  SALE COMPLETE");
        return true;
    }

    /// <summary>Complete a sale for a carried player stack dropped on the shop. Returns
    /// true if it sold (caller then consumes the carried stack); false if rejected.</summary>
    public bool CompleteSale(ItemData item, int count)
    {
        if (item == null || count <= 0) return false;

        if (!IsShopOpen || openShop == null || !openShop.CanSell || !item.sellable)
        {
            Fail(sellFailSound, "ITEM NOT TRADABLE");
            return false;
        }

        int total = item.sellValue * count;
        AddMoney(total);
        Success(sellSuccessSound, $"+${total}  SALE COMPLETE");
        return true;
    }

    // ---- Hover info ------------------------------------------------------

    // Only SHOP slots drive the shop's title/description/price. Hovering the player's
    // own items leaves the shop panel untouched (they show in the backpack tooltip).
    void UpdateHoverInfo()
    {
        if (shopPanel == null) return;

        SlotUI s = SlotUnderPointer();
        if (s == null) return;                          // over nothing -> leave shop info as-is

        var loc = s.Location;
        if (loc.area != InventoryArea.Shop) return;     // personal slots don't change shop info

        var entry = openShop.EntryAt(loc.index);
        var slot = InventoryManager.Instance.GetSlot(loc);
        if (entry != null && slot != null && !slot.IsEmpty)
        {
            bool afford = Money() >= entry.price;
            shopPanel.ShowInfo(slot.item.itemName, slot.item.description, "$" + entry.price,
                               afford ? shopPanel.affordableColor : shopPanel.unaffordableColor);
        }
        else shopPanel.ClearInfo();                     // empty shop slot
    }

    // ---- Money / feedback ------------------------------------------------

    float Money() => playerCharacter != null ? playerCharacter.GetStatValue(moneyStat) : 0f;

    void Spend(int amount)
    {
        if (playerCharacter != null) playerCharacter.ModifyStat(moneyStat, -amount);
        if (trackExpenseInRecap && recap != null) recap.AddExpense(amount);
    }

    void AddMoney(int amount)
    {
        if (playerCharacter != null) playerCharacter.ModifyStat(moneyStat, amount);
    }

    void Success(AudioClip clip, string message)
    {
        PlaySound(clip);
        ForceMessage(message, shopPanel != null ? shopPanel.affordableColor : Color.green);
    }

    void Fail(AudioClip clip, string message)
    {
        PlaySound(clip);
        ForceMessage(message, shopPanel != null ? shopPanel.unaffordableColor : Color.red);
    }

    void ForceMessage(string message, Color color)
    {
        if (shopPanel != null) shopPanel.ShowMessage(message, color);
        forcedUntil = Time.unscaledTime + Mathf.Max(0f, forcedMessageDuration);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    void ResolveRefs()
    {
        if (playerCharacter == null) playerCharacter = FindObjectOfType<Character>();
        if (recap == null) recap = FindObjectOfType<NightRecapManager>();
    }

    // ---- Pointer ---------------------------------------------------------

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
