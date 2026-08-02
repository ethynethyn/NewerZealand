using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Syncs player name into DialogueTextProcessor
/// and toggles objects on/off after name submission.
/// </summary>
public class PlayerNameBridge : MonoBehaviour
{
    [Header("On Name Submitted")]

    [Tooltip("These objects will be enabled once the player submits their name")]
    public List<GameObject> enableAfterName = new List<GameObject>();

    [Tooltip("These objects will be disabled once the player submits their name")]
    public List<GameObject> disableAfterName = new List<GameObject>();

    private bool _synced = false;

    void Update()
    {
        if (_synced) return;

        if (PlayerNameManager.PlayerName != "Player" &&
            !string.IsNullOrWhiteSpace(PlayerNameManager.PlayerName))
        {
            Sync();
        }
    }

    private void Sync()
    {
        _synced = true;

        DialogueTextProcessor.PlayerName = PlayerNameManager.PlayerName;
        Debug.Log($"[PlayerNameBridge] Synced player name: {PlayerNameManager.PlayerName}");

        // Enable objects
        foreach (GameObject obj in enableAfterName)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Disable objects
        foreach (GameObject obj in disableAfterName)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    // Call this manually if you ever change PlayerNameManager.PlayerName at runtime
    public void ForceSync()
    {
        _synced = false;
    }
}