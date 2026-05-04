using UnityEngine;

public class PlayerNameSetter : MonoBehaviour
{
    public string defaultName = "Player";

    private void Awake()
    {
        if (PlayerPrefs.HasKey("player_name"))
        {
            PlayerNameManager.PlayerName = PlayerPrefs.GetString("player_name");
        }
        else
        {
            PlayerNameManager.PlayerName = defaultName;
        }

        // Always sync dialogue system
        DialogueTextProcessor.PlayerName = PlayerNameManager.PlayerName;
    }
}