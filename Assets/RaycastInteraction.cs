using UnityEngine;

public class RaycastTriggerInteraction : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera playerCamera;
    public float rayDistance = 3f;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public string requiredTag = "Interactable";

    [Header("Animator Settings")]
    public string boolParameterName = "IsOpen"; // Animator bool

    private Animator lastAnimator;
    private Outline lastOutline; // Track last outlined object

    void Update()
    {
        HandleOutline(); // Update outlines every frame

        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    void TryInteract()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("[RaycastTriggerInteraction] No player camera assigned!");
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Animator animator = FindAnimator(hit.collider.transform);
            if (animator == null)
            {
                Debug.Log($"[RaycastTriggerInteraction] No Animator found on {hit.collider.name} or its parents.");
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !animator.CompareTag(requiredTag))
                return;

            // If we’re aiming at a new object, remember it
            if (animator != lastAnimator)
                lastAnimator = animator;

            bool isOpen = animator.GetBool(boolParameterName);
            animator.SetBool(boolParameterName, !isOpen);

            Debug.Log($"[RaycastTriggerInteraction] {(isOpen ? "Closing" : "Opening")} {animator.name}");
        }
        else
        {
            Debug.Log("[RaycastTriggerInteraction] Raycast hit nothing.");
        }
    }

    void HandleOutline()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Outline currentOutline = hit.collider.GetComponent<Outline>();

            if (currentOutline != lastOutline)
            {
                if (lastOutline != null)
                    lastOutline.enabled = false;

                if (currentOutline != null)
                    currentOutline.enabled = true;

                lastOutline = currentOutline;
            }
        }
        else
        {
            if (lastOutline != null)
            {
                lastOutline.enabled = false;
                lastOutline = null;
            }
        }
    }

    Animator FindAnimator(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            Animator a = t.GetComponent<Animator>();
            if (a != null) return a;
            t = t.parent;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * rayDistance);
    }
}
