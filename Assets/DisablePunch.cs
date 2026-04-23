using UnityEngine;

public class DisablePunch : MonoBehaviour
{
    [Tooltip("Drag the GameObject that has HandUIController on it")]
    public HandUIController handUIController;

    void Awake()
    {
        if (handUIController == null)
            handUIController = FindObjectOfType<HandUIController>();
    }

    void OnEnable()
    {
        SetPunchEnabled(false);
    }

    void OnDisable()
    {
        SetPunchEnabled(true);
    }

    private void SetPunchEnabled(bool enabled)
    {
        if (handUIController == null)
        {
            Debug.LogWarning("[DisablePunch] No HandUIController found.");
            return;
        }

        handUIController.punchEnabled = enabled;
    }
}