using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RestockShelf : MonoBehaviour
{
    [System.Serializable]
    public class ShelfItem
    {
        public GameObject prefab;
        public Transform spawnPoint;
        public int maxQuantity = 3;
        public float respawnDelay = 5f;

        [HideInInspector] public int currentCount;
        [HideInInspector] public bool respawning;
    }

    [Header("Shelf Items")]
    public List<ShelfItem> shelfItems = new List<ShelfItem>();

    [Header("Detection")]
    public LayerMask pickupLayer;

    private Dictionary<GameObject, ShelfItem> prefabLookup = new Dictionary<GameObject, ShelfItem>();

    void Start()
    {
        // Build lookup + initialize counts
        foreach (var item in shelfItems)
        {
            if (item.prefab == null || item.spawnPoint == null)
                continue;

            prefabLookup[item.prefab] = item;

            // Spawn initial stock
            for (int i = 0; i < item.maxQuantity; i++)
            {
                SpawnItem(item);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsOnPickupLayer(other.gameObject))
            return;

        GameObject root = GetRootPrefab(other.gameObject);
        if (root == null)
            return;

        if (!prefabLookup.ContainsKey(root))
            return;

        ShelfItem item = prefabLookup[root];
        item.currentCount--;

        if (item.currentCount < item.maxQuantity && !item.respawning)
        {
            StartCoroutine(RespawnAfterDelay(item));
        }
    }

    IEnumerator RespawnAfterDelay(ShelfItem item)
    {
        item.respawning = true;
        yield return new WaitForSeconds(item.respawnDelay);

        if (item.currentCount < item.maxQuantity)
        {
            SpawnItem(item);
        }

        item.respawning = false;
    }

    void SpawnItem(ShelfItem item)
    {
        Instantiate(item.prefab, item.spawnPoint.position, item.spawnPoint.rotation);
        item.currentCount++;
    }

    bool IsOnPickupLayer(GameObject obj)
    {
        return ((1 << obj.layer) & pickupLayer) != 0;
    }

    GameObject GetRootPrefab(GameObject obj)
    {
        // Handles nested colliders / child objects
        return obj.transform.root.gameObject;
    }
}
