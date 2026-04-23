using UnityEngine;
using System.Collections.Generic;

public class ClassroomZone : MonoBehaviour
{
    public string className;
    public List<Transform> seats = new List<Transform>();

    [Header("Player phase objects")]
    public GameObject[] playerStartObjects;
    public GameObject[] playerMiddleObjects;
    public GameObject[] playerEndObjects;

    private Dictionary<Transform, bool> occupied = new Dictionary<Transform, bool>();

    void Awake()
    {
        foreach (var seat in seats)
            occupied[seat] = false;
    }

    public Transform GetFreeSeat()
    {
        foreach (var seat in seats)
        {
            if (!occupied[seat])
            {
                occupied[seat] = true;
                return seat;
            }
        }
        return null;
    }

    public void ResetSeats()
    {
        var keys = new List<Transform>(occupied.Keys);
        foreach (var k in keys)
            occupied[k] = false;
    }

}