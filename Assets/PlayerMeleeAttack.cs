using UnityEngine;
using System.Collections;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Attack Settings")]
    public float attackRange = 2.5f;
    public float attackDelay = 0.2f;
    public float impactDelay = 0.1f;

    [Header("Damage")]
    public float damage = -1f;

    [Header("Physics Impact")]
    public float forceAmount = 6f;

    [Header("Input")]
    public KeyCode attackKey = KeyCode.Mouse0;

    private bool canAttack = true;

    void Update()
    {
        if (Input.GetKeyDown(attackKey) && canAttack)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;

        // windup
        yield return new WaitForSeconds(attackDelay);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange))
        {
            // impact timing
            yield return new WaitForSeconds(impactDelay);

            NPCDeath npc = hit.collider.GetComponentInParent<NPCDeath>();

            bool canDamage = true;

            // -------------------------
            // HIT VALIDATION (NPC ONLY)
            // -------------------------
            if (npc != null)
            {
                canDamage = npc.TryHit();
            }

            if (canDamage)
            {
                // -------------------------
                // HIT ANIMATION
                // -------------------------
                Animator anim = hit.collider.GetComponentInParent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Hit");
                }

                // -------------------------
                // DAMAGE SYSTEM
                // -------------------------
                Character character = hit.collider.GetComponentInParent<Character>();
                if (character != null)
                {
                    character.ModifyStat("Health", damage);

                    float hp = character.GetStatValue("Health");

                    if (hp <= 0 && npc != null)
                    {
                        npc.Die();
                    }
                }
            }

            // -------------------------
            // PHYSICS IMPACT (NON-NPC ONLY)
            // -------------------------
            Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();

            if (rb != null && npc == null)
            {
                Vector3 forceDir = playerCamera.transform.forward;
                rb.AddForce(forceDir * forceAmount, ForceMode.Impulse);
            }
        }

        // attack cooldown
        yield return new WaitForSeconds(0.2f);
        canAttack = true;
    }
}