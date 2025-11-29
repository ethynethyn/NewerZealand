using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ObjectInteraction : MonoBehaviour
{
    [Header("Player Settings")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public float holdTime = 1f; // How long to hold E
    private float holdTimer = 0f;
    private bool playerInRange = false;

    [Header("UI Settings")]
    public TextMeshProUGUI promptUI;
    public string promptText = "Hold E to consume";
    public Slider holdProgressSlider; // Optional progress bar

    [System.Serializable]
    public class StatChange
    {
        public Character targetCharacter;
        public string statName;
        public float changeAmount;
    }

    [Header("Stat Modifications")]
    public List<StatChange> statChanges;

    [Header("Optional Animation")]
    public Animator objectAnimator;
    public string interactAnimationName = "Interact";

    [Header("Events")]
    public UnityEvent onInteractionSuccess;
    public UnityEvent onInteractionFail;

    [Header("Consumption Settings")]
    public bool destroyAfterConsume = true; // Destroy THIS object after hold completes

    private void Start()
    {
        if (promptUI != null)
            promptUI.gameObject.SetActive(false);

        if (holdProgressSlider != null)
        {
            holdProgressSlider.gameObject.SetActive(false);
            holdProgressSlider.minValue = 0f;
            holdProgressSlider.maxValue = holdTime;
            holdProgressSlider.value = 0f;
        }
    }

    private void Update()
    {
        if (!playerInRange)
        {
            ResetHold();
            return;
        }

        if (Input.GetKey(interactKey))
        {
            holdTimer += Time.deltaTime;

            if (holdProgressSlider != null)
            {
                holdProgressSlider.gameObject.SetActive(true);
                holdProgressSlider.value = holdTimer;
            }

            if (holdTimer >= holdTime)
            {
                ExecuteInteraction();
                ConsumeObject();
            }
        }
        else if (holdTimer > 0f)
        {
            ResetHold(); // Player released early
        }
    }

    private void ResetHold()
    {
        holdTimer = 0f;
        if (holdProgressSlider != null)
        {
            holdProgressSlider.value = 0f;
            holdProgressSlider.gameObject.SetActive(false);
        }
    }

    private void ExecuteInteraction()
    {
        bool success = false;

        // Apply stat changes
        foreach (var change in statChanges)
        {
            if (change.targetCharacter != null)
            {
                change.targetCharacter.ModifyStat(change.statName, change.changeAmount);
                success = true;
                Debug.Log($"Changed {change.statName} by {change.changeAmount} on {change.targetCharacter.characterName}");
            }
        }

        // Trigger animation if assigned
        if (objectAnimator != null && !string.IsNullOrEmpty(interactAnimationName))
            objectAnimator.SetTrigger(interactAnimationName);

        // Invoke events
        if (success)
            onInteractionSuccess.Invoke();
        else
            onInteractionFail.Invoke();
    }

    private void ConsumeObject()
    {
        if (destroyAfterConsume)
            Destroy(gameObject); // Destroy the object itself
        else
            gameObject.SetActive(false); // Just hide it if desired
    }

    private void OnTriggerEnter(Collider other)
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

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (promptUI != null)
                promptUI.gameObject.SetActive(false);

            ResetHold();
        }
    }
}
