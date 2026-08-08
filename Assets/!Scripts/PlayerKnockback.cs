using UnityEngine;

/// <summary>
/// Standalone knockback for the PLAYER. Works with a CharacterController (no Rigidbody
/// needed), a Rigidbody, or a plain Transform. Attach to the exact object that's in the
/// boss's "Player" field.
///
/// It drives movement in LateUpdate — AFTER your movement script's Update — so for the
/// common case (a controller that moves via cc.Move) the shove adds on top and isn't
/// cancelled. If your controller instead writes transform.position directly every frame,
/// use SimplePlayerController (or fold the knockback into your own controller) instead.
/// </summary>
[DisallowMultipleComponent]
public class PlayerKnockback : MonoBehaviour, IKnockbackReceiver
{
    [Tooltip("How quickly the horizontal shove bleeds off (higher = snappier stop).")]
    public float decay = 6f;
    [Tooltip("How long the knockback drives movement, in seconds.")]
    public float overrideDuration = 0.35f;
    [Tooltip("Gravity applied during the shove so an upward pop arcs back down (CharacterController only).")]
    public float gravity = 25f;
    [Tooltip("Log to the Console each time a shove is received. Turn off once it works.")]
    public bool debugLog = true;

    CharacterController cc;
    Rigidbody rb;
    Vector3 vel;
    float timer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        if (cc == null && rb == null)
            Debug.LogWarning($"PlayerKnockback on '{name}': no CharacterController or Rigidbody found — it will move the Transform directly.");
    }

    public void ApplyKnockback(Vector3 velocity)
    {
        vel = velocity;
        timer = overrideDuration;
        if (debugLog)
            Debug.Log($"PlayerKnockback: shove received on '{name}'  (CharacterController={cc != null}, Rigidbody={rb != null}), velocity={velocity}");
        if (cc == null && rb != null && !rb.isKinematic)
            rb.linearVelocity = velocity;   // Unity 6+: rename to linearVelocity
    }

    void LateUpdate()
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;

        if (cc != null && cc.enabled)
        {
            vel.y -= gravity * Time.deltaTime;         // let the pop settle
            cc.Move(vel * Time.deltaTime);
        }
        else if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = vel;                         // Unity 6+: linearVelocity
        }
        else
        {
            transform.position += vel * Time.deltaTime;
        }

        // decay horizontal; gravity owns vertical
        Vector3 flat = Vector3.Lerp(new Vector3(vel.x, 0f, vel.z), Vector3.zero, decay * Time.deltaTime);
        vel = new Vector3(flat.x, vel.y, flat.z);
    }
}
