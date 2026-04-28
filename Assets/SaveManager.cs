using UnityEngine;

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

    // =========================
    // SAVE GAME
    // =========================
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

    // =========================
    // LOAD GAME
    // =========================
    public void LoadGame()
    {
        Debug.Log("Loading Game...");

        LoadPlayer();
        playerCharacter.LoadStats();
        worldCharacter.LoadStats();

        LoadAllObjects();

        Debug.Log("Game Loaded!");
    }

    // =========================
    // NEW GAME
    // =========================
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

    // =========================
    // PLAYER
    // =========================
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

    // =========================
    // OBJECT SYSTEM (NEW)
    // =========================
    void SaveAllObjects()
    {
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>();

        foreach (var obj in objects)
        {
            obj.SaveState();
        }
    }

    void LoadAllObjects()
    {
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>();

        foreach (var obj in objects)
        {
            obj.LoadState();
        }
    }

    void ClearAllObjectSaves()
    {
        SaveableObject[] objects = FindObjectsOfType<SaveableObject>();

        foreach (var obj in objects)
        {
            obj.ClearSave();
        }
    }
}