using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The star icon + number on screen. Put this on an empty UI object
/// INSIDE New_InventoryCanvas, so it inherits the DontDestroyOnLoad for free.
/// </summary>
public class New_StarUI : MonoBehaviour
{
    [Tooltip("The TextMeshPro text showing the number.")]
    public TMP_Text countText;

    [Tooltip("Stuck in front of the number. 'x' gives you x0, x1, x2...")]
    public string prefix = "x";

    [Header("Juice")]
    [Tooltip("What scales up when you gain a star. Leave empty to use this object.")]
    public RectTransform punchTarget;
    public bool punchOnGain = true;
    public float punchTime = 0.2f;
    public float punchScale = 1.35f;

    Vector3 baseScale = Vector3.one;
    int lastShown = -1;

    void Awake()
    {
        if (punchTarget == null) punchTarget = transform as RectTransform;
        if (punchTarget != null) baseScale = punchTarget.localScale;
    }

    void OnEnable()
    {
        New_StarFlags.OnStarCountChanged += HandleChanged;
        Refresh(false);   // catch up on stars gained before this UI existed
    }

    void OnDisable()
    {
        New_StarFlags.OnStarCountChanged -= HandleChanged;
    }

    void HandleChanged(int newCount)
    {
        Refresh(true);
    }

    void Refresh(bool allowPunch)
    {
        int c = New_StarFlags.Count;

        if (countText != null) countText.text = prefix + c;
        else Debug.LogWarning("New_StarUI: countText not assigned.", this);

        bool gained = c > lastShown && lastShown >= 0;
        lastShown = c;

        if (allowPunch && punchOnGain && gained && punchTarget != null && isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(Punch());
        }
    }

    IEnumerator Punch()
    {
        float t = 0f;
        while (t < punchTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / punchTime);
            float s = 1f + (punchScale - 1f) * Mathf.Sin(p * Mathf.PI);
            punchTarget.localScale = baseScale * s;
            yield return null;
        }
        punchTarget.localScale = baseScale;
    }
}