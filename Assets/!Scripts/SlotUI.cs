using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single hotbar/backpack/container/shop slot — purely display. All clicking and
/// dragging is handled centrally by DragAndDropController (which raycasts to find the
/// slot under the cursor), so this just needs a background Image with Raycast Target
/// ON so the raycast can hit it.
///
/// It also registers itself in a static list so the fly/pop animation (InventoryFX)
/// can look up any slot's on-screen position by its SlotLocation.
///
/// Recommended prefab layout (see README):
///   - background Image  : Raycast Target ON, STRETCHED to fill the whole cell
///   - icon Image        : Raycast Target OFF, stretched to fill (small padding), Preserve Aspect ON
///   - count TMP text     : Raycast Target OFF
///   - selection highlight: optional child (hotbar only), Raycast Target OFF
/// </summary>
public class SlotUI : MonoBehaviour
{
    [Header("References")]
    public Image backgroundImage;          // Raycast Target ON, fills the cell
    public Image iconImage;                // Raycast Target OFF
    public TMP_Text countText;             // Raycast Target OFF
    public GameObject selectionHighlight;  // optional (hotbar)

    [HideInInspector] public InventoryArea area;
    [HideInInspector] public int index;
    [HideInInspector] public bool interactable = true;  // false for the hand slot

    public SlotLocation Location => new SlotLocation(area, index);

    // ---- Static registry (for animation lookups) -------------------------

    static readonly List<SlotUI> All = new List<SlotUI>();

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// <summary>The active slot at a location, or null. (Reads live area/index, so it
    /// is correct even though Init runs after the slot registers.)</summary>
    public static SlotUI Find(SlotLocation loc)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var s = All[i];
            if (s != null && s.isActiveAndEnabled && s.area == loc.area && s.index == loc.index)
                return s;
        }
        return null;
    }

    // ---- Setup / display -------------------------------------------------

    public void Init(InventoryArea area, int index, bool interactable)
    {
        this.area = area;
        this.index = index;
        this.interactable = interactable;
        if (selectionHighlight != null) selectionHighlight.SetActive(false);
    }

    public void Refresh()
    {
        var slot = InventoryManager.Instance != null ? InventoryManager.Instance.GetSlot(Location) : null;
        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null) { iconImage.enabled = false; iconImage.sprite = null; }
            if (countText != null) countText.text = "";
        }
        else
        {
            if (iconImage != null) { iconImage.enabled = true; iconImage.sprite = slot.item.icon; }
            if (countText != null) countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(selected);
    }

    // ---- Pop animation (called by InventoryFX on arrival) ----------------

    Coroutine popRoutine;

    public void Pop(float scale, float duration)
    {
        if (!isActiveAndEnabled) return;
        RectTransform t = iconImage != null ? (RectTransform)iconImage.transform : (RectTransform)transform;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine(t, scale, duration));
    }

    IEnumerator PopRoutine(RectTransform t, float scale, float duration)
    {
        float half = Mathf.Max(0.02f, duration) * 0.5f;
        float e = 0f;
        while (e < half) { e += Time.unscaledDeltaTime; t.localScale = Vector3.Lerp(Vector3.one, Vector3.one * scale, e / half); yield return null; }
        e = 0f;
        while (e < half) { e += Time.unscaledDeltaTime; t.localScale = Vector3.Lerp(Vector3.one * scale, Vector3.one, e / half); yield return null; }
        t.localScale = Vector3.one;
        popRoutine = null;
    }
}
