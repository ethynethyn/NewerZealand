using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;

public class WeaponWheelUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup weaponWheelCanvas;
    public RectTransform wheelCenter;
    public InventoryManager inventoryManager;
    public StarterAssetsInputs starterAssetsInputs;

    [Header("Opening Animation")]
    [Tooltip("Object to enable during opening (like bag animation)")]
    public GameObject openingAnimationObject;

    [Tooltip("How long the opening animation plays before showing wheel")]
    public float openingAnimationDuration = 0.5f;

    [Header("Wheel Settings")]
    public Key openWheelKey = Key.Q;
    public float selectionDeadzone = 50f;

    [Header("UI Slot References - Must be 8 items each")]
    public List<Image> slotBackgrounds = new List<Image>();
    public List<Image> slotIcons = new List<Image>();
    public List<TextMeshProUGUI> slotQuantityTexts = new List<TextMeshProUGUI>();
    public List<GameObject> slotHoverObjects = new List<GameObject>();

    [Header("Central Display")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI errorText;

    [Header("Visual Settings")]
    public Color selectedColor = Color.yellow;
    public Color unselectedColor = Color.white;
    public Color specialSlotColor = Color.cyan;

    private bool isWheelOpen = false;
    private bool isPlayingAnimation = false;
    private int currentSelectedSlot = -1;
    private int lastSelectedSlot = -1; // Remember last selection
    private float errorMessageTimer = 0f;
    private const float ERROR_DISPLAY_TIME = 3f;

    private void Start()
    {
        // Validate references
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager reference is missing!");
        }

        // Hide wheel at start
        if (weaponWheelCanvas != null)
        {
            weaponWheelCanvas.alpha = 0f;
            weaponWheelCanvas.interactable = false;
            weaponWheelCanvas.blocksRaycasts = false;
        }

        // Hide error text
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }

        // Hide animation object
        if (openingAnimationObject != null)
        {
            openingAnimationObject.SetActive(false);
        }

        // Hide all hover objects initially
        foreach (var hoverObj in slotHoverObjects)
        {
            if (hoverObj != null)
                hoverObj.SetActive(false);
        }

        Debug.Log("WeaponWheelUI initialized");
    }

    private void Update()
    {
        // Handle error message timer
        if (errorMessageTimer > 0f)
        {
            errorMessageTimer -= Time.unscaledDeltaTime;
            if (errorMessageTimer <= 0f && errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        // Don't process input during animation
        if (isPlayingAnimation)
            return;

        // Open/Close wheel
        if (Keyboard.current != null && Keyboard.current[openWheelKey].isPressed)
        {
            if (!isWheelOpen)
            {
                StartCoroutine(OpenWheelWithAnimation());
            }
            else
            {
                UpdateWheelSelection();
            }
        }
        else if (isWheelOpen)
        {
            CloseWheel();
        }

        // Handle scroll wheel
        if (isWheelOpen && Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0 && currentSelectedSlot >= 0)
            {
                CycleSlotItem(scroll > 0);
            }
        }
    }

    IEnumerator OpenWheelWithAnimation()
    {
        isPlayingAnimation = true;

        // Store what player was holding
        if (inventoryManager != null)
        {
            inventoryManager.StoreHeldItem();
        }

        // Pause camera input
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorInputForLook = false;
            starterAssetsInputs.look = Vector2.zero;
        }

        // Slow time
        Time.timeScale = 0.2f;

        // Keep cursor hidden
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        // Play animation if exists
        if (openingAnimationObject != null && openingAnimationDuration > 0f)
        {
            openingAnimationObject.SetActive(true);
            yield return new WaitForSecondsRealtime(openingAnimationDuration);
            openingAnimationObject.SetActive(false);
        }

        // Now show the wheel
        OpenWheel();

        isPlayingAnimation = false;
    }

    void OpenWheel()
    {
        isWheelOpen = true;

        if (weaponWheelCanvas != null)
        {
            weaponWheelCanvas.alpha = 1f;
            weaponWheelCanvas.interactable = true;
            weaponWheelCanvas.blocksRaycasts = true;
        }

        // Start at last selected slot
        currentSelectedSlot = lastSelectedSlot;

        // Update all slot visuals
        RefreshAllSlots();

        Debug.Log($"Weapon wheel opened, starting at slot {currentSelectedSlot}");
    }

    void CloseWheel()
    {
        isWheelOpen = false;

        if (weaponWheelCanvas != null)
        {
            weaponWheelCanvas.alpha = 0f;
            weaponWheelCanvas.interactable = false;
            weaponWheelCanvas.blocksRaycasts = false;
        }

        // Restore time
        Time.timeScale = 1f;

        // Re-enable camera input
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorInputForLook = true;
        }

        // Handle selection
        if (currentSelectedSlot >= 0 && inventoryManager != null)
        {
            var category = inventoryManager.GetCategory(currentSelectedSlot);

            if (category != null)
            {
                if (category.isAddSlot)
                {
                    string errorMessage;
                    bool success = inventoryManager.TryAddHeldItemToInventory(out errorMessage);

                    if (!success)
                    {
                        ShowError(errorMessage);
                    }
                    else
                    {
                        inventoryManager.OnAddSlotSelected();
                    }
                }
                else if (category.isExitSlot)
                {
                    inventoryManager.SpawnItemToHands(currentSelectedSlot);
                }
                else
                {
                    inventoryManager.SpawnItemToHands(currentSelectedSlot);
                }
            }

            // Remember this selection for next time
            lastSelectedSlot = currentSelectedSlot;
        }

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hide all hover objects
        foreach (var hoverObj in slotHoverObjects)
        {
            if (hoverObj != null)
                hoverObj.SetActive(false);
        }

        // Reset current selection
        currentSelectedSlot = -1;

        Debug.Log("Weapon wheel closed");
    }

    void UpdateWheelSelection()
    {
        if (Mouse.current == null || wheelCenter == null)
            return;

        // Get mouse position relative to wheel center
        Vector2 mousePos = Mouse.current.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelCenter,
            mousePos,
            null,
            out Vector2 localMousePos
        );

        // Check deadzone
        float distanceFromCenter = localMousePos.magnitude;
        if (distanceFromCenter < selectionDeadzone)
        {
            // Don't change selection in deadzone, keep current
            return;
        }

        // Calculate angle
        float mouseAngle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;
        if (mouseAngle < 0)
            mouseAngle += 360f;

        // Adjust to start from top
        mouseAngle = (mouseAngle + 90f) % 360f;

        // Determine slot
        float slotAngleSize = 360f / 8f;
        int selectedSlot = Mathf.FloorToInt(mouseAngle / slotAngleSize);

        if (selectedSlot != currentSelectedSlot)
        {
            currentSelectedSlot = selectedSlot;
            RefreshAllSlots();
        }
    }

    void RefreshAllSlots()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("Cannot refresh slots - InventoryManager is null");
            return;
        }

        // Update each slot
        for (int i = 0; i < 8; i++)
        {
            UpdateSlot(i);
        }

        // Update center text
        UpdateCenterText();
    }

    void UpdateSlot(int slotIndex)
    {
        var category = inventoryManager.GetCategory(slotIndex);
        if (category == null)
        {
            Debug.LogWarning($"Category {slotIndex} is null");
            return;
        }

        bool isSelected = (currentSelectedSlot == slotIndex);

        // Update background color
        if (slotIndex < slotBackgrounds.Count && slotBackgrounds[slotIndex] != null)
        {
            if (isSelected)
            {
                slotBackgrounds[slotIndex].color = selectedColor;
            }
            else if (category.isAddSlot || category.isExitSlot)
            {
                slotBackgrounds[slotIndex].color = specialSlotColor;
            }
            else
            {
                slotBackgrounds[slotIndex].color = unselectedColor;
            }
        }

        // Update hover object
        if (slotIndex < slotHoverObjects.Count && slotHoverObjects[slotIndex] != null)
        {
            slotHoverObjects[slotIndex].SetActive(isSelected);
        }

        // Update icon/object and quantity
        if (category.isAddSlot)
        {
            // Hide all item display objects
            inventoryManager.HideAllDisplayObjectsInCategory(slotIndex);

            // Hide slot icon if it exists
            if (slotIndex < slotIcons.Count && slotIcons[slotIndex] != null)
            {
                slotIcons[slotIndex].enabled = false;
            }

            if (slotIndex < slotQuantityTexts.Count && slotQuantityTexts[slotIndex] != null)
            {
                slotQuantityTexts[slotIndex].text = "ADD";
            }
        }
        else if (category.isExitSlot)
        {
            // Hide all item display objects
            inventoryManager.HideAllDisplayObjectsInCategory(slotIndex);

            // Hide slot icon if it exists
            if (slotIndex < slotIcons.Count && slotIcons[slotIndex] != null)
            {
                slotIcons[slotIndex].enabled = false;
            }

            if (slotIndex < slotQuantityTexts.Count && slotQuantityTexts[slotIndex] != null)
            {
                slotQuantityTexts[slotIndex].text = "EXIT";
            }
        }
        else
        {
            // Show item display objects or fallback to slot icon
            var currentItem = category.GetCurrentItem();
            bool hasActiveDisplayObject = false;

            if (currentItem != null)
            {
                // Try to show display object for this item
                hasActiveDisplayObject = inventoryManager.ShowDisplayObjectForItem(slotIndex, currentItem);
            }
            else
            {
                // No item, hide all display objects in this category
                inventoryManager.HideAllDisplayObjectsInCategory(slotIndex);
            }

            // Show slot icon only if no display object is active and icon exists
            if (slotIndex < slotIcons.Count && slotIcons[slotIndex] != null)
            {
                if (!hasActiveDisplayObject && slotIcons[slotIndex].sprite != null)
                {
                    slotIcons[slotIndex].enabled = true;
                    slotIcons[slotIndex].color = Color.white;
                }
                else
                {
                    slotIcons[slotIndex].enabled = false;
                }
            }

            // Update quantity text
            if (slotIndex < slotQuantityTexts.Count && slotQuantityTexts[slotIndex] != null)
            {
                if (currentItem != null && currentItem.quantity > 0)
                {
                    slotQuantityTexts[slotIndex].text = currentItem.quantity.ToString();
                }
                else
                {
                    slotQuantityTexts[slotIndex].text = "";
                }
            }
        }
    }

    void UpdateCenterText()
    {
        if (itemNameText == null)
            return;

        if (currentSelectedSlot < 0)
        {
            itemNameText.text = "";
            return;
        }

        var category = inventoryManager.GetCategory(currentSelectedSlot);
        if (category == null)
        {
            itemNameText.text = "";
            return;
        }

        if (category.isAddSlot)
        {
            itemNameText.text = "Add Item to Inventory";
        }
        else if (category.isExitSlot)
        {
            itemNameText.text = "Exit (No Changes)";
        }
        else
        {
            var currentItem = category.GetCurrentItem();
            if (currentItem != null && currentItem.itemPrefab != null)
            {
                Value valueComp = currentItem.itemPrefab.GetComponent<Value>();
                if (valueComp != null)
                {
                    itemNameText.text = valueComp.itemName;
                }
                else
                {
                    itemNameText.text = currentItem.itemPrefab.name;
                }
            }
            else
            {
                itemNameText.text = category.categoryName + " (Empty)";
            }
        }
    }

    void CycleSlotItem(bool forward)
    {
        if (inventoryManager != null && currentSelectedSlot >= 0)
        {
            var category = inventoryManager.GetCategory(currentSelectedSlot);

            // Don't cycle special slots
            if (category != null && !category.isAddSlot && !category.isExitSlot)
            {
                inventoryManager.CycleItemInCategory(currentSelectedSlot, forward);
                RefreshAllSlots();
            }
        }
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);
            errorMessageTimer = ERROR_DISPLAY_TIME;
            Debug.Log($"Error shown: {message}");
        }
    }

    public bool IsWheelOpen()
    {
        return isWheelOpen;
    }
}