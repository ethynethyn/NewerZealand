using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem.Controls;

public class PlayerPickUp : MonoBehaviour
{
    [Header("References")]
    public Transform holdPoint;
    public Transform inspectPoint;
    public GameObject pickupUI;
    public LayerMask pickupLayer;
    public StarterAssetsInputs starterAssetsInputs;
    public FirstPersonController firstPersonController;

    [Header("Pickup Settings")]
    public float pickupRange = 3f;
    public float moveForce = 600f;
    public float inspectRotateSpeed = 2f;

    [Tooltip("Delay in seconds before you can drop/throw after picking up an item")]
    public float dropDelay = 1f;

    [Header("Input Bindings")]
    [Tooltip("Key to pick up objects")]
    public Key pickupKey = Key.E;

    [Tooltip("Key to throw/drop objects (can use mouse buttons)")]
    public Key throwKey = Key.None;

    [Tooltip("Use Mouse0 (Left Click) for throw if true")]
    public bool useMouseLeftForThrow = true;

    [Tooltip("Key to toggle inspect mode (can use mouse buttons)")]
    public Key inspectKey = Key.None;

    [Tooltip("Use Mouse1 (Right Click) for inspect if true")]
    public bool useMouseRightForInspect = true;

    [Header("Inspect Lighting")]
    public GameObject inspectLightObject;

    [Header("Tooltip UI")]
    public TextMeshProUGUI tooltipUI;
    public Vector3 tooltipOffset = new Vector3(0f, 1.5f, 0f);
    public float tooltipFollowSpeed = 5f;

    private GameObject heldObject;
    private Rigidbody heldRB;
    private PickupableItem heldPickupableItem;
    private bool isInspecting = false;
    private float lastPickupTime = -999f;

    private GameObject currentTooltipTarget;
    private Weight currentWeight;
    private Value currentValue;
    private Coroutine tooHeavyRoutine;
    private bool showingTooHeavy = false;

