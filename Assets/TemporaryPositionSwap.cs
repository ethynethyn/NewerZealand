using UnityEngine;

public class PositionSwapOnToggle : MonoBehaviour
{
    [Header("Target References")]
    public Transform objectToMove;
    public Transform targetPosition;

    private Vector3 originalPosition;
    private bool hasStoredOriginal = false;

    void OnEnable()
    {
        if (objectToMove == null) return;

        if (!hasStoredOriginal)
        {
            originalPosition = objectToMove.position;
            hasStoredOriginal = true;
        }

        if (targetPosition != null)
        {
            objectToMove.position = targetPosition.position;
        }
    }

    void OnDisable()
    {
        if (objectToMove == null) return;

        if (hasStoredOriginal)
        {
            objectToMove.position = originalPosition;
        }
    }
}