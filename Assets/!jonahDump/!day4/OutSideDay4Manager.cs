using DialogueEditor;
using UnityEngine;
using System.Collections;

public class OutSideDay4Manager : MonoBehaviour
{

    [SerializeField] private NPCConversation gointosewers;
    public void ALRIGHTBOYS()
    {
        StartCoroutine(ALRIGHTBOYSn());
    }
    private IEnumerator ALRIGHTBOYSn()
    {
        yield return new WaitForSeconds(3f);
        ConversationManager.Instance.StartConversation(gointosewers);


    }
}
