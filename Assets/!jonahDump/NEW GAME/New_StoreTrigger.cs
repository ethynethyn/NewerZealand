using UnityEngine;

/// <summary>
/// Stick this on Mr Green (or the counter) with a trigger collider.
/// Walk up, press the key, shop opens.
/// </summary>
public class New_StoreTrigger : MonoBehaviour
{
    public New_Store store;
    public string playerTag = "Player";

    [Tooltip("Off = press the key while standing in the trigger. On = opens the second you touch it.")]
    public bool openOnTouch = false;
    public KeyCode interactKey = KeyCode.Z;

    bool playerInside;

    void Update()
    {
        if (New_Store.IsOpen) return;
        if (openOnTouch) return;

        if (playerInside && Input.GetKeyDown(interactKey)) OpenStore();
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
        if (openOnTouch) OpenStore();
    }

    /// <summary>Also fine to hook to a dialogue node or a UnityEvent.</summary>
    public void OpenStore()
    {
        if (store != null) store.Open();
        else Debug.LogWarning("New_StoreTrigger: store not assigned.", this);
    }
}
