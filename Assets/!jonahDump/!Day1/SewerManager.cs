using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SewerManager : MonoBehaviour
{
    public void Fadenext()
    {
        StartCoroutine(Fadenext2());

    }
    private IEnumerator Fadenext2()
    {
        yield return new WaitForSeconds(3.5f);
        SceneManager.LoadScene("Schoolday1CHIMNEY");


    }
}
