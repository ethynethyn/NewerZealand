using UnityEngine;
using System.Collections.Generic;

public class Customer : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] chairLocations; // Transforms of available chairs
    public Transform floorArea; // Parent transform of floor wandering area
    public Transform restaurantExit; // Entry/exit point
    public float lerpSpeed = 2f;
    public float floorWanderDuration = 3f; // How long to wander floor before trying seats
    public float waitTimeout = 30f; // How long customer waits before giving up

    [System.Serializable]
    public class OrderConfig
    {
        public GameObject orderPrefab; // The order GameObject
        public Transform orderSpawnLocation; // Specific position to spawn the order
        public Sprite orderImage; // Image to display on docket (drink picture, etc)
    }

    [Header("Order Configuration")]
    public List<OrderConfig> orders = new List<OrderConfig>(); // Configurable order/docket pairs

    [Header("References")]
    private Character playerCharacter; // Reference to player for strikes stat

    private Transform targetChair;
    private GameObject currentOrder;
    private Docket currentDocket;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isLeaving = false;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isSeated = false;
    private bool hasClaimedChair = false;
    private float floorTimer = 0f;
    private bool isOnFloor = false;
    private int tableNumber = 0;

    // Static tracker of which chairs are claimed
    private static Dictionary<Transform, Customer> claimedChairs = new Dictionary<Transform, Customer>();

    public void Initialize()
    {
        playerCharacter = FindPlayerCharacter();

        // Spread out on spawn
        SpreadOutOnSpawn();

        // Start on floor
        StartCoroutine(FloorWanderThenTrySeat());
    }

    void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
        }

        if (isWaiting)
        {
            UpdateWaitTimer();
        }
    }

    void LateUpdate()
    {
        if (isLeaving)
            Despawn();
    }

    void SpreadOutOnSpawn()
    {
        // Add random offset to spawn position to spread customers out
        float randomX = Random.Range(-1f, 1f);
        float randomZ = Random.Range(-1f, 1f);
        float randomY = Random.Range(-30f, 30f); // Random Y rotation

        transform.position += new Vector3(randomX, 0, randomZ);
        transform.Rotate(0, randomY, 0);
    }

    System.Collections.IEnumerator FloorWanderThenTrySeat()
    {
        isOnFloor = true;
        floorTimer = 0f;

        // Randomize floor wander duration per customer (between 60-80% and 120-140% of base)
        float randomizedDuration = floorWanderDuration * Random.Range(0.6f, 1.4f);

        // Wander on floor
        WanderFloor();

        // Wait for randomized floor duration
        yield return new WaitForSeconds(randomizedDuration);

        isOnFloor = false;

        // Try to find a seat
        TryToSeat();
    }

    void WanderFloor()
    {
        if (floorArea == null)
        {
            Debug.LogError("Customer: floorArea not assigned!");
            return;
        }

        // Pick random point within floor area bounds
        Bounds bounds = GetBounds(floorArea);
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            transform.position.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        MoveTo(randomPoint);
    }

    void TryToSeat()
    {
        if (chairLocations == null || chairLocations.Length == 0)
        {
            Debug.LogWarning("Customer: No chair locations assigned!");
            return;
        }

        // Find nearest available chair
        targetChair = FindNearestChair();

        if (targetChair != null)
        {
            // Claim the seat immediately
            claimedChairs[targetChair] = this;
            hasClaimedChair = true;

            // Move to it
            MoveTo(targetChair.position);
        }
        else
        {
            // No chairs available, go back to floor
            WanderFloor();
            StartCoroutine(FloorWanderThenTrySeat());
        }
    }

    Transform FindNearestChair()
    {
        Transform nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform chair in chairLocations)
        {
            // Skip claimed chairs
            if (claimedChairs.ContainsKey(chair)) continue;

            float distance = Vector3.Distance(transform.position, chair.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = chair;
            }
        }

        return nearest;
    }

    bool IsChairOccupied(Transform chair)
    {
        return claimedChairs.ContainsKey(chair);
    }

    void MoveTo(Vector3 destination)
    {
        targetPosition = destination;
        isMoving = true;
    }

    void MoveTowardsTarget()
    {
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < 0.1f)
        {
            transform.position = targetPosition;
            isMoving = false;

            // Check if we reached a chair
            if (targetChair != null && Vector3.Distance(transform.position, targetChair.position) < 0.2f)
            {
                isSeated = true;
                AssignTableNumber();
                OrderFood();
            }

            return;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
    }

    void AssignTableNumber()
    {
        // Find which chair index this is
        for (int i = 0; i < chairLocations.Length; i++)
        {
            if (chairLocations[i] == targetChair)
            {
                tableNumber = i + 1; // Tables numbered 1, 2, 3, etc
                break;
            }
        }
    }

    void OrderFood()
    {
        if (orders.Count == 0)
        {
            Debug.LogWarning("Customer: No orders configured!");
            Leave();
            return;
        }

        // Select random order config
        OrderConfig selectedOrder = orders[Random.Range(0, orders.Count)];

        // Spawn order at specified location
        if (selectedOrder.orderPrefab != null)
        {
            Quaternion orderRotation = Quaternion.Euler(-90, 0, 0);

            if (selectedOrder.orderSpawnLocation != null)
                currentOrder = Instantiate(selectedOrder.orderPrefab, selectedOrder.orderSpawnLocation.position, orderRotation);
            else
                currentOrder = Instantiate(selectedOrder.orderPrefab, targetChair.position + Vector3.up, orderRotation);

            Debug.Log($"Customer spawned order at {currentOrder.transform.position}, Active: {currentOrder.activeSelf}");
        }
        else
        {
            Debug.LogWarning("Customer: Order prefab is null!");
            Leave();
            return;
        }

        // Create docket and add to manager
        string orderName = selectedOrder.orderPrefab.name.Replace("(Clone)", "").Trim();
        currentDocket = new Docket(tableNumber, orderName, currentOrder, selectedOrder.orderImage);
        DocketManager.Get().AddDocket(currentDocket);

        isWaiting = true;
        waitTimer = 0f;
        Debug.Log($"Customer started waiting for order. Order active: {currentOrder.activeSelf}. Timeout: {waitTimeout}s");
    }

    void UpdateWaitTimer()
    {
        if (currentOrder == null)
        {
            Debug.Log("Customer: currentOrder is null, leaving");
            if (currentDocket != null)
                DocketManager.Get().RemoveDocket(currentDocket);

            Leave();
            return;
        }

        if (!currentOrder.activeSelf)
        {
            Debug.Log("Customer: Order completed/disabled, leaving");
            // Order was completed/taken
            if (currentDocket != null)
                DocketManager.Get().RemoveDocket(currentDocket);

            Leave();
            return;
        }

        waitTimer += Time.deltaTime;

        if (waitTimer >= waitTimeout)
        {
            Debug.Log("Customer: Timeout reached, leaving");
            // Timeout - customer gets frustrated
            if (currentOrder != null)
                Destroy(currentOrder);

            if (currentDocket != null)
                DocketManager.Get().RemoveDocket(currentDocket);

            // Decrease strikes on player
            if (playerCharacter != null)
                playerCharacter.ModifyStat("Strikes", -1);

            Leave();
        }
    }

    void Leave()
    {
        isWaiting = false;
        isLeaving = true;
        isSeated = false;

        // Release the chair
        if (targetChair != null && claimedChairs.ContainsKey(targetChair) && claimedChairs[targetChair] == this)
        {
            claimedChairs.Remove(targetChair);
        }

        if (restaurantExit != null)
        {
            MoveTo(restaurantExit.position);
        }
        else
        {
            Debug.LogWarning("Customer: restaurantExit not assigned!");
            Destroy(gameObject);
        }
    }

    void Despawn()
    {
        if (isLeaving && restaurantExit != null)
        {
            float distanceToExit = Vector3.Distance(transform.position, restaurantExit.position);
            if (distanceToExit < 0.2f)
            {
                Destroy(gameObject);
            }
        }
    }

    Bounds GetBounds(Transform t)
    {
        Renderer renderer = t.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        Collider collider = t.GetComponent<Collider>();
        if (collider != null)
            return collider.bounds;

        // Fallback to a simple box around the transform
        return new Bounds(t.position, Vector3.one * 10f);
    }

    Character FindPlayerCharacter()
    {
        // Search for player character in scene
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character c in allCharacters)
        {
            if (c.gameObject.CompareTag("Player"))
                return c;
        }
        return null;
    }
}