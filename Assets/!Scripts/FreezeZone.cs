using UnityEngine;

public class FreezeZone : MonoBehaviour
{
    [Header("Objects to manage in the trolley")]
    public FreezableObject[] objectsInTrolley;

    [Header("Control Object")]
    public GameObject handleObject; // Object whose active state triggers the freeze/parenting

    private bool isActive = false;

    private void Update()
    {
        if (handleObject == null) return;

        // Check if the handle is active
        if (handleObject.activeSelf && !isActive)
        {
            ActivateTrolley();
        }
        else if (!handleObject.activeSelf && isActive)
        {
            DeactivateTrolley();
        }
    }

    private void ActivateTrolley()
    {
        isActive = true;

        foreach (FreezableObject obj in objectsInTrolley)
        {
            if (obj == null) continue;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) continue;

            // Make kinematic so it moves with the trolley
            rb.isKinematic = true;

            // Parent to trolley
            obj.transform.parent = transform;

            // Optional: mark as frozen in FreezableObject
            obj.Freeze();
        }
    }

    private void DeactivateTrolley()
    {
        isActive = false;

        foreach (FreezableObject obj in objectsInTrolley)
        {
            if (obj == null) continue;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) continue;

            // Unparent from trolley
            obj.transform.parent = null;

            // Restore physics
            rb.isKinematic = false;

            // Optional: unfreeze in FreezableObject
            obj.Unfreeze();
        }
    }
}
