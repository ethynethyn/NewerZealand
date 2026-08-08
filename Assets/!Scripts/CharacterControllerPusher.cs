using UnityEngine;

/// <summary>
/// OPTIONAL — only needed if your PLAYER moves with a CharacterController.
/// A CharacterController won't push Rigidbodies on its own; this makes it shove
/// dynamic bodies it walks into (like the boss during his break).
///
/// If your player already uses a Rigidbody, you don't need this at all — normal
/// collisions will push the boss.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CharacterControllerPusher : MonoBehaviour
{
    [Tooltip("How hard the player shoves dynamic Rigidbodies.")]
    public float pushForce = 4f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;   // nothing to push (e.g. boss is frozen mid-attack)

        // Push horizontally only, so we don't drive things down through the floor.
        Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

        // If you're on Unity 6+, you can use body.linearVelocity instead of body.velocity.
        body.linearVelocity = dir * pushForce;
    }
}
