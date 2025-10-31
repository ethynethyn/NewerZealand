using UnityEngine;
using Ashsvp;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine.InputSystem;

public class VehicleController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private string vehicleTag = "Vehicle";

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private MonoBehaviour playerInput;

    [Header("Seat Mount Settings")]
    [Tooltip("Name of the seat transform inside the vehicle to attach the player to.")]
    [SerializeField] private string seatMountName = "Seat";

    [Header("Exit Settings")]
    [Tooltip("Offset (in local space of the seat) to place the player when exiting the vehicle.")]
    [SerializeField] private Vector3 exitOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Objects to Deactivate While Driving")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Objects to Activate While Driving")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    private bool isInVehicle = false;
    private GameObject currentVehicle;
    private InputManager_SVP vehicleInputManager;
    private Transform seatMount;

    // Store original transform values
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Added for input reset
    private PlayerInput playerInputSystem;
    private StarterAssetsInputs starterInputs;

    void Start()
    {
        if (playerRoot != null)
        {
            playerInputSystem = playerRoot.GetComponent<PlayerInput>();
            starterInputs = playerRoot.GetComponent<StarterAssetsInputs>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isInVehicle)
                TryEnterVehicle();
            else
                ExitVehicle();
        }
    }

    private void TryEnterVehicle()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.collider.CompareTag(vehicleTag))
            {
                vehicleInputManager = hit.collider.GetComponentInParent<InputManager_SVP>();
                if (vehicleInputManager != null)
                {
                    currentVehicle = hit.collider.GetComponentInParent<Transform>().gameObject;

                    seatMount = currentVehicle.transform.Find(seatMountName);
                    if (seatMount == null)
                    {
                        Debug.LogWarning("[VehicleInteraction] No seat mount named '" + seatMountName + "' found on " + currentVehicle.name + ". Defaulting to vehicle root.");
                        seatMount = currentVehicle.transform;
                    }

                    EnterVehicle();
                }
            }
        }
    }

    private void EnterVehicle()
    {
        isInVehicle = true;

        // Save player transform
        originalScale = playerRoot.transform.localScale;
        originalRotation = playerRoot.transform.rotation;
        originalPosition = playerRoot.transform.position;

        // ✅ Proper input deactivation (like PauseMenuController)
        if (playerInputSystem != null)
        {
            playerInputSystem.DeactivateInput();
        }
        else if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (playerRoot.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (playerRoot.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        // Attach to seat
        playerRoot.transform.SetParent(seatMount, true);
        playerRoot.transform.localPosition = Vector3.zero;
        playerRoot.transform.localRotation = Quaternion.identity;
        playerRoot.transform.localScale = Vector3.one; // prevent scale distortion

        // Hide player model while in vehicle (optional)
        playerRoot.SetActive(false);

        vehicleInputManager.enabled = true;

        // Manage object visibility
        foreach (var obj in objectsToDeactivate)
            if (obj) obj.SetActive(false);

        foreach (var obj in objectsToActivate)
            if (obj) obj.SetActive(true);

        Debug.Log("[VehicleInteraction] Entered vehicle: " + currentVehicle.name);
    }

    private void ExitVehicle()
    {
        if (!isInVehicle || !currentVehicle) return;

        // ✅ Reset vehicle input before disabling
        if (vehicleInputManager != null)
        {
            vehicleInputManager.ResetInputs();
            vehicleInputManager.enabled = false;
        }

        playerRoot.transform.SetParent(null, true);
        playerRoot.SetActive(true);

        if (playerRoot.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (playerRoot.TryGetComponent<Collider>(out var col))
            col.enabled = false; // temporarily disable to avoid instant collision

        // Determine safe exit position
        Vector3 exitPos = seatMount != null
            ? seatMount.TransformPoint(exitOffset)
            : currentVehicle.transform.TransformPoint(exitOffset);

        Quaternion exitRot = seatMount != null
            ? seatMount.rotation
            : currentVehicle.transform.rotation;

        playerRoot.transform.position = exitPos;
        playerRoot.transform.rotation = exitRot;
        playerRoot.transform.localScale = originalScale;

        // Enable collider next frame to prevent immediate collision
        StartCoroutine(EnableColliderNextFrame(col));

        // ✅ Proper input reactivation & clearing like PauseMenuController
        if (playerInputSystem != null)
        {
            playerInputSystem.ActivateInput();
        }
        else if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }

        // Restore activation states
        foreach (var obj in objectsToDeactivate)
            if (obj) obj.SetActive(true);

        foreach (var obj in objectsToActivate)
            if (obj) obj.SetActive(false);

        Debug.Log("[VehicleInteraction] Exited vehicle: " + currentVehicle.name);

        currentVehicle = null;
        vehicleInputManager = null;
        seatMount = null;
        isInVehicle = false;
    }

    private IEnumerator EnableColliderNextFrame(Collider col)
    {
        yield return null; // wait one frame
        if (col != null)
            col.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactRange);
        }

        // Draw exit offset gizmo if seat mount is available
        if (seatMount != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(seatMount.TransformPoint(exitOffset), 0.2f);
        }
    }
}
