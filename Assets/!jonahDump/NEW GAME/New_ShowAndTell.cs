using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Show and tell. Call Begin() from a DialogueEditor node event.
/// Player picks one item they own, hits Present, gets stars based on the
/// item's rarity, and onItemPresented fires so dialogue can react.
///
/// Nothing is consumed. Items are never removed from the inventory.
/// </summary>
public class New_ShowAndTell : MonoBehaviour
{
    [System.Serializable]
    public class ItemEvent : UnityEvent<New_ItemID> { }

    /// <summary>True while the picker is up.</summary>
    public static bool IsOpen { get; private set; }

    [Header("Star payout per rarity")]
    public int commonStars = 1;
    public int rareStars = 5;
    public int legendaryStars = 20;

    [Header("Refs")]
    [Tooltip("The panel that gets switched on and off. NOT this object.")]
    public GameObject panelRoot;

    public New_ItemDatabase database;
    public New_PickerSlot slotPrefab;

    [Tooltip("Object with the Grid Layout Group. Squares get parented here.")]
    public Transform slotParent;

    [Tooltip("Optional. Shows the name of whatever is currently selected.")]
    public TMP_Text selectedNameText;

    [Tooltip("Optional. Shows the rarity of whatever is currently selected.")]
    public TMP_Text selectedRarityText;

    [Tooltip("Optional. Shown instead of the grid when you own nothing yet.")]
    public GameObject emptyMessage;

    public Button presentButton;
    public Button cancelButton;

    [Header("Behaviour")]
    public bool allowCancel = true;
    public bool closeWithEscape = true;
    public bool freeCursorWhileOpen = true;

    [Tooltip("Optional. Scripts switched off while the picker is open, e.g. your player controller.")]
    public MonoBehaviour[] disableWhileOpen;

    [Header("Events")]
    [Tooltip("Fires AFTER the stars are handed out. Hook your dialogue reaction here.")]
    public ItemEvent onItemPresented;
    public UnityEvent onCancelled;

    /// <summary>What was presented last, for anything that needs it after the fact.</summary>
    public New_ItemID LastItem { get; private set; }
    public New_ItemRarity LastRarity { get; private set; }
    public int LastStars { get; private set; }

    readonly List<New_PickerSlot> slots = new List<New_PickerSlot>();
    New_ItemID selected;
    bool hasSelection;

    CursorLockMode prevCursorLock = CursorLockMode.None;
    bool prevCursorVisible = true;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (presentButton != null) presentButton.onClick.AddListener(Present);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        IsOpen = false;
    }

    void OnDestroy()
    {
        if (IsOpen) SetLocked(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (closeWithEscape && allowCancel && Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        if (freeCursorWhileOpen && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ============================================================
    //  THIS IS THE FUNCTION YOU CALL FROM DIALOGUE
    // ============================================================
    public void Begin()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("New_ShowAndTell: panelRoot not assigned.", this);
            return;
        }

        BuildSlots();

        hasSelection = false;
        RefreshSelectionUI();

        panelRoot.SetActive(true);
        SetLocked(true);
    }

    void BuildSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) Destroy(slots[i].gameObject);
        }
        slots.Clear();

        if (slotPrefab == null || slotParent == null)
        {
            Debug.LogWarning("New_ShowAndTell: slotPrefab or slotParent not assigned.", this);
            return;
        }

        IReadOnlyList<New_ItemID> owned = New_ItemFlags.OwnedInOrder;

        for (int i = 0; i < owned.Count; i++)
        {
            New_PickerSlot s = Instantiate(slotPrefab, slotParent);
            s.transform.localScale = Vector3.one;
            s.Setup(this, database, owned[i]);
            slots.Add(s);
        }

        if (emptyMessage != null) emptyMessage.SetActive(owned.Count == 0);
    }

    /// <summary>Called by a picker square when you click it.</summary>
    public void Select(New_ItemID id)
    {
        selected = id;
        hasSelection = true;
        RefreshSelectionUI();
    }

    void RefreshSelectionUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) slots[i].SetHighlighted(hasSelection && slots[i].Item.Equals(selected));
        }

        if (presentButton != null) presentButton.interactable = hasSelection;

        if (!hasSelection)
        {
            if (selectedNameText != null) selectedNameText.text = "";
            if (selectedRarityText != null) selectedRarityText.text = "";
            return;
        }

        if (selectedNameText != null)
        {
            selectedNameText.text = (database != null) ? database.GetName(selected) : selected.ToString();
        }

        if (selectedRarityText != null)
        {
            selectedRarityText.text = GetRarity(selected).ToString();
        }
    }

    /// <summary>Wired to the Present button.</summary>
    public void Present()
    {
        if (!hasSelection) return;

        New_ItemID id = selected;

        LastItem = id;
        LastRarity = GetRarity(id);
        LastStars = GetStarValue(id);

        New_StarFlags.Add(LastStars);

        Close();

        if (onItemPresented != null) onItemPresented.Invoke(id);
    }

    public void Cancel()
    {
        if (!allowCancel) return;

        Close();
        if (onCancelled != null) onCancelled.Invoke();
    }

    void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        SetLocked(false);
    }

    // ------------------------------------------------------------

    public New_ItemRarity GetRarity(New_ItemID id)
    {
        return (database != null) ? database.GetRarity(id) : New_ItemRarity.Common;
    }

    /// <summary>Per-item override wins, otherwise the rarity's payout.</summary>
    public int GetStarValue(New_ItemID id)
    {
        if (database != null)
        {
            int over = database.GetStarOverride(id);
            if (over >= 0) return over;
        }

        switch (GetRarity(id))
        {
            case New_ItemRarity.Legendary: return legendaryStars;
            case New_ItemRarity.Rare:      return rareStars;
            default:                       return commonStars;
        }
    }

    void SetLocked(bool locked)
    {
        IsOpen = locked;

        if (disableWhileOpen != null)
        {
            for (int i = 0; i < disableWhileOpen.Length; i++)
            {
                if (disableWhileOpen[i] != null) disableWhileOpen[i].enabled = !locked;
            }
        }

        if (freeCursorWhileOpen)
        {
            if (locked)
            {
                prevCursorLock = Cursor.lockState;
                prevCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = prevCursorLock;
                Cursor.visible = prevCursorVisible;
            }
        }
    }
}
