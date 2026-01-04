using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera playerCamera; // Drag your player camera here in the inspector

    void LateUpdate()
    {
        if (playerCamera == null) return;

        // Make the object face the camera
        transform.forward = playerCamera.transform.forward;
    }
}
