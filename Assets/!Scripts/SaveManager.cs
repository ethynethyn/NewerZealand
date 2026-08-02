using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Character playerCharacter;
    public Character worldCharacter;
    [Header("Spawned Object Registry")]
    public SpawnedObjectRegistry spawnedRegistry;

    public GameObject continueButton;

    [Header("UI Color Save")]
    public Image colorImage; // assign your UI Image here

    [Header("Active State Save")]
    public Transform stateParent; // assign parent object here

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

        // ✅ Save player name
        PlayerPrefs.SetString("player_name", PlayerNameManager.PlayerName);

        // ✅ Save UI color
        SaveUIColor();

        // ✅ Save active states
        SaveActiveStates();

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

        if (spawnedRegistry != null)
        {
            Debug.Log("[SaveManager] Calling ReinstantiateAll...");
            List<GameObject> spawned = spawnedRegistry.ReinstantiateAll();
            Debug.Log($"[SaveManager] ReinstantiateAll returned {spawned.Count} objects");
        }
        else
        {
            Debug.LogError("[SaveManager] spawnedRegistry is NULL! Assign it in the Inspector.");
        }

        LoadAllObjects();

        if (PlayerPrefs.HasKey("player_name"))
        {
            PlayerNameManager.PlayerName = PlayerPrefs.GetString("player_name");
            DialogueTextProcessor.PlayerName = PlayerNameManager.PlayerName;
        }

        LoadUIColor();
        LoadActiveStates();

        Debug.Log("Game Loaded!");
    }

    public void NewGame()
    {
        Debug.Log("Clearing Save Data...");

        PlayerPrefs.DeleteKey("player_x");
        PlayerPrefs.DeleteKey("player_y");
        PlayerPrefs.DeleteKey("player_z");
        PlayerPrefs.DeleteKey("player_name");
        PlayerPrefs.DeleteKey("SaveExists");

        ClearUIColor();
        ClearActiveStates();

        ClearAllObjectSaves();
        spawnedRegistry?.ClearAllSpawnedSaves();
        PlayerPrefs.Save();

        if (continueButton != null)
            continueButton.SetActive(false);

        Debug.Log("New Game Ready");
    }

    // ---------------- PLAYER ----------------

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

    // ---------------- UI COLOR ----------------

    void SaveUIColor()
    {
        if (colorImage == null) return;

        Color c = colorImage.color;

        PlayerPrefs.SetFloat("ui_r", c.r);
        PlayerPrefs.SetFloat("ui_g", c.g);
        PlayerPrefs.SetFloat("ui_b", c.b);
        PlayerPrefs.SetFloat("ui_a", c.a);
    }

    void LoadUIColor()
    {
        if (colorImage == null) return;
        if (!PlayerPrefs.HasKey("ui_r")) return;

        Color c = new Color(
            PlayerPrefs.GetFloat("ui_r"),
            PlayerPrefs.GetFloat("ui_g"),
            PlayerPrefs.GetFloat("ui_b"),
            PlayerPrefs.GetFloat("ui_a")
        );

        colorImage.color = c;
    }

    void ClearUIColor()
    {
        PlayerPrefs.DeleteKey("ui_r");
        PlayerPrefs.DeleteKey("ui_g");
        PlayerPrefs.DeleteKey("ui_b");
        PlayerPrefs.DeleteKey("ui_a");
    }

    // ---------------- ACTIVE STATES ----------------

    void SaveActiveStates()
    {
        if (stateParent == null) return;

        for (int i = 0; i < stateParent.childCount; i++)
        {
            GameObject child = stateParent.GetChild(i).gameObject;
            PlayerPrefs.SetInt("obj_active_" + i, child.activeSelf ? 1 : 0);
        }
    }

    void LoadActiveStates()
    {
        if (stateParent == null) return;

        for (int i = 0; i < stateParent.childCount; i++)
        {
            if (!PlayerPrefs.HasKey("obj_active_" + i)) continue;

            GameObject child = stateParent.GetChild(i).gameObject;
            bool isActive = PlayerPrefs.GetInt("obj_active_" + i) == 1;
            child.SetActive(isActive);
        }
    }

    void ClearActiveStates()
    {
        if (stateParent == null) return;

        for (int i = 0; i < stateParent.childCount; i++)
        {
            PlayerPrefs.DeleteKey("obj_active_" + i);
        }
    }

    // ---------------- EXISTING OBJECT SYSTEM ----------------

    void SaveAllObjects()
    {
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>(true);

        foreach (var obj in objects)
            obj.SaveState();
        spawnedRegistry?.SaveAllSpawned();
    }

    void LoadAllObjects()
    {
        SaveableObject.pendingBagAssignments.Clear();

        SaveableObject[] objects = FindObjectsOfType<SaveableObject>(true);
        foreach (var obj in objects)
            obj.LoadState();

        Dictionary<string, BackpackItemStorage> bagLookup =
            new Dictionary<string, BackpackItemStorage>();

        BackpackItemStorage[] allBags = FindObjectsOfType<BackpackItemStorage>(true);
        foreach (var bag in allBags)
        {
            SaveableObject bagSave = bag.GetComponent<SaveableObject>();
            if (bagSave != null)
                bagLookup[bagSave.uniqueID] = bag;
        }

        foreach (var (item, bagID) in SaveableObject.pendingBagAssignments)
        {
            if (!bagLookup.TryGetValue(bagID, out BackpackItemStorage targetBag))
            {
                Debug.LogWarning($"[SaveManager] Could not find bag with ID {bagID} for item {item.name}");
                continue;
            }

            if (!targetBag.storedItems.Contains(item))
                targetBag.storedItems.Add(item);

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            DebugPrintSaveData();
    }

    public void DebugPrintSaveData()
    {
        Debug.Log("=== SAVE DATA DEBUG ===");
        string ids = PlayerPrefs.GetString("SPAWNED_IDS", "");
        Debug.Log($"SPAWNED_IDS: '{ids}'");

        if (!string.IsNullOrEmpty(ids))
        {
            foreach (var id in ids.Split(','))
            {
                if (string.IsNullOrEmpty(id)) continue;
                string prefab = PlayerPrefs.GetString("SPAWNED_PREFAB_" + id, "MISSING");
                string x = PlayerPrefs.GetFloat("OBJ_" + id + "_x", -9999f).ToString();
                string y = PlayerPrefs.GetFloat("OBJ_" + id + "_y", -9999f).ToString();
                string z = PlayerPrefs.GetFloat("OBJ_" + id + "_z", -9999f).ToString();
                string active = PlayerPrefs.GetInt("OBJ_" + id + "_active", -1).ToString();
                Debug.Log($"  ID: {id} | Prefab: {prefab} | Pos: ({x},{y},{z}) | Active: {active}");
            }
        }
        else
        {
            Debug.LogWarning("SPAWNED_IDS is empty — nothing was registered or save was cleared");
        }
        Debug.Log("=== END DEBUG ===");
    }

}