using System.Collections.Generic;
using UnityEngine;

public class CashConvertersDesk : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject with the Character script here.")]
    public Character playerCharacter;

    [Tooltip("The object that activates selling.")]
    public GameObject sellActivationObject;

    [Header("Settings")]
    [Tooltip("The stat to increase when selling items.")]
    public string currencyStatName = "Money";

    private List<Value> itemsOnCounter = new List<Value>();
    private bool hasSold = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detect any object with a Value component
        Value item = other.GetComponent<Value>();
        if (item != null && !itemsOnCounter.Contains(item))
        {
            itemsOnCounter.Add(item);
            Debug.Log($"Added item to counter: {item.itemName} (worth {item.value})");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Value item = other.GetComponent<Value>();
        if (item != null && itemsOnCounter.Contains(item))
        {
            itemsOnCounter.Remove(item);
            Debug.Log($"Removed item from counter: {item.itemName}");
        }
    }

    private void Update()
    {
        if (sellActivationObject != null && sellActivationObject.activeSelf && !hasSold)
        {
            SellItems();
            hasSold = true;

            // Deactivate the sell activation object after sale
            sellActivationObject.SetActive(false);
        }

        if (sellActivationObject != null && !sellActivationObject.activeSelf)
        {
            hasSold = false;
        }
    }

    private void SellItems()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("PawnShop: Player Character not assigned in Inspector!");
            return;
        }

        float totalValue = 0f;
        foreach (var item in itemsOnCounter)
        {
            totalValue += item.value;
            Destroy(item.gameObject);
        }

        itemsOnCounter.Clear();

        if (totalValue > 0)
        {
            playerCharacter.ModifyStat(currencyStatName, totalValue);
            Debug.Log($"Sold items for {totalValue}. Added to {playerCharacter.characterName}'s {currencyStatName} stat.");
        }
    }
}
