using System.Collections.Generic;
using UnityEngine;

public class TrolleyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public Rigidbody trolleyRigidbody;

    [Header("Trolley Settings")]
    public Transform itemParent; // Empty child transform where items will sit
    public float itemSpacing = 0.5f; // distance between items
    public Collider topCollider; // Collider on top of trolley to detect pickups

    [Header("Pickup Settings")]
    public LayerMask pickupLayer; // Set to "Pickup" layer

    private List<GameObject> itemsInTrolley = new List<GameObject>();

    void Start()
    {
        if (trolleyRigidbody == null)
            trolleyRigidbody = GetComponent<Rigidbody>();
        if (itemParent == null)
            itemParent = transform;
        if (topCollider == null)
            Debug.LogWarning("Top Collider not assigned! Items won’t be detected.");
    }

    void FixedUpdate()
    {
        // Simple movement using Rigidbody
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v) * speed * Time.fixedDeltaTime;
        trolleyRigidbody.MovePosition(trolleyRigidbody.position + move);
    }

    void OnTriggerEnter(Collider other)
    {
        // Only handle objects on the pickup layer
        if (((1 << other.gameObject.layer) & pickupLayer) != 0)
        {
            AddItemToTrolley(other.gameObject);
        }
    }

    public void AddItemToTrolley(GameObject item)
    {
        if (item == null) return;
        if (itemsInTrolley.Contains(item)) return; // Avoid duplicates

        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        if (itemRb == null)
        {
            Debug.LogWarning("Item has no Rigidbody!");
            return;
        }

        // Optional: freeze rotation for stability
        itemRb.constraints = RigidbodyConstraints.FreezeRotation;

        // Position the item on the trolley
        Vector3 nextPos = GetNextItemPosition();
        item.transform.position = nextPos;

        // Attach with FixedJoint
        FixedJoint joint = item.AddComponent<FixedJoint>();
        joint.connectedBody = trolleyRigidbody;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;

        itemsInTrolley.Add(item);
    }

    Vector3 GetNextItemPosition()
    {
        // Stack items in a simple grid pattern
        int count = itemsInTrolley.Count;
        int row = count / 3;
        int col = count % 3;

        Vector3 offset = new Vector3(col * itemSpacing, row * itemSpacing, 0);
        return itemParent.position + offset;
    }
}
