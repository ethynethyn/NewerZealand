using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

// Ends the current scene. Keep the GameObject holding this DISABLED, then enable
// it (SetActive true) — from classwork completion, a UnityEvent, dialogue, etc. —
// to start a countdown that loads the next period's scene.
//
// You can optionally bump progression indices during the countdown (e.g. advance
// Class Halls so the NEXT class loads the new scene, per your day structure).
public class TriggerNextPeriod : MonoBehaviour
{
    public enum ChangeMode { Advance, SetTo }
    public enum ClassPeriodAction { LeaveUnchanged, Advance, SetTo }

    [System.Serializable]
    public class ProgressionChange
    {
        [Tooltip("Which progression to change.")]
        public PeriodType type;
        [Tooltip("Advance = +1 (next scene). SetTo = jump to a specific 0-based index.")]
        public ChangeMode mode = ChangeMode.Advance;
        [Tooltip("Only used when mode = SetTo.")]
        public int value;
    }

    [Header("What to load next")]
    [Tooltip("Which period type's CURRENT scene to load when the countdown ends.")]
    public PeriodType nextPeriod = PeriodType.Recess;

    [Header("Class period")]
    [Tooltip("What this trigger does to the class period.\n" +
             "Advance = move to the next period (wraps to Period 1 after the last).\n" +
             "SetTo = jump to a specific period.\n" +
             "LeaveUnchanged = don't touch it.")]
    public ClassPeriodAction classPeriodAction = ClassPeriodAction.LeaveUnchanged;
    [Tooltip("Only used when action = SetTo. 0 = Period 1, 1 = Period 2, 2 = Period 3.")]
    public int classPeriodValue = 0;

    [Header("Timing")]
    [Tooltip("Seconds to wait after this object is enabled before the scene changes.")]
    public float delaySeconds = 2f;
    [Tooltip("Use unscaled time so the countdown still runs if Time.timeScale is 0 (e.g. during a freeze).")]
    public bool useUnscaledTime = false;

    [Header("Optional progression changes (applied at the END of the countdown)")]
    [Tooltip("e.g. Advance ClassHalls so the next class is the new scene.")]
    public List<ProgressionChange> progressionChanges = new List<ProgressionChange>();

    [Header("Events")]
    [Tooltip("Fired the moment the countdown starts (good for bell SFX, fades, etc.).")]
    public UnityEvent onCountdownStarted;
    [Tooltip("Fired right before the scene actually loads.")]
    public UnityEvent onBeforeSceneChange;

    private bool started;

    void OnEnable()
    {
        // Fires once when this object is switched on. Guarded so a re-enable in the
        // same lifetime won't double-trigger. (A fresh scene = a fresh instance.)
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        onCountdownStarted?.Invoke();

        if (delaySeconds > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delaySeconds);
            else                 yield return new WaitForSeconds(delaySeconds);
        }

        var mgr = SceneProgressionManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[TriggerNextPeriod] No SceneProgressionManager found — cannot change scene.");
            yield break;
        }

        // Apply progression changes so that the NEXT time that type is entered,
        // it's the new scene.
        foreach (var change in progressionChanges)
        {
            if (change == null) continue;
            if (change.mode == ChangeMode.Advance) mgr.AdvancePeriod(change.type);
            else                                   mgr.SetIndex(change.type, change.value);
        }

        // Adjust the class period. Applies whether we're heading into class now or
        // just preparing it for the next class scene.
        switch (classPeriodAction)
        {
            case ClassPeriodAction.Advance: mgr.AdvanceClassPeriod(); break;
            case ClassPeriodAction.SetTo:   mgr.SetClassPeriod(classPeriodValue); break;
        }

        onBeforeSceneChange?.Invoke();

        mgr.GoToPeriod(nextPeriod);
    }
}
