using UnityEngine;

// Add this component to every pickupable prefab.
// It records its own localScale on Awake, before any pickup or
// parenting logic has a chance to corrupt it.
public class OriginalScale : MonoBehaviour
{
    public Vector3 scale;

    void Awake()
    {
        scale = transform.localScale;
    }
}