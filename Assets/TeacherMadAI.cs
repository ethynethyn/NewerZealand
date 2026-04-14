using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class TeacherAI : MonoBehaviour
{
    [Header("Chase Settings")]
    public Transform player;
    private NavMeshAgent agent;

    [Header("Catch Settings")]
    public float catchTimeRequired = 2f;
    private float catchTimer = 0f;
    private bool playerInZone = false;

    [Header("UI")]
    public TextMeshProUGUI caughtText; // TMP instead of Image

    [Header("Detention")]
    public Transform detentionPoint;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (caughtText != null)
            caughtText.enabled = false;
    }

    private void Update()
    {
        if (player == null) return;

        // Always chase player
        agent.SetDestination(player.position);

        if (playerInZone)
        {
            catchTimer += Time.deltaTime;

            if (caughtText != null)
            {
                caughtText.enabled = true;

                // OPTIONAL: show countdown
                float timeLeft = catchTimeRequired - catchTimer;
                caughtText.text = "DETECTED: " + timeLeft.ToString("F1");
            }

            if (catchTimer >= catchTimeRequired)
            {
                SendToDetention();
            }
        }
        else
        {
            catchTimer = 0f;

            if (caughtText != null)
                caughtText.enabled = false;
        }
    }

    private void SendToDetention()
    {
        // If using CharacterController, disable before teleport
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = detentionPoint.position;

        if (cc != null) cc.enabled = true;

        catchTimer = 0f;
        playerInZone = false;

        if (caughtText != null)
            caughtText.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = false;
    }
}