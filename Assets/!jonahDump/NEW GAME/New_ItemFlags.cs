using System.Collections.Generic;

// ============================================================
//  ADD YOUR ITEMS HERE. This enum is the master list.
//  Add to the bottom rather than reordering, it keeps things sane.
// ============================================================
public enum New_ItemID
{
    Sword,
    RustyKey,
    Pie,
    Phone,
    BusTicket
}

/// <summary>
/// Static flags for "do I have this item or not".
/// Statics live outside the scene, so this survives every scene load for free.
/// Items are never removed, so there is deliberately no Take() method.
/// </summary>
public static class New_ItemFlags
{
    static readonly bool[] owned = new bool[System.Enum.GetValues(typeof(New_ItemID)).Length];

    // Pickup order, so the UI can rebuild the row in the order you actually got stuff.
    static readonly List<New_ItemID> order = new List<New_ItemID>();

    /// <summary>Fires the moment a NEW item is gained. New_InventoryUI listens to this.</summary>
    public static event System.Action<New_ItemID> OnItemGained;

    public static bool Has(New_ItemID id)
    {
        return owned[(int)id];
    }

    /// <summary>The one real entry point. Safe to call twice, second call does nothing.</summary>
    public static void Give(New_ItemID id)
    {
        if (owned[(int)id]) return;

        owned[(int)id] = true;
        order.Add(id);

        if (OnItemGained != null) OnItemGained(id);
    }

    public static IReadOnlyList<New_ItemID> OwnedInOrder { get { return order; } }

    public static int TotalItemCount { get { return owned.Length; } }

    // ------------------------------------------------------------
    //  Named static bools. Same data as the array above, just nicer
    //  to read in Ink conditions / dialogue checks / StaticManager-style code:
    //      if (New_ItemFlags.has_RustyKey) { ... }
    //  Writing "true" to one of these routes through Give(), so the UI
    //  square still spawns. Writing false does nothing on purpose.
    // ------------------------------------------------------------
    public static bool has_Sword     { get { return Has(New_ItemID.Sword); }     set { if (value) Give(New_ItemID.Sword); } }
    public static bool has_RustyKey  { get { return Has(New_ItemID.RustyKey); }  set { if (value) Give(New_ItemID.RustyKey); } }
    public static bool has_Pie       { get { return Has(New_ItemID.Pie); }       set { if (value) Give(New_ItemID.Pie); } }
    public static bool has_Phone     { get { return Has(New_ItemID.Phone); }     set { if (value) Give(New_ItemID.Phone); } }
    public static bool has_BusTicket { get { return Has(New_ItemID.BusTicket); } set { if (value) Give(New_ItemID.BusTicket); } }

    /// <summary>
    /// Wipe everything. Call this on "New Game".
    /// Also useful if you have Domain Reload turned off in Enter Play Mode Options,
    /// because statics will otherwise carry over between play sessions in the editor.
    /// </summary>
    public static void ResetAll()
    {
        for (int i = 0; i < owned.Length; i++) owned[i] = false;
        order.Clear();
    }
}
