using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Put this on a GameObject that has a NavMeshAgent (and sits on a baked NavMesh).
/// On start it runs to the player, grabs them, carries them to a destination, then
/// enables/disables a list of objects.
///
/// The player is "held" by pinning their position to a hold point every LateUpdate —
/// after your movement script runs — so it can't be fought by input or gravity. Enable
/// your existing movement-blocker object at the grab (Movement Blocker field) so input
/// is off while mouse-look stays on.
///
/// Setup:
///  1. Bake a NavMesh (Window > AI > Navigation) so the floor is walkable and the
///     destination is reachable.
///  2. Put this + a NavMeshAgent on the abductor object, sitting on the NavMesh.
///     Set the NavMeshAgent's Speed for how fast it runs at the player.
///  3. Assign Player, Destination, your Movement Blocker, and the object lists.
///  4. (Optional) make a child empty on the abductor where the player should be held
///     and drag it into Hold Point.
///
/// Note: Grab Distance / Arrive Distance must be >= the NavMeshAgent's Stopping Distance
/// (which defaults to 0), or the agent stops short and never triggers.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Abductor : MonoBehaviour
{
    public enum Phase { Idle, Chasing, Carrying, Done }

    [Header("Targets")]
    public Transform player;
    [Tooltip("Where the player gets taken.")]
    public Transform destination;
    [Tooltip("Optional child point where the player is held. Falls back to this object + Hold Offset.")]
    public Transform holdPoint;
    [Tooltip("Used only when Hold Point is empty. Local to the abductor (X=right, Y=up, Z=forward).")]
    public Vector3 holdOffset = new Vector3(0f, 1f, 0.6f);

    [Header("Distances")]
    [Tooltip("How close the agent must get to the player to grab.")]
    public float grabDistance = 1.5f;
    [Tooltip("How close to the destination counts as arrived.")]
    public float arriveDistance = 1.5f;

    [Header("On grab")]
    [Tooltip("Your existing object that stops player movement but keeps mouse look. Enabled when grabbed.")]
    public GameObject movementBlocker;

    [Header("On arrival")]
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    [Header("Behaviour")]
    [Tooltip("Start chasing automatically. Turn off and call BeginAbduction() to trigger it yourself.")]
    public bool beginOnStart = true;
    [Tooltip("Keep pinning the player in place after arrival. Off = the player is released at the drop point.")]
    public bool holdAfterArrival = true;

    [Header("Events")]
    public UnityEvent onGrab;
    public UnityEvent onArrive;

    NavMeshAgent agent;
    Phase phase = Phase.Idle;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Start() { if (beginOnStart) BeginAbduction(); }

    /// <summary>Kick off the chase (e.g. from a trigger or another event).</summary>
    public void BeginAbduction()
    {
        if (player == null) { Debug.LogWarning("Abductor: no Player assigned."); return; }
        if (!agent.isOnNavMesh) Debug.LogWarning("Abductor: agent isn't on a baked NavMesh — it won't move. Bake a NavMesh and place it on one.");
        phase = Phase.Chasing;
    }

    void Update()
    {
        switch (phase)
        {
            case Phase.Chasing:
                if (player == null) return;
                if (agent.isOnNavMesh) agent.SetDestination(player.position);   // re-path to the moving player
                if (HorizontalDistance(transform.position, player.position) <= grabDistance)
                    Grab();
                break;

            case Phase.Carrying:
                if (destination == null) { Debug.LogWarning("Abductor: no Destination assigned."); return; }
                if (HorizontalDistance(transform.position, destination.position) <= arriveDistance)
                    Arrive();
                break;
        }
    }

    void LateUpdate()
    {
        // Pin the player after their own movement has run, so nothing can drag them off.
        if (player == null) return;
        if (phase == Phase.Carrying || (phase == Phase.Done && holdAfterArrival))
            player.position = HoldPosition();
    }

    void Grab()
    {
        phase = Phase.Carrying;
        if (movementBlocker != null) movementBlocker.SetActive(true);   // kill input, keep mouse-look
        if (destination != null && agent.isOnNavMesh) agent.SetDestination(destination.position);
        onGrab?.Invoke();
    }

    void Arrive()
    {
        phase = Phase.Done;
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.velocity = Vector3.zero;

        if (objectsToEnable != null)
            foreach (var go in objectsToEnable) if (go != null) go.SetActive(true);
        if (objectsToDisable != null)
            foreach (var go in objectsToDisable) if (go != null) go.SetActive(false);

        onArrive?.Invoke();
    }

    Vector3 HoldPosition()
    {
        if (holdPoint != null) return holdPoint.position;
        return transform.position + transform.TransformDirection(holdOffset);
    }

    float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
