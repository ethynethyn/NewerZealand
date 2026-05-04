using System.Collections.Generic;
using UnityEngine;

public class LootAndVendor : MonoBehaviour
{
    [Header("Loot Table")]
    public List<LootAndVendorEntry> lootTable = new List<LootAndVendorEntry>();

    [Header("Vendor Settings")]
    public bool isVendor = false;
    public string currencyStatName = "Money";
    public Character playerCharacter;

    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("Infinite Supply")]
    public bool infiniteSupply = true;

    // -----------------------------
    // TAKE ITEM
    // -----------------------------
    public GameObject TakeItem(int index)
    {
        if (index < 0 || index >= lootTable.Count)
            return null;

        LootAndVendorEntry entry = lootTable[index];

        if (entry.prefab == null)
            return null;

        // -----------------------------
        // MONEY CHECK (vendor only)
        // -----------------------------
        if (isVendor && playerCharacter != null && entry.cost > 0f)
        {
            float money = playerCharacter.GetStatValue(currencyStatName);
            if (money < entry.cost)
            {
                Debug.Log("Not enough money");
                return null;
            }
            playerCharacter.ModifyStat(currencyStatName, -entry.cost);
        }

        // -----------------------------
        // SPAWN ITEM
        // -----------------------------
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject item = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
        item.SetActive(true);

        // ✅ Register with the save system — must be AFTER SetActive so uniqueID is ready
        if (SpawnedObjectRegistry.Instance != null)
            SpawnedObjectRegistry.Instance.Register(item, entry.prefab.name);

        return item; // ✅ Single return, after registration
    }

    // -----------------------------
    // UI HELPERS
    // -----------------------------
    public float GetCost(int index)
    {
        if (index < 0 || index >= lootTable.Count)
            return 0f;
        return lootTable[index].cost;
    }

    public int Count()
    {
        return lootTable.Count;
    }
}