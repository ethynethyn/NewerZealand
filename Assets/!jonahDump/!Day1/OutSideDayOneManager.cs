using DialogueEditor;
using StarterAssets;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class OutSideDayOneManager : MonoBehaviour
{
    [SerializeField] private NPCConversation conversation;

    public GameObject MainCamera;
    public GameObject Player;
    public GameObject Cutscene1Camera;
    public GameObject PlayerPlace;

    private void Start()
    {
        
    }
    public void gonow()
    {
        MainCamera.transform.position = PlayerPlace.transform.position;  
    }
    public void fade2black()
    {
        StartCoroutine(goblack());

    }
    private IEnumerator goblack()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Sewers1");
    }


    public void CutScene1()
    {
        Cutscene1Camera.SetActive(true);
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
