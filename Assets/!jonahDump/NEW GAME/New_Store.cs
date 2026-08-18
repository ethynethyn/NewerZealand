using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The shop. Put this on a Canvas that lives in the shop scene
/// (no DontDestroyOnLoad, each shop is its own thing).
///
/// To stop the player moving while it's open, either drag your movement
/// script into "Disable While Open", or put this at the top of its Update:
///     if (New_Store.IsOpen) return;
/// </summary>
public class New_Store : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public New_ItemID item;
        public int price = 5;
    }

    /// <summary>True while any store panel is open.</summary>
    public static bool IsOpen { get; private set; }

    [Header("Stock")]
    public List<Entry> stock = new List<Entry>();

    [Header("Refs")]
    [Tooltip("The panel that gets switched on and off. NOT this object.")]
    public GameObject storeRoot;

    public New_ItemDatabase database;
    public New_StoreItem rowPrefab;

    [Tooltip("Object with the Vertical Layout Group. Rows get parented here.")]
    public Transform rowParent;

    [Tooltip("Shows how many stars you have, inside the shop.")]
    public TMP_Text starCountText;
    public string starPrefix = "x";

    public Button closeButton;

    [Header("Behaviour")]
    public bool closeWithEscape = true;

    [Tooltip("Optional. Scripts switched off while the shop is open, e.g. your player controller.")]
    public MonoBehaviour[] disableWhileOpen;

    [Tooltip("Also sets Time.timeScale to 0. UI animations use unscaled time so they still play.")]
    public bool freezeTime = false;

    [Tooltip("Unlocks and shows the mouse while the shop is open, then puts it back how it was on close.")]
    public bool freeCursorWhileOpen = true;

    CursorLockMode prevCursorLock = CursorLockMode.None;
    bool prevCursorVisible = true;

    readonly List<New_StoreItem> rows = new List<New_StoreItem>();
    bool built;

    void Awake()
    {
        if (storeRoot != null) storeRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        IsOpen = false;
    }

    void OnEnable()
    {
        New_StarFlags.OnStarCountChanged += HandleStarsChanged;
    }

    void OnDisable()
    {
        New_StarFlags.OnStarCountChanged -= HandleStarsChanged;
    }

    void OnDestroy()
    {
        // don't leave the game frozen if the scene unloads mid-shop
        if (IsOpen) SetLocked(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // some controllers re-grab the cursor every frame, so keep taking it back
        if (freeCursorWhileOpen && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ------------------------------------------------------------

    public void Open()
    {
        if (storeRoot == null)
        {
            Debug.LogWarning("New_Store: storeRoot not assigned.", this);
            return;
        }

        Build();
        RefreshAll();

        storeRoot.SetActive(true);
        SetLocked(true);
    }

    public void Close()
    {
        if (storeRoot != null) storeRoot.SetActive(false);
        SetLocked(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    // ------------------------------------------------------------

    void Build()
    {
        if (built) return;
        if (rowPrefab == null || rowParent == null)
        {
            Debug.LogWarning("New_Store: rowPrefab or rowParent not assigned.", this);
            return;
        }

        built = true;

        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i] == null) continue;
            New_StoreItem row = Instantiate(rowPrefab, rowParent);
            row.transform.localScale = Vector3.one;
            row.Setup(this, stock[i], database);
            rows.Add(row);
        }
    }

    public void RefreshAll()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) rows[i].Refresh();
        }

        if (starCountText != null) starCountText.text = starPrefix + New_StarFlags.Count;
    }

    void HandleStarsChanged(int newCount)
    {
        RefreshAll();
    }

    /// <summary>Called by the row when you click it.</summary>
    public void TryBuy(Entry entry)
    {
        if (entry == null) return;

        if (New_ItemFlags.Has(entry.item)) return;        // already crossed off
        if (!New_StarFlags.TrySpend(entry.price)) return; // not enough stars

        New_InventoryUI.Give(entry.item);                 // square pops into the top bar
        RefreshAll();
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

        if (freezeTime) Time.timeScale = locked ? 0f : 1f;
    }
}