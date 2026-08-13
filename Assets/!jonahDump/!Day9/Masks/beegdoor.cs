using DialogueEditor;
using UnityEngine;

public class beegdoor : MonoBehaviour
{

    [SerializeField] private NPCConversation DoorTry;

    public GameObject cam4;
    private bool done;
    public GameObject follow1;
    public GameObject follow2;

    public GameObject bruh1;
    public GameObject bruh2;
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (!done)
            {

                cam4.SetActive(true);
                ConversationManager.Instance.StartConversation(DoorTry);
                follow1.SetActive(false);
                follow2.SetActive(false);
                bruh1.SetActive(true);
                bruh2.SetActive(true); 
                done = true;
            }


        }

    }

}
