using UnityEngine;
using UnityEngine.Events;
using TMPro;

// Simple class to hold a stat change
[System.Serializable]
public class StatChange
{
    public string statName = "Health";
    public float amount = 10f;
}

public class MultiStatInteractionTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public string interactKey = "e";
    public TextMeshProUGUI promptUI;          // Drag in your "Press E" UI
    public string promptText = "Press E to interact";

    [Header("Target Settings")]
    public Character targetCharacter;         // Whose stats will be modified
    public StatChange[] statChanges;          // Multiple stat changes

    [Header("Optional Cost Settings")]
    public string costStat = "Energy";
    public float costAmount = 5f;
    public bool requireCost = false;

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f;
    private float lastUseTime = -999f;

    [Header("Events")]
    public UnityEvent onInteractionSuccess;
    public UnityEvent onInteractionFail;

    private bool playerInRange = false;

    void Start()
    {
        if (promptUI != null)
            promptUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || Time.time < lastUseTime + cooldownTime)
            return;

        if (Input.GetKeyDown(interactKey.ToLower()))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (targetCharacter == null)
        {
            Debug.LogWarning("No target Character assigned.");
            return;
        }

        // Check for cost
        if (requireCost)
        {
            float currentCostStat = targetCharacter.GetStatValue(costStat);
            if (currentCostStat < costAmount)
            {
                Debug.Log("Not enough " + costStat + " to interact.");
                onInteractionFail.Invoke();
                return;
            }
            else
            {
                targetCharacter.ModifyStat(costStat, -costAmount);
            }
        }

        // Apply multiple stat changes
        foreach (StatChange change in statChanges)
        {
            targetCharacter.ModifyStat(change.statName, change.amount);
            Debug.Log($"Stat '{change.statName}' changed by {change.amount} on {targetCharacter.characterName}.");
        }

        lastUseTime = Time.time;
        onInteractionSuccess.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (promptUI != null)
            {
                promptUI.gameObject.SetActive(true);
                promptUI.text = promptText;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (promptUI != null)
                promptUI.gameObject.SetActive(false);
        }
    }
}
