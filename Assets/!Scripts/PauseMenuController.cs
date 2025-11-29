using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using StarterAssets; // So we can reference StarterAssetsInputs

public class PauseMenuController : MonoBehaviour
{
    [Header("Main Pause Menu UI")]
    public GameObject pauseMenuUI;

    [Header("Objects to ENABLE when paused")]
    public List<GameObject> enableOnPause = new List<GameObject>();

    [Header("Objects to DISABLE when paused")]
    public List<GameObject> disableOnPause = new List<GameObject>();

    private bool isPaused = false;
    private PlayerInput playerInput;
    private StarterAssetsInputs starterInputs;

    void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        starterInputs = FindObjectOfType<StarterAssetsInputs>();
    }

    void Update()
    {
        // Detect Escape or Start button
        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        // Pause or resume time
        Time.timeScale = isPaused ? 0f : 1f;

        // Enable/disable menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(isPaused);

        // Toggle objects
        foreach (GameObject obj in enableOnPause)
            if (obj != null)
                obj.SetActive(isPaused);

        foreach (GameObject obj in disableOnPause)
            if (obj != null)
                obj.SetActive(!isPaused);

        // Cursor behavior
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;

        //  Disable or enable input actions to prevent stuck movement
        if (playerInput != null)
        {
            if (isPaused)
            {
                playerInput.DeactivateInput();
            }
            else
            {
                playerInput.ActivateInput();
            }
        }

        //  Optional: clear custom input flags so movement doesn’t “stick”
        if (starterInputs != null && !isPaused)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }
    }
}
