using DialogueEditor;
using UnityEngine;
using System.Collections;

public class OutsideDay3Part2 : MonoBehaviour
{
    [SerializeField] private NPCConversation RoxyComesIn;
    [SerializeField] private NPCConversation BeginDialogue;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ConversationManager.Instance.StartConversation(BeginDialogue);

            
        }
    }
    public void WaitForHerToComeIn()
    {
        StartCoroutine(SheComesIn());
    }
    private IEnumerator SheComesIn()
    {
        yield return new WaitForSeconds(0.5f);
        ConversationManager.Instance.StartConversation(RoxyComesIn);


    }
}
