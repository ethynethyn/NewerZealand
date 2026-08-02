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

    // ── NEW: CLASS MINIGAME TRIGGER ───────────────────────────────────
    // The classwork minigame trigger (the ClassMinigameTrigger component) for THIS class.
    // It ENABLES when the player locks into this class, but is deliberately NOT disabled
    // when the class ends — unlike the lock-in objects above. The ClassMinigameTrigger
    // turns itself off when the bell rings (after returning player input etc.) in a way
    // that doesn't disrupt that, so we leave all of its disabling to it.
    //
    // Start it DISABLED by default: either un-tick the ClassMinigameTrigger component's
    // enabled checkbox, or leave its GameObject inactive. SetLockInObjects(true) turns
    // both on, so whichever you pick works.
    [Header("Class Minigame Trigger")]
    [Tooltip("The ClassMinigameTrigger for this class. Enabled on lock-in, never disabled here " +
             "(it disables itself on the bell). Start it DISABLED in the inspector.")]
    public ClassMinigameTrigger classMinigameTrigger;

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
        if (lockInObjects != null)
        {
            foreach (var go in lockInObjects)
                if (go != null) go.SetActive(active);
        }

        // The class minigame trigger turns ON with the lock-in, but is intentionally NOT
        // turned off here. The ClassMinigameTrigger disables itself when the bell rings
        // (after returning input, etc.), so forcing it off here could disrupt that.
        if (active && classMinigameTrigger != null)
        {
            classMinigameTrigger.gameObject.SetActive(true);
            classMinigameTrigger.enabled = true;
        }
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