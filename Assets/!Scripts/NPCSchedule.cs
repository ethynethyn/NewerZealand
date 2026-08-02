using UnityEngine;

[CreateAssetMenu(menuName = "School/NPC Schedule")]
public class NPCSchedule : ScriptableObject
{
    // 3 classes per day, split across two alternating days.
    // A day = odd calendar days (1, 3, 5...) → the FIRST 3 classes.
    // B day = even calendar days (2, 4, 6...) → the LAST 3 classes.

    [Header("A Day (odd days: 1, 3, 5...)")]
    public string aPeriod1Class;
    public string aPeriod2Class;
    public string aPeriod3Class;

    [Header("B Day (even days: 2, 4, 6...)")]
    public string bPeriod1Class;
    public string bPeriod2Class;
    public string bPeriod3Class;

    // periodIndex is 0, 1 or 2 (the three Class periods in a day).
    public string GetClass(int periodIndex, bool isADay)
    {
        if (isADay)
        {
            switch (periodIndex)
            {
                case 0: return aPeriod1Class;
                case 1: return aPeriod2Class;
                case 2: return aPeriod3Class;
            }
        }
        else
        {
            switch (periodIndex)
            {
                case 0: return bPeriod1Class;
                case 1: return bPeriod2Class;
                case 2: return bPeriod3Class;
            }
        }
        return "";
    }
}