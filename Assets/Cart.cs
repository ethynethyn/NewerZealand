using UnityEngine;

public class Cart : MonoBehaviour
{
    [Header("Settings")]
    public Transform carryPoint;           // Where objects will snap
    public LayerMask pickupLayer;          // Layer of objects to carry
    public GameObject activationObject;    // Object whose active state enables carry

    private bool isActive => activationObject != null && activationObject.activeInHierarchy;

    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;  // Only run if the activation object is active

        // Only affect objects on the pickup layer
        if (((1 << other.gameObject.layer) & pickupLayer) != 0)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                // Freeze physics
                rb.isKinematic = true;

                // Parent it to the trolley
                rb.transform.SetParent(carryPoint);
            }
        }
    }

    private void Update()
    {
        // Unparent everything if activation object is inactive
        if (!isActive)
        {
            foreach (Transform child in carryPoint)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic)
                {
                    rb.isKinematic = false;      // unfreeze physics
                    child.SetParent(null);       // unparent
                }
            }
        }
    }
}
