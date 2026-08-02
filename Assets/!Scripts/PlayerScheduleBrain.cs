using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerScheduleBrain : MonoBehaviour
{
    public PlayerSchedule schedule;

    // ── End-of-period events ──────────────────────────────────────────
    // Each fires when its period ENDS: enable-objects turn ON, disable-objects
    // turn OFF, then everything snaps back after that event's activeDuration.
    // To get SEPARATE recess vs lunch events, set the Lunch period's state to
    // "Lunch" (not "Recess") on the SchoolTimeController.
    [Header("End-of-Period Events")]
    public TimedObjectEvent endOfPeriod1;   // 1st class (periodIndex 0) ends
    public TimedObjectEvent endOfPeriod2;   // 2nd class (periodIndex 1) ends
    public TimedObjectEvent endOfPeriod3;   // 3rd class (periodIndex 2) ends
    public TimedObjectEvent endOfRecess;    // Recess ends
    public TimedObjectEvent endOfLunch;     // Lunch ends

    private SchoolState currentState;
    private int currentPeriodIndex = -1;
    private int currentCheckpoint = -1;
    private ClassroomZone currentClassroom;

    // ── Door / lock-in state ──────────────────────────────────────────
    private ClassroomZone playerInsideZone;   // class trigger the player currently stands in
    private bool lockedIn;                      // locked into the active class?
    // Only ONE class is ever open at a time: the next/active class, plus
    // whatever room the player is physically standing in (so they can always
    // walk out). When they leave a finished room its door shuts behind them.

    private static readonly float[] CheckpointRatios = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    // Tracks each timed event's running revert so re-firing the same event is clean.
    private class RunningEvent
    {
        public Coroutine routine;
        public GameObject[] enableObjs;
        public bool[] enableStates;
        public GameObject[] disableObjs;
        public bool[] disableStates;
    }
    private readonly Dictionary<TimedObjectEvent, RunningEvent> runningEvents
        = new Dictionary<TimedObjectEvent, RunningEvent>();

    // Cached controller (lazy-found, then reused).
    private SchoolTimeController controller;
    private SchoolTimeController Controller
    {
        get
        {
            if (controller == null) controller = FindObjectOfType<SchoolTimeController>();
            return controller;
        }
    }

    void OnEnable() => SchoolTimeController.OnStateChanged += HandleStateChanged;
    void OnDisable() => SchoolTimeController.OnStateChanged -= HandleStateChanged;

    void Start() => SnapToCurrentState();

    void Update()
    {
        if (currentState != SchoolState.Class) return;
        RefreshCheckpoint();
    }

    // ── State boundary crossed ────────────────────────────────────────

    void HandleStateChanged(SchoolState state, int periodIndex)
    {
        SchoolState previousState = currentState;
        int previousPeriodIndex = currentPeriodIndex;
        ClassroomZone previousClassroom = currentClassroom;

        currentState = state;
        currentPeriodIndex = periodIndex;

        // Fire the end-of-period event for whatever period just ended.
        if (previousState == SchoolState.Class && previousClassroom != null)
            EndOfClass(previousPeriodIndex, previousClassroom);
        else if (previousState == SchoolState.Recess)
            FireTimedEvent(endOfRecess);
        else if (previousState == SchoolState.Lunch)
            FireTimedEvent(endOfLunch);

        if (state == SchoolState.Class)
        {
            currentClassroom = ClassroomRegistry.Instance.GetClassroom(GetClassName(periodIndex));
            currentCheckpoint = -1;
            lockedIn = false;

            // The player is often TELEPORTED out of the previous room when it ends,
            // so that room's trigger-exit never fired and playerInsideZone is stale.
            // If it points at any room other than the new class, the player can't
            // really be in it → clear it. Together with RefreshDoors() below, this
            // shuts EVERY other class door the moment a new class activates.
            if (playerInsideZone != null && playerInsideZone != currentClassroom)
                playerInsideZone = null;

            // Player genuinely standing inside when class starts → locked in ON TIME.
            if (currentClassroom != null && playerInsideZone == currentClassroom)
                LockIn(currentClassroom, true);

            RefreshCheckpoint();
        }
        else
        {
            currentClassroom = null;
            currentCheckpoint = -1;
            lockedIn = false;
        }

        RefreshDoors();
    }

    // ── Trigger callbacks (called by ClassTriggerZone) ────────────────

    public void OnPlayerEnteredClassTrigger(ClassroomZone zone)
    {
        playerInsideZone = zone;

        // Walking into the active class while it is ALREADY running → locked in LATE.
        // (Walking in BEFORE the class is active does nothing → free roam.)
        if (currentState == SchoolState.Class && zone == currentClassroom && !lockedIn)
            LockIn(zone, false);
        else
            RefreshDoors();
    }

    public void OnPlayerExitedClassTrigger(ClassroomZone zone)
    {
        if (playerInsideZone == zone)
            playerInsideZone = null;

        // They left the room → its door shuts behind them, leaving only the
        // next/active class open.
        RefreshDoors();
    }

    // ── Lock-in ───────────────────────────────────────────────────────

    void LockIn(ClassroomZone zone, bool onTime)
    {
        lockedIn = true;
        zone.SetLockInObjects(true);   // turns on classwork trigger etc.

        if (onTime)
            Debug.Log($"[PlayerSchedule] \u2705 Locked into {zone.className} ON TIME");
        else
            Debug.Log($"[PlayerSchedule] \u23F0 Locked into {zone.className} LATE");

        // TODO: feed the on-time / late result into your scoring system here.

        RefreshDoors();   // shuts the door behind the player
    }

    // ── End of class ──────────────────────────────────────────────────

    void EndOfClass(int periodIndex, ClassroomZone zone)
    {
        // Fire this period's end-of-class timed event (enable/disable + auto-revert).
        FireTimedEvent(GetEndOfPeriodEvent(periodIndex));

        // Turn lock-in objects back off.
        zone.SetLockInObjects(false);

        // Disable this class's checkpoint objects.
        for (int i = 0; i < CheckpointRatios.Length; i++)
            SetObjects(zone.GetCheckpointObjects(i), false);

        // Unlock. The room's door reopens (via RefreshDoors) only because the
        // player is still standing in it; it shuts again the moment they leave.
        lockedIn = false;

        Debug.Log($"[PlayerSchedule] \uD83D\uDD14 End of period {periodIndex} ({zone.className})");
    }

    // ── Timed events (enable/disable now, auto-revert later) ──────────

    void FireTimedEvent(TimedObjectEvent evt)
    {
        if (evt == null) return;

        // If this exact event is still mid-cycle, revert it first for a clean slate.
        if (runningEvents.TryGetValue(evt, out var prev) && prev != null)
        {
            if (prev.routine != null) StopCoroutine(prev.routine);
            Restore(prev.enableObjs, prev.enableStates);
            Restore(prev.disableObjs, prev.disableStates);
            runningEvents.Remove(evt);
        }

        // Snapshot the CURRENT state of every affected object so we can restore it.
        var run = new RunningEvent
        {
            enableObjs = evt.enableObjects,
            enableStates = Snapshot(evt.enableObjects),
            disableObjs = evt.disableObjects,
            disableStates = Snapshot(evt.disableObjects)
        };
        runningEvents[evt] = run;

        // Apply the change.
        SetObjects(evt.enableObjects, true);
        SetObjects(evt.disableObjects, false);

        string tag = string.IsNullOrEmpty(evt.label) ? "event" : evt.label;
        Debug.Log($"[PlayerSchedule] \u25B6 Timed '{tag}' fired (revert in {evt.activeDuration}s)");

        // Schedule the revert. activeDuration <= 0 → permanent (no revert).
        if (evt.activeDuration > 0f)
            run.routine = StartCoroutine(RevertAfter(evt, run, evt.activeDuration));
    }

    IEnumerator RevertAfter(TimedObjectEvent evt, RunningEvent run, float delay)
    {
        yield return new WaitForSeconds(delay);

        Restore(run.enableObjs, run.enableStates);
        Restore(run.disableObjs, run.disableStates);

        // Only clear if this run is still the active one for the event.
        if (runningEvents.TryGetValue(evt, out var current) && current == run)
            runningEvents.Remove(evt);

        string tag = string.IsNullOrEmpty(evt.label) ? "event" : evt.label;
        Debug.Log($"[PlayerSchedule] \u25C0 Timed '{tag}' reverted");
    }

    static bool[] Snapshot(GameObject[] objs)
    {
        if (objs == null) return null;
        var states = new bool[objs.Length];
        for (int i = 0; i < objs.Length; i++)
            states[i] = objs[i] != null && objs[i].activeSelf;
        return states;
    }

    static void Restore(GameObject[] objs, bool[] states)
    {
        if (objs == null || states == null) return;
        for (int i = 0; i < objs.Length && i < states.Length; i++)
            if (objs[i] != null) objs[i].SetActive(states[i]);
    }

    // ── Door control (single owner) ───────────────────────────────────

    void RefreshDoors()
    {
        if (ClassroomRegistry.Instance == null) return;

        // The class the player is allowed into right now: the active class while
        // in session, otherwise the NEXT upcoming class.
        ClassroomZone target = (currentState == SchoolState.Class)
            ? currentClassroom
            : ComputeNextClassZone();

        foreach (var zone in ClassroomRegistry.Instance.classrooms)
        {
            if (zone == null) continue;

            // Open only when NOT locked in, and only the next/active class OR the
            // room the player is physically inside (so they can always exit).
            // The instant they leave a finished room, this evaluates false → it shuts.
            bool open = !lockedIn && (zone == target || zone == playerInsideZone);

            zone.SetDoorOpen(open);
        }
    }

    // Finds the ClassroomZone of the next upcoming Class period today.
    ClassroomZone ComputeNextClassZone()
    {
        var tc = Controller;
        if (tc == null || tc.character == null) return null;

        float hour = tc.character.GetStatValue(tc.timeStatName) % 24f;
        bool isADay = tc.IsADay();

        SchoolPeriod best = default;
        bool found = false;

        foreach (var p in tc.periods)
        {
            if (p.state != SchoolState.Class) continue;
            if (p.startHour <= hour) continue;   // only classes still ahead today

            if (!found || p.startHour < best.startHour)
            {
                best = p;
                found = true;
            }
        }

        if (!found) return null;

        string className = GetClassNameForDay(best.periodIndex, isADay);
        return ClassroomRegistry.Instance.GetClassroom(className);
    }

    // ── Late-wake catch-up ────────────────────────────────────────────

    void SnapToCurrentState()
    {
        var tc = Controller;
        if (tc == null) return;

        float hour = tc.character.GetStatValue(tc.timeStatName) % 24f;

        foreach (var p in tc.periods)
        {
            if (!tc.IsInPeriodPublic(hour, p)) continue;

            currentState = p.state;
            currentPeriodIndex = p.periodIndex;

            if (p.state == SchoolState.Class)
            {
                currentClassroom = ClassroomRegistry.Instance.GetClassroom(GetClassName(p.periodIndex));
                currentCheckpoint = -1;
                RefreshCheckpoint();
            }
            break;
        }

        RefreshDoors();
    }

    // ── Per-frame checkpoint check ────────────────────────────────────

    void RefreshCheckpoint()
    {
        var tc = Controller;
        if (tc == null || currentClassroom == null) return;

        float hour = tc.character.GetStatValue(tc.timeStatName) % 24f;

        // Find the active SchoolPeriod
        SchoolPeriod sp = default;
        bool found = false;
        foreach (var p in tc.periods)
        {
            if (p.state == SchoolState.Class && p.periodIndex == currentPeriodIndex)
            { sp = p; found = true; break; }
        }
        if (!found) return;

        float duration = sp.endHour - sp.startHour;
        if (duration <= 0f) return;

        float ratio = Mathf.Clamp01((hour - sp.startHour) / duration);

        // Walk checkpoints from highest to lowest to find which one we've passed
        int target = 0;
        for (int i = CheckpointRatios.Length - 1; i >= 0; i--)
        {
            if (ratio >= CheckpointRatios[i])
            {
                target = i;
                break;
            }
        }

        SetCheckpoint(target);
    }

    // ── Object switching ──────────────────────────────────────────────

    void SetCheckpoint(int index)
    {
        if (index == currentCheckpoint) return;

        // Disable previous checkpoint's objects
        if (currentCheckpoint >= 0)
            SetObjects(currentClassroom.GetCheckpointObjects(currentCheckpoint), false);

        currentCheckpoint = index;

        // Enable new checkpoint's objects
        SetObjects(currentClassroom.GetCheckpointObjects(currentCheckpoint), true);

        Debug.Log($"[PlayerSchedule] Period {currentPeriodIndex} \u2192 checkpoint {index} ({CheckpointRatios[index] * 100}%)");
    }

    static void SetObjects(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var go in objs)
            if (go != null) go.SetActive(active);
    }

    // ── Schedule helpers ──────────────────────────────────────────────

    // Uses today's A/B day automatically.
    string GetClassName(int periodIndex)
    {
        var tc = Controller;
        bool isADay = (tc != null) && tc.IsADay();
        return GetClassNameForDay(periodIndex, isADay);
    }

    string GetClassNameForDay(int periodIndex, bool isADay)
    {
        if (schedule == null) return "";
        return schedule.GetClass(periodIndex, isADay);
    }

    TimedObjectEvent GetEndOfPeriodEvent(int periodIndex)
    {
        switch (periodIndex)
        {
            case 0: return endOfPeriod1;
            case 1: return endOfPeriod2;
            case 2: return endOfPeriod3;
            default: return null;
        }
    }
}

// ──────────────────────────────────────────────────────────────────────
// Used by the End-of-Period Events above. NOT a component — you never attach
// it to a GameObject; it only appears as fields inside PlayerScheduleBrain.
//
// When fired: enableObjects turn ON, disableObjects turn OFF. After
// activeDuration seconds every object snaps back to EXACTLY the state it was in
// when the event fired (captured automatically). activeDuration <= 0 = permanent.
// ──────────────────────────────────────────────────────────────────────
[System.Serializable]
public class TimedObjectEvent
{
    [Tooltip("Optional label so you can tell events apart in the Inspector / logs.")]
    public string label;

    [Tooltip("Turned ON when the event fires, then restored when it reverts.")]
    public GameObject[] enableObjects;

    [Tooltip("Turned OFF when the event fires, then restored when it reverts.")]
    public GameObject[] disableObjects;

    [Tooltip("Seconds the change stays applied before reverting. 0 or less = permanent (never reverts).")]
    public float activeDuration = 3f;
}