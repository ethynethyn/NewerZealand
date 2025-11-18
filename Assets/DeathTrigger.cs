using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class DeathTrigger : MonoBehaviour
{
    [Header("Vignette Stages (small to large)")]
    public List<GameObject> vignetteStages = new List<GameObject>();

    [Header("Depression Active Effects")]
    public List<GameObject> depressionEffects = new List<GameObject>();

    [Header("Objects to Disable When Dead")]
    public List<GameObject> disableOnDeath = new List<GameObject>();

    [Header("Death Settings")]
    public float timeUntilDeath = 5f;
    public float fadeSpeed = 2f;
    public float fadeBackSpeed = 1f;
    public GameObject deathEventObject;

    private List<CanvasGroup> stageGroups = new List<CanvasGroup>();
    private float timer = 0f;
    private float stageDuration = 1f;
    private int currentStage = 0;
    private bool dead = false;
    private bool depressionActive = false;

    void Awake()
    {
        stageGroups.Clear();
        foreach (var go in vignetteStages)
        {
            if (go == null)
            {
                stageGroups.Add(null);
                continue;
            }

            go.SetActive(true);
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            stageGroups.Add(cg);
        }

        foreach (var effect in depressionEffects)
            if (effect != null) effect.SetActive(false);

        if (deathEventObject != null)
            deathEventObject.SetActive(false);

        stageDuration = vignetteStages.Count > 0 ? timeUntilDeath / vignetteStages.Count : timeUntilDeath;
        stageDuration = Mathf.Max(stageDuration, 0.01f);
    }

    void Update()
    {
        // If dead, allow restart
        if (dead)
        {
            if (Input.GetKeyDown(KeyCode.R))
                RestartScene();

            return;
        }

        if (!depressionActive) return;

        timer += Time.deltaTime;
        int stageIndex = Mathf.FloorToInt(timer / stageDuration);

        if (stageIndex != currentStage && stageIndex < stageGroups.Count)
        {
            currentStage = stageIndex;
            StartCoroutine(FadeStage(currentStage));
        }

        if (timer >= timeUntilDeath)
        {
            TriggerDeath();
        }
    }

    private IEnumerator FadeStage(int index)
    {
        for (int i = 0; i < stageGroups.Count; i++)
        {
            CanvasGroup cg = stageGroups[i];
            if (cg == null) continue;

            if (i == index)
                StartCoroutine(FadeCanvas(cg, 1f, false));
            else if (i == index - 1)
                StartCoroutine(FadeCanvas(cg, 0f, false));
        }
        yield break;
    }

    private IEnumerator RecoverStages(int fromStage)
    {
        for (int i = fromStage; i >= 0; i--)
        {
            CanvasGroup cg = stageGroups[i];
            if (cg != null)
                yield return StartCoroutine(FadeCanvas(cg, 0f, true));
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float target, bool isRecovery)
    {
        float speed = isRecovery ? fadeBackSpeed : fadeSpeed;

        while (!Mathf.Approximately(cg.alpha, target))
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    private void TriggerDeath()
    {
        dead = true;

        if (deathEventObject != null)
            deathEventObject.SetActive(true);

        SetDepressionEffects(false);
        DisableDeathObjects();
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetStages()
    {
        timer = 0f;
        currentStage = 0;

        foreach (var cg in stageGroups)
            if (cg != null) cg.alpha = 0f;

        if (deathEventObject != null)
            deathEventObject.SetActive(false);

        SetDepressionEffects(false);
    }

    public void ActivateDepression()
    {
        if (dead) return;
        if (depressionActive) return;

        depressionActive = true;
        timer = 0f;
        currentStage = 0;

        SetDepressionEffects(true);
        StartCoroutine(FadeStage(currentStage));
    }

    public void DeactivateDepression()
    {
        if (!depressionActive) return;

        depressionActive = false;
        timer = 0f;
        currentStage = 0;

        SetDepressionEffects(false);
        StartCoroutine(RecoverStages(stageGroups.Count - 1));
    }

    private void SetDepressionEffects(bool enable)
    {
        foreach (var effect in depressionEffects)
        {
            if (effect != null)
                effect.SetActive(enable);
        }
    }

    private void DisableDeathObjects()
    {
        foreach (var obj in disableOnDeath)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}
