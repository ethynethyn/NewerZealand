using UnityEngine;
using System.Collections.Generic;

public class BackpackWorldObject : MonoBehaviour
{
    public BackpackItemStorage storage;
    public BackpackController backpackController;
    public PlayerPickUp playerPickup;
    public float addCooldown = 3f;

    [HideInInspector] public bool hasBeenSold = false;
    private float lastAddTime = -999f;
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private bool wasFullLastFrame = false;

    public Vector3 GetAndClearScale(GameObject item)
    {
        if (originalScales.TryGetValue(item, out Vector3 scale))
        {
            originalScales.Remove(item);
            return scale;
        }
        return Vector3.one;
    }

    public void NotifyItemRemoved()
    {
        lastAddTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (storage == null) return;

        if (storage.IsFull())
        {
            if (backpackController != null)
                backpackController.ShowBagFullMessage();
            return;
        }

        PickupableItem pickup = other.GetComponent<PickupableItem>();
        if (pickup == null) return;

        if (backpackController != null && backpackController.IsEquipped) return;
        if (Time.time < lastAddTime + addCooldown) return;
        if (storage.storedItems.Contains(other.gameObject)) return;

        OriginalScale originalScale = other.GetComponent<OriginalScale>();
        if (originalScale != null)
            originalScales[other.gameObject] = originalScale.scale;
        else
            originalScales[other.gameObject] = other.transform.lossyScale;

        lastAddTime = Time.time;
        storage.AddItem(other.gameObject);

        // Clear the player's held state immediately so no extra E press is needed
        if (playerPickup != null && playerPickup.GetHeldObject() == other.gameObject)
            playerPickup.ForceDropHeldObject();

        if (storage.IsFull() && !wasFullLastFrame)
        {
            if (backpackController != null)
                backpackController.ShowBagFullMessage();
            wasFullLastFrame = true;
        }
    }

    void Update()
    {
        if (storage == null) return;
        if (!storage.IsFull())
            wasFullLastFrame = false;
    }
}