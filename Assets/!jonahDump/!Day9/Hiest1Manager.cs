using DialogueEditor;
using UnityEngine;
using System.Collections;


public class Hiest1Manager : MonoBehaviour
{
    public GameObject explode;
    public GameObject fire;
    public GameObject gweedo;
    public ApproachOnActivate approach;

    public GameObject Camera1;
    public ConversationManager cm;
    public GameObject granny;
    public PlayerFollowerTrail fpt;

    public void TwoStairs()
    {
        StartCoroutine(TwoStairsC());

    }
    private IEnumerator TwoStairsC()
    {
        yield return new WaitForSeconds(1.2f);
        Camera1.SetActive(false);
        cm.EndConversation();
        fpt.enabled = true;
    }



    public void WaitTillCrash()
    {
        StartCoroutine(waitthencrash());

    }
    private IEnumerator waitthencrash()
    {
        yield return new WaitForSeconds(1.4f);
        explode.SetActive(true);
        fire.SetActive(true);
        gweedo.GetComponent<Animator>().Play("crash2");
        approach.enabled = true;
        granny.SetActive(true);
    }
}
