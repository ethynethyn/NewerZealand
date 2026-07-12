using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// The bag / backpack window. Opening it mirrors your pause menu: freezes time,
/// frees the mouse, clears player input and disables the FPS controller, and
/// toggles your "mouse focus" objects.
/// </summary>
public class InventoryPanelUI : MonoBehaviour
{
    /// <summary>Global flag other scripts use to suppress gameplay input while open.</summary>
    public static bool IsOpen { get; private set; }

    [Header("Open / Close")]
    public Key openKey = Key.Tab;
    [Tooltip("Root object of the bag UI (enabled while open).")]
    public GameObject inventoryPanel;

    [Header("Slot Building")]
    public SlotUI slotPrefab;
    public Transform backpackContainer;   // e.g. a GridLayoutGroup

    [Header("Freeze / Focus (same idea as your pause script)")]
    public bool freezeTime = true;
    public bool disableFirstPersonController = true;
    [Tooltip("Objects enabled while the inventory is open (e.g. your mouse-focus object).")]
    public List<GameObject> enableOnOpen = new List<GameObject>();
    [Tooltip("Objects disabled while the inventory is open.")]
    public List<GameObject> disableOnOpen = new List<GameObject>();

    readonly List<SlotUI> slots = new List<SlotUI>();
    StarterAssetsInputs starterInputs;
    FirstPersonController fpController;

    void Start()
    {
        starterInputs = FindObjectOfType<StarterAssetsInputs>();
        fpController = FindObjectOfType<FirstPersonController>();

        BuildSlots();
        InventoryManager.Instance.OnChanged += RefreshAll;
        RefreshAll();

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        IsOpen = false;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnChanged -= RefreshAll;
    }

    void BuildSlots()
    {
        var mgr = InventoryManager.Instance;
        for (int i = 0; i < mgr.backpack.Length; i++)
        {
            var s = Instantiate(slotPrefab, backpackContainer);
            s.Init(InventoryArea.Backpack, i, interactable: true);
            slots.Add(s);
        }
    }

    void RefreshAll()
    {
        foreach (var s in slots) s.Refresh();
    }

    // Re-find the player rig lazily in case it wasn't in the scene at Start().
    void EnsureRefs()
    {
        if (starterInputs == null) starterInputs = FindObjectOfType<StarterAssetsInputs>();
        if (fpController == null) fpController = FindObjectOfType<FirstPersonController>();
    }

    void Update()
    {
        // Update() ignores timeScale, so this still works while frozen.
        if (Keyboard.current != null && Keyboard.current[openKey].wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    public void Open()
    {
        IsOpen = true;
        EnsureRefs();
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        RefreshAll();

        if (freezeTime) Time.timeScale = 0f;

        foreach (var o in enableOnOpen) if (o != null) o.SetActive(true);
        foreach (var o in disableOnOpen) if (o != null) o.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disabling the FPS controller stops all camera look while open.
        if (disableFirstPersonController && fpController != null) fpController.enabled = false;

        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
            starterInputs.cursorInputForLook = false;   // also gates mouse look input
        }
    }

    public void Close()
    {
        // Don't lose a stack the player was carrying on the cursor.
        if (DragAndDropController.Instance != null)
            DragAndDropController.Instance.ReturnCarriedToInventory();

        IsOpen = false;
        EnsureRefs();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (freezeTime) Time.timeScale = 1f;

        foreach (var o in enableOnOpen) if (o != null) o.SetActive(false);
        foreach (var o in disableOnOpen) if (o != null) o.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (disableFirstPersonController && fpController != null) fpController.enabled = true;

        if (starterInputs != null)
            starterInputs.cursorInputForLook = true;

        // If a loot container was open alongside the inventory, tear it down too.
        if (LootController.Instance != null)
            LootController.Instance.OnInventoryClosed();
    }
}