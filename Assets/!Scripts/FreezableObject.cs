using UnityEngine;

public class FreezableObject : MonoBehaviour
{
    private Rigidbody rb;
    private bool isFrozen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("FreezableObject requires a Rigidbody.");
        }
    }

    public void Freeze()
    {
        if (rb == null) return;
        isFrozen = true;
        rb.isKinematic = true; // freezes physics
    }

    public void Unfreeze()
    {
        if (rb == null) return;
        isFrozen = false;
        rb.isKinematic = false; // restores physics
    }

    public bool IsFrozen()
    {
        return isFrozen;
    }
}
