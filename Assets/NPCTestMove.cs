using UnityEngine;
using UnityEngine.AI;

public class NPCTestMove : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform testTarget;

    void Start()
    {
        Invoke(nameof(TestMove), 2f);
    }

    void TestMove()
    {
        Debug.Log("[TEST] Forcing movement");

        agent.isStopped = false;
        agent.ResetPath();

        agent.SetDestination(testTarget.position);

        Debug.Log("[TEST] Destination set: " + testTarget.position);
    }
}