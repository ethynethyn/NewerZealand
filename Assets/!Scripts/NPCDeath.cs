using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCDeath : MonoBehaviour
{
    [Header("References")]
    public GameObject aliveObject;
    public GameObject deadObject;
    public Animator animator;
    public NavMeshAgent agent;

    [Header("Hit Settings")]
    public float hitCooldown = 0.4f;
    public string hitTrigger = "Hit";

    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float wanderInterval = 3f;

    [Header("Flee Settings")]
    public float fleeDistance = 12f;
    public float fleeDuration = 2f;

    [Header("Death Settings")]
    public float deathDelay = 0.3f;

    private bool isDead = false;
    private bool canBeHit = true;
    private bool isFleeing = false;

    private Transform player;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (aliveObject == null)
            aliveObject = gameObject;

        if (deadObject != null)
            deadObject.SetActive(false);

        //  IMPORTANT: force animator from aliveObject (fixes your issue)
        if (aliveObject != null)
            animator = aliveObject.GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        StartCoroutine(WanderRoutine());
    }

    // -------------------------
    // HIT ENTRY POINT
    // -------------------------
    public bool TryHit()
    {
        if (isDead || !canBeHit)
            return false;

        Debug.Log("TryHit SUCCESS");

        //  ALWAYS re-grab animator in case setup changes
        if (animator == null && aliveObject != null)
            animator = aliveObject.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            Debug.Log("Triggering Hit animation: " + hitTrigger);
            animator.SetTrigger(hitTrigger);
        }
        else
        {
            Debug.LogWarning("No Animator found on aliveObject");
        }

        StartCoroutine(HitCooldownRoutine());
        StartCoroutine(FleeRoutine());

        return true;
    }

    IEnumerator HitCooldownRoutine()
    {
        canBeHit = false;
        yield return new WaitForSeconds(hitCooldown);
        canBeHit = true;
    }

    // -------------------------
    // WANDERING
    // -------------------------
    IEnumerator WanderRoutine()
    {
        while (!isDead)
        {
            if (!isFleeing && agent != null)
            {
                Vector3 randomPoint = Random.insideUnitSphere * wanderRadius + transform.position;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            yield return new WaitForSeconds(wanderInterval);
        }
    }

    // -------------------------
    // FLEE BEHAVIOUR
    // -------------------------
    IEnumerator FleeRoutine()
    {
        if (agent == null || player == null)
            yield break;

        isFleeing = true;

        float timer = 0f;

        while (timer < fleeDuration && !isDead)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            Vector3 targetPos = transform.position + dir * fleeDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            timer += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        isFleeing = false;
    }

    // -------------------------
    // DEATH
    // -------------------------
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);

        if (agent != null)
            agent.isStopped = true;

        if (aliveObject != null)
            aliveObject.SetActive(false);

        if (deadObject != null)
            deadObject.SetActive(true);
    }
}