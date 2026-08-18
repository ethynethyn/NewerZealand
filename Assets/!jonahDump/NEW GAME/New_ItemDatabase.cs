using System.Collections.Generic;
using UnityEngine;

public enum New_ItemRarity
{
    Common,
    Rare,
    Legendary
}

/// <summary>
/// Maps every New_ItemID to its art. Create one asset:
/// Right click in Project -> Create -> New Inventory -> Item Database
/// </summary>
[CreateAssetMenu(fileName = "New_ItemDatabase", menuName = "New Inventory/Item Database")]
public class New_ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public New_ItemID id;

        [Tooltip("What the store row calls it. Leave blank to just use the enum name.")]
        public string displayName;

        [Tooltip("The MS Paint drawing of the item itself, sits inside the square.")]
        public Sprite icon;

        [Tooltip("Optional. Leave empty to use defaultSquare. Use this if a specific item needs its own frame.")]
        public Sprite squareOverride;

        [Header("Show and tell")]
        public New_ItemRarity rarity = New_ItemRarity.Common;

        [Tooltip("Leave at -1 to use the rarity's star payout. Set a number to override just this item.")]
        public int starValueOverride = -1;
    }

    [Tooltip("The MS Paint square that every item sits in.")]
    public Sprite defaultSquare;

    public List<Entry> entries = new List<Entry>();

    Dictionary<New_ItemID, Entry> lookup;

    public Entry Get(New_ItemID id)
    {
        if (lookup == null || lookup.Count != entries.Count)
        {
            lookup = new Dictionary<New_ItemID, Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null) continue;
                lookup[entries[i].id] = entries[i];
            }
        }

        Entry e;
        return lookup.TryGetValue(id, out e) ? e : null;
    }

    public string GetName(New_ItemID id)
    {
        Entry e = Get(id);
        if (e != null && !string.IsNullOrEmpty(e.displayName)) return e.displayName;
        return id.ToString();
    }

    public New_ItemRarity GetRarity(New_ItemID id)
    {
        Entry e = Get(id);
        return (e != null) ? e.rarity : New_ItemRarity.Common;
    }

    /// <summary>Returns -1 when the item has no override and should use its rarity.</summary>
    public int GetStarOverride(New_ItemID id)
    {
        Entry e = Get(id);
        return (e != null) ? e.starValueOverride : -1;
    }
}