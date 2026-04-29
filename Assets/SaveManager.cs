using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Character playerCharacter;
    public Character worldCharacter;

    public GameObject continueButton;

    void Start()
    {
        if (continueButton != null)
            continueButton.SetActive(PlayerPrefs.HasKey("SaveExists"));
    }

    public void SaveGame()
    {
        Debug.Log("Saving Game...");

        SavePlayer();
        playerCharacter.SaveStats();
        worldCharacter.SaveStats();
        SaveAllObjects();

        PlayerPrefs.SetInt("SaveExists", 1);
        PlayerPrefs.Save();

        if (continueButton != null)
            continueButton.SetActive(true);

        Debug.Log("Game Saved!");
    }

    public void LoadGame()
    {
        Debug.Log("Loading Game...");

        LoadPlayer();
        playerCharacter.LoadStats();
        worldCharacter.LoadStats();
        LoadAllObjects();

        Debug.Log("Game Loaded!");
    }

    public void NewGame()
    {
        Debug.Log("Clearing Save Data...");

        PlayerPrefs.DeleteKey("player_x");
        PlayerPrefs.DeleteKey("player_y");
        PlayerPrefs.DeleteKey("player_z");
        PlayerPrefs.DeleteKey("SaveExists");

        ClearAllObjectSaves();
        PlayerPrefs.Save();

        if (continueButton != null)
            continueButton.SetActive(false);

        Debug.Log("New Game Ready");
    }

    void SavePlayer()
    {
        PlayerPrefs.SetFloat("player_x", player.position.x);
        PlayerPrefs.SetFloat("player_y", player.position.y);
        PlayerPrefs.SetFloat("player_z", player.position.z);
    }

    void LoadPlayer()
    {
        if (!PlayerPrefs.HasKey("player_x")) return;

        player.position = new Vector3(
            PlayerPrefs.GetFloat("player_x"),
            PlayerPrefs.GetFloat("player_y"),
            PlayerPrefs.GetFloat("player_z")
        );
    }

    void SaveAllObjects()
    {
        // FindObjectsOfType with true includes inactive objects
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>(true);

        foreach (var obj in objects)
            obj.SaveState();
    }

    void LoadAllObjects()
    {
        // PHASE 1: Clear pending list, then load all objects (including inactive)
        SaveableObject.pendingBagAssignments.Clear();

        SaveableObject[] objects = FindObjectsOfType<SaveableObject>(true);
        foreach (var obj in objects)
            obj.LoadState();

        // PHASE 2: Build a lookup of bagID -> BackpackItemStorage
        // so we can assign items to the right bag
        Dictionary<string, BackpackItemStorage> bagLookup =
            new Dictionary<string, BackpackItemStorage>();

        BackpackItemStorage[] allBags = FindObjectsOfType<BackpackItemStorage>(true);
        foreach (var bag in allBags)
        {
            SaveableObject bagSave = bag.GetComponent<SaveableObject>();
            if (bagSave != null)
                bagLookup[bagSave.uniqueID] = bag;
        }

        // PHASE 3: Process all pending bag assignments
        foreach (var (item, bagID) in SaveableObject.pendingBagAssignments)
        {
            if (!bagLookup.TryGetValue(bagID, out BackpackItemStorage targetBag))
            {
                Debug.LogWarning($"[SaveManager] Could not find bag with ID {bagID} for item {item.name}");
                continue;
            }

            // Re-add to the bag's stored list (it was cleared on scene load)
            if (!targetBag.storedItems.Contains(item))
                targetBag.storedItems.Add(item);

            // Ensure the item is inactive and has no physics active
            // (mirrors what ShrinkIntoBag does at the end)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            foreach (Collider col in item.GetComponentsInChildren<Collider>())
                col.enabled = false;

            item.SetActive(false);
        }

        SaveableObject.pendingBagAssignments.Clear();
    }

    void ClearAllObjectSaves()
    {
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>(true);
        foreach (var obj in objects)
            obj.ClearSave();
    }
}