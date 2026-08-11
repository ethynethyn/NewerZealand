using UnityEngine;

public class GuardLook : MonoBehaviour
{
    public GameObject cam1;
    public GameObject e1;
    public GameObject e2;
    public GameObject e3;
    public GameObject b1;
    public GameObject b2;
    public GameObject b3;
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            cam1.SetActive(true);
            e1.SetActive(true);
            e2.SetActive(true);
            e3.SetActive(true);
            b1.SetActive(false);
            b2.SetActive(false);
            b3.SetActive(false);

        }

    }

}
