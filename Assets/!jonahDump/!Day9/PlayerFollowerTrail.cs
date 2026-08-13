using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Goes on the player. Once activated it drops a breadcrumb trail behind itself
/// and parks each assigned follower a set distance back along that trail, so
/// they walk the path you actually walked instead of cutting corners.
/// Each follower gets its own height and spacing nudge on top of the shared ones.
/// </summary>
public class PlayerFollowerTrail : MonoBehaviour
{
    [System.Serializable]
    public class FollowerSlot
    {
        [Tooltip("The object that follows.")]
        public Transform transform;

        [Tooltip("Height nudge for this one only, added on top of the shared offset. Use it when a character's pivot sits differently to the rest.")]
        public float extraHeight;

        [Tooltip("Extra gap in front of this one only, added on top of the shared spacing. Use it for bulky characters.")]
        public float extraSpacing;

        // Runtime only, not saved with the scene.
        [System.NonSerialized] public Vector3 velocity;
        [System.NonSerialized] public bool placed;
        [System.NonSerialized] public bool checkedPhysics;
    }

    [Header("Followers")]
    [Tooltip("Top of the list follows closest. Disabled ones are skipped and everyone behind moves up.")]
    [SerializeField] private List<FollowerSlot> followers = new List<FollowerSlot>();

    [Header("Activation")]
    [Tooltip("Start following as soon as this component is enabled. Untick if you'd rather call Activate() yourself.")]
    [SerializeField] private bool activateOnEnable = true;

    [Header("Spacing")]
    [Tooltip("Shared gap between followers, measured along the walked path rather than straight line.")]
    [SerializeField] private float spacing = 1.5f;

    [Tooltip("How far you move before a breadcrumb is dropped. Smaller = tighter corners, slightly more work.")]
    [SerializeField] private float sampleDistance = 0.15f;

    [Header("Motion")]
    [Tooltip("Extra lag on top of the trail delay. 0 = sits exactly on the path.")]
    [SerializeField] private float smoothTime = 0f;

    [Tooltip("Turn to face the direction of travel along the path.")]
    [SerializeField] private bool faceTravelDirection = true;

    [Tooltip("Degrees per second for that turn. 0 or less snaps instantly.")]
    [SerializeField] private float turnSpeed = 540f;

    [Tooltip("Keep followers upright instead of pitching with slopes.")]
    [SerializeField] private bool yAxisOnly = true;

    [Tooltip("Shared height offset applied to everyone. Per-character nudges add on top of this.")]
    [SerializeField] private float heightOffset = 0f;

    [Tooltip("Switch any follower Rigidbody to kinematic with no gravity, so physics stops dragging them to the floor.")]
    [SerializeField] private bool takeOverRigidbodies = true;

    [Header("Debug")]
    [SerializeField] private bool drawTrailGizmo = false;

    // Breadcrumbs, newest first. The player's live position is the head and isn't stored here.
    private readonly List<Vector3> trail = new List<Vector3>();
    private readonly List<FollowerSlot> activeSlots = new List<FollowerSlot>();

    private bool isActive;
    private Vector3 activationPosition;
    private float stallTimer;
    private bool hasEverMoved;
    private bool stallWarned;

    public bool IsActive => isActive;

    private void OnEnable()
    {
        if (activateOnEnable) Activate();
    }

    private void LateUpdate()
    {
        if (!isActive) return;

        CheckForStall();
        RebuildActiveList();
        RecordCrumb();
        PlaceFollowers(false);
        TrimTrail();
    }

    // ---------- public API ----------

    /// <summary>Starts the whole thing and drops everyone currently enabled into formation.</summary>
    public void Activate()
    {
        isActive = true;
        activationPosition = transform.position;
        stallTimer = 0f;
        hasEverMoved = false;
        stallWarned = false;
        SnapToFormation();
    }

    /// <summary>Stops following. Everyone stays exactly where they are.</summary>
    public void Deactivate()
    {
        isActive = false;
    }

    /// <summary>Adds someone to the back of the line with their own height and spacing nudges.</summary>
    public FollowerSlot AddFollower(Transform follower, float extraHeight = 0f, float extraSpacing = 0f)
    {
        if (follower == null) return null;

        FollowerSlot existing = FindSlot(follower);
        if (existing != null) return existing;

        FollowerSlot slot = new FollowerSlot
        {
            transform = follower,
            extraHeight = extraHeight,
            extraSpacing = extraSpacing
        };

        followers.Add(slot);
        return slot;
    }

    /// <summary>Drops someone from the line. Everyone behind shuffles forward.</summary>
    public void RemoveFollower(Transform follower)
    {
        FollowerSlot slot = FindSlot(follower);
        if (slot != null) followers.Remove(slot);
    }

    /// <summary>Changes one character's height nudge at runtime, e.g. when they crouch or transform.</summary>
    public void SetExtraHeight(Transform follower, float extraHeight)
    {
        FollowerSlot slot = FindSlot(follower);
        if (slot != null) slot.extraHeight = extraHeight;
    }

    public FollowerSlot FindSlot(Transform follower)
    {
        for (int i = 0; i < followers.Count; i++)
            if (followers[i] != null && followers[i].transform == follower) return followers[i];

        return null;
    }

