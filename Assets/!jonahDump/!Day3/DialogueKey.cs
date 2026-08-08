using DialogueEditor;
using UnityEngine;

public class DialogueKey : MonoBehaviour
{
    [SerializeField] private KeyCode key = KeyCode.E;
    [SerializeField] private NPCConversation conversation;

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            ConversationManager.Instance.StartConversation(conversation);
        }
    }
}