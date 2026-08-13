using DialogueEditor;
using UnityEngine;
using System.Collections;

public class RacManager : MonoBehaviour
{
    [SerializeField] private NPCConversation burning;

    public GameObject Cam1;
    public GameObject Cam2;
    public GameObject Cam3;
    public GameObject Cam4;
    public GameObject MainCam;
    public GameObject MarioCartUI;
    public GameObject alienParent;
    public GameObject Explodion;
    public GameObject fireGm;
    public GameObject burningGuy;
    public GameObject StampCam;

    private bool explode;
    void Start()
    {
        StartCoroutine(RaceCam1());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            explode = true;
        }
        if (explode)
        {
            explode = false;
            StartCoroutine(Explode());
        }
    }


    public void Stamp()
    {
        StartCoroutine(StampTme());

    }
    private IEnumerator StampTme()
    {
        yield return new WaitForSeconds(2f);
        //StampCam.SetActive(false);
        fireGm.SetActive(false);

    }


    private IEnumerator Explode()
    {
        yield return new WaitForSeconds(0f);
        Explodion.SetActive(true);
        StartCoroutine(Fire());
        burningGuy.SetActive(true);

    }
    private IEnumerator Fire()
    {
        yield return new WaitForSeconds(0.8f);
        fireGm.SetActive(true);
        ConversationManager.Instance.StartConversation(burning);


    }

    private IEnumerator RaceCam1()
    {
        yield return new WaitForSeconds(4f);
        Cam1.SetActive(false);
        Cam2.SetActive(true);
        StartCoroutine(RaceCam2());

    }
    private IEnumerator RaceCam2()
    {
        yield return new WaitForSeconds(4.5f);
        Cam2.SetActive(false);
        Cam3.SetActive(true);
        StartCoroutine(RaceCam3());

    }
    private IEnumerator RaceCam3()
    {
        yield return new WaitForSeconds(4f);
        Cam3.SetActive(false);
        Cam4.SetActive(true);
        StartCoroutine(RaceCam4());
        MarioCartUI.SetActive(true);

    }
    private IEnumerator RaceCam4()
    {
        yield return new WaitForSeconds(3f);
        Cam4.SetActive(false);
        MainCam.SetActive(true);
        alienParent.GetComponent<Animator>().Play("go"); 

    }

}
