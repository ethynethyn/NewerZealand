using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using StarterAssets;

public class TypingInteractionZone : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";
    private bool playerInZone = false;

    [Header("UI")]
    public GameObject interactIcon;
    public GameObject typingCanvas;
    public TMP_Text promptText;
    public TMP_InputField inputField;

    [Header("Typing Prompts")]
    public List<string> prompts = new List<string>();

    [Header("Wrong Answer Objects")]
    public List<GameObject> incorrectObjects = new List<GameObject>();
    public float incorrectDisplayTime = 1f;

    [System.Serializable]
    public class StatChange
    {
        public Character targetCharacter;
        public string statName;
        public float amount;
    }

    [System.Serializable]
    public class Outcome
    {
        [Range(0, 100)]
        public float chance;

        [TextArea]
        public string message;

        public List<StatChange> statChanges = new List<StatChange>();
    }

    [System.Serializable]
    public class PostCompletionObject
    {
        public GameObject obj;
        [Range(0, 100)]
        public float activationChance = 100f;
        public bool deactivateInstead = false;
    }

    [Header("Outcomes")]
    public List<Outcome> outcomes = new List<Outcome>();

    [Header("Post Completion")]
    public List<PostCompletionObject> postCompletionObjects = new List<PostCompletionObject>();

    [Header("Destroy Settings")]
    public bool destroyOnComplete = false;

    private int currentPromptIndex = 0;
    private bool isInteracting = false;

    private PlayerInput playerInput;
    private StarterAssetsInputs starterInputs;

    void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        starterInputs = FindObjectOfType<StarterAssetsInputs>();

        if (interactIcon != null)
            interactIcon.SetActive(false);

        if (typingCanvas != null)
            typingCanvas.SetActive(false);
    }

    void Update()
    {
        if (playerInZone && !isInteracting && Input.GetKeyDown(KeyCode.E))
        {
            StartInteraction();
        }

        if (isInteracting)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                CheckInput();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitInteraction();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;

            if (interactIcon != null)
                interactIcon.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;

            if (interactIcon != null)
                interactIcon.SetActive(false);
        }
    }

    void StartInteraction()
    {
        isInteracting = true;

        Time.timeScale = 0f;

        if (playerInput != null)
            playerInput.DeactivateInput();

        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (typingCanvas != null)
            typingCanvas.SetActive(true);

        if (interactIcon != null)
            interactIcon.SetActive(false);

        currentPromptIndex = 0;
        ShowPrompt();
    }

    void ShowPrompt()
    {
        if (prompts.Count == 0) return;

        promptText.text = prompts[currentPromptIndex];

        inputField.text = "";
        inputField.ActivateInputField();
    }

    void CheckInput()
    {
        string correct = prompts[currentPromptIndex];
        string playerInput = inputField.text;

        if (playerInput == correct)
        {
            currentPromptIndex++;

            if (currentPromptIndex >= prompts.Count)
            {
                CompleteInteraction();
            }
            else
            {
                ShowPrompt();
            }
        }
        else
        {
            StartCoroutine(HandleIncorrect());
        }
    }

    IEnumerator HandleIncorrect()
    {
        foreach (GameObject obj in incorrectObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(incorrectDisplayTime);

        foreach (GameObject obj in incorrectObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        ShowPrompt();
    }

    void ExitInteraction()
    {
        isInteracting = false;

        Time.timeScale = 1f;

        if (typingCanvas != null)
            typingCanvas.SetActive(false);

        if (playerInput != null)
            playerInput.ActivateInput();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (interactIcon != null && playerInZone)
            interactIcon.SetActive(true);
    }

    void CompleteInteraction()
    {
        isInteracting = false;

        Time.timeScale = 1f;

        if (typingCanvas != null)
            typingCanvas.SetActive(false);

        if (playerInput != null)
            playerInput.ActivateInput();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Outcome outcome = RunRandomOutcome();

        if (outcome != null)
            ApplyOutcome(outcome);

        ProcessPostCompletionObjects();

        if (destroyOnComplete)
            gameObject.SetActive(false);
    }

    Outcome RunRandomOutcome()
    {
        if (outcomes.Count == 0)
            return null;

        float total = 0f;
        foreach (var o in outcomes)
            total += o.chance;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var o in outcomes)
        {
            cumulative += o.chance;
            if (roll <= cumulative)
                return o;
        }

        return outcomes[outcomes.Count - 1];
    }

    void ApplyOutcome(Outcome outcome)
    {
        NightRecapManager recapManager = FindObjectOfType<NightRecapManager>();

        foreach (var change in outcome.statChanges)
        {
            if (change.targetCharacter != null)
            {
                if (change.statName == "Money" && change.amount > 0 && recapManager != null)
                {
                    recapManager.AddEarnings(change.amount);
                }

                change.targetCharacter.ModifyStat(change.statName, change.amount);
            }
        }

        Debug.Log(outcome.message);
    }

    void ProcessPostCompletionObjects()
    {
        foreach (var postObj in postCompletionObjects)
        {
            if (postObj.obj == null) continue;

            float roll = Random.Range(0f, 100f);
            if (roll <= postObj.activationChance)
            {
                postObj.obj.SetActive(!postObj.deactivateInstead);
            }
        }
    }
}