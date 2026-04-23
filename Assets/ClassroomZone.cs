using UnityEngine;
using System.Collections.Generic;

public class ClassroomZone : MonoBehaviour
{
    public string className;
    public List<Transform> seats = new List<Transform>();

    [Header("Focus")]
    public Transform focusTarget; // e.g. the blackboard transform

    [Header("Player Checkpoint Objects")]
    [Tooltip("Activated at the very start of class (0%)")]
    public GameObject[] checkpoint0Objects;
    [Tooltip("Activated at 25% through class")]
    public GameObject[] checkpoint25Objects;
    [Tooltip("Activated at 50% through class")]
    public GameObject[] checkpoint50Objects;
    [Tooltip("Activated at 75% through class")]
    public GameObject[] checkpoint75Objects;
    [Tooltip("Activated when class ends (100%)")]
    public GameObject[] checkpoint100Objects;

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

    // Returns the objects for checkpoint index 0–4
    public GameObject[] GetCheckpointObjects(int index)
    {
        switch (index)
        {
            case 0: return checkpoint0Objects;
            case 1: return checkpoint25Objects;
            case 2: return checkpoint50Objects;
            case 3: return checkpoint75Objects;
            case 4: return checkpoint100Objects;
            default: return null;
        }
    }
}