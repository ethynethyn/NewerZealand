using UnityEngine;

/// <summary>
/// A star sitting in the world. Same deal as New_ItemPickup:
/// trigger collider, 2D or 3D, both handled.
///
/// Each star needs its own ID so the game knows which ones you already took.
/// Leave starID blank and it builds one automatically from the scene name
/// and the star's position, which is unique enough for anything sane.
/// </summary>
public class New_StarPickup : MonoBehaviour
{
    [Tooltip("Leave blank to auto-generate. Only fill this in if you want a hand-written id.")]
    public string starID = "";

    [Tooltip("How many stars this one is worth. Usually 1.")]
    public int value = 1;

    public string playerTag = "Player";

    [Header("Interaction")]
    [Tooltip("Off = walk over it. On = stand on it and press the key.")]
    public bool requireKeyPress = false;
    public KeyCode interactKey = KeyCode.Z;

    bool playerInside;

    void Awake()
    {
        if (string.IsNullOrEmpty(starID))
        {
            Vector3 p = transform.position;
            starID = gameObject.scene.name + "_star_"
                   + Mathf.RoundToInt(p.x * 10f) + "_"
                   + Mathf.RoundToInt(p.y * 10f) + "_"
                   + Mathf.RoundToInt(p.z * 10f);
        }
    }

    void Start()
    {
        // already grabbed this one on a previous visit, stay gone
        if (New_StarFlags.HasCollected(starID))
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

    /// <summary>Also fine to call from a UnityEvent or a dialogue node.</summary>
    public void Pickup()
    {
        if (New_StarFlags.Collect(starID, value))
        {
            gameObject.SetActive(false);
        }
    }
}
