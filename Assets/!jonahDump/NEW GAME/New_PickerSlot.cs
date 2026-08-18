using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A clickable version of the inventory square, used in the show and tell picker.
/// Easiest way to build the prefab: duplicate your New_Slot prefab, add a Button,
/// swap New_InventorySlot for this script.
/// </summary>
public class New_PickerSlot : MonoBehaviour
{
    public Image squareImage;
    public Image iconImage;
    public Button button;

    [Tooltip("Outline / glow object shown when this one is selected. Starts turned off.")]
    public GameObject highlight;

    New_ShowAndTell owner;
    New_ItemID item;

    public New_ItemID Item { get { return item; } }

    public void Setup(New_ShowAndTell o, New_ItemDatabase db, New_ItemID id)
    {
        owner = o;
        item = id;
        gameObject.name = "New_Picker_" + id;

        New_ItemDatabase.Entry e = (db != null) ? db.Get(id) : null;

        if (squareImage != null)
        {
            Sprite sq = null;
            if (e != null && e.squareOverride != null) sq = e.squareOverride;
            else if (db != null) sq = db.defaultSquare;
            if (sq != null) squareImage.sprite = sq;
        }

        if (iconImage != null)
        {
            iconImage.sprite = (e != null) ? e.icon : null;
            iconImage.enabled = (iconImage.sprite != null);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetHighlighted(false);
    }

    void OnClick()
    {
        if (owner != null) owner.Select(item);
    }

    public void SetHighlighted(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }
}
