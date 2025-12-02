using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using StarterAssets;

public class WeaponWheelUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup weaponWheelCanvas;
    public RectTransform wheelCenter;
    public InventoryManager inventoryManager;
    public StarterAssetsInputs starterAssetsInputs;

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
    private int currentSelectedSlot = -1;
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

        // Open/Close wheel
        if (Keyboard.current != null && Keyboard.current[openWheelKey].isPressed)
        {
            if (!isWheelOpen)
            {
                OpenWheel();
            }
            UpdateWheelSelection();
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

    void OpenWheel()
    {
        isWheelOpen = true;

        if (weaponWheelCanvas != null)
        {
            weaponWheelCanvas.alpha = 1f;
            weaponWheelCanvas.interactable = true;
            weaponWheelCanvas.blocksRaycasts = true;
        }

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

        // Keep cursor hidden and locked to center
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        // Update all slot visuals
        RefreshAllSlots();

        Debug.Log("Weapon wheel opened");
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
        }

        // Lock cursor back
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reset selection
        currentSelectedSlot = -1;

        // Hide all hover objects
        foreach (var hoverObj in slotHoverObjects)
        {
            if (hoverObj != null)
                hoverObj.SetActive(false);
        }

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
            if (currentSelectedSlot != -1)
            {
                currentSelectedSlot = -1;
                RefreshAllSlots();
            }
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

        // Update icon and quantity
        if (category.isAddSlot)
        {
            // Show ADD text
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
            // Show EXIT text
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
            // Show item icon and quantity
            var currentItem = category.GetCurrentItem();

            if (slotIndex < slotIcons.Count && slotIcons[slotIndex] != null)
            {
                if (currentItem != null && currentItem.itemIcon != null)
                {
                    slotIcons[slotIndex].sprite = currentItem.itemIcon;
                    slotIcons[slotIndex].enabled = true;
                    slotIcons[slotIndex].color = Color.white; // Ensure icon is visible
                }
                else
                {
                    slotIcons[slotIndex].enabled = false;
                }
            }

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