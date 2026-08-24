using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on a TRIGGER collider (a Box Collider covering the classroom works best).
///
///   1. While the player is inside, Granny won't lock on / chase.
///   2. Granny uses this collider's boundary to stay JUST OUTSIDE the room (she never targets
///      a point inside, so she physically can't path in — she jams at the rim).
///   3. If Granny crosses in anyway, she's told to back off.
///
/// Requirements:
///   - "Is Trigger" ticked.
///   - Player tagged "Player" (or change 'playerTag').
///   - Use a Box (or other convex) collider so the boundary math is accurate.
///   - Player needs a CharacterController OR Rigidbody for trigger events to fire.
///
/// Zones are tracked in a SET, not a counter. With a counter, overlapping zones desync:
/// enter A, enter B, exit B -> the count drops to 0 and CurrentZone goes null while you're
/// still standing in A. That left Granny "safe but nowhere to wait", trailing you forever.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GrannySafeZone : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    static readonly HashSet<GrannySafeZone> occupied = new HashSet<GrannySafeZone>();

    /// <summary>True while the player is inside at least one safe zone.</summary>
    public static bool PlayerInSafeZone => occupied.Count > 0;

    /// <summary>The zone the player is currently in. Never null while PlayerInSafeZone is true.</summary>
    public static GrannySafeZone CurrentZone { get; private set; }

    Collider zoneCollider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { occupied.Clear(); CurrentZone = null; }

    void Awake() { zoneCollider = GetComponent<Collider>(); }

    void OnDisable() { Vacate(this); }   // disabled/destroyed mid-play shouldn't strand the flag

    static void Occupy(GrannySafeZone zone)
    {
        occupied.Add(zone);
        CurrentZone = zone;
    }

    static void Vacate(GrannySafeZone zone)
    {
        if (!occupied.Remove(zone)) return;

        if (CurrentZone != zone) return;

        // fall back to any other zone the player is still standing in
        CurrentZone = null;
        foreach (var z in occupied)
        {
            if (z != null) { CurrentZone = z; break; }
        }
    }

    /// <summary>Point on this zone's boundary where the line from 'from' (outside) to 'insidePoint' crosses in.</summary>
    public bool TryGetEntryPoint(Vector3 from, Vector3 insidePoint, out Vector3 entry)
    {
        entry = insidePoint;
        if (zoneCollider == null) return false;

        Vector3 d = insidePoint - from;
        float dist = d.magnitude;
        if (dist < 0.001f) return false;

        Ray ray = new Ray(from, d / dist);
        if (zoneCollider.Raycast(ray, out RaycastHit hit, dist + 0.1f))
        {
            entry = hit.point;
            return true;
        }
        return false;
    }

    /// <summary>Nearest point on this zone's surface to 'p' (fallback boundary).</summary>
    public Vector3 ClosestBoundary(Vector3 p) => zoneCollider != null ? zoneCollider.ClosestPoint(p) : p;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Occupy(this);
            return;
        }

        var granny = other.GetComponentInParent<GrannyAI>();
        if (granny != null) granny.ForceRetreat();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) Vacate(this);
    }
}