using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class StatInteractionTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Tooltip("The key to press for interaction")]
    public KeyCode interactKey = KeyCode.E;

    public TextMeshProUGUI promptUI;
    public string promptText = "Press E to interact";

    [Header("Target Settings")]
    public Character targetCharacter;
    public string statToModify = "Health";
    public float statChangeAmount = 10f;

    [Header("Optional Cost Settings")]
    public string costStat = "Energy";
    public float costAmount = 5f;
    public bool requireCost = false;

    [Header("Cooldown Settings")]
    public float cooldownTime = 1f;
    private float lastUseTime = -999f;

    [Header("Destruction Settings")]
    public bool destroyOnSuccess = false;
    public bool destroyParentInstead = true;
    public float destroyDelay = 0f;

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

        if (GetKeyOrMouseDown(interactKey))
        {
            TryInteract();
        }
    }

    bool GetKeyOrMouseDown(KeyCode key)
    {
        if (key == KeyCode.Mouse0) return Input.GetMouseButtonDown(0);
        if (key == KeyCode.Mouse1) return Input.GetMouseButtonDown(1);
        if (key == KeyCode.Mouse2) return Input.GetMouseButtonDown(2);
        if (key == KeyCode.Mouse3) return Input.GetMouseButtonDown(3);
        if (key == KeyCode.Mouse4) return Input.GetMouseButtonDown(4);
        if (key == KeyCode.Mouse5) return Input.GetMouseButtonDown(5);
        if (key == KeyCode.Mouse6) return Input.GetMouseButtonDown(6);

        return Input.GetKeyDown(key);
    }

    void TryInteract()
    {
        if (targetCharacter == null)
        {
            Debug.LogWarning("No target Character assigned.");
            return;
        }

        NightRecapManager recapManager = FindObjectOfType<NightRecapManager>();

        // Cost check
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
                if (costStat == "Money" && recapManager != null)
                {
                    recapManager.AddExpense(costAmount);
                }

                targetCharacter.ModifyStat(costStat, -costAmount);
            }
        }

        // Apply stat change
        targetCharacter.ModifyStat(statToModify, statChangeAmount);
        lastUseTime = Time.time;

        Debug.Log($"Stat '{statToModify}' changed by {statChangeAmount} on {targetCharacter.characterName}.");

        onInteractionSuccess.Invoke();

        // 🔥 NEW: Handle destruction
        HandleDestruction();
    }

    void HandleDestruction()
    {
        if (!destroyOnSuccess) return;

        GameObject targetToDestroy = destroyParentInstead && transform.parent != null
            ? transform.parent.gameObject
            : gameObject;

        if (destroyDelay > 0f)
            Destroy(targetToDestroy, destroyDelay);
        else
            Destroy(targetToDestroy);
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