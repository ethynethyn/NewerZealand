using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A world shop (a locker-like object you look at and press E to open). Owns its own
/// slots, shown to the right of the backpack like a loot container.
///
///   - BUY: stock entries fill the slots; each has its own price and a "buy once" flag.
///     Buying moves one unit to the backpack for its price. Buy-once items are removed
///     from the shop after purchase; others restock endlessly.
///   - SELL: the player drops (or shift-clicks) items into the shop to sell them.
///     Whether an item can be sold and for how much is set on the ItemData.
///
/// Put this on an object with a Collider, on whatever layer your ShopInteractor uses.
/// </summary>
public class Shop : MonoBehaviour
{
    public enum ShopMode { BuyAndSell, BuyOnly, SellOnly }

    [Header("Identity")]
    public string displayName = "Shop";

    [Header("Mode")]
    public ShopMode mode = ShopMode.BuyAndSell;

    [Header("Size")]
    [Min(1)] public int slotCount = 12;

    [Header("Stock (buyable items)")]
    public List<ShopEntry> stock = new List<ShopEntry>();

    [Serializable]
    public class ShopEntry
    {
        public ItemData item;
        [Min(0)] public int price = 10;
        [Tooltip("If ON, this item is removed from the shop once bought.")]
        public bool buyOnce = false;
    }

    // ---- Runtime --------------------------------------------------------

    InventorySlot[] slots;
    ShopEntry[] slotEntry;   // parallel to slots; null = a free slot usable for selling
    bool built;

    /// <summary>Fired when the shop's own contents change (e.g. a buy-once item removed).</summary>
    public event Action Changed;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int SlotCount => Mathf.Max(1, slotCount);
    public ShopMode Mode => mode;
    public bool CanBuy => mode != ShopMode.SellOnly;
    public bool CanSell => mode != ShopMode.BuyOnly;

    public InventorySlot[] Slots { get { EnsureBuilt(); return slots; } }

    void Awake() => EnsureBuilt();

    void EnsureBuilt()
    {
        if (built && slots != null && slots.Length == SlotCount) return;

        slots = new InventorySlot[SlotCount];
        slotEntry = new ShopEntry[SlotCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot();

        if (CanBuy)
        {
            int idx = 0;
            foreach (var e in stock)
            {
                if (e == null || e.item == null || e.item.isEmptyHand) continue;
                if (idx >= slots.Length) break;
                slots[idx].Set(e.item, 1);   // one unit shown per stock slot
                slotEntry[idx] = e;
                idx++;
            }
        }

        built = true;
    }

    /// <summary>The stock entry backing a slot, or null if it's a free/sell slot.</summary>
    public ShopEntry EntryAt(int index)
    {
        EnsureBuilt();
        return (index >= 0 && index < slotEntry.Length) ? slotEntry[index] : null;
    }

    /// <summary>Remove a buy-once item after purchase, then close the gap so the
    /// remaining stock has no empty holes.</summary>
    public void RemoveSlot(int index)
    {
        EnsureBuilt();
        if (index < 0 || index >= slots.Length) return;
        slots[index].Clear();
        slotEntry[index] = null;
        Compact();
        Changed?.Invoke();
    }

    // Pack all buyable stock to the front; empty (sell) slots end up at the back.
    void Compact()
    {
        int write = 0;
        for (int read = 0; read < slots.Length; read++)
        {
            if (slotEntry[read] != null && !slots[read].IsEmpty)
            {
                if (write != read)
                {
                    slots[write].Set(slots[read].item, slots[read].count);
                    slotEntry[write] = slotEntry[read];
                    slots[read].Clear();
                    slotEntry[read] = null;
                }
                write++;
            }
        }
    }

    /// <summary>Find a slot to drop a sale into: a free (non-stock) empty slot if any,
    /// else any slot. Returns false only if the shop has no slots.</summary>
    public bool TryGetSellSlot(out int index)
    {
        EnsureBuilt();
        for (int i = 0; i < slots.Length; i++)
            if (slotEntry[i] == null && slots[i].IsEmpty) { index = i; return true; }
        index = slots.Length > 0 ? slots.Length - 1 : -1;
        return index >= 0;
    }

    public void RaiseChanged() => Changed?.Invoke();
}