    /// <summary>
    /// Fakes a trail running straight back from the player and drops everyone onto it.
    /// Call after a teleport or scene load so nobody walks the old path to catch up.
    /// </summary>
    public void SnapToFormation()
    {
        trail.Clear();
        RebuildActiveList();

        float step = Mathf.Max(sampleDistance, 0.01f);
        int crumbs = Mathf.Max(2, Mathf.CeilToInt((NeededDistance() + step * 2f) / step));

        for (int i = 1; i <= crumbs; i++)
            trail.Add(transform.position - transform.forward * (step * i));

        PlaceFollowers(true);
    }

    // ---------- diagnostics ----------

    /// <summary>No trail gets laid if this transform never moves, so say so out loud.</summary>
    private void CheckForStall()
    {
        if (hasEverMoved || stallWarned) return;

        if (Vector3.Distance(transform.position, activationPosition) > 0.05f)
        {
            hasEverMoved = true;
            return;
        }

        stallTimer += Time.deltaTime;
        if (stallTimer < 3f) return;

        stallWarned = true;
        Debug.LogWarning(
            $"{name}: PlayerFollowerTrail has been active for 3s without this transform moving, " +
            "so no trail is being recorded and followers will sit still. " +
            "Put this script on the object whose transform actually moves.", this);
    }

    /// <summary>Stops gravity and CharacterControllers from fighting the positions we write.</summary>
    private void NeutralisePhysics(Transform follower)
    {
        Rigidbody body = follower.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        CharacterController controller = follower.GetComponent<CharacterController>();
        if (controller != null && controller.enabled)
        {
            Debug.LogWarning($"{follower.name} has an enabled CharacterController, which will fight the follow positions. Disable it on followers.", follower);
        }
    }

    // ---------- roster ----------

    private void RebuildActiveList()
    {
        activeSlots.Clear();

        for (int i = 0; i < followers.Count; i++)
        {
            FollowerSlot slot = followers[i];
            if (slot == null || slot.transform == null) continue;

            if (!slot.transform.gameObject.activeInHierarchy)
            {
                // Forget it, so it snaps to its slot rather than sliding there when it comes back.
                slot.placed = false;
                continue;
            }

            if (!slot.checkedPhysics && takeOverRigidbodies)
            {
                slot.checkedPhysics = true;
                NeutralisePhysics(slot.transform);
            }

            activeSlots.Add(slot);
        }
    }

    // ---------- trail ----------

    private void RecordCrumb()
    {
        if (trail.Count == 0 || Vector3.Distance(transform.position, trail[0]) >= sampleDistance)
            trail.Insert(0, transform.position);
    }

    /// <summary>Walks back along the trail and finds the exact point that far behind the player.</summary>
    private bool TryGetTrailPoint(float distanceBack, out Vector3 position, out Vector3 direction)
    {
        Vector3 current = transform.position;
        float remaining = distanceBack;

        for (int i = 0; i < trail.Count; i++)
        {
            Vector3 older = trail[i];
            Vector3 segment = older - current;
            float length = segment.magnitude;

            if (length < 0.0001f)
            {
                current = older;
                continue;
            }

            if (length >= remaining)
            {
                position = current + segment * (remaining / length);
                direction = -segment / length; // travel runs from the older point toward the newer one
                return true;
            }

            remaining -= length;
            current = older;
        }

        // Trail isn't long enough yet, so leave them be this frame.
        position = current;
        direction = transform.forward;
        return false;
    }

    private void TrimTrail()
    {
        float keep = NeededDistance() + sampleDistance * 2f;
        float walked = 0f;
        Vector3 previous = transform.position;

        for (int i = 0; i < trail.Count; i++)
        {
            walked += Vector3.Distance(previous, trail[i]);
            previous = trail[i];

            if (walked > keep)
            {
                trail.RemoveRange(i + 1, trail.Count - i - 1);
                return;
            }
        }
    }

    private float NeededDistance()
    {
        float total = 0f;
        for (int i = 0; i < activeSlots.Count; i++)
            total += spacing + activeSlots[i].extraSpacing;

        return Mathf.Max(total, spacing);
    }

    // ---------- placement ----------

    private void PlaceFollowers(bool instant)
    {
        float distanceBack = 0f;

        for (int i = 0; i < activeSlots.Count; i++)
        {
            FollowerSlot slot = activeSlots[i];

            // Each character's own gap stacks onto the running total.
            distanceBack += spacing + slot.extraSpacing;

            if (!TryGetTrailPoint(distanceBack, out Vector3 point, out Vector3 direction)) continue;

            point.y += heightOffset + slot.extraHeight;

            // Anyone who hasn't been positioned yet gets dropped straight onto their slot.
            bool snap = instant || smoothTime <= 0f || !slot.placed;

            if (snap)
            {
                slot.transform.position = point;
                slot.velocity = Vector3.zero;
            }
            else
            {
                slot.transform.position = Vector3.SmoothDamp(slot.transform.position, point, ref slot.velocity, smoothTime);
            }

            if (faceTravelDirection)
            {
                if (yAxisOnly) direction.y = 0f;

                if (direction.sqrMagnitude >= 0.0001f)
                {
                    Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    slot.transform.rotation = (snap || turnSpeed <= 0f)
                        ? desired
                        : Quaternion.RotateTowards(slot.transform.rotation, desired, turnSpeed * Time.deltaTime);
                }
            }

            slot.placed = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawTrailGizmo || trail.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector3 previous = transform.position;
        foreach (Vector3 point in trail)
        {
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}