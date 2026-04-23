using UnityEngine;

public class SkateResetTrigger : MonoBehaviour
{
    public SlopeSlideController playerSlide; // drag your player here

    void OnEnable()
    {
        if (playerSlide != null)
        {
            playerSlide.ForceResetState();
        }

        gameObject.SetActive(false); // disable itself immediately after
    }
}