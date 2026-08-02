using UnityEngine;

public class EnableAfterTime : MonoBehaviour
{
    public GameObject targetObject; // drag your object here
    public float delay = 3f;

    void OnEnable()
    {
        Invoke(nameof(EnableTarget), delay);
    }

    void EnableTarget()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}