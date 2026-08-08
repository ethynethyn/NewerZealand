using DialogueEditor;
using UnityEngine;

public class Day3Part2Manager : MonoBehaviour
{
    [SerializeField] private NPCConversation DavidSpeach;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ConversationManager.Instance.StartConversation(DavidSpeach);

        }
    }


}
