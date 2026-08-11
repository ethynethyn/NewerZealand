using DialogueEditor;
using UnityEngine;

public class WhenTouch : MonoBehaviour
{
    public GameObject cam1;
    public GameObject cam2;
    public GameObject arnold2;
    
    public GameObject thing1;
    public GameObject thing2;
    public GameObject thing3;
    public GameObject thing4;

    public GameObject an1;
    public GameObject an2;
    public GameObject an3;

    [SerializeField] private NPCConversation anroldlight;
    private bool done;
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (done == false)
            {
                done = true;
                cam2.SetActive(true);
                arnold2.SetActive(true);
                ConversationManager.Instance.StartConversation(anroldlight);
                thing1.SetActive(false);
                thing2.SetActive(false);
                thing3.SetActive(false);
                thing4.SetActive(false);
                an1.SetActive(true);
                an2.SetActive(true);
                an3.SetActive(true);
            }



        }

    }



}
