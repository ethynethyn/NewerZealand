using DialogueEditor;
using UnityEngine;
using System.Collections;

public class Hiest2 : MonoBehaviour
{
    public GameObject fire;
    public GameObject exp2;
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
