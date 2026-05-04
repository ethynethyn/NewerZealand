using UnityEngine;
using System.Collections.Generic;

public class SaveableObject : MonoBehaviour
{
    [Header("Unique ID (DO NOT DUPLICATE)")]
    public string uniqueID;

    public static List<(GameObject obj, string bagID)> pendingBagAssignments
        = new List<(GameObject, string)>();

    void Awake()
    {
        // Only generate a new ID if one hasn't been assigned yet
        // ReinstantiateAll sets the ID after Instantiate so we must not overwrite it
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            Debug.LogWarning($"Generated new Save ID for {gameObject.name}: {uniqueID}");
        }
    }

    public void SaveState()
    {
        string key = "OBJ_" + uniqueID;

        PlayerPrefs.SetFloat(key + "_x", transform.position.x);
        PlayerPrefs.SetFloat(key + "_y", transform.position.y);
        PlayerPrefs.SetFloat(key + "_z", transform.position.z);
        PlayerPrefs.SetInt(key + "_active", gameObject.activeSelf ? 1 : 0);

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
            PlayerPrefs.DeleteKey(key + "_bag");
    }

    public void LoadState()
    {
        string key = "OBJ_" + uniqueID;

        if (!PlayerPrefs.HasKey(key + "_x"))
            return;

        if (PlayerPrefs.HasKey(key + "_bag"))
        {
            string bagID = PlayerPrefs.GetString(key + "_bag");
            pendingBagAssignments.Add((gameObject, bagID));
            return;
        }

        transform.position = new Vector3(
            PlayerPrefs.GetFloat(key + "_x"),
            PlayerPrefs.GetFloat(key + "_y"),
            PlayerPrefs.GetFloat(key + "_z")
        );

        gameObject.SetActive(PlayerPrefs.GetInt(key + "_active") == 1);
    }

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