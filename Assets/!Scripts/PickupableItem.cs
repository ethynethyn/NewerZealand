using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PickupableItem : MonoBehaviour
{
    [Header("Item Type")]
    [Tooltip("Consumable: Item is deleted after use. Reusable: Item goes on cooldown and can be used again.")]
    public ItemType itemType = ItemType.Consumable;

    [Header("Visual Objects - Idle State")]
    [Tooltip("Objects active when item is held but not being used")]
    public List<GameObject> idleObjects = new List<GameObject>();

    [Header("Visual Objects - Interaction State")]
    [Tooltip("Objects active when 'E' is pressed and item is being used")]
    public List<GameObject> interactionObjects = new List<GameObject>();

    [Header("Interaction Settings")]
    [Tooltip("How long the interaction lasts when left click is pressed (in seconds)")]
    public float interactionDuration = 2f;

    [Header("Reusable Item Settings")]
    [Tooltip("Only applies if itemType is Reusable - time before item can be used again")]
    public float cooldownDuration = 5f;

    // Internal state
    private bool isHeld = false;
    private bool isInteracting = false;
    private bool isOnCooldown = false;
    private PlayerPickUp playerPickUp;

    public enum ItemType
    {
        Consumable,  // Deleted after use
        Reusable     // Goes on cooldown, can be used again
    }

    private void Start()
    {
        // Make sure all objects start deactivated
        SetObjectsActive(idleObjects, false);
        SetObjectsActive(interactionObjects, false);
    }

    private void Update()
    {
        // Only allow interaction if held and not already interacting/on cooldown
        if (isHeld && !isInteracting && !isOnCooldown && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(UseItem());
        }
    }

    /// <summary>
    /// Called by PlayerPickUp when this object is picked up
    /// </summary>
    public void OnPickedUp(PlayerPickUp pickup)
    {
        isHeld = true;
        playerPickUp = pickup;

        // Activate idle objects
        SetObjectsActive(idleObjects, true);
        SetObjectsActive(interactionObjects, false);

        Debug.Log($"{gameObject.name} picked up - Idle objects activated");
    }

    /// <summary>
    /// Called by PlayerPickUp when this object is dropped/thrown
    /// </summary>
    public void OnDropped()
    {
        isHeld = false;
        playerPickUp = null;

        // Deactivate all objects when dropped
        SetObjectsActive(idleObjects, false);
        SetObjectsActive(interactionObjects, false);

        // Stop any ongoing interactions
        StopAllCoroutines();
        isInteracting = false;

        Debug.Log($"{gameObject.name} dropped - All objects deactivated");
    }

    IEnumerator UseItem()
    {
        isInteracting = true;

        // Switch from idle to interaction objects
        SetObjectsActive(idleObjects, false);
        SetObjectsActive(interactionObjects, true);

        Debug.Log($"{gameObject.name} - Interaction started for {interactionDuration} seconds");

        // Wait for interaction duration
        yield return new WaitForSeconds(interactionDuration);

        isInteracting = false;

        // Handle based on item type
        if (itemType == ItemType.Consumable)
        {
            Debug.Log($"{gameObject.name} - Consumable used, destroying object");

            // Force player to drop/release the item before destroying
            if (playerPickUp != null)
            {
                playerPickUp.ForceDropHeldObject();
            }

            Destroy(gameObject);
        }
        else // Reusable
        {
            Debug.Log($"{gameObject.name} - Starting cooldown for {cooldownDuration} seconds");

            // Return to idle state
            SetObjectsActive(interactionObjects, false);
            SetObjectsActive(idleObjects, true);

            // Start cooldown
            StartCoroutine(CooldownTimer());
        }
    }

    IEnumerator CooldownTimer()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;

        Debug.Log($"{gameObject.name} - Cooldown finished, ready to use again");
    }

    void SetObjectsActive(List<GameObject> objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    // Public getter for other scripts to check cooldown status
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    public bool IsInteracting()
    {
        return isInteracting;
    }

    private void OnDestroy()
    {
        // Clean up if destroyed while held
        if (playerPickUp != null && isHeld)
        {
            playerPickUp.ForceDropHeldObject();
        }
    }
}