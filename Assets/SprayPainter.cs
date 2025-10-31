using UnityEngine;

public class SprayPainter : MonoBehaviour
{
    [Header("References")]
    public GameObject sprayCanHeld;  // Must be active to spray
    public Camera playerCamera;      // Player camera
    public GameObject sprayPrefab;   // Prefab of spray decal (quad)

    [Header("Spray Settings")]
    public float sprayDistance = 5f;     // Max distance to spray
    public float sprayInterval = 0.3f;   // Time between sprays
    public float sprayScale = 0.3f;      // Size of spray
    public float sprayOffset = 0.01f;    // Distance from surface
    public float edgeMargin = 0.05f;     // Prevent edges

    [Header("Layers")]
    public LayerMask blockedLayer;       // Cannot spray on this layer

    private float sprayTimer = 0f;

    void Update()
    {
        if (!sprayCanHeld.activeSelf) return;

        sprayTimer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F) && sprayTimer >= sprayInterval)
        {
            if (SpawnSpray())
                sprayTimer = 0f;
        }
    }

    bool SpawnSpray()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Raycast ignoring triggers
        if (!Physics.Raycast(ray, out RaycastHit hit, sprayDistance, ~0, QueryTriggerInteraction.Ignore))
            return false;

        // Blocked layer check
        if (((1 << hit.collider.gameObject.layer) & blockedLayer) != 0)
            return false;

        Vector3 spawnPos = hit.point + hit.normal * sprayOffset;

        // Orient spray flush with the wall, upright
        Quaternion spawnRot = Quaternion.LookRotation(-hit.normal, Vector3.up);

        // Determine axes along surface for edge checking
        Vector3 right = Vector3.Cross(hit.normal, Vector3.up).normalized * sprayScale * 0.5f;
        if (right.magnitude < 0.01f)
            right = Vector3.Cross(hit.normal, Vector3.forward).normalized * sprayScale * 0.5f;

        Vector3 up = Vector3.Cross(right, hit.normal).normalized * sprayScale * 0.5f;

        // Check corners to prevent hanging off edges
        Vector3[] corners = new Vector3[]
        {
            spawnPos + right + up,
            spawnPos + right - up,
            spawnPos - right + up,
            spawnPos - right - up
        };

        foreach (Vector3 corner in corners)
        {
            if (!Physics.Raycast(corner + hit.normal * 0.01f, -hit.normal, out RaycastHit cornerHit, 0.1f, ~0, QueryTriggerInteraction.Ignore))
                return false;

            if (cornerHit.collider != hit.collider)
                return false;
        }

        // Spawn spray
        GameObject spray = Instantiate(sprayPrefab, spawnPos, spawnRot);

        // Random rotation around surface normal for natural look
        spray.transform.Rotate(hit.normal, Random.Range(0f, 360f), Space.World);

        // Scale spray
        spray.transform.localScale = Vector3.one * sprayScale;

        return true;
    }
}
