using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Looks at loot containers with the crosshair, shows the container's name label
/// (same follow-the-object behaviour as WorldItemInteractor), and opens it with E.
/// Pressing E again while a container is open closes it.
///
/// Put containers on their own layer and set that here so this doesn't clash with
/// the world-item pickup interactor.
/// </summary>
public class LootContainerInteractor : MonoBehaviour
{
    [Header("Camera / Ray")]
    public Camera playerCamera;
    public float rayDistance = 3f;
    [Tooltip("Set to ONLY your loot-container layer.")]
    public LayerMask containerLayer = ~0;

    [Header("Input")]
    public Key interactKey = Key.E;

    [Header("Name Label (same behaviour as world items)")]
    public TMP_Text nameLabel;
    public Vector3 labelWorldOffset = new Vector3(0f, 0.5f, 0f);
    public float labelFollowSpeed = 12f;

    [Header("Outline (optional)")]
    public bool useOutline = false;

    LootContainer current;
    Outline lastOutline;

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (nameLabel != null) nameLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        bool pressed = Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;

        // While a container is open, E closes it and no crosshair label is shown.
        if (LootController.Instance != null && LootController.Instance.IsContainerOpen)
        {
            SetCurrent(null);
            if (pressed) LootController.Instance.Close();
            return;
        }

        // Suppressed while any panel is open or the game is frozen.
        if (InventoryPanelUI.IsOpen || Time.timeScale == 0f)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(Raycast());

        if (current != null && pressed && LootController.Instance != null)
        {
            LootController.Instance.OpenContainer(current);
            SetCurrent(null);
            return;
        }

        UpdateLabel();
    }

    LootContainer Raycast()
    {
        if (playerCamera == null) return null;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, containerLayer))
            return hit.collider.GetComponentInParent<LootContainer>();
        return null;
    }

    void SetCurrent(LootContainer c)
    {
        if (c == current) return;
        current = c;

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
