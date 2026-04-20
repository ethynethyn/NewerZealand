using UnityEngine;
using DialogueEditor;
using System.Collections;

public class ConversationStarter : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;

    [Header("UI")]
    public GameObject interactionImage;
    public Transform dialogueButtonParent; // 👈 IMPORTANT: parent holding spawned buttons

    [Header("Settings")]
    public float interactRange = 3f;
    public LayerMask npcLayer;

    [Header("Dialogue")]
    public NPCConversation myConversation;

    private bool isLookingAtNPC = false;      // current state
    private bool wasLookingAtNPC = false;     // previous frame state

    private void Start()
    {
        if (interactionImage != null)
            interactionImage.SetActive(false);
    }

    private void Update()
    {
        HandleLookState();
        HandleEnter();
        HandleExit();
    }

    void HandleLookState()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        wasLookingAtNPC = isLookingAtNPC;

        isLookingAtNPC = Physics.Raycast(ray, interactRange, npcLayer);

        if (interactionImage != null &&
            ConversationManager.Instance != null &&
            !ConversationManager.Instance.IsConversationActive)
        {
            interactionImage.SetActive(isLookingAtNPC);
        }
    }

    void HandleEnter()
    {
        if (!isLookingAtNPC) return;
        if (ConversationManager.Instance == null) return;
        if (ConversationManager.Instance.IsConversationActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ConversationManager.Instance.StartConversation(myConversation);

            if (interactionImage != null)
                interactionImage.SetActive(false);

            // 🔥 KEY FIX: wait a frame then disable spawned UI children
            StartCoroutine(DisableDialogueButtonsNextFrame());
        }
    }

    void HandleExit()
    {
        if (ConversationManager.Instance == null) return;
        if (!ConversationManager.Instance.IsConversationActive) return;

        if (wasLookingAtNPC && !isLookingAtNPC)
        {
            ConversationManager.Instance.EndConversation();

            if (interactionImage != null)
                interactionImage.SetActive(false);
        }
    }

    IEnumerator DisableDialogueButtonsNextFrame()
    {
        yield return null; // still needed (lets UI spawn)

        yield return new WaitForSeconds(0.05f); // small buffer for Dialogue Editor setup

        if (dialogueButtonParent == null) yield break;

        for (int i = 0; i < dialogueButtonParent.childCount; i++)
        {
            dialogueButtonParent.GetChild(i).gameObject.SetActive(false);
        }
    }
}