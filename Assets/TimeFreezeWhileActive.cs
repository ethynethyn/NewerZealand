using UnityEngine;

public class TimeFreezeWhileActive : MonoBehaviour
{
    [Header("Time Control")]
    public Character worldCharacter;
    public string timeStatName = "Time";

    private CharacterStat cachedTimeStat;

    private void Awake()
    {
        CacheTimeStat();
    }

    private void OnEnable()
    {
        SetTimeFrozen(true);
    }

    private void OnDisable()
    {
        SetTimeFrozen(false);
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

    private void SetTimeFrozen(bool frozen)
    {
        if (cachedTimeStat != null)
        {
            // If frozen = true → stop time changing
            // If frozen = false → allow time to change again
            cachedTimeStat.autoChangeEnabled = !frozen;
        }
    }
}