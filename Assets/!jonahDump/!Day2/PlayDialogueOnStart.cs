using UnityEngine;
using DialogueEditor;

/// <summary>
/// Plays a single conversation when the scene starts.
/// Drop this on any GameObject and assign the conversation in the inspector.
/// </summary>
public class PlayDialogueOnStart : MonoBehaviour
{
    [SerializeField] private NPCConversation conversation;

    [Tooltip("Optional delay before the dialogue kicks off.")]
    [Min(0f)] public float delay = 0f;

    void Start()
    {
        if (conversation == null)
        {
            Debug.LogError("[PlayDialogueOnStart] No conversation assigned!", this);
            return;
        }

        if (delay > 0f)
            Invoke(nameof(Play), delay);
        else
            Play();
    }

    void Play()
    {
        if (ConversationManager.Instance == null)
        {
            Debug.LogError("[PlayDialogueOnStart] No ConversationManager in the scene — " +
                           "drag the ConversationManager prefab in.", this);
            return;
        }

        ConversationManager.Instance.StartConversation(conversation);
    }
}