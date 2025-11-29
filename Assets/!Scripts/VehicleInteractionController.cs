using UnityEngine;
using Ezereal;
using StarterAssets;

public class VehicleInteractionControl : MonoBehaviour
{
    [Header("References")]
    public GameObject player;               // Player root object
    public Transform exitPoint;             // Safe exit location
    public Camera playerCamera;             // Player main camera
    public GameObject vehicle;              // Vehicle root
    public MonoBehaviour playerController;  // Player movement script (FirstPersonController)

    [Header("Objects to Toggle")]
    public GameObject[] enableOnEnter;      // Objects to enable when in vehicle
    public GameObject[] disableOnEnter;     // Objects to disable when in vehicle

    [Header("UI & Settings")]
    public GameObject interactPromptUI;     // Optional "Press E" prompt
    public KeyCode interactKey = KeyCode.E;
    public float toggleCooldown = 0.3f;     // Prevent rapid enter/exit

    [Header("Interaction Trigger")]
    public Collider interactionTrigger;     // Trigger collider for vehicle interaction

    [Header("Exit Safety")]
    public Vector3 exitCheckSize = new Vector3(1f, 2f, 1f); // Half-extents of the box to check
    public float exitCheckOffsetY = 0.5f;                    // Height offset for the check
    public LayerMask exitIgnoreLayer;                        // Layer to ignore during exit check

    private bool isPlayerInVehicle = false;
    private float lastToggleTime = -1f;
    private bool playerInTrigger = false;

    private EzerealCarController carController;
    private EzerealCameraController carCameraController;
    private Camera[] vehicleCameras;

    void Awake()
    {
        if (vehicle != null)
        {
            carController = vehicle.GetComponent<EzerealCarController>();
            carCameraController = vehicle.GetComponentInChildren<EzerealCameraController>(true);
            vehicleCameras = vehicle.GetComponentsInChildren<Camera>(true);

            // Disable vehicle input by default
            if (carController != null) carController.enabled = false;
            if (carCameraController != null) carCameraController.enabled = false;
            if (vehicleCameras != null)
                foreach (Camera cam in vehicleCameras) cam.enabled = false;
        }

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    void Update()
    {
        if (Time.time - lastToggleTime < toggleCooldown) return;

        if (playerInTrigger && Input.GetKeyDown(interactKey))
        {
            lastToggleTime = Time.time;

            if (!isPlayerInVehicle)
                EnterVehicle();
            else
            {
                if (CanExit())
                    ExitVehicle();
                else
                    Debug.Log("Cannot exit vehicle: exit area blocked!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            playerInTrigger = true;
            if (interactPromptUI != null && !isPlayerInVehicle)
                interactPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            playerInTrigger = false;
            if (interactPromptUI != null)
                interactPromptUI.SetActive(false);
        }
    }

    void EnterVehicle()
    {
        isPlayerInVehicle = true;

        if (playerController != null) playerController.enabled = false;
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            playerCamera.tag = "Untagged";
        }

        if (carController != null) carController.enabled = true;
        if (carCameraController != null) carCameraController.enabled = true;

        if (vehicleCameras != null && vehicleCameras.Length > 0)
        {
            foreach (Camera cam in vehicleCameras) cam.enabled = false;
            vehicleCameras[0].enabled = true;
            vehicleCameras[0].tag = "MainCamera";
        }

        ToggleObjects(enableOnEnter, true);
        ToggleObjects(disableOnEnter, false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    void ExitVehicle()
    {
        isPlayerInVehicle = false;

        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 exitPosition = exitPoint.position + Vector3.up * exitCheckOffsetY;
        Quaternion exitRotation = Quaternion.Euler(0f, vehicle.transform.eulerAngles.y, 0f);
        player.transform.position = exitPosition;
        player.transform.rotation = exitRotation;

        if (controller != null) controller.enabled = true;

        if (playerController != null) playerController.enabled = true;

        var starterInputs = player.GetComponent<StarterAssetsInputs>();
        if (starterInputs != null)
        {
            starterInputs.MoveInput(Vector2.zero);
            starterInputs.LookInput(Vector2.zero);
            starterInputs.JumpInput(false);
            starterInputs.SprintInput(false);

            starterInputs.enabled = false;
            starterInputs.enabled = true;
        }

        var fpsController = player.GetComponent<FirstPersonController>();
        if (fpsController != null)
        {
            fpsController.enabled = false;
            fpsController.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.transform.rotation = exitRotation;
            playerCamera.tag = "MainCamera";
        }

        if (carController != null) carController.enabled = false;
        if (carCameraController != null) carCameraController.enabled = false;
        if (vehicleCameras != null)
            foreach (Camera cam in vehicleCameras) cam.enabled = false;

        ToggleObjects(enableOnEnter, false);
        ToggleObjects(disableOnEnter, true);
    }

    /// <summary>
    /// Check if the exit point is clear by casting a box around it, ignoring a specified layer.
    /// </summary>
    bool CanExit()
    {
        Vector3 boxCenter = exitPoint.position + Vector3.up * exitCheckOffsetY;
        Vector3 halfExtents = exitCheckSize * 0.5f;

        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.identity, ~exitIgnoreLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject != vehicle && hit.gameObject != player)
                return false;
        }
        return true;
    }

    void ToggleObjects(GameObject[] objects, bool state)
    {
        if (objects == null) return;
        foreach (var obj in objects)
            if (obj != null)
                obj.SetActive(state);
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionTrigger != null)
        {
            Gizmos.color = Color.green;

            if (interactionTrigger is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.position, sphere.radius);
            }
            else if (interactionTrigger is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.transform.position + box.center, box.size);
            }
            else if (interactionTrigger is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.transform.position, capsule.radius);
            }
            else
            {
                Gizmos.DrawWireSphere(interactionTrigger.transform.position, 0.5f);
            }
        }

        // Draw exit check box
        Gizmos.color = Color.red;
        Vector3 boxCenter = exitPoint != null ? exitPoint.position + Vector3.up * exitCheckOffsetY : Vector3.zero;
        Gizmos.DrawWireCube(boxCenter, exitCheckSize);
    }
}
