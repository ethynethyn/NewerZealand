using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class LootVendorController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public PlayerPickUp playerPickup;

    [Header("UI")]
    public TextMeshProUGUI lootUI;

    [Header("Settings")]
    public float interactRange = 3f;
    public float itemCooldown = 0.25f;

    [Header("Feedback")]
    public float messageDuration = 1f;

    private LootAndVendor currentContainer;

    private int selectedIndex = 0;
    private float lastActionTime = -999f;

    private bool showingMessage = false;
    private bool messageActive = false;
    private float messageTimer = 0f;

    void Update()
    {
        HandleLook();
        HandleScroll();
        HandleUse();
        HandleMessageTimer();
    }

    // -----------------------------
    // LOOK DETECTION
    // -----------------------------
    void HandleLook()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            LootAndVendor lav = hit.collider.GetComponentInParent<LootAndVendor>();

            if (lav != null)
            {
                currentContainer = lav;

                if (!messageActive && lootUI)
                {
                    lootUI.gameObject.SetActive(true);
                    UpdateUI();
                }
                return;
            }
        }

        currentContainer = null;

        if (!messageActive && lootUI)
            lootUI.gameObject.SetActive(false);
    }

    // -----------------------------
    // SCROLL
    // -----------------------------
    void HandleScroll()
    {
        if (currentContainer == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0.1f) selectedIndex--;
        if (scroll < -0.1f) selectedIndex++;

        selectedIndex = Mathf.Clamp(selectedIndex, 0, currentContainer.lootTable.Count - 1);

        UpdateUI();
    }

    // -----------------------------
    // USE / BUY ITEM
    // -----------------------------
    void HandleUse()
    {
        if (currentContainer == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        if (Time.time < lastActionTime + itemCooldown)
            return;

        lastActionTime = Time.time;

        GameObject item = currentContainer.TakeItem(selectedIndex);

        if (item == null)
        {
            ShowMessage("<color=red>Not enough money!</color>");
            return;
        }

        // Give to player
        item.transform.position = playerPickup.holdPoint.position;
        item.transform.rotation = Quaternion.identity;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        playerPickup.ForcePickUpObject(item);
    }

    // -----------------------------
    // UI
    // -----------------------------
    void UpdateUI()
    {
        if (messageActive) return;
        if (lootUI == null || currentContainer == null) return;

        List<LootAndVendorEntry> items = currentContainer.lootTable;

        if (items.Count == 0)
        {
            lootUI.text = "<color=yellow>(empty)</color>";
            return;
        }

        string text = "<color=yellow>Vendor</color>\n\n";

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].prefab == null) continue;

            string price = items[i].cost > 0 ? " ($" + items[i].cost + ")" : "";

            if (i == selectedIndex)
                text += "<color=yellow>> " + items[i].prefab.name + price + "</color>\n";
            else
                text += "  " + items[i].prefab.name + price + "\n";
        }

        lootUI.text = text;
    }

    // -----------------------------
    // MESSAGE SYSTEM
    // -----------------------------
    void ShowMessage(string msg)
    {
        if (lootUI == null) return;

        messageActive = true;
        showingMessage = true;
        messageTimer = Time.time + messageDuration;

        lootUI.gameObject.SetActive(true);
        lootUI.text = msg;
    }

    void HandleMessageTimer()
    {
        if (!showingMessage) return;

        if (Time.time >= messageTimer)
        {
            showingMessage = false;
            messageActive = false;

            if (currentContainer == null)
                lootUI.gameObject.SetActive(false);
            else
                UpdateUI();
        }
    }
}