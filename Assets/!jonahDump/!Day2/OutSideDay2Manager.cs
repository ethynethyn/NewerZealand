using DialogueEditor;
using UnityEngine;
using System.Collections;


public class OutSideDay2Manager : MonoBehaviour
{
    [SerializeField] private NPCConversation ChimneyHopless;
    [SerializeField] private NPCConversation StevenCryDialogue;


    public GameObject maincamera;
    public GameObject StevenCryCamera;


    private bool TalkedWithSteven;
    private bool talkedwithnateorlewis;
    public GameObject chimneyDialogue1;
    public GameObject chimneyDialogue2;
    public GameObject GrateBrosDialogue1;
    public GameObject GrateBrosDialogue2;
    public void talkedWithSteven()
    {
        if (talkedwithnateorlewis)
        {
            TalkedWithSteven = true;

        }
    }
    public void talkedWithNateOrLewis()
    {
        talkedwithnateorlewis = true;
    }
    private void Update()
    {
        if (TalkedWithSteven && talkedwithnateorlewis)
        {
            talkedwithnateorlewis = false;
            TalkedWithSteven = false;
            chimneyDialogue1.SetActive(false);
            chimneyDialogue2.SetActive(true);
            GrateBrosDialogue1.SetActive(false);
            GrateBrosDialogue2.SetActive(true);
        }

        if (JonahStaticManager.leftSteven2)
        {
            maincamera.SetActive(false);
            StevenCryCamera.SetActive(true);
            JonahStaticManager.leftSteven2 = false;
            StartCoroutine(PlayStevenCryingDialogue());
            
        }

    }
    private IEnumerator PlayStevenCryingDialogue()
    {
        yield return new WaitForSeconds(1f);
        ConversationManager.Instance.StartConversation(StevenCryDialogue);


    }

    public void StartChimneyGoingToStevenCutScene()
    {
        StartCoroutine(ChimneyBonkCutscene());
    }
    private IEnumerator ChimneyBonkCutscene()
    {
        yield return new WaitForSeconds(7f);
        ConversationManager.Instance.StartConversation(ChimneyHopless);


    }

    public void LockToStevenWhenleaven()
    {
        JonahStaticManager.leftSteven = true;
    }


}
