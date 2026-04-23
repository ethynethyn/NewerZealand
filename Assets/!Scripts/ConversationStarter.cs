using UnityEngine;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    public NPCConversation myConversation;

    private NPCBrain brain;

    void Awake()
    {
        brain = GetComponent<NPCBrain>();
    }

    public void OnConversationStart(Transform player)
    {
        if (brain != null)
        {
            brain.FaceTarget(player);
        }
    }

    public void OnConversationEnd()
    {
        if (brain != null)
        {
            brain.StopFacingTarget();
        }
    }
}