using UnityEngine;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    public NPCConversation myConversation;

    [Header("Freeze + face during conversation")]
    public NPCBrain npcBrain;      // auto-filled if on the same GameObject
    public Transform faceTarget;   // fallback if StartTalking isn't given a target

    private static ConversationStarter s_activeSpeaker;

    void Reset() { npcBrain = GetComponent<NPCBrain>(); }
    void Awake() { if (npcBrain == null) npcBrain = GetComponent<NPCBrain>(); }

    void OnEnable() { ConversationManager.OnConversationEnded += HandleConversationEnded; }

    void OnDisable()
    {
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
        if (s_activeSpeaker == this)   // safety: don't leave them frozen if disabled mid-talk
        {
            if (npcBrain != null) npcBrain.ExitConversation();
            s_activeSpeaker = null;
        }
    }

    /// <summary> Begin talking to THIS npc. Pass the thing it should turn to face. </summary>
    public void StartTalking(Transform lookOverride = null)
    {
        if (myConversation == null || ConversationManager.Instance == null) return;
        if (ConversationManager.Instance.IsConversationActive) return;

        Transform look = (lookOverride != null) ? lookOverride : faceTarget;

        s_activeSpeaker = this;
        if (npcBrain != null) npcBrain.EnterConversation(look);
        ConversationManager.Instance.StartConversation(myConversation);
    }

    private void HandleConversationEnded()
    {
        if (s_activeSpeaker != this) return;
        if (npcBrain != null) npcBrain.ExitConversation();
        s_activeSpeaker = null;
    }
}