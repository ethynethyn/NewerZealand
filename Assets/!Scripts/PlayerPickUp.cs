using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using TMPro;
using System.Collections;

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

    [Header("Inspect Lighting")]
    public GameObject inspectLightObject;

    [Header("Tooltip UI")]
    public TextMeshProUGUI tooltipUI;
    public Vector3 tooltipOffset = new Vector3(0f, 1.5f, 0f);
    public float tooltipFollowSpeed = 5f;

    private GameObject heldObject;
    private Rigidbody heldRB;
    private bool isInspecting = false;

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

        // Pickup with E
        if (canPickup && Keyboard.current.eKey.wasPressedThisFrame && heldObject == null && targetObject != null)
        {
            TryPickUp(targetObject);
        }

        // Toggle inspect with right-click
        if (heldObject && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleInspect();
        }

        // Drop/Throw with left-click (works both in inspect mode and normal holding)
        if (heldObject && Mouse.current.leftButton.wasPressedThisFrame)
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

    void TryPickUp(GameObject obj)
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

    void PickUp(GameObject obj)
    {
        Debug.Log("Picked up: " + obj.name);

        heldObject = obj;
        heldRB = obj.GetComponent<Rigidbody>();

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

        // Hide tooltip while holding
        if (tooltipUI != null)
            tooltipUI.gameObject.SetActive(false);
    }

    void Drop()
    {
        // Gentle drop (for inspect mode)
        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;
        // No force applied - just drops straight down

        heldObject = null;
        heldRB = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
    }

    void Throw()
    {
        // Throw with force (for normal holding)
        heldRB.transform.parent = null;
        heldRB.useGravity = true;
        heldRB.linearDamping = 0f;
        heldRB.angularDamping = 0.05f;
        heldRB.constraints = RigidbodyConstraints.None;
        heldRB.AddForce(Camera.main.transform.forward * 10f, ForceMode.Impulse);

        heldObject = null;
        heldRB = null;
        isInspecting = false;

        if (starterAssetsInputs != null)
            starterAssetsInputs.cursorInputForLook = true;

        if (inspectLightObject != null)
            inspectLightObject.SetActive(false);
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