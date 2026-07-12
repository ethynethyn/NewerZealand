using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single hotbar/backpack slot — purely display now. All clicking/dragging is
/// handled centrally by DragAndDropController (which raycasts to find the slot
/// under the cursor), so this component just needs a background Image with
/// Raycast Target ON so the raycast can hit it.
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
}