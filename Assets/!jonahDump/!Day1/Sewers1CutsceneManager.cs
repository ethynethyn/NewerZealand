using UnityEngine;
using DialogueEditor;

public class Sewers1CutsceneManager : MonoBehaviour
{
    [SerializeField] private NPCConversation conversation;

    public void Talk()
    {
        if (conversation == null) return;
        if (ConversationManager.Instance == null) return;
        if (ConversationManager.Instance.IsConversationActive) return;

        ConversationManager.Instance.StartConversation(conversation);
    }
}