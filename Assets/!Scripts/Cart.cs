using UnityEngine;
using System.Collections.Generic;

public class Cart : MonoBehaviour
{
    [Header("Settings")]
    public Transform carryPoint;           // Where objects will snap
    public LayerMask pickupLayer;          // Layer of objects to carry
    public GameObject activationObject;    // Object whose active state enables carry

    private bool isActive => activationObject != null && activationObject.activeInHierarchy;

    private readonly List<Rigidbody> carriedObjects = new List<Rigidbody>();

    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;

        // Only affect objects on the pickup layer
        if (((1 << other.gameObject.layer) & pickupLayer) != 0)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic && !carriedObjects.Contains(rb))
            {
                // Freeze physics
                rb.isKinematic = true;

                // Disable colliders
                foreach (var col in rb.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // Parent to carry point
                rb.transform.SetParent(carryPoint, true);
                carriedObjects.Add(rb);
            }
        }
    }

    private void Update()
    {
        // If activation is off, release everything
        if (!isActive && carriedObjects.Count > 0)
        {
            ReleaseAll();
        }

        // Safety check: remove null references if objects were destroyed
        for (int i = carriedObjects.Count - 1; i >= 0; i--)
        {
            if (carriedObjects[i] == null)
                carriedObjects.RemoveAt(i);
        }
    }

    private void ReleaseAll()
    {
        foreach (var rb in carriedObjects)
        {
            if (rb == null) continue;

            // Restore physics
            rb.isKinematic = false;

            // Re-enable colliders
            foreach (var col in rb.GetComponentsInChildren<Collider>())
                col.enabled = true;

            // Unparent safely
            rb.transform.SetParent(null, true);
        }

        carriedObjects.Clear();
    }

    // Optional: force release via other scripts
    public void ForceRelease()
    {
        ReleaseAll();
    }
}
