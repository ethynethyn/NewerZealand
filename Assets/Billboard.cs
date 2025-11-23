using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera playerCamera;

    void Start()
    {
        // Finds the actual rendering camera (the one Cinemachine controls)
        playerCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;

        // Rotate toward camera
        transform.forward = playerCamera.transform.forward;
    }
}
