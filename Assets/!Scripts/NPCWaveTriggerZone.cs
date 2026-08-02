using UnityEngine;

public class NPCWaveTriggerZone : MonoBehaviour
{
    private HandUIController handUI;

    void Start()
    {
        handUI = FindObjectOfType<HandUIController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (handUI != null)
            handUI.SetNPCNearby(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (handUI != null)
            handUI.SetNPCNearby(false);
    }
}