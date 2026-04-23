using UnityEngine;

public class PlayerScheduleBrain : MonoBehaviour
{
    public PlayerSchedule schedule;

    private SchoolState currentState;
    private int currentPeriodIndex = -1;
    private int currentCheckpoint = -1;
    private ClassroomZone currentClassroom;

    // The five ratio thresholds, matching checkpoint indices 0–4
    private static readonly float[] CheckpointRatios = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    void OnEnable() => SchoolTimeController.OnStateChanged += HandleStateChanged;
    void OnDisable() => SchoolTimeController.OnStateChanged -= HandleStateChanged;

    void Start() => SnapToCurrentState();

    void Update()
    {
        if (currentState != SchoolState.Class) return;
        RefreshCheckpoint();
    }

    // ── State boundary crossed ─────────────────────────────────────────────

    void HandleStateChanged(SchoolState state, int periodIndex)
    {
        currentState = state;
        currentPeriodIndex = periodIndex;

        if (state == SchoolState.Class)
        {
            currentClassroom = ClassroomRegistry.Instance.GetClassroom(GetClassName(periodIndex));
            currentCheckpoint = -1;
            RefreshCheckpoint();
        }
        else
        {
            DisableAll();
            currentClassroom = null;
            currentCheckpoint = -1;
        }
    }

    // ── Late-wake catch-up ─────────────────────────────────────────────────

    void SnapToCurrentState()
    {
        var tc = FindObjectOfType<SchoolTimeController>();
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
            return;
        }
    }

    // ── Per-frame checkpoint check ─────────────────────────────────────────

    void RefreshCheckpoint()
    {
        var tc = FindObjectOfType<SchoolTimeController>();
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

    // ── Object switching ───────────────────────────────────────────────────

    void SetCheckpoint(int index)
    {
        if (index == currentCheckpoint) return;

        // Disable previous checkpoint's objects
        if (currentCheckpoint >= 0)
            SetObjects(currentClassroom.GetCheckpointObjects(currentCheckpoint), false);

        currentCheckpoint = index;

        // Enable new checkpoint's objects
        SetObjects(currentClassroom.GetCheckpointObjects(currentCheckpoint), true);

        Debug.Log($"[PlayerSchedule] Period {currentPeriodIndex} → checkpoint {index} ({CheckpointRatios[index] * 100}%)");
    }

    void DisableAll()
    {
        if (currentClassroom == null) return;
        for (int i = 0; i < CheckpointRatios.Length; i++)
            SetObjects(currentClassroom.GetCheckpointObjects(i), false);
    }

    static void SetObjects(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var go in objs)
            if (go != null) go.SetActive(active);
    }

    // ── Schedule helpers ───────────────────────────────────────────────────

    string GetClassName(int periodIndex)
    {
        if (schedule == null) return "";
        switch (periodIndex)
        {
            case 0: return schedule.period1Class;
            case 1: return schedule.period2Class;
            case 2: return schedule.period3Class;
            case 3: return schedule.period4Class;
            default: return "";
        }
    }
}