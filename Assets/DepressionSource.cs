using UnityEngine;

public class DepressionSource : MonoBehaviour
{
    [Header("Reference to the DeathManager")]
    public DeathTrigger deathManager;

    private void OnEnable()
    {
        if (deathManager != null)
            deathManager.ActivateDepression();
    }

    private void OnDisable()
    {
        if (deathManager != null)
            deathManager.DeactivateDepression();
    }
}
