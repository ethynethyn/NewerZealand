using UnityEngine;

[System.Serializable]
public struct SchoolPeriod
{
    public string name;

    public float startHour;
    public float endHour;

    public SchoolState state;
    public int periodIndex; // only used if Class
}