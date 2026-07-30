using UnityEngine;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    public NPCConversation myConversation;

    [Header("Freeze + face during conversation")]
    public NPCBrain npcBrain;      // auto-filled if on the same GameObject
    public Transform faceTarget;   // the player; auto-found by tag if left empty
    public string playerTag = "Player";

    private static ConversationStarter s_activeSpeaker;

    void Reset() { npcBrain = GetComponent<NPCBrain>(); }

    void Awake()
    {
        if (npcBrain == null) npcBrain = GetComponent<NPCBrain>();
        if (faceTarget == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindWithTag(playerTag);
            if (p != null) faceTarget = p.transform;
        }
    }

    void OnEnable() { ConversationManager.OnConversationEnded += HandleConversationEnded; }

    void OnDisable()
    {
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
        // Safety: don't leave the NPC frozen if this gets disabled mid-talk
        if (s_activeSpeaker == this)
        {
            if (npcBrain != null) npcBrain.ExitConversation();
            s_activeSpeaker = null;
        }
    }

    /// <summary> Call this to begin talking to THIS npc. </summary>
    public void StartTalking()
    {
        if (myConversation == null || ConversationManager.Instance == null) return;

        s_activeSpeaker = this;
        if (npcBrain != null) npcBrain.EnterConversation(faceTarget);
        ConversationManager.Instance.StartConversation(myConversation);
    }

    private void HandleConversationEnded()
    {
        if (s_activeSpeaker != this) return;
        if (npcBrain != null) npcBrain.ExitConversation();
        s_activeSpeaker = null;
    }
}