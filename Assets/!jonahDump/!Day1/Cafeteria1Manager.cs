using DialogueEditor;
using StarterAssets;
using UnityEngine;
using System.Collections;


public class Cafeteria1Manager : MonoBehaviour
{
    [SerializeField] private NPCConversation conversation;

    public GameObject MainCamera;
    public GameObject Player;
    public GameObject sitDowncamera;
    public void SitDown()
    {
        sitDowncamera.SetActive(true);
        //
        MainCamera.SetActive(false);
    }
    public void Talk()
    {
        StartCoroutine(TalkWhenFree());
    }

    private IEnumerator TalkWhenFree()
    {
        if (conversation == null) { Debug.LogError("No conversation assigned", this); yield break; }
        if (ConversationManager.Instance == null) { Debug.LogError("No ConversationManager"); yield break; }

        // let the current conversation finish its fade-out
        while (ConversationManager.Instance.IsConversationActive)
            yield return null;

        ConversationManager.Instance.StartConversation(conversation);
    }
}
