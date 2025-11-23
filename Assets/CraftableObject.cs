using UnityEngine;
using System.Collections;

public class CraftableObject : MonoBehaviour
{
    private bool hasCrafted = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrafted) return;

        CraftableObject otherCraft = collision.gameObject.GetComponent<CraftableObject>();
        if (otherCraft == null) return;

        if (CraftingManager.Instance.TryCraft(this.gameObject, otherCraft.gameObject, out GameObject resultPrefab))
        {
            hasCrafted = true;
            otherCraft.hasCrafted = true;

            // Midpoint between the two ingredients
            Vector3 spawnPos = (transform.position + otherCraft.transform.position) / 2f;

            StartCoroutine(CraftSequence(otherCraft.gameObject, resultPrefab, spawnPos));
        }
    }

    private IEnumerator CraftSequence(GameObject other, GameObject resultPrefab, Vector3 spawnPos)
    {
        // Read global timing from CraftingManager
        float anticipationTime = CraftingManager.Instance.anticipationTime;
        float shrinkTime = CraftingManager.Instance.shrinkTime;
        float fadeInTime = CraftingManager.Instance.fadeInTime;

        // Freeze physics
        Rigidbody rbA = GetComponent<Rigidbody>();
        Rigidbody rbB = other.GetComponent<Rigidbody>();
        if (rbA) rbA.isKinematic = true;
        if (rbB) rbB.isKinematic = true;

        // --- Anticipation delay ---
        yield return new WaitForSeconds(anticipationTime);

        // --- Shrink ingredients ---
        float t = 0f;
        Vector3 startScaleA = transform.localScale;
        Vector3 startScaleB = other.transform.localScale;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(1f, 0f, t / shrinkTime);
            transform.localScale = startScaleA * s;
            other.transform.localScale = startScaleB * s;
            yield return null;
        }

        // --- Determine safe spawn position ---
        Vector3 safeSpawn = spawnPos + Vector3.up * 0.5f;

        // --- Spawn puff VFX BEFORE destroying ingredients ---
        if (CraftingManager.Instance.craftVFX != null)
            Instantiate(CraftingManager.Instance.craftVFX, safeSpawn, Quaternion.identity);

        // --- Trigger SFX & popup immediately ---
        CraftingManager.Instance.SpawnCraftFeedback(resultPrefab, safeSpawn);

        // Destroy ingredients
        Destroy(this.gameObject);
        Destroy(other);

        // --- Spawn crafted prefab ---
        GameObject crafted = Instantiate(resultPrefab, safeSpawn, Quaternion.identity);
        Vector3 prefabScale = resultPrefab.transform.localScale;
        crafted.transform.localScale = prefabScale * 0.8f; // start smaller for fade-in

        // --- Optional progress bar ---
        if (CraftingManager.Instance.progressBar != null)
        {
            CraftingManager.Instance.progressBar.gameObject.SetActive(true);
            CraftingManager.Instance.progressBar.fillAmount = 0f;
        }

        // --- Fade-in / bounce effect ---
        yield return StartCoroutine(ScaleBounceEffect(crafted.transform, prefabScale, fadeInTime));

        // Hide progress bar
        if (CraftingManager.Instance.progressBar != null)
            CraftingManager.Instance.progressBar.gameObject.SetActive(false);

        Debug.Log("Crafted prefab spawned: " + crafted.name + " at " + safeSpawn);
    }

    // Smooth bounce/fade-in effect relative to prefab's original scale
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
