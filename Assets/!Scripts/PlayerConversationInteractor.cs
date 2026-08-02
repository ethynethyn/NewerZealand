using UnityEngine;
using DialogueEditor;
using System.Collections;

public class PlayerConversationInteractor : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;

    [Header("UI")]
    public GameObject interactionImage;
    public Transform dialogueButtonParent;

    [Header("Settings")]
    public float interactRange = 3f;

    private ConversationStarter currentNPC;
    private ConversationStarter previousNPC;

    void Start()
    {
        if (interactionImage != null)
            interactionImage.SetActive(false);
    }

    void Update()
    {
        DetectNPC();
        HandleEnter();
        HandleExit();
    }

    void DetectNPC()
    {
        previousNPC = currentNPC;
        currentNPC = null;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            currentNPC = hit.collider.GetComponent<ConversationStarter>();
        }

        if (interactionImage != null &&
            ConversationManager.Instance != null &&
            !ConversationManager.Instance.IsConversationActive)
        {
            interactionImage.SetActive(currentNPC != null);
        }
    }

    void HandleEnter()
    {
        if (currentNPC == null) return;
        if (ConversationManager.Instance == null) return;
        if (ConversationManager.Instance.IsConversationActive) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // Was: ConversationManager.Instance.StartConversation(currentNPC.myConversation);
            currentNPC.StartTalking(playerCamera);   // freezes the NPC + faces the player

            if (interactionImage != null)
                interactionImage.SetActive(false);

            StartCoroutine(DisableDialogueButtonsNextFrame());
        }
    }

    void HandleExit()
    {
        if (ConversationManager.Instance == null) return;
        if (!ConversationManager.Instance.IsConversationActive) return;

        // Right-click ends the conversation; looking around is now free
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            ConversationManager.Instance.EndConversation();
            if (interactionImage != null)
                interactionImage.SetActive(false);
        }
    }

    IEnumerator DisableDialogueButtonsNextFrame()
    {
        yield return null;
        yield return new WaitForSeconds(0.05f);

        if (dialogueButtonParent == null) yield break;

        for (int i = 0; i < dialogueButtonParent.childCount; i++)
        {
            dialogueButtonParent.GetChild(i).gameObject.SetActive(false);
        }
    }
}