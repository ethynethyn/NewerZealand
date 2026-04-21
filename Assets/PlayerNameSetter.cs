using UnityEngine;

public class PlayerNameSetter : MonoBehaviour
{
    public string playerName = "Player";

    private void Awake()
    {
        DialogueTextProcessor.PlayerName = playerName;
    }
}