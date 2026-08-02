using UnityEngine;

// Put this on a CHILD object of a ClassroomZone that has a Collider (e.g. a
// BoxCollider) marked as a trigger. It detects the player walking in/out and
// reports to the PlayerScheduleBrain, which owns ALL door + lock-in decisions.
//
// The player needs a Collider + (Rigidbody or CharacterController) for trigger
// events to fire, and should be tagged "Player".
[RequireComponent(typeof(Collider))]
public class ClassTriggerZone : MonoBehaviour
{
    private ClassroomZone zone;
    private PlayerScheduleBrain brain;

    public ClassroomZone Zone => zone;

    void Awake()
    {
        // Force trigger mode so overlaps fire OnTriggerEnter/Exit.
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Find the ClassroomZone this trigger lives under and register with it.
        zone = GetComponentInParent<ClassroomZone>();
        if (zone != null)
            zone.trigger = this;
        else
            Debug.LogError("❌ ClassTriggerZone has no parent ClassroomZone: " + name);
    }

    void Start()
    {
        brain = FindObjectOfType<PlayerScheduleBrain>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (brain == null) brain = FindObjectOfType<PlayerScheduleBrain>();
        if (brain != null && zone != null)
            brain.OnPlayerEnteredClassTrigger(zone);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (brain == null) brain = FindObjectOfType<PlayerScheduleBrain>();
        if (brain != null && zone != null)
            brain.OnPlayerExitedClassTrigger(zone);
    }

    bool IsPlayer(Collider other)
    {
        // Primary: the built-in "Player" tag.
        if (other.CompareTag("Player")) return true;
        // Fallback: anything carrying the player's brain in its hierarchy.
        if (other.GetComponentInParent<PlayerScheduleBrain>() != null) return true;
        return false;
    }
}