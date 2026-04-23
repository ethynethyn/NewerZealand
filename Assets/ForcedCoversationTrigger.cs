using UnityEngine;
using DialogueEditor;
using System.Collections;

public class ForcedConversationTrigger : MonoBehaviour
{
    [Header("Conversation")]
    public NPCConversation forcedConversation;

    [Header("Player Lock")]
    public GameObject[] objectsToDisable;

    [Header("Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        StartConversation();
    }

    void StartConversation()
    {
        if (ConversationManager.Instance == null) return;

        hasTriggered = true;

        // Disable movement / systems
        SetObjectsActive(false);

        // Start dialogue
        ConversationManager.Instance.StartConversation(forcedConversation);

        // Start monitoring for end
        StartCoroutine(WaitForConversationEnd());
    }

    IEnumerator WaitForConversationEnd()
    {
        while (ConversationManager.Instance != null &&
               ConversationManager.Instance.IsConversationActive)
        {
            yield return null;
        }

        EndConversation();
    }

    void EndConversation()
    {
        SetObjectsActive(true);
    }

    void SetObjectsActive(bool state)
    {
        if (objectsToDisable == null) return;

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            if (objectsToDisable[i] != null)
                objectsToDisable[i].SetActive(state);
        }
    }
}