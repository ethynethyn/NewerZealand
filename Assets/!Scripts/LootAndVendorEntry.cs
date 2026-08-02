using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootAndVendorEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnChance = 1f;
    public float cost = 0f;


public List<LootAndVendorEntry> lootTable = new List<LootAndVendorEntry>();

}