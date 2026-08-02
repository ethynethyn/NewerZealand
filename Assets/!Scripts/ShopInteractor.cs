using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Looks at shops with the crosshair, shows the shop's name label (same follow-the-
/// object behaviour as the loot/world interactors), and opens it with E. Pressing E
/// again while the shop is open closes it.
///
/// Shops can share a layer with loot containers if you like — this only reacts to
/// objects that actually have a Shop component.
/// </summary>
public class ShopInteractor : MonoBehaviour
{
    [Header("Camera / Ray")]
    public Camera playerCamera;
    public float rayDistance = 3f;
    [Tooltip("Set to the layer(s) your shops live on.")]
    public LayerMask shopLayer = ~0;

    [Header("Input")]
    public Key interactKey = Key.E;

    [Header("Name Label")]
    public TMP_Text nameLabel;
    public Vector3 labelWorldOffset = new Vector3(0f, 0.5f, 0f);
    public float labelFollowSpeed = 12f;

    [Header("Outline (optional)")]
    public bool useOutline = false;

    Shop current;
    Outline lastOutline;

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (nameLabel != null) nameLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        bool pressed = Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;

        // While the shop is open, E closes it and no crosshair label is shown.
        if (ShopController.Instance != null && ShopController.Instance.IsShopOpen)
        {
            SetCurrent(null);
            if (pressed) ShopController.Instance.Close();
            return;
        }

        // Suppressed while any panel is open or the game is frozen.
        if (InventoryPanelUI.IsOpen || Time.timeScale == 0f)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(Raycast());

        if (current != null && pressed && ShopController.Instance != null)
        {
            ShopController.Instance.OpenShop(current);
            SetCurrent(null);
            return;
        }

        UpdateLabel();
    }

    Shop Raycast()
    {
        if (playerCamera == null) return null;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, shopLayer))
            return hit.collider.GetComponentInParent<Shop>();
        return null;
    }

    void SetCurrent(Shop c)
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
