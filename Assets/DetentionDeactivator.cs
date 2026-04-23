using UnityEngine;

public class DetentionDeactivator : MonoBehaviour
{
    [Tooltip("Drag the GameObject that has DetentionActivator on it")]
    public DetentionActivator detentionActivator;

    void Awake()
    {
        if (detentionActivator == null)
            detentionActivator = FindObjectOfType<DetentionActivator>();
    }

    void Start()
    {
        if (detentionActivator != null)
            detentionActivator.DeactivateDetention();
        else
            Debug.LogWarning("[DetentionDeactivator] No DetentionActivator found.");

        // Self-disable so it can be triggered again next time
        gameObject.SetActive(false);
    }
}