using UnityEngine;
using System.Collections.Generic;

public class RecessZone : MonoBehaviour
{
    [Header("Identity")]
    public string zoneName;

    [Header("Access Control")]
    public bool restricted = false;
    public bool teachersOnly = false;

    [Header("Spots")]
    public List<Transform> hangoutPoints = new List<Transform>();

    private Dictionary<Transform, bool> occupied = new Dictionary<Transform, bool>();

    void Awake()
    {
        foreach (var point in hangoutPoints)
            occupied[point] = false;
    }

    public Transform GetFreeSpot()
    {
        foreach (var point in hangoutPoints)
        {
            if (!occupied[point])
            {
                occupied[point] = true;
                return point;
            }
        }

        // fallback sharing
        if (hangoutPoints.Count > 0)
            return hangoutPoints[Random.Range(0, hangoutPoints.Count)];

        return null;
    }

    public void ResetSpots()
    {
        var keys = new List<Transform>(occupied.Keys);
        foreach (var k in keys)
            occupied[k] = false;
    }
}