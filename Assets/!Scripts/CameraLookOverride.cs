using UnityEngine;

public class CameraLookAtOverride : MonoBehaviour
{
    [Header("Target")]
    public Transform lookTarget;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Settings")]
    public float rotationSpeed = 5f;

    private bool activeOverride = false;

    void OnEnable()
    {
        activeOverride = true;
    }

    void OnDisable()
    {
        activeOverride = false;
    }

    void LateUpdate()
    {
        if (!activeOverride) return;
        if (cameraTransform == null || lookTarget == null) return;

        Vector3 direction = (lookTarget.position - cameraTransform.position).normalized;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}