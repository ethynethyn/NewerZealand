using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The shop panel shown beside the backpack (where a loot container would appear).
/// Uses the SAME slot prefab so it matches, plus its own description + price text.
/// ShopController drives Open/Close and the text.
/// </summary>
public class ShopPanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root object of the shop panel (its own background). Enabled while open.")]
    public GameObject panelRoot;
    public SlotUI slotPrefab;
    public Transform slotContainer;    // e.g. a GridLayoutGroup
    public TMP_Text titleText;

    [Header("Info Area")]
    [Tooltip("Optional: shows the hovered SHOP item's name.")]
    public TMP_Text itemNameText;
    [Tooltip("Shows the hovered item's description, and forced messages like 'Item purchased'.")]
    public TMP_Text descriptionText;
    [Tooltip("Shows the price / sale value, coloured by affordability or a forced message.")]
    public TMP_Text priceText;

    [Header("Colours")]
    public Color affordableColor = new Color(0.35f, 0.85f, 0.4f); // green
    public Color unaffordableColor = new Color(0.9f, 0.3f, 0.3f);  // red
    public Color normalDescriptionColor = Color.white;

    readonly List<SlotUI> slots = new List<SlotUI>();
    Shop current;
    int builtCount = -1;
    bool subscribedToManager;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open(Shop shop)
    {
        current = shop;
        if (titleText != null) titleText.text = shop.DisplayName;

        BuildSlots(shop.SlotCount);
        Subscribe();

        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshAll();
        ClearInfo();
    }

    public void Close()
    {
        Unsubscribe();
        current = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void BuildSlots(int count)
    {
        if (builtCount == count) return;

        foreach (var s in slots) if (s != null) Destroy(s.gameObject);
        slots.Clear();

        for (int i = 0; i < count; i++)
        {
            var s = Instantiate(slotPrefab, slotContainer);
            s.Init(InventoryArea.Shop, i, interactable: true);
            slots.Add(s);
        }
        builtCount = count;
    }

    void RefreshAll()
    {
        foreach (var s in slots) if (s != null) s.Refresh();
    }

    // ---- Info text -------------------------------------------------------

    /// <summary>Show a description + a coloured price string (normal hover state).</summary>
    public void ShowInfo(string itemName, string description, string price, Color priceColor)
    {
        if (itemNameText != null)
        {
            itemNameText.text = itemName;
            itemNameText.color = normalDescriptionColor;
        }
        if (descriptionText != null)
        {
            descriptionText.text = description;
            descriptionText.color = normalDescriptionColor;
        }
        if (priceText != null)
        {
            priceText.text = price;
            priceText.color = priceColor;
        }
    }

    /// <summary>Show a forced message (e.g. "Item purchased") in place of the description.</summary>
    public void ShowMessage(string message, Color color)
    {
        if (itemNameText != null) itemNameText.text = "";
        if (descriptionText != null)
        {
            descriptionText.text = message;
            descriptionText.color = color;
        }
        if (priceText != null) priceText.text = "";
    }

    public void ClearInfo()
    {
        if (itemNameText != null) itemNameText.text = "";
        if (descriptionText != null) { descriptionText.text = ""; descriptionText.color = normalDescriptionColor; }
        if (priceText != null) priceText.text = "";
    }

    // ---- Change subscriptions -------------------------------------------

    void Subscribe()
    {
        if (!subscribedToManager && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged += RefreshAll;
            subscribedToManager = true;
        }
        if (current != null) current.Changed += RefreshAll;
    }

    void Unsubscribe()
    {
        if (current != null) current.Changed -= RefreshAll;
        if (subscribedToManager && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged -= RefreshAll;
            subscribedToManager = false;
        }
    }

    void OnDestroy() => Unsubscribe();
}
