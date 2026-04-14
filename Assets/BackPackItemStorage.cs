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

    private System.Collections.IEnumerator ShrinkIntoBag(GameObject item)
    {
        if (item == null) yield break;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        Vector3 startScale = item.transform.localScale;
        Vector3 endScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / shrinkTime;
            if (item != null)
                item.transform.localScale = Vector3.Lerp(startScale, endScale, t);
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