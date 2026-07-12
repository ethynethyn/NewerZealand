using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Shared types
// ---------------------------------------------------------------------------

public enum InventoryArea { Hotbar, Backpack, Container }

/// <summary>One inventory cell: an item type + how many of it.</summary>
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int count;

    public bool IsEmpty => item == null || count <= 0;
    public int MaxStack => item != null ? Mathf.Max(1, item.maxStackSize) : 0;
    public int SpaceLeft => item == null ? 0 : Mathf.Max(0, MaxStack - count);
    public bool IsFull => item != null && count >= MaxStack;

    public void Set(ItemData newItem, int newCount)
    {
        item = newItem;
        count = newItem == null ? 0 : Mathf.Max(0, newCount);
        if (count <= 0) Clear();
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }
}

/// <summary>Identifies any slot in the whole inventory (hotbar or backpack).</summary>
public struct SlotLocation
{
    public InventoryArea area;
    public int index;
    public SlotLocation(InventoryArea area, int index) { this.area = area; this.index = index; }
    public bool Equals(SlotLocation o) => area == o.area && index == o.index;
}

// ---------------------------------------------------------------------------
// Manager
// ---------------------------------------------------------------------------

/// <summary>
/// Single source of truth for the inventory. Put ONE of these in your scene.
/// UI scripts read from it and subscribe to its events; they never store item data themselves.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Hotbar")]
    [Min(1)]
    [Tooltip("Total hotbar slots INCLUDING the empty-hand slot at index 0.")]
    public int hotbarSize = 3;

    [Tooltip("The special empty-hand item shown in hotbar slot 0. Give it a 'hand' icon " +
             "and leave its hand sprites empty so it reads as a bare hand.")]
    public ItemData emptyHandItem;

    [Header("Backpack")]
    [Min(0)]
    public int backpackSize = 12;

    [Header("Starting Items (optional, for testing)")]
    public List<StartingStack> startingItems = new List<StartingStack>();

    [SerializeField] private int selectedHotbarIndex = 0;
    public int SelectedHotbarIndex => selectedHotbarIndex;

    // Runtime data (built in Awake, not shown in inspector).
    [NonSerialized] public InventorySlot[] hotbar;
    [NonSerialized] public InventorySlot[] backpack;

    /// <summary>The loot container currently open; its slots are addressed via
    /// InventoryArea.Container. Null when no container is open.</summary>
    [NonSerialized] public LootContainer openContainer;

    /// <summary>Fired whenever the CONTENTS of any slot change.</summary>
    public event Action OnChanged;
    /// <summary>Fired whenever the selected hotbar index changes.</summary>
    public event Action OnSelectionChanged;

    [Serializable]
    public class StartingStack
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }

    // ---- Lifecycle -------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildSlots();

        foreach (var stack in startingItems)
            if (stack != null && stack.item != null)
                AddItem(stack.item, stack.amount);
    }

    void BuildSlots()
    {
        hotbar = new InventorySlot[Mathf.Max(1, hotbarSize)];
        for (int i = 0; i < hotbar.Length; i++) hotbar[i] = new InventorySlot();
        if (emptyHandItem != null) hotbar[0].Set(emptyHandItem, 1); // reserved hand slot

        backpack = new InventorySlot[Mathf.Max(0, backpackSize)];
        for (int i = 0; i < backpack.Length; i++) backpack[i] = new InventorySlot();

        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbar.Length - 1);
    }

    // ---- Queries ---------------------------------------------------------

    public bool IsHandSlot(SlotLocation loc) => loc.area == InventoryArea.Hotbar && loc.index == 0;

    public InventorySlot GetSlot(SlotLocation loc)
    {
        InventorySlot[] arr;
        switch (loc.area)
        {
            case InventoryArea.Hotbar: arr = hotbar; break;
            case InventoryArea.Backpack: arr = backpack; break;
            case InventoryArea.Container: arr = openContainer != null ? openContainer.Slots : null; break;
            default: arr = null; break;
        }
        if (arr == null || loc.index < 0 || loc.index >= arr.Length) return null;
        return arr[loc.index];
    }

    /// <summary>Register/clear the loot container the Container area maps to.</summary>
    public void SetOpenContainer(LootContainer container) => openContainer = container;

    public InventorySlot GetSelectedSlot() => hotbar[selectedHotbarIndex];

    /// <summary>True if the selection should read as a bare hand
    /// (the hand slot itself, OR any empty non-hand slot).</summary>
    public bool IsSelectedEmptyHand()
    {
        if (selectedHotbarIndex == 0) return true;
        return hotbar[selectedHotbarIndex].IsEmpty;
    }

    /// <summary>The real item currently held, or null if bare hand.</summary>
    public ItemData GetSelectedItem()
    {
        return IsSelectedEmptyHand() ? null : hotbar[selectedHotbarIndex].item;
    }

    // ---- Selection -------------------------------------------------------

    public void SetSelectedIndex(int index)
    {
        int n = hotbar.Length;
        index = ((index % n) + n) % n; // wrap
        if (index == selectedHotbarIndex) return;
        selectedHotbarIndex = index;
        OnSelectionChanged?.Invoke();
    }

    /// <summary>dir = +1 (next) or -1 (previous), wraps around.</summary>
    public void ScrollSelection(int dir) => SetSelectedIndex(selectedHotbarIndex + dir);

    // ---- Adding ----------------------------------------------------------

    /// <summary>
    /// Adds items following the pickup rules:
    ///   1) top up the SELECTED slot if it already holds this item
    ///   2) else place into the SELECTED slot if empty
    ///   3) else stack onto matching stacks (hotbar then backpack)
    ///   4) else first empty hotbar slot, then first empty backpack slot
    /// Pass avoidHotbarIndex to skip a slot (used by transform-on-use so the
    /// empty can never lands in the slot you're actively drinking from).
    /// Returns leftover amount that did not fit.
    /// </summary>
    public int AddItem(ItemData item, int amount = 1, int avoidHotbarIndex = -1)
    {
        if (item == null || amount <= 0) return 0;
        if (item.isEmptyHand) return amount; // never add the hand item

        int remaining = amount;
        int sel = selectedHotbarIndex;
        bool selUsable = sel != 0 && sel != avoidHotbarIndex;

        // 1) top up selected
        if (selUsable && hotbar[sel].item == item)
            remaining = StackInto(hotbar[sel], remaining);

        // 2) place into selected if empty
        if (remaining > 0 && selUsable && hotbar[sel].IsEmpty)
            remaining = PlaceInto(hotbar[sel], item, remaining);

        // 3) stack onto matching stacks elsewhere
        if (remaining > 0) remaining = StackAcross(hotbar, item, remaining, 1, avoidHotbarIndex);
        if (remaining > 0) remaining = StackAcross(backpack, item, remaining, 0, -1);

        // 4) first empty slot, hotbar then backpack
        if (remaining > 0) remaining = FillEmpty(hotbar, item, remaining, 1, avoidHotbarIndex);
        if (remaining > 0) remaining = FillEmpty(backpack, item, remaining, 0, -1);

        OnChanged?.Invoke();
        return remaining;
    }

    int StackInto(InventorySlot slot, int amount)
    {
        int add = Mathf.Min(slot.SpaceLeft, amount);
        slot.count += add;
        return amount - add;
    }

    int PlaceInto(InventorySlot slot, ItemData item, int amount)
    {
        int add = Mathf.Min(item.maxStackSize, amount);
        slot.Set(item, add);
        return amount - add;
    }

    int StackAcross(InventorySlot[] arr, ItemData item, int amount, int start, int avoidHotbarIndex)
    {
        for (int i = start; i < arr.Length && amount > 0; i++)
        {
            if (arr == hotbar && i == avoidHotbarIndex) continue;
            if (arr[i].item == item && !arr[i].IsFull)
                amount = StackInto(arr[i], amount);
        }
        return amount;
    }

    int FillEmpty(InventorySlot[] arr, ItemData item, int amount, int start, int avoidHotbarIndex)
    {
        for (int i = start; i < arr.Length && amount > 0; i++)
        {
            if (arr == hotbar && i == avoidHotbarIndex) continue;
            if (arr[i].IsEmpty)
                amount = PlaceInto(arr[i], item, amount);
        }
        return amount;
    }

    // ---- Removing / consuming -------------------------------------------

    public void ConsumeFromSelected(int amount = 1)
    {
        var slot = hotbar[selectedHotbarIndex];
        if (slot.IsEmpty) return;
        slot.count -= amount;
        if (slot.count <= 0) slot.Clear();
        OnChanged?.Invoke();
    }

    // ---- Cursor operations (drag / click-carry) --------------------------
    // These are driven by DragAndDropController, which holds the "carried" stack.

    /// <summary>Pick up a slot's whole stack (never the hand slot). False if nothing taken.</summary>
    public bool TakeStack(SlotLocation loc, out ItemData item, out int count)
    {
        item = null; count = 0;
        if (IsHandSlot(loc)) return false;
        var slot = GetSlot(loc);
        if (slot == null || slot.IsEmpty) return false;

        item = slot.item; count = slot.count;
        slot.Clear();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Pick up half a slot's stack, rounded up (Minecraft-style). False if nothing taken.</summary>
    public bool TakeHalf(SlotLocation loc, out ItemData item, out int count)
    {
        item = null; count = 0;
        if (IsHandSlot(loc)) return false;
        var slot = GetSlot(loc);
        if (slot == null || slot.IsEmpty) return false;

        int half = Mathf.CeilToInt(slot.count * 0.5f);
        item = slot.item; count = half;
        slot.count -= half;
        if (slot.count <= 0) slot.Clear();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Drop the entire carried stack onto a slot. Empty -> fills it; same item ->
    /// merges (leftover stays carried); different item -> swaps (you then carry the
    /// old contents). item/count are updated to whatever is left on the cursor.
    /// </summary>
    public void PlaceStack(SlotLocation loc, ref ItemData item, ref int count)
    {
        if (item == null || count <= 0) return;
        if (IsHandSlot(loc)) return;               // can't drop into the hand slot
        var slot = GetSlot(loc);
        if (slot == null) return;

        if (slot.IsEmpty)
        {
            int place = Mathf.Min(item.maxStackSize, count);
            slot.Set(item, place);
            count -= place;
            if (count <= 0) { item = null; count = 0; }
        }
        else if (slot.item == item)
        {
            int move = Mathf.Min(slot.SpaceLeft, count);
            slot.count += move;
            count -= move;
            if (count <= 0) { item = null; count = 0; }
        }
        else
        {
            var oldItem = slot.item; int oldCount = slot.count;
            slot.Set(item, count);
            item = oldItem; count = oldCount;       // now carrying what was there
        }

        OnChanged?.Invoke();
    }

    /// <summary>Drop ONE carried item onto a slot (empty, or matching &amp; not full). True if placed.</summary>
    public bool PlaceOne(SlotLocation loc, ItemData item, ref int count)
    {
        if (item == null || count <= 0) return false;
        if (IsHandSlot(loc)) return false;
        var slot = GetSlot(loc);
        if (slot == null) return false;

        if (slot.IsEmpty)
        {
            slot.Set(item, 1);
            count -= 1;
            OnChanged?.Invoke();
            return true;
        }
        if (slot.item == item && !slot.IsFull)
        {
            slot.count += 1;
            count -= 1;
            OnChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Shift-click quick move.
    ///   - With a loot container open: a container slot goes to your inventory,
    ///     and one of your slots goes into the container.
    ///   - Otherwise: hotbar &lt;-&gt; backpack.
    /// The hand slot (hotbar index 0) is never used or emptied.
    /// </summary>
    public void QuickMove(SlotLocation from)
    {
        if (IsHandSlot(from)) return;
        var src = GetSlot(from);
        if (src == null || src.IsEmpty) return;

        ItemData item = src.item;
        int amount = src.count;
        int leftover;

        if (openContainer != null)
        {
            if (from.area == InventoryArea.Container)
                leftover = AddItem(item, amount);                            // chest -> inventory
            else
                leftover = DepositInto(openContainer.Slots, 0, item, amount); // inventory -> chest
        }
        else
        {
            InventorySlot[] target = from.area == InventoryArea.Hotbar ? backpack : hotbar;
            int start = (target == hotbar) ? 1 : 0;                          // skip the hand slot
            leftover = DepositInto(target, start, item, amount);
        }

        src.Set(item, leftover);                    // remainder stays (clears if 0)
        OnChanged?.Invoke();
    }

    /// <summary>Top up matching stacks, then fill empty slots, in an array. Returns leftover.</summary>
    int DepositInto(InventorySlot[] arr, int start, ItemData item, int amount)
    {
        if (arr == null) return amount;
        for (int i = start; i < arr.Length && amount > 0; i++)
            if (arr[i].item == item && !arr[i].IsFull)
                amount = StackInto(arr[i], amount);
        for (int i = start; i < arr.Length && amount > 0; i++)
            if (arr[i].IsEmpty)
                amount = PlaceInto(arr[i], item, amount);
        return amount;
    }
}