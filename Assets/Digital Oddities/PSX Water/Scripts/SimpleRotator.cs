using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    protected enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Attributes")]
    [Tooltip("The axis to rotate around")]
    [SerializeField] RotationAxis axis = RotationAxis.Y;

    [Tooltip("Rotation speed in degrees per second")]
    [SerializeField] float speed = 90f;

    void Update()
    {
        // Determine the rotation axis based on the selection
        Vector3 rotationAxis = GetRotationAxis();

        // Rotate the object around the chosen axis
        transform.Rotate(rotationAxis, speed * Time.deltaTime);
    }

    private Vector3 GetRotationAxis()
    {
        switch (axis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            case RotationAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.up;
        }
    }
}