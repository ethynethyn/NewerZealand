using UnityEngine;
using System.Collections;

public class CraftableObject : MonoBehaviour
{
    [Header("Crafting Behavior")]
    public bool destroyOnCraft = true;
    public bool shrinkOnCraft = true;
    public int usesBeforeDestroy = 1;
    public float cooldownTime = 0f;

    [Header("Spawn Override")]
    public bool useCustomSpawnPoint = false;
    public Transform customSpawnPoint;

    [Header("Crafting Distance Check")]
    [Tooltip("How close two objects must be to craft when one is kinematic (e.g., inspecting)")]
    public float craftDistance = 0.5f;

    private bool hasCrafted = false;
    private bool onCooldown = false;
    private int usesRemaining;

    private void Awake()
    {
        usesRemaining = usesBeforeDestroy;
    }

    private void Update()
    {
        if (hasCrafted || onCooldown) return;

        // Find all CraftableObjects in scene
        CraftableObject[] allCraftables = FindObjectsOfType<CraftableObject>();

        foreach (var other in allCraftables)
        {
            if (other == this || other.hasCrafted || other.onCooldown) continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance <= craftDistance)
            {
                if (CraftingManager.Instance.TryCraft(gameObject, other.gameObject, out GameObject resultPrefab))
                {
                    hasCrafted = true;
                    other.hasCrafted = true;

                    Vector3 spawnPos = DetermineSpawnPosition(other);
                    StartCoroutine(CraftSequence(other, resultPrefab, spawnPos));
                    break;
                }
            }
        }
    }

    private Vector3 DetermineSpawnPosition(CraftableObject other)
    {
        if (useCustomSpawnPoint && customSpawnPoint != null) return customSpawnPoint.position;
        if (other.useCustomSpawnPoint && other.customSpawnPoint != null) return other.customSpawnPoint.position;
        return (transform.position + other.transform.position) * 0.5f;
    }

    private IEnumerator CraftSequence(CraftableObject other, GameObject resultPrefab, Vector3 spawnPos)
    {
        float anticipationTime = CraftingManager.Instance.anticipationTime;
        float shrinkTime = CraftingManager.Instance.shrinkTime;
        float fadeInTime = CraftingManager.Instance.fadeInTime;

        Rigidbody rbA = GetComponent<Rigidbody>();
        Rigidbody rbB = other.GetComponent<Rigidbody>();

        if (rbA) rbA.isKinematic = true;
        if (rbB) rbB.isKinematic = true;

        yield return new WaitForSeconds(anticipationTime);

        float t = 0f;
        Vector3 startScaleA = transform.localScale;
        Vector3 startScaleB = other.transform.localScale;

        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(1f, 0f, t / shrinkTime);

            if (shrinkOnCraft) transform.localScale = startScaleA * s;
            if (other.shrinkOnCraft) other.transform.localScale = startScaleB * s;

            yield return null;
        }

        Vector3 safeSpawn = spawnPos + Vector3.up * 0.02f;

        if (CraftingManager.Instance.craftVFX != null)
            Instantiate(CraftingManager.Instance.craftVFX, safeSpawn, Quaternion.identity);

        CraftingManager.Instance.SpawnCraftFeedback(resultPrefab, safeSpawn);

        HandlePostCraft(startScaleA);
        other.HandlePostCraft(startScaleB);

        GameObject crafted = Instantiate(resultPrefab, safeSpawn, Quaternion.identity);
        Vector3 prefabScale = resultPrefab.transform.localScale;
        crafted.transform.localScale = prefabScale * 0.8f;

        yield return StartCoroutine(ScaleBounceEffect(crafted.transform, prefabScale, fadeInTime));
    }

    private void HandlePostCraft(Vector3 originalScale)
    {
        if (usesRemaining > 0) usesRemaining--;
        bool shouldDestroy = destroyOnCraft && usesRemaining == 0;

        if (shouldDestroy)
        {
            Destroy(gameObject);
            return;
        }

        if (shrinkOnCraft) transform.localScale = originalScale;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        hasCrafted = false;

        if (cooldownTime > 0f) StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        onCooldown = false;
    }

    private IEnumerator ScaleBounceEffect(Transform target, Vector3 originalScale, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float scaleFactor = Mathf.SmoothStep(0.8f, 1f, progress);
            target.localScale = originalScale * scaleFactor;
            yield return null;
        }

        target.localScale = originalScale;
    }
}
