using DialogueEditor;
using System.Collections;
using UnityEngine;

public class DelayedDialogue : MonoBehaviour
{
    [SerializeField] private NPCConversation dialodge;
    [SerializeField] private float delay = 1f;

    public void PlayDialogueAfterDelay()
    {
        StartCoroutine(PlayDialogueCoroutine());
    }

    private IEnumerator PlayDialogueCoroutine()
    {
        yield return new WaitForSeconds(delay);
        ConversationManager.Instance.StartConversation(dialodge);
    }
}