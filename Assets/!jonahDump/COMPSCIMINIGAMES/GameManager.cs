using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one level: spawns the hearts, respawns the player on a hit, and shows the
/// flashing YOU WIN / YOU LOSE text. Put one of these in every level scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private WPlayerController player;
    [Tooltip("Empty object marking the start / respawn position.")]
    [SerializeField] private Transform startPoint;

    [Header("Hearts")]
    [Tooltip("How many hearts the player starts with.")]
    [SerializeField] private int startingHearts = 5;
    [Tooltip("A UI Image prefab for one heart.")]
    [SerializeField] private GameObject heartPrefab;
    [Tooltip("Parent the hearts spawn under (give it a Horizontal Layout Group).")]
    [SerializeField] private Transform heartContainer;

    [Header("End text (leave these disabled in the scene)")]
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;
    [Tooltip("How many times per second the win/lose text flashes.")]
    [SerializeField] private float flashesPerSecond = 3f;

    [Header("After winning / losing")]
    [SerializeField] private bool restartLevelOnLose = true;
    [SerializeField] private float restartDelay = 2f;
    [SerializeField] private bool loadNextSceneOnWin = false;
    [SerializeField] private float nextSceneDelay = 2f;

    private int currentHearts;
    private readonly List<GameObject> hearts = new List<GameObject>();
    private bool isGameOver;

    void Start()
    {
        currentHearts = startingHearts;
        SpawnHearts();

        if (winText) winText.SetActive(false);
        if (loseText) loseText.SetActive(false);

        if (player && startPoint) player.MoveTo(startPoint.position);
    }

    private void SpawnHearts()
    {
        hearts.Clear();
        if (!heartPrefab || !heartContainer) return;

        for (int i = 0; i < startingHearts; i++)
            hearts.Add(Instantiate(heartPrefab, heartContainer));
    }

    /// <summary>Called by the player when it touches a blue ball.</summary>
    public void PlayerHit()
    {
        if (isGameOver) return;

        currentHearts--;
        RefreshHearts();

        if (currentHearts <= 0)
            Lose();
        else if (player && startPoint)
            player.MoveTo(startPoint.position); // send them back to the start
    }

    private void RefreshHearts()
    {
        // Hide hearts from the right end as they are lost.
        for (int i = 0; i < hearts.Count; i++)
            if (hearts[i]) hearts[i].SetActive(i < currentHearts);
    }

    /// <summary>Called by the player when it reaches the green end square.</summary>
    public void ReachedGoal()
    {
        if (isGameOver) return;
        Win();
    }

    private void Win()
    {
        isGameOver = true;
        if (player) player.LockInput(true);
        if (winText) StartCoroutine(Flash(winText));
        if (loadNextSceneOnWin) StartCoroutine(LoadNextScene());
    }

    private void Lose()
    {
        isGameOver = true;
        if (player) player.LockInput(true);
        if (loseText) StartCoroutine(Flash(loseText));
        if (restartLevelOnLose) StartCoroutine(RestartLevel());
    }

    private IEnumerator Flash(GameObject target)
    {
        float half = 0.5f / Mathf.Max(0.1f, flashesPerSecond);
        while (true)
        {
            target.SetActive(true);
            yield return new WaitForSeconds(half);
            target.SetActive(false);
            yield return new WaitForSeconds(half);
        }
    }

    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(nextSceneDelay);
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            Debug.Log("That was the last level in Build Settings.");
    }
}
