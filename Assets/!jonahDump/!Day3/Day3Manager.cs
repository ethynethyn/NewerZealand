using DialogueEditor;
using UnityEngine;
using System.Collections;

public class Day3Manager : MonoBehaviour
{
    [SerializeField] private NPCConversation AnswerIsNo;
    [SerializeField] private NPCConversation DavidSpeach;


    public GameObject David1;
    public GameObject David2;
    public GameObject GateBrosDialogue1;
    public GameObject GateBrosDialogue2;
    private bool GotRaemen;
    private bool TalkedWithEveryGirl;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            GotRaemen = true;
        }
        if (GotRaemen)
        {
            David1.SetActive(false);
            David2.SetActive(true);
            GotRaemen = false;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {   
            TalkedWithEveryGirl = true;
        }
        if (TalkedWithEveryGirl)
        {
            GateBrosDialogue1.SetActive(false);
            GateBrosDialogue2.SetActive(true);
            TalkedWithEveryGirl = false;
        }
    }
    public void WaitForDavidEating()
    {
        StartCoroutine(WaitForDavidEatingC());
    }

    public void WaitForCameraToPanToDavid()
    {
        StartCoroutine(CameraToDavid());
    }
    private IEnumerator CameraToDavid()
    {
        yield return new WaitForSeconds(0.5f);
        ConversationManager.Instance.StartConversation(DavidSpeach);


    }

    private IEnumerator WaitForDavidEatingC()
    {
        yield return new WaitForSeconds(1.7f);
        ConversationManager.Instance.StartConversation(AnswerIsNo);


    }



}
