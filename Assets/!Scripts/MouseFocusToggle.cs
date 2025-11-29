using UnityEngine;

public class MouseFocusToggle : MonoBehaviour
{
    void OnEnable()
    {
        // Enable point-and-click mode
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDisable()
    {
        // Return to locked (game) mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
