using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// A trimmed-down version of your RaycastTriggerInteraction (no Animator logic).
/// Looks at what's under the crosshair; shows the item's name label; picks it up on E.
/// Optional Outline highlight (uses the same Outline component your other script uses).
/// </summary>
public class WorldItemInteractor : MonoBehaviour
{
    [Header("Camera / Ray")]
    public Camera playerCamera;
    public float rayDistance = 3f;
    [Tooltip("Set this to ONLY your world-item layer so it doesn't clash with the physics-grab system.")]
    public LayerMask itemLayer = ~0;

    [Header("Input")]
    public Key pickupKey = Key.E;

    [Header("Name Label (like your tooltip UI)")]
    [Tooltip("A screen-space TMP text that follows the looked-at item.")]
    public TMP_Text nameLabel;
    public Vector3 labelWorldOffset = new Vector3(0f, 0.5f, 0f);
    public float labelFollowSpeed = 12f;

    [Header("Outline (optional)")]
    public bool useOutline = false;

    WorldItem current;
    Outline lastOutline;

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (nameLabel != null) nameLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (InventoryPanelUI.IsOpen || Time.timeScale == 0f)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(Raycast());

        if (current != null && Keyboard.current != null && Keyboard.current[pickupKey].wasPressedThisFrame)
        {
            if (current.TryPickup())
            {
                if (useOutline && lastOutline != null) { lastOutline.enabled = false; lastOutline = null; }
                Destroy(current.gameObject);
                SetCurrent(null);
            }
        }

        UpdateLabel();
    }

    WorldItem Raycast()
    {
        if (playerCamera == null) return null;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, itemLayer))
            return hit.collider.GetComponentInParent<WorldItem>();
        return null;
    }

    void SetCurrent(WorldItem wi)
    {
        if (wi == current) return;
        current = wi;

        if (useOutline)
        {
            if (lastOutline != null) lastOutline.enabled = false;
            lastOutline = current != null ? current.GetComponentInParent<Outline>() : null;
            if (lastOutline != null) lastOutline.enabled = true;
        }

        if (nameLabel != null)
        {
            if (current != null)
            {
                nameLabel.text = current.DisplayName;
                nameLabel.gameObject.SetActive(true);
            }
            else
            {
                nameLabel.gameObject.SetActive(false);
            }
        }
    }

    void UpdateLabel()
    {
        if (nameLabel == null || current == null || !nameLabel.gameObject.activeSelf) return;

        Vector3 world = current.transform.position + labelWorldOffset;
        Vector3 screen = playerCamera.WorldToScreenPoint(world);
        if (screen.z < 0f) { nameLabel.gameObject.SetActive(false); return; }

        nameLabel.transform.position =
            Vector3.Lerp(nameLabel.transform.position, screen, Time.deltaTime * labelFollowSpeed);
    }
}
