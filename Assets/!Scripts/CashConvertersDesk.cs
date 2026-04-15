using System.Collections.Generic;
using UnityEngine;

public class CashConvertersDesk : MonoBehaviour
{
    [Header("References")]
    public Character playerCharacter;
    public GameObject sellActivationObject;

    [Header("Settings")]
    public string currencyStatName = "Money";

    private List<Value> itemsOnCounter = new List<Value>();
    private List<BackpackWorldObject> bagsOnCounter = new List<BackpackWorldObject>();

    private bool hasSold = false;

    private void OnTriggerEnter(Collider other)
    {
        // -----------------------------
        // NORMAL ITEMS
        // -----------------------------
        Value item = other.GetComponent<Value>();
        if (item != null && !itemsOnCounter.Contains(item))
        {
            itemsOnCounter.Add(item);
            Debug.Log("Added item: " + item.itemName);
            return;
        }

        // -----------------------------
        // BACKPACKS (STORE, DON'T SELL YET)
        // -----------------------------
        BackpackWorldObject bag = other.GetComponent<BackpackWorldObject>();
        if (bag != null && bag.storage != null && !bagsOnCounter.Contains(bag))
        {
            bagsOnCounter.Add(bag);
            Debug.Log("Backpack placed on counter");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Value item = other.GetComponent<Value>();
        if (item != null && itemsOnCounter.Contains(item))
        {
            itemsOnCounter.Remove(item);
        }

        BackpackWorldObject bag = other.GetComponent<BackpackWorldObject>();
        if (bag != null && bagsOnCounter.Contains(bag))
        {
            bagsOnCounter.Remove(bag);
        }
    }

    private void Update()
    {
        if (sellActivationObject != null && sellActivationObject.activeSelf && !hasSold)
        {
            SellAll();
            hasSold = true;

            sellActivationObject.SetActive(false);
        }

        if (sellActivationObject != null && !sellActivationObject.activeSelf)
        {
            hasSold = false;
        }
    }

    // -----------------------------
    // SELL EVERYTHING
    // -----------------------------
    private void SellAll()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("PlayerCharacter not assigned!");
            return;
        }

        float totalValue = 0f;

        // SELL NORMAL ITEMS
        foreach (var item in itemsOnCounter)
        {
            if (item == null) continue;

            totalValue += item.value;
            Destroy(item.gameObject);
        }

        itemsOnCounter.Clear();

        // SELL BACKPACK CONTENTS
        foreach (var bag in bagsOnCounter)
        {
            if (bag == null || bag.storage == null) continue;

            List<GameObject> items = bag.storage.storedItems;

            foreach (GameObject obj in items)
            {
                if (obj == null) continue;

                Value val = obj.GetComponent<Value>();
                if (val != null)
                {
                    totalValue += val.value;
                }

                Destroy(obj);
            }

            items.Clear();

            // Optional: destroy the bag itself after selling
            // Destroy(bag.gameObject);
        }

        bagsOnCounter.Clear();

        // GIVE MONEY
        if (totalValue > 0)
        {
            playerCharacter.ModifyStat(currencyStatName, totalValue);
            Debug.Log("Sold everything for: " + totalValue);
        }
    }
}