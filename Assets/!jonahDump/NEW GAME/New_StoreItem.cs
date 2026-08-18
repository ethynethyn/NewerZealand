using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One line in the shop list: price, star, item name, and the scribble
/// that crosses it out once you've bought it. Goes on the row prefab.
/// </summary>
public class New_StoreItem : MonoBehaviour
{
    [Tooltip("Shows the number of stars it costs.")]
    public TMP_Text priceText;

    [Tooltip("Shows the item name.")]
    public TMP_Text nameText;

    [Tooltip("Optional. Shows the item's art next to the name.")]
    public Image iconImage;

    [Tooltip("Your MS Paint scribble line. Turned on once the item is bought.")]
    public GameObject strikethrough;

    [Tooltip("Button on the row. Clicking anywhere on the row buys it.")]
    public Button buyButton;

    [Tooltip("Optional. Dims the whole row once bought.")]
    public CanvasGroup fade;

    [Range(0.1f, 1f)] public float soldAlpha = 0.55f;

    New_Store store;
    New_Store.Entry entry;

    public void Setup(New_Store owner, New_Store.Entry e, New_ItemDatabase db)
    {
        store = owner;
        entry = e;

        if (priceText != null) priceText.text = e.price.ToString();

        if (nameText != null)
        {
            nameText.text = (db != null) ? db.GetName(e.item) : e.item.ToString();
        }

        if (iconImage != null)
        {
            New_ItemDatabase.Entry data = (db != null) ? db.Get(e.item) : null;
            iconImage.sprite = (data != null) ? data.icon : null;
            iconImage.enabled = (iconImage.sprite != null);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnClick);
        }

        gameObject.name = "New_StoreRow_" + e.item;
        Refresh();
    }

    void OnClick()
    {
        if (store != null && entry != null) store.TryBuy(entry);
    }

    /// <summary>Re-reads the flags and updates the crossed-out / greyed-out look.</summary>
    public void Refresh()
    {
        if (entry == null) return;

        bool sold = New_ItemFlags.Has(entry.item);
        bool affordable = New_StarFlags.CanAfford(entry.price);

        if (strikethrough != null) strikethrough.SetActive(sold);
        if (buyButton != null) buyButton.interactable = !sold && affordable;
        if (fade != null) fade.alpha = sold ? soldAlpha : 1f;
    }
}