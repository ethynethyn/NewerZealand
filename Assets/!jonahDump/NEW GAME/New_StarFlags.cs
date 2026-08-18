using System.Collections.Generic;

/// <summary>
/// How many stars you have. Static, so it survives every scene load.
/// Also remembers WHICH stars you already grabbed, so re-entering a room
/// doesn't let you pick the same one up again.
/// </summary>
public static class New_StarFlags
{
    static int count;

    // ids of stars already picked up in the world
    static readonly HashSet<string> collected = new HashSet<string>();

    /// <summary>Fires whenever the number changes. New_StarUI listens to this.</summary>
    public static event System.Action<int> OnStarCountChanged;

    public static int Count { get { return count; } }

    public static bool HasCollected(string starID)
    {
        if (string.IsNullOrEmpty(starID)) return false;
        return collected.Contains(starID);
    }

    /// <summary>
    /// Used by world pickups. Returns false if that exact star was already taken.
    /// </summary>
    public static bool Collect(string starID, int amount)
    {
        if (!string.IsNullOrEmpty(starID))
        {
            if (collected.Contains(starID)) return false;
            collected.Add(starID);
        }

        Add(amount);
        return true;
    }

    /// <summary>
    /// Plain "give me N stars" with no world object attached.
    /// Call this from dialogue, a boss reward, whatever:
    ///     New_StarFlags.Add(3);
    /// </summary>
    public static void Add(int amount)
    {
        if (amount == 0) return;

        count += amount;
        if (count < 0) count = 0;

        if (OnStarCountChanged != null) OnStarCountChanged(count);
    }

    public static bool CanAfford(int price)
    {
        return count >= price;
    }

    /// <summary>
    /// Take stars away. Returns false and changes nothing if you can't afford it,
    /// so it's safe to call straight from a buy button.
    /// </summary>
    public static bool TrySpend(int price)
    {
        if (price <= 0) return true;
        if (count < price) return false;

        count -= price;
        if (OnStarCountChanged != null) OnStarCountChanged(count);
        return true;
    }

    /// <summary>Call on "New Game". Also handy if you have Domain Reload turned off.</summary>
    public static void ResetAll()
    {
        count = 0;
        collected.Clear();
        if (OnStarCountChanged != null) OnStarCountChanged(count);
    }
}