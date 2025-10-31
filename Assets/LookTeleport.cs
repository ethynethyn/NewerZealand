using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class LookTeleport : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public FirstPersonController playerController;

    [Header("Teleport Settings")]
    public Transform teleportTarget; // Assign the empty for the teleport location
    public float maxDistance = 20f;  // Max raycast distance
    public Key teleportKey = Key.E;  // Key to teleport
    public bool useSmoothTeleport = false;
    public float smoothSpeed = 10f;
    public bool maintainRotation = true; // Keep player rotation?

    private bool isTeleporting = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Update()
    {
        HandleTeleportInput();

        if (isTeleporting)
        {
            if (useSmoothTeleport)
            {
                playerController.transform.position = Vector3.Lerp(playerController.transform.position, targetPosition, Time.deltaTime * smoothSpeed);
                playerController.transform.rotation = Quaternion.Slerp(playerController.transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);

                if (Vector3.Distance(playerController.transform.position, targetPosition) < 0.01f)
                    FinishTeleport();
            }
            else
            {
                FinishTeleport();
            }
        }
    }

    private void HandleTeleportInput()
    {
        if (Keyboard.current[teleportKey].wasPressedThisFrame)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    TeleportPlayer();
                }
            }
        }
    }

    private void TeleportPlayer()
    {
        if (teleportTarget == null) return;

        targetPosition = teleportTarget.position;
        targetRotation = maintainRotation ? playerController.transform.rotation : teleportTarget.rotation;

        if (playerController != null)
            playerController.enabled = false;

        isTeleporting = true;

        if (!useSmoothTeleport)
        {
            playerController.transform.position = targetPosition;
            playerController.transform.rotation = targetRotation;
        }
    }

    private void FinishTeleport()
    {
        isTeleporting = false;
        if (playerController != null)
            playerController.enabled = true;
    }
}
