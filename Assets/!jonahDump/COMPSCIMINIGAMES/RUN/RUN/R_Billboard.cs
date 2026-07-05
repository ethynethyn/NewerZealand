using UnityEngine;

// Makes the 2D player sprite face the camera, stay upright as the world rolls,
// and float so its feet sit ON the surface instead of clipping through it.
// Put this on the sprite child. Toggle flip180 if the art shows up back-to-front.
[DisallowMultipleComponent]
public class R_Billboard : MonoBehaviour
{
    public Camera cam;
    public R_PlayerController player;   // auto-found in a parent if left empty
    public bool flip180 = false;

    [Tooltip("How far the sprite's center sits above the current floor, past the player's collider. Raise if feet clip into tiles.")]
    public float liftAboveFloor = 0.5f;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<R_PlayerController>();
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // keep the sprite lifted away from whatever surface is currently "down"
        if (player != null)
            transform.localPosition = -player.GravityDir * liftAboveFloor;

        // face the same way the camera looks, using the camera's up so we roll with it
        Vector3 fwd = cam.transform.forward;
        Vector3 up = cam.transform.up;
        transform.rotation = Quaternion.LookRotation(flip180 ? -fwd : fwd, up);
    }
}