using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    void LateUpdate()
    {
        if (playerCamera == null) return;

        // Opposite direction (flipped)
        Vector3 direction = transform.position - playerCamera.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Preserve original X and Z
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, targetRotation.eulerAngles.y, euler.z);
    }
}