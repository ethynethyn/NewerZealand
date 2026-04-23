using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerInputToggle : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public FirstPersonController controller;
    public CharacterController characterController;

    [Header("Settings")]
    public bool disableOnStart = false;

    private Vector3 storedVelocity;

    void Start()
    {
        if (disableOnStart)
            DisablePlayer();
    }

    public void DisablePlayer()
    {
        // Disable input system
        if (playerInput != null)
            playerInput.enabled = false;

        // Stop controller movement
        if (controller != null)
            controller.enabled = false;

        // Clear any residual movement
        if (characterController != null)
        {
            characterController.Move(Vector3.zero);
        }

        // Optional: zero out rigidbody if somehow present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void EnablePlayer()
    {
        // Re-enable controller first
        if (controller != null)
            controller.enabled = true;

        // Then input
        if (playerInput != null)
            playerInput.enabled = true;
    }

    // This lets you trigger via GameObject activation
    void OnEnable()
    {
        DisablePlayer(); // being active = player locked
    }

    void OnDisable()
    {
        EnablePlayer(); // being inactive = player free
    }


}