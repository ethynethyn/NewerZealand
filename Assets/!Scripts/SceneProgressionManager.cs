using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent manager that remembers WHICH class / recess / lunch scene the player
// is up to, and loads the correct one when a period begins. Survives scene loads
// (DontDestroyOnLoad), so progression is preserved as scenes swap.
//
// Indices are 0-based and line up with the lists in the MainProgressionSceneList
// asset (element 0 = "Lunch 1", element 1 = "Lunch 2", ...).
//
// Changing an index does NOT reload the current scene — it takes effect the NEXT
// time that period type is entered. So if dialogue advances Lunch during Lunch 1,
// the player still finishes Lunch 1 and only sees the new scene at the next lunch.
public class SceneProgressionManager : MonoBehaviour
{
    public static SceneProgressionManager Instance { get; private set; }

    [Header("Scene Structure")]
    public MainProgressionSceneList sceneList;

    [Header("Current Progression (0-based index into each list)")]
    public int classIndex = 0;
    public int recessIndex = 0;
    public int lunchIndex = 0;

    [Header("Current Class Period (0 = Period 1, 1 = Period 2, 2 = Period 3)")]
    [Tooltip("Which period the next Class Halls scene runs. Set by TriggerNextPeriod, " +
             "read by ClassPeriodStarter. Lets you reuse one class scene for any period.")]
    public int currentClassPeriod = 0;

    void Awake()
    {
        // One persistent instance. Duplicates (from a Core prefab in every scene)
        // destroy themselves so the original keeps the live progression.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Load the CURRENT scene for a period type ──────────────────────

    public void GoToPeriod(PeriodType type)
    {
        int index = GetIndex(type);
        SceneReference scene = sceneList != null ? sceneList.Get(type, index) : null;

        if (scene == null || !scene.IsValid)
        {
            Debug.LogError($"[SceneProgression] No valid {type} scene at index {index}. " +
                           "Check the Main Progression Scene List and that the scene is in Build Settings.");
            return;
        }

        Debug.Log($"[SceneProgression] Loading {type} '{scene.SceneName}' (index {index}).");
        SceneManager.LoadScene(scene.SceneName);
    }

    // No-arg wrappers — handy for UnityEvents and the TriggerNextPeriod dropdown.
    public void GoToClassHalls() => GoToPeriod(PeriodType.ClassHalls);
    public void GoToRecess()     => GoToPeriod(PeriodType.Recess);
    public void GoToLunch()      => GoToPeriod(PeriodType.Lunch);

    // ── Class period (which of the 3 class periods the class scene runs) ──
    // 0 = Period 1, 1 = Period 2, 2 = Period 3. Changing this does NOT reload;
    // it's read when the next class scene starts (by ClassPeriodStarter).
    public void SetClassPeriod(int period) => currentClassPeriod = Mathf.Max(0, period);
    public int GetClassPeriod() => currentClassPeriod;

    // Convenience: set the period AND load the current class scene in one call.
    public void GoToClassHallsForPeriod(int period)
    {
        SetClassPeriod(period);
        GoToPeriod(PeriodType.ClassHalls);
    }

    // ── Progression control (UnityEvent-friendly) ────────────────────
    // Use the Advance* methods for the common "move to the next scene" case —
    // they avoid any off-by-one confusion with indices.

    public void SetClassIndex(int i)  => classIndex  = ClampIndex(PeriodType.ClassHalls, i);
    public void SetRecessIndex(int i) => recessIndex = ClampIndex(PeriodType.Recess, i);
    public void SetLunchIndex(int i)  => lunchIndex  = ClampIndex(PeriodType.Lunch, i);

    public void AdvanceClassHalls() => SetClassIndex(classIndex + 1);
    public void AdvanceRecess()     => SetRecessIndex(recessIndex + 1);
    public void AdvanceLunch()      => SetLunchIndex(lunchIndex + 1);

    // ── Generic helpers (for code / TriggerNextPeriod) ────────────────

    public void AdvancePeriod(PeriodType type) => SetIndex(type, GetIndex(type) + 1);

    public void SetIndex(PeriodType type, int i)
    {
        switch (type)
        {
            case PeriodType.ClassHalls: SetClassIndex(i);  break;
            case PeriodType.Recess:     SetRecessIndex(i); break;
            case PeriodType.Lunch:      SetLunchIndex(i);  break;
        }
    }

    public int GetIndex(PeriodType type)
    {
        switch (type)
        {
            case PeriodType.ClassHalls: return classIndex;
            case PeriodType.Recess:     return recessIndex;
            case PeriodType.Lunch:      return lunchIndex;
            default: return 0;
        }
    }

    int ClampIndex(PeriodType type, int i)
    {
        int count = sceneList != null ? sceneList.Count(type) : 0;
        if (count <= 0) return Mathf.Max(0, i);   // list empty at edit time — just remember it

        int clamped = Mathf.Clamp(i, 0, count - 1);
        if (clamped != i)
            Debug.LogWarning($"[SceneProgression] {type} index {i} out of range (0..{count - 1}); clamped to {clamped}.");
        return clamped;
    }
}
