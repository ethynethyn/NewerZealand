using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackpackItemStorage : MonoBehaviour
{
    [Header("Capacity")]
    public int maxCapacity = 10;

    [Header("Stored Items")]
    public List<GameObject> storedItems = new List<GameObject>();

    [Header("Visual")]
    public Transform dropPoint;
    public float shrinkTime = 0.25f;

    public bool IsFull()
    {
        return storedItems.Count >= maxCapacity;
    }

    public bool AddItem(GameObject item)
    {
        if (IsFull()) return false;
        storedItems.Add(item);
        StartCoroutine(ShrinkIntoBag(item));
        return true;
    }

    private IEnumerator ShrinkIntoBag(GameObject item)
    {
        if (item == null) yield break;

        // Kill physics and collisions immediately
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in item.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Vector3 startScale = item.transform.localScale;
        Vector3 startPos = item.transform.position;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime / shrinkTime;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (item != null)
            {
                // Shrink toward zero scale
                item.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);

                // Move toward the bag's center as it shrinks
                item.transform.position = Vector3.Lerp(startPos, transform.position, eased);
            }

            yield return null;
        }

        if (item != null)
            item.SetActive(false);
    }

    public GameObject RemoveItem(int index)
    {
        if (index < 0 || index >= storedItems.Count) return null;
        GameObject item = storedItems[index];
        storedItems.RemoveAt(index);
        return item;
    }
}