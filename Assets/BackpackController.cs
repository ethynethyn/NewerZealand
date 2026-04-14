using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class BackpackController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public PlayerPickUp playerPickup;

    [Header("UI")]
    public TextMeshProUGUI backpackUI;

    [Header("Equipped UI")]
    public GameObject backpackIconUI;

    [Header("Settings")]
    public float interactRange = 3f;
    public float itemCooldown = 3f;

    [Header("Bag Full Feedback")]
    public float bagFullMessageDuration = 1f;

    private BackpackWorldObject currentBackpack;
    private BackpackItemStorage currentStorage;

    private bool isEquipped;
    private int selectedIndex;

    private float lastItemActionTime = -999f;

    private float bagFullTimer;
    private bool showingBagFull;

    public bool IsEquipped => isEquipped;

    void Update()
    {
        HandleLookAtBackpack();
        HandleToggleBackpack();
        HandleScroll();
        HandleUseItem();
        HandleBagFullMessage();
    }

    bool TryConsumeActionSlot()
    {
        if (Time.time < lastItemActionTime + itemCooldown)
            return false;

        lastItemActionTime = Time.time;
        return true;
    }

    void HandleLookAtBackpack()
    {
        if (isEquipped)
        {
            if (!showingBagFull && backpackUI)
                backpackUI.gameObject.SetActive(false);
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            BackpackWorldObject bag = hit.collider.GetComponentInParent<BackpackWorldObject>();

            if (bag != null)
            {
                currentBackpack = bag;
                currentStorage = bag.storage;

                if (!showingBagFull && backpackUI)
                {
                    backpackUI.gameObject.SetActive(true);
                    UpdateUI();
                }
                return;
            }
        }

        currentBackpack = null;
        currentStorage = null;

        if (!showingBagFull && backpackUI)
            backpackUI.gameObject.SetActive(false);
    }

    void HandleToggleBackpack()
    {
        if (!Keyboard.current.bKey.wasPressedThisFrame) return;

        if (isEquipped)
            DropBackpack();
        else
            TryEquipBackpack();
    }

    void TryEquipBackpack()
    {
        if (isEquipped) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            BackpackWorldObject bag = hit.collider.GetComponentInParent<BackpackWorldObject>();
            if (bag == null) return;

            currentBackpack = bag;
            currentStorage = bag.storage;

            bag.gameObject.SetActive(false);

            isEquipped = true;
            selectedIndex = 0;

            if (backpackIconUI)
                backpackIconUI.SetActive(true);

            if (backpackUI)
            {
                backpackUI.gameObject.SetActive(true);
                UpdateUI();
            }
        }
    }

    void DropBackpack()
    {
        if (currentBackpack == null) return;

        currentBackpack.gameObject.SetActive(true);

        Vector3 dropPosition = playerCamera.position + playerCamera.forward * 0.8f;
        currentBackpack.transform.position = dropPosition;

        currentBackpack.transform.rotation = Quaternion.Euler(-90f, -90f, 0f);

        Rigidbody rb = currentBackpack.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.AddForce(playerCamera.forward * 2f, ForceMode.Impulse);
        }

        if (backpackIconUI)
            backpackIconUI.SetActive(false);

        if (backpackUI)
            backpackUI.gameObject.SetActive(false);

        currentBackpack = null;
        currentStorage = null;
        isEquipped = false;
    }

    void HandleScroll()
    {
        if (currentStorage == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0.1f) selectedIndex--;
        if (scroll < -0.1f) selectedIndex++;

        selectedIndex = Mathf.Clamp(selectedIndex, 0,
            Mathf.Max(0, currentStorage.storedItems.Count - 1));

        UpdateUI();
    }

    void HandleUseItem()
    {
        // NEW: block usage while equipped
        if (isEquipped) return;

        if (currentStorage == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (currentStorage.storedItems.Count == 0) return;

        if (!TryConsumeActionSlot()) return;

        GameObject item = currentStorage.RemoveItem(selectedIndex);
        if (item == null) return;

        if (currentBackpack != null)
            currentBackpack.NotifyItemRemoved();

        item.transform.SetParent(null);

        if (currentBackpack != null)
            item.transform.localScale = currentBackpack.GetAndClearScale(item);
        else
            item.transform.localScale = Vector3.one;

        item.transform.position = playerPickup.holdPoint.position;
        item.transform.rotation = Quaternion.identity;

        item.SetActive(true);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerPickup.ForcePickUpObject(item);

        selectedIndex = Mathf.Clamp(selectedIndex, 0,
            Mathf.Max(0, currentStorage.storedItems.Count - 1));

        UpdateUI();
    }

    public void ShowBagFullMessage()
    {
        if (backpackUI == null) return;

        showingBagFull = true;
        bagFullTimer = Time.time + bagFullMessageDuration;

        backpackUI.gameObject.SetActive(true);
        backpackUI.text = "<color=red>Bag Full!</color>";
    }

    void HandleBagFullMessage()
    {
        if (!showingBagFull) return;

        if (Time.time >= bagFullTimer)
        {
            showingBagFull = false;

            if (!isEquipped)
                backpackUI.gameObject.SetActive(false);
            else
                UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (backpackUI == null || currentStorage == null) return;

        List<GameObject> items = currentStorage.storedItems;

        int current = items.Count;
        int max = currentStorage.maxCapacity;

        if (current == 0)
        {
            backpackUI.text = "<color=yellow>Backpack (" + current + "/" + max + ")</color>\n(empty)";
            return;
        }

        string text = "<color=yellow>Backpack (" + current + "/" + max + ")</color>\n\n";

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;

            if (i == selectedIndex)
                text += "<color=yellow>> " + items[i].name + "</color>\n";
            else
                text += "  " + items[i].name + "\n";
        }

        backpackUI.text = text;
    }
}