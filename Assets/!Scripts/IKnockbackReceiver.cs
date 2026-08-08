using UnityEngine;

/// <summary>
/// Anything the boss can shove implements this. Both PlayerKnockback and
/// SimplePlayerController implement it, so the boss's kick works with either.
/// </summary>
public interface IKnockbackReceiver
{
    void ApplyKnockback(Vector3 velocity);
}
