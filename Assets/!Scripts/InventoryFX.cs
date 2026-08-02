using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the shift-click "fly + pop" flourish: spawns a temporary copy of an item's
/// icon that lerps from one slot to another, then pops the destination slot. Put ONE
/// in the scene. Uses UNSCALED time so it works whether or not the game is frozen.
///
/// Assumes a Screen Space - Overlay canvas (same as the drag ghost). If you leave
/// Fly Layer empty it will use the drag ghost's parent canvas automatically.
/// </summary>
public class InventoryFX : MonoBehaviour
{
    public static InventoryFX Instance { get; private set; }

    [Header("Fly Layer")]
    [Tooltip("Parent for flying icons — a top Screen Space - Overlay canvas. " +
             "Leave empty to reuse the drag ghost's canvas.")]
    public RectTransform flyLayer;

    [Header("Flight")]
    public float flyDuration = 0.18f;
    public AnimationCurve flyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public Vector2 flyIconSize = new Vector2(48f, 48f);

    [Header("Pop (on arrival)")]
    [Tooltip("Scale the destination slot icon bounces to.")]
    public float popScale = 1.35f;
    public float popDuration = 0.14f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    RectTransform Layer
    {
        get
        {
            if (flyLayer != null) return flyLayer;
            if (DragAndDropController.Instance != null && DragAndDropController.Instance.ghostImage != null)
                return DragAndDropController.Instance.ghostImage.transform.parent as RectTransform;
            return null;
        }
    }

    /// <summary>Fly a copy of an icon from one slot to another, then pop the destination.</summary>
    public void FlyBetweenSlots(SlotLocation from, SlotLocation to, Sprite icon, Action onArrive = null)
    {
        var fromUI = SlotUI.Find(from);
        var toUI = SlotUI.Find(to);
        if (icon == null || fromUI == null || toUI == null || Layer == null)
        {
            onArrive?.Invoke();   // can't animate -> still fire the callback
            return;
        }
        StartCoroutine(FlyRoutine(fromUI.transform.position, toUI.transform.position, icon, to, onArrive));
    }

    IEnumerator FlyRoutine(Vector3 fromPos, Vector3 toPos, Sprite icon, SlotLocation dest, Action onArrive)
    {
        var go = new GameObject("FlyIcon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(Layer, false);
        rt.sizeDelta = flyIconSize;
        rt.position = fromPos;

        var img = go.GetComponent<Image>();
        img.sprite = icon;
        img.raycastTarget = false;
        img.preserveAspect = true;

        float t = 0f;
        float dur = Mathf.Max(0.02f, flyDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = flyEase.Evaluate(Mathf.Clamp01(t / dur));
            rt.position = Vector3.LerpUnclamped(fromPos, toPos, u);
            yield return null;
        }

        Destroy(go);

        var toUI = SlotUI.Find(dest);
        if (toUI != null) toUI.Pop(popScale, popDuration);

        onArrive?.Invoke();
    }
}
