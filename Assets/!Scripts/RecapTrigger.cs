using UnityEngine;

public class RecapTrigger : MonoBehaviour
{
    [SerializeField] private NightRecapManager recapManager;

    void OnEnable()
    {
        if (recapManager == null)
        {
            recapManager = FindObjectOfType<NightRecapManager>();

            if (recapManager == null)
            {
                Debug.LogError("RecapTrigger: Could not find NightRecapManager in scene!");
                return;
            }
        }

        recapManager.TriggerRecap();
        Debug.Log("Recap triggered!");
    }
}