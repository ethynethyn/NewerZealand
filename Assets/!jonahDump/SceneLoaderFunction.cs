using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderFunction : MonoBehaviour
{
    public string scenename;
    public void LoadScene()
    {
        SceneManager.LoadScene(scenename);
    }
}
