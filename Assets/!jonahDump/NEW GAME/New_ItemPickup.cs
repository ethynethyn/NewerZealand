using UnityEngine;

/// <summary>
/// Stick this on a thing in the world with a trigger collider (2D or 3D, both handled).
/// </summary>
public class New_ItemPickup : MonoBehaviour
{
    public New_ItemID item;
    public string playerTag = "Player";

    [Tooltip("Turn the object off after pickup, and keep it off on future visits to this scene.")]
    public bool disableOnPickup = true;

    [Header("Interaction")]
    [Tooltip("Off = walk over it. On = stand on it and press the key.")]
    public bool requireKeyPress = false;
    public KeyCode interactKey = KeyCode.Z;

    bool playerInside;

    void Start()
    {
        // items are never removed, so don't respawn a pickup you already grabbed
        if (disableOnPickup && New_ItemFlags.Has(item))
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (requireKeyPress && playerInside && Input.GetKeyDown(interactKey))
        {
            Pickup();
        }
    }

    // ---- 2D ----
    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag(playerTag)) Enter(); }
    void OnTriggerExit2D(Collider2D other)  { if (other.CompareTag(playerTag)) playerInside = false; }

    // ---- 3D ----
    void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) Enter(); }
    void OnTriggerExit(Collider other)  { if (other.CompareTag(playerTag)) playerInside = false; }

    void Enter()
    {
        playerInside = true;
        if (!requireKeyPress) Pickup();
    }

    /// <summary>Also fine to call from a UnityEvent, dialogue node, or Ink external function.</summary>
    public void Pickup()
    {
        New_InventoryUI.Give(item);
        if (disableOnPickup) gameObject.SetActive(false);
    }
}
