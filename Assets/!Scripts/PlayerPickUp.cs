using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

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
    public float moveForce = 600f; // Increased default for faster snap
    public float inspectRotateSpeed = 2f; // Lowered for more precise control

    [Header("Inspect Lighting")]
    public GameObject inspectLightObject; // Assign your light GameObject here

    private GameObject heldObject;
    private Rigidbody heldRB;
    private bool isInspecting = false;

    private void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer);

        // Show pickup UI if looking at object & not holding anything
        if (pickupUI != null)
            pickupUI.SetActive(hitSomething && heldObject == null);

        // Pickup with E
        if (hitSomething && Keyboard.current.eKey.wasPressedThisFrame && heldObject == null)
        {
            PickUp(hit.collider.gameObject);
        }

        // Toggle inspect with right-click
        if (heldObject && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleInspect();
        }

        // Throw with left-click (only outside inspect mode)
        if (heldObject && Mouse.current.leftButton.wasPressedThisFrame && !isInspecting)
        {
            Throw();
        }

        // Rotate while inspecting
        if (isInspecting)
        {
            RotateInspectedObject();
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

        heldRB.useGravity = false;
        heldRB.linearDamping = 10f;
        heldRB.angularDamping = 10f;
        heldRB.constraints = RigidbodyConstraints.FreezeRotation;
        heldRB.transform.parent = holdPoint;
    }

    void Throw()
    {
        Debug.Log("Throwing object.");

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
            Debug.Log("Inspecting object.");
            heldRB.transform.position = inspectPoint.position;
            // Keep current rotation so rotation stays consistent
            // Reset velocity so it doesn't move
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
            Debug.Log("Exiting inspect mode.");

            if (inspectLightObject != null)
                inspectLightObject.SetActive(false);

            if (starterAssetsInputs != null)
            {
                starterAssetsInputs.cursorInputForLook = true;
            }
        }
    }

    void RotateInspectedObject()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        heldObject.transform.Rotate(Camera.main.transform.up, -mouseDelta.x * inspectRotateSpeed, Space.World);
        heldObject.transform.Rotate(Camera.main.transform.right, mouseDelta.y * inspectRotateSpeed, Space.World);
    }
}
