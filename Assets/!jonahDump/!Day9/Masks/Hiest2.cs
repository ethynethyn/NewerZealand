using DialogueEditor;
using UnityEngine;
using System.Collections;

public class Hiest2 : MonoBehaviour
{
    public GameObject fire;
    public GameObject exp2;
    public GameObject exp3;

    public void Explode2()
    {
        StartCoroutine(exp22());

    }
    private IEnumerator exp22()
    {
        yield return new WaitForSeconds(1f);
        exp3.SetActive(true);


    }
    public void Explode()
    {
        StartCoroutine(exp());

    }
    private IEnumerator exp()
    {
        yield return new WaitForSeconds(2f);
        fire.SetActive(true);
        exp2.SetActive(true);   


    }
}
