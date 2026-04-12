using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public string playerName = "Player";
    public bool hasSetName = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetName(string name)
    {
        playerName = name;
        hasSetName = true;
    }
}