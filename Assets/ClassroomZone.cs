using UnityEngine;
using System.Collections.Generic;

public class ClassroomZone : MonoBehaviour
{
    public string className;
    public List<Transform> seats = new List<Transform>();

    [Header("Focus")]
    public Transform focusTarget;

    // ── NEW: DOOR SWAP ────────────────────────────────────────────────
    // Assign BOTH per class. closedDoor blocks the player; openDoor lets them in.
    // Leave closedDoor active in the scene and openDoor inactive by default —
    // the brain flips them at runtime.
    [Header("Doors (assign both)")]
    [Tooltip("Shown when the class is CLOSED (blocks the player). Active by default.")]
    public GameObject closedDoor;
    [Tooltip("Shown when the class is OPEN (this is the player's next class). Inactive by default.")]
    public GameObject openDoor;

    // ── NEW: LOCK-IN OBJECTS ──────────────────────────────────────────
    // Activated when the player is locked into THIS class (e.g. the "classwork"
    // trigger box at their desk). Leave these inactive by default.
    [Header("Lock-In Objects")]
    [Tooltip("Activated while the player is locked into this class. Inactive by default.")]
    public GameObject[] lockInObjects;

    // Wired up automatically by the ClassTriggerZone child at runtime.
    [HideInInspector] public ClassTriggerZone trigger;

    [Header("Player Checkpoint Objects")]
    public GameObject[] checkpoint0Objects;
    public GameObject[] checkpoint25Objects;
    public GameObject[] checkpoint50Objects;
    public GameObject[] checkpoint75Objects;
    public GameObject[] checkpoint100Objects;

    private Dictionary<Transform, bool> occupied = new Dictionary<Transform, bool>();

    // prevents reapplying checkpoints
    private HashSet<int> appliedCheckpoints = new HashSet<int>();

    void Awake()
    {
        foreach (var seat in seats)
            occupied[seat] = false;
    }

    // ── NEW: DOOR CONTROL ─────────────────────────────────────────────
    // open == true  → openDoor ON,  closedDoor OFF (player can enter)
    // open == false → closedDoor ON, openDoor   OFF (player blocked)
    public void SetDoorOpen(bool open)
    {
        if (openDoor != null) openDoor.SetActive(open);
        if (closedDoor != null) closedDoor.SetActive(!open);
    }

    // ── NEW: LOCK-IN CONTROL ──────────────────────────────────────────
    public void SetLockInObjects(bool active)
    {
        if (lockInObjects == null) return;
        foreach (var go in lockInObjects)
            if (go != null) go.SetActive(active);
    }

    // ── SEATS ─────────────────────────────────────────────────────────
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

    // ── CHECKPOINTS (unchanged) ───────────────────────────────────────
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

    public void ResetCheckpoints()
    {
        appliedCheckpoints.Clear();
    }
}