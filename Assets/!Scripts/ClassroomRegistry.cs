using UnityEngine;
using System.Collections.Generic;

public class ClassroomRegistry : MonoBehaviour
{
    public static ClassroomRegistry Instance;

    public List<ClassroomZone> classrooms = new List<ClassroomZone>();
 

    void Awake()
    {
        Instance = this;
    }

    public List<RecessZone> recessZones = new List<RecessZone>();

    public RecessZone GetRecessZone(string name)
    {
        foreach (var zone in recessZones)
        {
            if (zone.zoneName == name)
                return zone;
        }

        return null; // allow fallback
    }

    public RecessZone GetRandomRecessZone()
    {
        if (recessZones.Count == 0) return null;
        return recessZones[Random.Range(0, recessZones.Count)];
    }
    public ClassroomZone GetClassroom(string className)
    {
        foreach (var c in classrooms)
        {
            if (c.className == className)
                return c;
        }

        Debug.LogError("❌ Classroom not found: " + className);
        return null;
    }

    public void ResetAllClassrooms()
    {
        foreach (var classroom in classrooms)
        {
            classroom.ResetSeats();
        }

        Debug.Log("🪑 All classroom seats reset");
    }
}