using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnedObjectData
{
    public string prefabName;
    public string uniqueID;
    public float x, y, z;
    public int active;
    public string bagID;
}

public class SpawnedObjectRegistry : MonoBehaviour
{
    public static SpawnedObjectRegistry Instance;

    [Header("Prefab Library — add every spawnable prefab here")]
    public List<GameObject> prefabLibrary = new List<GameObject>();

    private Dictionary<string, GameObject> _prefabLookup;

    void Awake()
    {
        Instance = this;
        _prefabLookup = new Dictionary<string, GameObject>();
        foreach (var p in prefabLibrary)
            if (p != null) _prefabLookup[p.name] = p;
    }

    // ── Registration ──────────────────────────────────────────────

    public void Register(GameObject instance, string prefabName)
    {
        SaveableObject so = instance.GetComponent<SaveableObject>();
        if (so == null)
        {
            Debug.LogWarning($"[Registry] {instance.name} has no SaveableObject component!");
            return;
        }

        Debug.Log($"[Registry] Registering {instance.name} | ID: {so.uniqueID} | Prefab: {prefabName}");

        PlayerPrefs.SetString("SPAWNED_PREFAB_" + so.uniqueID, prefabName);

        string key = "SPAWNED_IDS";
        string existing = PlayerPrefs.GetString(key, "");
        List<string> ids = ParseIDs(existing);

        if (!ids.Contains(so.uniqueID))
        {
            ids.Add(so.uniqueID);
            PlayerPrefs.SetString(key, string.Join(",", ids));
        }

        // Save immediately so registration isn't lost if game closes before SaveGame()
        PlayerPrefs.Save();

        Debug.Log($"[Registry] SPAWNED_IDS is now: {PlayerPrefs.GetString(key)}");
    }

    // ── Save ──────────────────────────────────────────────────────

    public void SaveAllSpawned()
    {
        // Per-object data is handled by SaveableObject.SaveState()
        // Registration is written in Register() — nothing extra needed here
    }

    // ── Load ──────────────────────────────────────────────────────

    public List<GameObject> ReinstantiateAll()
    {
        List<GameObject> result = new List<GameObject>();

        string existing = PlayerPrefs.GetString("SPAWNED_IDS", "");
        List<string> ids = ParseIDs(existing);

        foreach (string id in ids)
        {
            string prefabName = PlayerPrefs.GetString("SPAWNED_PREFAB_" + id, "");
            if (string.IsNullOrEmpty(prefabName)) continue;

            if (!_prefabLookup.TryGetValue(prefabName, out GameObject prefab))
            {
                Debug.LogWarning($"[SpawnedObjectRegistry] Prefab '{prefabName}' not in library.");
                continue;
            }

            // Instantiate inactive so Awake doesn't fire and overwrite the ID we're about to set
            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            instance.SetActive(false);

            // Assign the correct saved ID before anything else reads it
            SaveableObject so = instance.GetComponent<SaveableObject>();
            if (so != null)
            {
                so.uniqueID = id;
                Debug.Log($"[Registry] Reinstantiated {prefabName} with ID {id}");
            }

            // Now safe to activate — Awake already ran, ID is locked in
            instance.SetActive(true);
            result.Add(instance);
        }

        return result;
    }

    // ── Clear ─────────────────────────────────────────────────────

    public void ClearAllSpawnedSaves()
    {
        string existing = PlayerPrefs.GetString("SPAWNED_IDS", "");
        List<string> ids = ParseIDs(existing);

        foreach (string id in ids)
            PlayerPrefs.DeleteKey("SPAWNED_PREFAB_" + id);

        PlayerPrefs.DeleteKey("SPAWNED_IDS");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private List<string> ParseIDs(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(raw)) return list;
        foreach (var s in raw.Split(','))
            if (!string.IsNullOrEmpty(s)) list.Add(s);
        return list;
    }
}