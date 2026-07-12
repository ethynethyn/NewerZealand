using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A world loot box / chest / locker. Put this on an object that also has a
/// Collider (a trigger BoxCollider is fine — raycasts hit triggers by default),
/// and put that object on your dedicated container layer so the interactor finds
/// it.
///
/// It owns its own slots, so its contents persist until refreshed. Loot is rolled
/// by PERCENTAGE (no weights): first a container-wide "fill" roll, then each item
/// rolls independently, and anything that spawns is scattered into random slots.
///
/// Set "Don't Refresh Loot" to exclude it from global resets — combine with
/// "Generate Loot On Start = off" to make a pure storage chest (e.g. a personal
/// locker) that starts empty and keeps whatever you put in it.
/// </summary>
public class LootContainer : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Name shown when you look at it and as the loot panel title (e.g. 'Locker').")]
    public string displayName = "Locker";

    [Header("Size")]
    [Min(1)] public int slotCount = 9;

    [Header("Loot Roll")]
    [Range(0f, 100f)]
    [Tooltip("Chance this container has ANY loot at all. e.g. 5 = usually empty.")]
    public float fillChance = 100f;

    [Tooltip("Possible items and their independent spawn chances.")]
    public List<LootEntry> lootTable = new List<LootEntry>();

    [Header("Refresh")]
    [Tooltip("Roll loot when the scene starts. Turn OFF for a storage chest that starts empty.")]
    public bool generateLootOnStart = true;

    [Tooltip("Exclude this container from global loot resets (LootResetter). " +
             "Its contents will never be wiped or re-rolled automatically.")]
    public bool dontRefreshLoot = false;

    [Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0f, 100f)] public float spawnChance = 50f;
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 1;
    }

    // ---- Runtime --------------------------------------------------------

    InventorySlot[] slots;
    bool hasGenerated;

    /// <summary>Fired when this container's own contents change (e.g. re-rolled loot).</summary>
    public event Action Changed;

    /// <summary>Every container that is currently enabled — used by LootResetter.</summary>
    static readonly List<LootContainer> all = new List<LootContainer>();
    public static IReadOnlyList<LootContainer> All => all;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public int SlotCount => Mathf.Max(1, slotCount);

    /// <summary>The container's slots (built on demand).</summary>
    public InventorySlot[] Slots { get { EnsureSlots(); return slots; } }

    void Awake() => EnsureSlots();

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        if (generateLootOnStart && !hasGenerated) RefreshLoot();
    }

    void EnsureSlots()
    {
        if (slots != null && slots.Length == SlotCount) return;
        slots = new InventorySlot[SlotCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot();
    }

    /// <summary>Clear and re-roll this container's loot (called by resets).</summary>
    public void RefreshLoot()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++) slots[i].Clear();

        // 1) Does it contain anything at all?
        if (UnityEngine.Random.value * 100f <= fillChance)
        {
            // 2) Roll each entry independently.
            var toPlace = new List<(ItemData item, int count)>();
            foreach (var e in lootTable)
            {
                if (e == null || e.item == null || e.item.isEmptyHand) continue;
                if (UnityEngine.Random.value * 100f > e.spawnChance) continue;
                int lo = Mathf.Max(1, Mathf.Min(e.minCount, e.maxCount));
                int hi = Mathf.Max(lo, Mathf.Max(e.minCount, e.maxCount));
                int count = UnityEngine.Random.Range(lo, hi + 1);
                if (count > 0) toPlace.Add((e.item, count));
            }

            // 3) Scatter into random empty slots.
            var order = new List<int>(slots.Length);
            for (int i = 0; i < slots.Length; i++) order.Add(i);
            Shuffle(order);

            int cursor = 0;
            foreach (var p in toPlace)
            {
                int remaining = p.count;
                while (remaining > 0 && cursor < order.Count)
                {
                    var slot = slots[order[cursor]];
                    if (slot.IsEmpty)
                    {
                        int place = Mathf.Min(remaining, p.item.maxStackSize);
                        slot.Set(p.item, place);
                        remaining -= place;
                    }
                    cursor++;
                }
            }
        }

        hasGenerated = true;
        Changed?.Invoke();
    }

    static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
