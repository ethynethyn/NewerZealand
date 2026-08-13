using DialogueEditor;
using System.Collections;
using UnityEngine;

public class EndScene1 : MonoBehaviour
{
    public GameObject Camera2;
    public GameObject thing1;
    public GameObject thing2;
    public GameObject thing3;
    [SerializeField] private NPCConversation coatdialogue;
    [SerializeField] private NPCConversation takeaway;

    public GameObject gran;

    public GameObject followingkids;
    public GameObject natenate;
    public GameObject nate2;
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            followingkids.SetActive(false);
            ConversationManager.Instance.StartConversation(coatdialogue);
            Camera2.SetActive(true);
            thing1.SetActive(true);
            thing2.SetActive(true);
            thing3.SetActive(true);
            gran.SetActive(false);

        }
    }
    public void GuyCOmesup()
    {
        natenate.SetActive(true);
        StartCoroutine(takeaway2());

    }
    private IEnumerator takeaway2()
    {
        yield return new WaitForSeconds(1.3f);
        ConversationManager.Instance.StartConversation(takeaway);

    }
    

}
