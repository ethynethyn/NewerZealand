using UnityEngine;
using System.Collections.Generic;

public class ClassroomZone : MonoBehaviour
{
    public string className;
    public List<Transform> seats = new List<Transform>();

    [Header("Focus")]
    public Transform focusTarget;

    [Header("Player Checkpoint Objects")]
    public GameObject[] checkpoint0Objects;
    public GameObject[] checkpoint25Objects;
    public GameObject[] checkpoint50Objects;
    public GameObject[] checkpoint75Objects;
    public GameObject[] checkpoint100Objects;

    private Dictionary<Transform, bool> occupied = new Dictionary<Transform, bool>();

    // ✅ NEW: prevents reapplying checkpoints
    private HashSet<int> appliedCheckpoints = new HashSet<int>();

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

    // ✅ Call this instead of GetCheckpointObjects
    public void ApplyCheckpoint(int index)
    {
        if (appliedCheckpoints.Contains(index))
            return; // already applied once → do nothing

        appliedCheckpoints.Add(index);

        GameObject[] objs = GetCheckpointObjects(index);
        if (objs == null) return;

        foreach (var obj in objs)
        {
            if (obj != null)
                obj.SetActive(true); // ONLY ever turns ON
        }
    }

    // Keeps your mapping
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

    // Optional: full reset if needed (restart day/class)
    public void ResetCheckpoints()
    {
        appliedCheckpoints.Clear();
    }
}