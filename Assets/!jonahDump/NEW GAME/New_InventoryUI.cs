using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The persistent on-screen inventory. Put this on a Canvas prefab,
/// drop a copy of that prefab into every scene. Duplicates delete themselves,
/// so you can hit play from any scene and still have your items.
/// </summary>
public class New_InventoryUI : MonoBehaviour
{
    public static New_InventoryUI Instance;

    [Header("Refs")]
    public New_ItemDatabase database;
    public New_InventorySlot slotPrefab;

    [Tooltip("The object with the Horizontal/Grid Layout Group. Squares get parented here.")]
    public Transform slotParent;

    readonly Dictionary<New_ItemID, New_InventorySlot> spawned = new Dictionary<New_ItemID, New_InventorySlot>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad only works on root objects
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        New_ItemFlags.OnItemGained += HandleItemGained;

        // catch up on anything picked up before this UI existed
        RebuildAll();
    }

    void OnDestroy()
    {
        New_ItemFlags.OnItemGained -= HandleItemGained;
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------
    //  CALL THIS FROM ANYWHERE:
    //      New_InventoryUI.Give(New_ItemID.Pie);
    //  Works even if the UI doesn't exist yet, the flag still gets set
    //  and the square appears as soon as an inventory shows up.
    // ------------------------------------------------------------
    public static void Give(New_ItemID id)
    {
        New_ItemFlags.Give(id);
    }

    public static bool Has(New_ItemID id)
    {
        return New_ItemFlags.Has(id);
    }

    void HandleItemGained(New_ItemID id)
    {
        AddSlot(id);
    }

    /// <summary>Nukes and respawns every square from the static flags.</summary>
    public void RebuildAll()
    {
        foreach (KeyValuePair<New_ItemID, New_InventorySlot> kv in spawned)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        spawned.Clear();

        IReadOnlyList<New_ItemID> owned = New_ItemFlags.OwnedInOrder;
        for (int i = 0; i < owned.Count; i++) AddSlot(owned[i]);
    }

    void AddSlot(New_ItemID id)
    {
        if (spawned.ContainsKey(id)) return;

        if (slotPrefab == null || slotParent == null)
        {
            Debug.LogWarning("New_InventoryUI: slotPrefab or slotParent not assigned.", this);
            return;
        }

        New_InventorySlot slot = Instantiate(slotPrefab, slotParent);
        slot.transform.localScale = Vector3.one;
        slot.Setup(database, id);
        spawned.Add(id, slot);
    }

    /// <summary>Hide/show the whole bar, e.g. during cutscenes.</summary>
    public void SetVisible(bool visible)
    {
        if (slotParent != null) slotParent.gameObject.SetActive(visible);
    }
}
