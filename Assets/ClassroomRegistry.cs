using UnityEngine;
using System.Collections.Generic;

public class ClassroomRegistry : MonoBehaviour
{
    public static ClassroomRegistry Instance;

    public List<ClassroomZone> classrooms = new List<ClassroomZone>();
    public RecessZone recessZone;

    void Awake()
    {
        Instance = this;
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