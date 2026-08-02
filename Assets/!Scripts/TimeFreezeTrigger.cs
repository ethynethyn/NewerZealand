using UnityEngine;

public class TimeFreezeTrigger : MonoBehaviour
{
    [Header("Time Control")]
    public Character worldCharacter;
    public string timeStatName = "Time";

    [Header("Trigger Settings")]
    public string playerTag = "Player";

    private CharacterStat cachedTimeStat;
    private bool hasTriggered = false;

    private void Awake()
    {
        CacheTimeStat();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;

            FreezeTime();

            // Disable immediately after triggering
            gameObject.SetActive(false);
        }
    }

    private void CacheTimeStat()
    {
        if (worldCharacter == null) return;

        foreach (var stat in worldCharacter.stats)
        {
            if (stat.definition != null && stat.definition.statName == timeStatName)
            {
                cachedTimeStat = stat;
                break;
            }
        }
    }

    private void FreezeTime()
    {
        if (cachedTimeStat != null)
        {
            cachedTimeStat.autoChangeEnabled = false;
            Debug.Log("Time frozen via trigger.");
        }
        else
        {
            Debug.LogWarning("Time stat not found!");
        }
    }
}