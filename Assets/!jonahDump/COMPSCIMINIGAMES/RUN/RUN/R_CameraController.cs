using UnityEngine;

// Follows the player from behind + above and rolls its "up" to match gravity,
// so the tunnel appears to rotate around you. That's the signature Run look.
[DisallowMultipleComponent]
public class R_CameraController : MonoBehaviour
{
    [Header("Target")]
    public R_PlayerController player;

    [Header("Framing")]
    public float distance = 9f;    // how far behind the player
    public float height = 3.5f;    // how far along "up" (away from the floor)
    public float lookAhead = 6f;   // aim point ahead down the tunnel

    [Header("Smoothing (higher = snappier)")]
    public float moveLerp = 10f;
    public float rollLerp = 8f;

    Vector3 currentUp = Vector3.up;

    void Start()
    {
        if (player == null) player = FindObjectOfType<R_PlayerController>();
        if (player != null)
        {
            currentUp = -player.GravityDir;
            SnapToTarget();
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        float dt = Time.deltaTime;

        // ease the up-vector toward the current floor's "up" (frame-rate independent)
        Vector3 targetUp = -player.GravityDir;
        currentUp = Vector3.Slerp(currentUp, targetUp, 1f - Mathf.Exp(-rollLerp * dt));

        Vector3 p = player.transform.position;
        Vector3 targetPos = p - Vector3.forward * distance + currentUp * height;
        transform.position = Vector3.Lerp(transform.position, targetPos,
                                          1f - Mathf.Exp(-moveLerp * dt));

        Vector3 lookAt = p + Vector3.forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(lookAt - transform.position, currentUp);
    }

    void SnapToTarget()
    {
        Vector3 p = player.transform.position;
        transform.position = p - Vector3.forward * distance + currentUp * height;
        Vector3 lookAt = p + Vector3.forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(lookAt - transform.position, currentUp);
    }
}
