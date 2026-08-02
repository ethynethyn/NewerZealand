using UnityEngine;

public class DisableAfterTime : MonoBehaviour
{
    public GameObject targetObject; // drag your object here
    public float delay = 3f;

    void OnEnable()
    {
        Invoke(nameof(DisableTarget), delay);
    }

    void DisableTarget()
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}