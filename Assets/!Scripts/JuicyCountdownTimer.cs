using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// A juicy TextMeshPro countdown timer.
///  - Each second the number "pops" to a larger scale then slowly shrinks until the next second.
///  - As the timer runs out it ramps up urgency: the whole timer grows, the pops get punchier,
///    the color shifts (green -> yellow -> red) and it starts to shake.
///  - When it hits zero it flashes "LATE" for a configurable time, then activates a list of GameObjects.
///
/// Works with both TextMeshProUGUI (Canvas UI) and world-space TextMeshPro.
/// Drop it on any GameObject, wire up the TMP text + the object list, and press play.
/// </summary>
[DisallowMultipleComponent]
public class JuicyCountdownTimer : MonoBehaviour
{
    public enum ActivationMoment
    {
        WhenLateStarts, // activate the objects the instant the countdown hits zero
        AfterLateFlash  // activate the objects once the LATE flash has finished
    }

    [Header("References")]
    [Tooltip("The TextMeshPro element that displays the countdown.")]
    [SerializeField] private TMP_Text timerText;

    [Header("Timing")]
    [Tooltip("Length of the countdown in seconds.")]
    [SerializeField] private float duration = 30f;
    [Tooltip("Start counting down automatically when the scene starts.")]
    [SerializeField] private bool startOnAwake = true;
    [Tooltip("Use unscaled time so the timer ignores Time.timeScale (game pauses, slow-mo, etc).")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Pop (per-second) animation")]
    [Tooltip("Resting scale the number settles toward between pops.")]
    [SerializeField] private float restScale = 1f;
    [Tooltip("Scale at the instant of each pop.")]
    [SerializeField] private float popScale = 1.4f;
    [Tooltip("How the pop decays back to rest over one second. 1 = fully popped, 0 = at rest.")]
    [SerializeField] private AnimationCurve popDecay = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.30f, 0.25f),
        new Keyframe(1f, 0f)
    );

    [Header("Urgency ramp (kicks in as time runs out)")]
    [Tooltip("Maximum overall scale multiplier applied to the whole timer at full urgency.")]
    [SerializeField] private float maxUrgencyScale = 1.6f;
    [Tooltip("Extra pop size added on top of Pop Scale at full urgency.")]
    [SerializeField] private float urgencyPopBonus = 0.4f;
    [Tooltip("Shape of the urgency ramp. Left = start of countdown, right = end. Keep it low early and ramp hard near the end for a menacing build-up.")]
    [SerializeField] private AnimationCurve urgencyGrowth = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.60f, 0.15f),
        new Keyframe(1f, 1f)
    );

    [Header("Color")]
    [Tooltip("Color across the countdown. Left = full time remaining, right = almost out. Green -> yellow -> red by default.")]
    [SerializeField] private Gradient colorOverTime = DefaultColorGradient();

    [Header("Shake (late-game urgency)")]
    [SerializeField] private bool enableShake = true;
    [Tooltip("Urgency (0-1) at which the shake begins.")]
    [SerializeField, Range(0f, 1f)] private float shakeStartUrgency = 0.75f;
    [Tooltip("Maximum shake offset (in the RectTransform's units) at full urgency.")]
    [SerializeField] private float maxShakeMagnitude = 8f;

    [Header("\"LATE\" flash")]
    [Tooltip("Text shown once the timer ends.")]
    [SerializeField] private string lateText = "LATE";
    [Tooltip("How long the LATE flash lasts, in seconds.")]
    [SerializeField] private float lateFlashDuration = 3f;
    [Tooltip("Flashes per second during the LATE state.")]
    [SerializeField] private float lateFlashesPerSecond = 4f;
    [Tooltip("The two colors the LATE text alternates between.")]
    [SerializeField] private Color lateColorA = new Color(1f, 0.15f, 0.15f);
    [SerializeField] private Color lateColorB = Color.white;
    [Tooltip("Scale punch applied on each LATE flash.")]
    [SerializeField] private float latePunchScale = 1.5f;

    [Header("Objects to activate")]
    [Tooltip("These GameObjects get SetActive(true) when the timer finishes.")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();
    [Tooltip("Disable the listed objects automatically when the timer starts (so they can be revealed at the end).")]
    [SerializeField] private bool deactivateObjectsOnStart = true;
    [Tooltip("When to activate the objects, relative to the LATE flash.")]
    [SerializeField] private ActivationMoment activateObjectsOn = ActivationMoment.AfterLateFlash;

    [Header("Events")]
    public UnityEvent onTick;             // fires every time the displayed number changes
    public UnityEvent onTimerComplete;    // fires the instant the countdown reaches zero
    public UnityEvent onObjectsActivated; // fires right after the objects are activated

    // ---- public read-only state ----
    public float TimeRemaining => timeRemaining;
    public bool IsRunning => running;

    // ---- runtime state ----
    private float timeRemaining;
    private bool running;
    private int lastShownSecond = -1;
    private float tickTimer;          // seconds since the last pop
    private RectTransform rect;
    private Vector3 baseLocalScale = Vector3.one;
    private Vector2 baseAnchoredPos;
    private Coroutine lateRoutine;

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private float TimeNow => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        if (timerText != null)
        {
            rect = timerText.rectTransform;
            baseLocalScale = rect != null ? rect.localScale : timerText.transform.localScale;
            if (rect != null) baseAnchoredPos = rect.anchoredPosition;
        }
    }

    private void Start()
    {
        if (startOnAwake) StartTimer();
    }

    // ------------------------------------------------------------ public control

    public void StartTimer() => StartTimer(duration);

    public void StartTimer(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
        timeRemaining = duration;
        running = true;
        lastShownSecond = -1;
        tickTimer = 0f;

        if (lateRoutine != null) { StopCoroutine(lateRoutine); lateRoutine = null; }

        if (deactivateObjectsOnStart)
            foreach (var go in objectsToActivate)
                if (go != null) go.SetActive(false);

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            SetScale(restScale);
            if (rect != null) rect.anchoredPosition = baseAnchoredPos;
        }
    }

    public void StopTimer() => running = false;                       // pause
    public void ResumeTimer() { if (timeRemaining > 0f) running = true; }
    public void ResetTimer() => StartTimer(duration);

    // ------------------------------------------------------------ update loop

    private void Update()
    {
        if (!running) return;

        timeRemaining -= DeltaTime;
        tickTimer += DeltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            running = false;
            HandleTimerComplete();
            return;
        }

        // 0 at the start of the countdown, 1 at the very end.
        float rawUrgency = 1f - Mathf.Clamp01(timeRemaining / duration);
        float urgency = Mathf.Clamp01(urgencyGrowth.Evaluate(rawUrgency));

        UpdateNumber();
        UpdateScaleAndColor(rawUrgency, urgency);
        UpdateShake(rawUrgency);
    }

    private void UpdateNumber()
    {
        int shown = Mathf.CeilToInt(timeRemaining); // 30..1, never shows 0
        if (shown != lastShownSecond)
        {
            lastShownSecond = shown;
            tickTimer = 0f; // trigger a fresh pop
            if (timerText != null) timerText.text = shown.ToString();
            onTick?.Invoke();
        }
    }

    private void UpdateScaleAndColor(float rawUrgency, float urgency)
    {
        if (timerText == null) return;

        // Per-second pop, decaying from the (urgency-boosted) pop scale back to rest.
        float decay = popDecay.Evaluate(Mathf.Clamp01(tickTimer)); // one-second window
        float thisPop = popScale + urgencyPopBonus * urgency;
        float perTick = Mathf.LerpUnclamped(restScale, thisPop, decay);

        // The whole timer grows as it runs out.
        float overall = Mathf.Lerp(1f, maxUrgencyScale, urgency);

        SetScale(perTick * overall);

        // Color follows the real progress of the countdown.
        timerText.color = colorOverTime.Evaluate(rawUrgency);
    }

    private void UpdateShake(float rawUrgency)
    {
        if (rect == null) return;

        Vector2 offset = Vector2.zero;
        if (enableShake && rawUrgency >= shakeStartUrgency)
        {
            float t = Mathf.InverseLerp(shakeStartUrgency, 1f, rawUrgency);
            float mag = maxShakeMagnitude * t;
            float n = TimeNow * 40f;
            offset = new Vector2(
                (Mathf.PerlinNoise(n, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, n) - 0.5f) * 2f
            ) * mag;
        }
        rect.anchoredPosition = baseAnchoredPos + offset;
    }

    // ------------------------------------------------------------ completion + LATE flash

    private void HandleTimerComplete()
    {
        onTimerComplete?.Invoke();

        if (rect != null) rect.anchoredPosition = baseAnchoredPos; // stop any shake

        if (activateObjectsOn == ActivationMoment.WhenLateStarts)
            ActivateObjects();

        lateRoutine = StartCoroutine(LateFlashRoutine());
    }

    private IEnumerator LateFlashRoutine()
    {
        if (timerText != null) timerText.text = lateText;

        float elapsed = 0f;
        while (elapsed < lateFlashDuration)
        {
            elapsed += DeltaTime;

            float phase = elapsed * lateFlashesPerSecond;
            bool onBeat = (Mathf.FloorToInt(phase) & 1) == 0;

            if (timerText != null)
            {
                timerText.color = onBeat ? lateColorA : lateColorB;

                // Punch on each flash: big at the start of a beat, settling toward rest.
                float within = phase - Mathf.Floor(phase);
                SetScale(Mathf.Lerp(latePunchScale, restScale, within));
            }
            yield return null;
        }

        if (timerText != null)
        {
            SetScale(restScale);
            timerText.color = lateColorA;
        }

        if (activateObjectsOn == ActivationMoment.AfterLateFlash)
            ActivateObjects();

        lateRoutine = null;
    }

    private void ActivateObjects()
    {
        foreach (var go in objectsToActivate)
            if (go != null) go.SetActive(true);

        onObjectsActivated?.Invoke();
    }

    // ------------------------------------------------------------ helpers

    private void SetScale(float s)
    {
        if (rect != null) rect.localScale = baseLocalScale * s;
        else if (timerText != null) timerText.transform.localScale = baseLocalScale * s;
    }

    private static Gradient DefaultColorGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.25f, 0.90f, 0.35f), 0.00f), // green
                new GradientColorKey(new Color(1.00f, 0.85f, 0.20f), 0.55f), // yellow
                new GradientColorKey(new Color(1.00f, 0.20f, 0.20f), 1.00f)  // red
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return g;
    }
}
