using UnityEngine;
using System.Collections.Generic;

public class SaveableObject : MonoBehaviour
{
    [Header("Unique ID (DO NOT DUPLICATE)")]
    public string uniqueID;

    // 🔥 Queue for bag assignments AFTER load
    public static List<(GameObject obj, string bagID)> pendingBagAssignments
        = new List<(GameObject, string)>();

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            Debug.LogWarning($"Generated new Save ID for {gameObject.name}: {uniqueID}");
        }
    }

    // =========================
    // SAVE
    // =========================
    public void SaveState()
    {
        string key = "OBJ_" + uniqueID;

        // Position
        PlayerPrefs.SetFloat(key + "_x", transform.position.x);
        PlayerPrefs.SetFloat(key + "_y", transform.position.y);
        PlayerPrefs.SetFloat(key + "_z", transform.position.z);

        // Active
        PlayerPrefs.SetInt(key + "_active", gameObject.activeSelf ? 1 : 0);

        // 🔥 Check if inside any backpack
        bool found = false;

        foreach (var bag in FindObjectsOfType<BackpackItemStorage>())
        {
            if (bag.storedItems.Contains(gameObject))
            {
                SaveableObject bagSave = bag.GetComponent<SaveableObject>();
                if (bagSave != null)
                {
                    PlayerPrefs.SetString(key + "_bag", bagSave.uniqueID);
                    found = true;
                }
                break;
            }
        }

        if (!found)
        {
            PlayerPrefs.DeleteKey(key + "_bag");
        }
    }

    // =========================
    // LOAD (PHASE 1 ONLY)
    // =========================
    public void LoadState()
    {
        string key = "OBJ_" + uniqueID;

        if (!PlayerPrefs.HasKey(key + "_x"))
            return;

        // 🔥 If this item is bagged, skip position restore entirely.
        // Phase 2 in SaveManager will handle re-adding it to the bag.
        if (PlayerPrefs.HasKey(key + "_bag"))
        {
            string bagID = PlayerPrefs.GetString(key + "_bag");
            pendingBagAssignments.Add((gameObject, bagID));
            return; // Don't touch position or SetActive — Phase 2 handles it
        }

        // Restore position only for world objects
        transform.position = new Vector3(
            PlayerPrefs.GetFloat(key + "_x"),
            PlayerPrefs.GetFloat(key + "_y"),
            PlayerPrefs.GetFloat(key + "_z")
        );

        // Restore active state
        gameObject.SetActive(PlayerPrefs.GetInt(key + "_active") == 1);
    }

    // =========================
    public void ClearSave()
    {
        string key = "OBJ_" + uniqueID;

        PlayerPrefs.DeleteKey(key + "_x");
        PlayerPrefs.DeleteKey(key + "_y");
        PlayerPrefs.DeleteKey(key + "_z");
        PlayerPrefs.DeleteKey(key + "_active");
        PlayerPrefs.DeleteKey(key + "_bag");
    }
}