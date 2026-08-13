using DialogueEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Day2P2PreMinecraftManager : MonoBehaviour
{
    public void GoMinecraft()
    {
        StartCoroutine(mcs());
    }
    

    private IEnumerator mcs()
    {
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("MinecraftJonah");
    }
}
