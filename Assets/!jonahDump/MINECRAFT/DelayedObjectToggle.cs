using System.Collections;
using UnityEngine;

public class DelayedObjectToggle : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delay = 1f;
    [SerializeField] private bool runOnStart = false;
    [Tooltip("Ignores Time.timeScale. Leave off unless you pause the game.")]
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Objects")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private void Start()
    {
        if (runOnStart) Run();
    }

    /// <summary>Waits for the inspector delay, then applies the toggles.</summary>
    public void Run()
    {
        Run(delay);
    }

    /// <summary>Waits for a custom delay, then applies the toggles.</summary>
    public void Run(float customDelay)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"{name}: can't run, this object is inactive.", this);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RunRoutine(customDelay));
    }

    /// <summary>Applies the toggles right now, no waiting.</summary>
    public void Apply()
    {
        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null)
                objectsToActivate[i].SetActive(true);
        }

        for (int i = 0; i < objectsToDeactivate.Length; i++)
        {
            if (objectsToDeactivate[i] != null)
                objectsToDeactivate[i].SetActive(false);
        }
    }

    /// <summary>Puts everything back the way it was.</summary>
    public void Revert()
    {
        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null)
                objectsToActivate[i].SetActive(false);
        }

        for (int i = 0; i < objectsToDeactivate.Length; i++)
        {
            if (objectsToDeactivate[i] != null)
                objectsToDeactivate[i].SetActive(true);
        }
    }

    /// <summary>Cancels a pending timer before it fires.</summary>
    public void Cancel()
    {
        StopAllCoroutines();
    }

    private IEnumerator RunRoutine(float wait)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(wait);
        else
            yield return new WaitForSeconds(wait);

        Apply();
    }
}