    private void Awake()
    {
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);   
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer);

        bool canPickup = false;
        GameObject targetObject = null;
        Weight targetWeight = null;
        Value targetValue = null;

        if (hitSomething)
        {
            Vector3 direction = (hit.collider.transform.position - Camera.main.transform.position).normalized;
            float distance = Vector3.Distance(Camera.main.transform.position, hit.collider.transform.position);
            int exceptionLayer = LayerMask.NameToLayer("RaycastCollisionException");
            int layerMask = ~pickupLayer & ~(1 << exceptionLayer);
            canPickup = !Physics.Raycast(Camera.main.transform.position, direction, distance, layerMask);

            targetObject = hit.collider.gameObject;
            if (targetObject != null)
            {
                targetWeight = targetObject.GetComponent<Weight>();
                targetValue = targetObject.GetComponent<Value>();
            }
        }

        // Show pickup UI
        if (pickupUI != null)
            pickupUI.SetActive(canPickup && heldObject == null);

        // Assign tooltip target if looking at object
        if (canPickup && heldObject == null && targetObject != null)
        {
            currentTooltipTarget = targetObject;
            currentWeight = targetWeight;
            currentValue = targetValue;

            if (tooltipUI != null && !tooltipUI.gameObject.activeSelf)
                tooltipUI.gameObject.SetActive(true);

            if (!showingTooHeavy)
                UpdateTooltipText();
        }
        else if (currentTooltipTarget == null || heldObject != null || !canPickup)
        {
            currentTooltipTarget = null;
            tooltipUI.gameObject.SetActive(false);
        }

        // Pickup with configurable key
        if (canPickup && Keyboard.current[pickupKey].wasPressedThisFrame && heldObject == null && targetObject != null)
        {
            TryPickUp(targetObject);
        }


        // Toggle inspect with configurable input
        if (heldObject && GetKeyPressed(inspectKey, useMouseRightForInspect, Mouse.current.rightButton))
        {
            ToggleInspect();
        }

        // Drop/Throw with configurable input (only if enough time has passed since pickup)
        if (heldObject && Time.time >= lastPickupTime + dropDelay && GetKeyPressed(throwKey, useMouseLeftForThrow, Mouse.current.leftButton))
        {
            if (isInspecting)
            {
                Drop(); // Drop gently when inspecting
            }
            else
            {
                Throw(); // Throw when not inspecting
            }
        }

        // Rotate while inspecting
        if (isInspecting)
        {
            RotateInspectedObject();
        }

        // Smoothly move tooltip toward object
        if (tooltipUI.gameObject.activeSelf && currentTooltipTarget != null)
        {
            Vector3 targetWorldPos = currentTooltipTarget.transform.position + tooltipOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
            tooltipUI.transform.position = Vector3.Lerp(tooltipUI.transform.position, screenPos, Time.deltaTime * tooltipFollowSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (heldObject && !isInspecting)
        {
            Vector3 targetPos = holdPoint.position;
            Vector3 direction = (targetPos - heldObject.transform.position);
            heldRB.linearVelocity = Vector3.zero;
            heldRB.AddForce(direction * moveForce);
        }
    }

    // Helper method to check both keyboard and mouse input
    bool GetKeyPressed(Key key, bool useMouseButton, ButtonControl mouseButton)
    {
        bool keyPressed = false;

        // Check keyboard key if not None
        if (key != Key.None && Keyboard.current != null)
        {
            keyPressed = Keyboard.current[key].wasPressedThisFrame;
        }

        // Check mouse button if enabled
        if (useMouseButton && Mouse.current != null)
        {
            keyPressed = keyPressed || mouseButton.wasPressedThisFrame;
        }

        return keyPressed;
    }

    public void TryPickUp(GameObject obj)
    {
        Weight weight = obj.GetComponent<Weight>();
        if (weight != null && !weight.TryPickupCheck())
        {
            if (tooHeavyRoutine != null)
                StopCoroutine(tooHeavyRoutine);
            tooHeavyRoutine = StartCoroutine(ShowTooHeavy());
            return;
        }

        PickUp(obj);
    }

    IEnumerator ShowTooHeavy()
    {
        showingTooHeavy = true;
        tooltipUI.color = Color.red;
        tooltipUI.text = "TOO HEAVY";
        yield return new WaitForSeconds(2f);
        showingTooHeavy = false;
        tooltipUI.color = Color.white;
        UpdateTooltipText();
    }

  

    public void PickUp(GameObject obj)
    {
        Debug.Log("Picked up: " + obj.name);

        heldObject = obj;
        heldRB = obj.GetComponent<Rigidbody>();
        heldPickupableItem = obj.GetComponent<PickupableItem>();
        lastPickupTime = Time.time; // Record pickup time

        if (heldRB == null)
        {
            Debug.LogWarning("No Rigidbody on object!");
            return;
        }

        FreezableObject freezable = obj.GetComponent<FreezableObject>();
        if (freezable != null && freezable.IsFrozen())
        {
            freezable.Unfreeze();
        }

        heldRB.useGravity = false;
        heldRB.linearDamping = 10f;
        heldRB.angularDamping = 10f;
        heldRB.constraints = RigidbodyConstraints.FreezeRotation;
        heldRB.transform.parent = holdPoint;

        // Notify PickupableItem component if it exists
        if (heldPickupableItem != null)
        {
            heldPickupableItem.OnPickedUp(this);
        }

        // Hide tooltip while holding
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    void Drop()
    {
        // Notify PickupableItem component before dropping
        if (heldPickupableItem != null)
        {
            heldPickupableItem.OnDropped();
        }

        // Gentle drop (for inspect mode)
        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;
        // No force applied - just drops straight down

        heldObject = null;
        heldRB = null;
        heldPickupableItem = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
    }

    void Throw()
    {
        // Notify PickupableItem component before throwing
        if (heldPickupableItem != null)
        {
            heldPickupableItem.OnDropped();
        }

        // Throw with force (for normal holding)
        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;
        heldRB.AddForce(Camera.main.transform.forward * 10f, ForceMode.Impulse);

        heldObject = null;
        heldRB = null;
        heldPickupableItem = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
    }

    /// <summary>
    /// Force drops the held object without throwing - used by PickupableItem when consumed
    /// </summary>
    public void ForceDropHeldObject()
    {
        if (heldObject == null)
            return;

        // Don't notify PickupableItem since it's the one calling this
        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;

        heldObject = null;
        heldRB = null;
        heldPickupableItem = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
    }

    /// <summary>
    /// Force pickup an object - used by inventory system
    /// </summary>
    public void ForcePickUpObject(GameObject obj)
    {
        if (obj == null)
            return;

        // Drop current item if holding one
        if (heldObject != null)
        {
            ForceDropHeldObject();
        }

        PickUp(obj);
    }

    /// <summary>
    /// Drop item in front of player - used by inventory system
    /// </summary>
    public void DropInFrontOfPlayer()
    {
        if (heldObject == null)
            return;

        // Notify PickupableItem component before dropping
        if (heldPickupableItem != null)
        {
            heldPickupableItem.OnDropped();
        }

        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;

        // Position in front of player
        heldRB.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;

        heldObject = null;
        heldRB = null;
        heldPickupableItem = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
    }

    /// <summary>
    /// Check if player is holding an object
    /// </summary>
    public bool IsHoldingObject()
    {
        return heldObject != null;
    }

    /// <summary>
    /// Get the currently held object
    /// </summary>
    public GameObject GetHeldObject()
    {
        return heldObject;
    }

    void ToggleInspect()
    {
        isInspecting = !isInspecting;

        if (isInspecting)
        {
            heldRB.transform.position = inspectPoint.position;
            heldRB.linearVelocity = Vector3.zero;
            heldRB.angularVelocity = Vector3.zero;

            if (inspectLightObject != null)
                inspectLightObject.SetActive(true);

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.look = Vector2.zero;
                starterAssetsInputs.cursorLocked = true;
                starterAssetsInputs.cursorInputForLook = false;
            }
        }
        else
        {
            if (inspectLightObject != null)
                inspectLightObject.SetActive(false);

            if (starterAssetsInputs != null)
                starterAssetsInputs.cursorInputForLook = true;
        }
    }

    void RotateInspectedObject()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        heldObject.transform.Rotate(Camera.main.transform.up, -mouseDelta.x * inspectRotateSpeed, Space.World);
        heldObject.transform.Rotate(Camera.main.transform.right, mouseDelta.y * inspectRotateSpeed, Space.World);
    }

    void UpdateTooltipText()
    {
        if (currentTooltipTarget == null || tooltipUI == null)
            return;

        // Use itemName from Value component if available, otherwise fall back to GameObject name
        string nameText = currentTooltipTarget.name; // Default fallback
        if (currentValue != null)
        {
            nameText = currentValue.itemName;
        }

        string weightText = currentWeight != null ? currentWeight.GetTotalWeight().ToString("0.00") + "kg" : "N/A";
        string valueText = currentValue != null ? currentValue.GetTotalValue().ToString("0.00") : "N/A";

        tooltipUI.text = $"{nameText}\nWeight: {weightText}\nValue: ${valueText}";
        tooltipUI.color = Color.white;
    }
}