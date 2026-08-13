using DialogueEditor;
using StarterAssets;
using UnityEngine;
using System.Collections;

public class OutSideManagerDayOnePart2 : MonoBehaviour
{
    [SerializeField] private NPCConversation first;

    [SerializeField] private NPCConversation conversation;
    [SerializeField] private NPCConversation AfterRunAround;

    private bool chimneyNextSpeach;
    public GameObject csChimney1;
    public GameObject csChimney2;
    public GameObject csChimney1d5;
    private bool chimneyjoinsparty;
    public GameObject DialogueGrateBoys1;
    public GameObject DialogueGrateBoys2;

    

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // locks cursor to center + hides it
        Cursor.visible = false;
        ConversationManager.Instance.StartConversation(first);

    }
    public void Update()
    {
        if (JonahStaticManager.ChimneyJoinsYourParty)
        {
            chimneyjoinsparty = true;
            JonahStaticManager.ChimneyJoinsYourParty = false;
        }
        if (chimneyjoinsparty)
        {
            chimneyjoinsparty = false;
            DialogueGrateBoys1.SetActive(false);
            DialogueGrateBoys2.SetActive(true);

        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            //StartCoroutine(TalkWhenFree());
            JonahStaticManager.PickedUpEraser = true;
        }
        if (JonahStaticManager.PickedUpEraser)
        {
            chimneyNextSpeach = true;
            JonahStaticManager.PickedUpEraser = false;
        }
        if (chimneyNextSpeach)
        {
            chimneyNextSpeach = false;
            csChimney1.SetActive(false);
            csChimney2.SetActive(true);
            csChimney1d5.SetActive(false);
        }

    }
    public void ChimneyJoins()
    {
        JonahStaticManager.ChimneyJoinsYourParty = true;
    }
    public void GrateBoysRunAround()
    {
        StartCoroutine(AfterRunArounDialogue());

    }
    public void WaitAndThenDialouge3()
    {
        StartCoroutine(Dialouge3Routine());
    }





    private IEnumerator AfterRunArounDialogue()
    {
        yield return new WaitForSeconds(1.3f);

        ConversationManager.Instance.StartConversation(AfterRunAround);
    }
    private IEnumerator Dialouge3Routine()
    {
        yield return new WaitForSeconds(1.9f);

        ConversationManager.Instance.StartConversation(conversation);
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
