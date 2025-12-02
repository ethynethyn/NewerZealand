using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        [Tooltip("The prefab of the item")]
        public GameObject itemPrefab;

        [Tooltip("The sprite to show in the UI")]
        public Sprite itemIcon;

        [Tooltip("Which category this item belongs to (0-7)")]
        [Range(0, 7)]
        public int categoryIndex = 0;

        [Tooltip("Order within the category (lower = earlier in list)")]
        public int orderInCategory = 0;

        [Header("Runtime Data")]
        [Tooltip("Current quantity in inventory")]
        public int quantity = 0;

        [Tooltip("Pool of disabled instances")]
        [HideInInspector]
        public List<GameObject> storedInstances = new List<GameObject>();
    }

    [System.Serializable]
    public class CategorySlot
    {
        public string categoryName = "Category";

        [Tooltip("Is this the 'Add to Inventory' slot?")]
        public bool isAddSlot = false;

        [Tooltip("Is this the 'Exit/Cancel' slot?")]
        public bool isExitSlot = false;

        [HideInInspector]
        public List<InventoryItem> availableItems = new List<InventoryItem>();

        [HideInInspector]
        public int currentItemIndex = 0;

        public InventoryItem GetCurrentItem()
        {
            if (availableItems.Count == 0 || currentItemIndex < 0 || currentItemIndex >= availableItems.Count)
                return null;
            return availableItems[currentItemIndex];
        }

        public void CycleItem(bool forward)
        {
            if (availableItems.Count == 0)
                return;

            if (forward)
            {
                currentItemIndex = (currentItemIndex + 1) % availableItems.Count;
            }
            else
            {
                currentItemIndex--;
                if (currentItemIndex < 0)
                    currentItemIndex = availableItems.Count - 1;
            }

            Debug.Log($"Cycled to item index {currentItemIndex} in category");
        }
    }

    [Header("Item Database")]
    [Tooltip("All items that can exist in the inventory")]
    public List<InventoryItem> allItems = new List<InventoryItem>();

    [Header("Category Setup")]
    [Tooltip("8 categories for the weapon wheel slots")]
    public List<CategorySlot> categories = new List<CategorySlot>();

    [Header("References")]
    public PlayerPickUp playerPickUp;
    public Transform playerHoldPoint;

    private GameObject heldItemBeforeWheel = null;

    private void Start()
    {
        // Ensure we have exactly 8 categories
        while (categories.Count < 8)
        {
            categories.Add(new CategorySlot { categoryName = $"Slot {categories.Count + 1}" });
        }

        // Initial setup
        RefreshCategories();

        Debug.Log($"InventoryManager initialized with {allItems.Count} items in database");
    }

    /// <summary>
    /// Refresh which items appear in which categories based on quantity
    /// </summary>
    public void RefreshCategories()
    {
        // Clear all category lists
        foreach (var category in categories)
        {
            category.availableItems.Clear();
            category.currentItemIndex = 0;
        }

        // Add items with quantity > 0 to their categories
        foreach (var item in allItems)
        {
            if (item.quantity > 0 && item.categoryIndex >= 0 && item.categoryIndex < categories.Count)
            {
                categories[item.categoryIndex].availableItems.Add(item);
                Debug.Log($"Added {item.itemPrefab.name} (qty: {item.quantity}) to category {item.categoryIndex}");
            }
        }

        // Sort items within each category
        foreach (var category in categories)
        {
            category.availableItems = category.availableItems
                .OrderBy(item => item.orderInCategory)
                .ToList();
        }

        Debug.Log("Categories refreshed");
    }

    /// <summary>
    /// Try to add the currently held item to inventory
    /// </summary>
    public bool TryAddHeldItemToInventory(out string errorMessage)
    {
        errorMessage = "";

        if (playerPickUp == null)
        {
            errorMessage = "PlayerPickUp reference is missing";
            Debug.LogError(errorMessage);
            return false;
        }

        if (!playerPickUp.IsHoldingObject())
        {
            errorMessage = "Not holding any item";
            Debug.Log(errorMessage);
            return false;
        }

        GameObject heldObject = playerPickUp.GetHeldObject();
        InventoryItem matchingItem = FindItemInDatabase(heldObject);

        if (matchingItem == null)
        {
            errorMessage = "This item cannot be stored in inventory";
            Debug.Log($"{errorMessage}: {heldObject.name}");
            return false;
        }

        // Increase quantity
        matchingItem.quantity++;

        // Drop from player hands
        playerPickUp.ForceDropHeldObject();

        // Disable and store instance
        heldObject.SetActive(false);
        matchingItem.storedInstances.Add(heldObject);

        RefreshCategories();

        Debug.Log($"Successfully added {matchingItem.itemPrefab.name} to inventory. New quantity: {matchingItem.quantity}");
        return true;
    }

    /// <summary>
    /// Spawn an item from inventory to player's hands
    /// </summary>
    public void SpawnItemToHands(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count)
        {
            Debug.LogWarning($"Invalid category index: {categoryIndex}");
            return;
        }

        CategorySlot category = categories[categoryIndex];

        // Handle exit slot
        if (category.isExitSlot)
        {
            if (heldItemBeforeWheel != null)
            {
                playerPickUp.ForcePickUpObject(heldItemBeforeWheel);
                heldItemBeforeWheel = null;
                Debug.Log("Restored held item");
            }
            return;
        }

        InventoryItem selectedItem = category.GetCurrentItem();
        if (selectedItem == null || selectedItem.quantity <= 0)
        {
            Debug.Log("No item to spawn or quantity is 0");
            return;
        }

        // Drop current item if holding one
        if (playerPickUp.IsHoldingObject())
        {
            playerPickUp.DropInFrontOfPlayer();
        }

        // Decrease quantity
        selectedItem.quantity--;

        GameObject itemToSpawn = null;

        // Try to reuse stored instance
        if (selectedItem.storedInstances.Count > 0)
        {
            itemToSpawn = selectedItem.storedInstances[0];
            selectedItem.storedInstances.RemoveAt(0);

            itemToSpawn.transform.position = playerHoldPoint.position;
            itemToSpawn.transform.rotation = Quaternion.identity;
            itemToSpawn.SetActive(true);

            Debug.Log($"Reused stored instance of {selectedItem.itemPrefab.name}");
        }
        else
        {
            itemToSpawn = Instantiate(selectedItem.itemPrefab, playerHoldPoint.position, Quaternion.identity);
            Debug.Log($"Instantiated new {selectedItem.itemPrefab.name}");
        }

        // Force pickup
        playerPickUp.ForcePickUpObject(itemToSpawn);

        RefreshCategories();

        Debug.Log($"Spawned {selectedItem.itemPrefab.name}. Remaining quantity: {selectedItem.quantity}");
    }

    /// <summary>
    /// Find an item in the database by comparing names
    /// </summary>
    InventoryItem FindItemInDatabase(GameObject obj)
    {
        string cleanedName = obj.name.Replace("(Clone)", "").Trim();

        foreach (var item in allItems)
        {
            if (item.itemPrefab != null && item.itemPrefab.name == cleanedName)
            {
                Debug.Log($"Found match: {cleanedName} in database");
                return item;
            }
        }

        Debug.Log($"No match found for: {cleanedName}");
        return null;
    }

    /// <summary>
    /// Store reference to held item before opening wheel
    /// </summary>
    public void StoreHeldItem()
    {
        if (playerPickUp != null && playerPickUp.IsHoldingObject())
        {
            heldItemBeforeWheel = playerPickUp.GetHeldObject();
            Debug.Log($"Stored held item: {heldItemBeforeWheel.name}");
        }
        else
        {
            heldItemBeforeWheel = null;
            Debug.Log("No item held to store");
        }
    }

    /// <summary>
    /// Called when add slot is selected
    /// </summary>
    public void OnAddSlotSelected()
    {
        heldItemBeforeWheel = null;
        Debug.Log("Add slot selected - cleared stored item");
    }

    // Public getters
    public CategorySlot GetCategory(int index)
    {
        if (index < 0 || index >= categories.Count)
            return null;
        return categories[index];
    }

    public InventoryItem GetCurrentItemInCategory(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count)
            return null;
        return categories[categoryIndex].GetCurrentItem();
    }

    public void CycleItemInCategory(int categoryIndex, bool forward)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count)
            return;
        categories[categoryIndex].CycleItem(forward);
    }
}