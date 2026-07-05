using UnityEngine;

// One grain of sugar.
// Lightweight: it just exposes its velocity (for the "has it settled?" check)
// and remembers which cup it's currently sitting inside (null = loose / still in play).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SugarGrain : MonoBehaviour
{
    private Rigidbody2D rb;

    // Set by SugarCup triggers. null means this grain is loose (counts as "in play").
    [System.NonSerialized] public SugarCup currentCup;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // Unity 6 renamed Rigidbody2D.velocity -> linearVelocity. This keeps it working on both.
    public Vector2 Velocity
    {
#if UNITY_6000_0_OR_NEWER
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
#else
        get => rb.velocity;
        set => rb.velocity = value;
#endif
    }

    public void SetVelocity(Vector2 v) => Velocity = v;

    // A sleeping body (Unity auto-sleeps slow ones) also counts as settled.
    public bool IsSettled(float velocityThreshold)
    {
        if (rb == null) return true;
        if (rb.IsSleeping()) return true;
        return Velocity.sqrMagnitude <= velocityThreshold * velocityThreshold;
    }
}
