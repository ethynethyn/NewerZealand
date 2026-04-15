using UnityEngine;

public class NPCWaveDetector : MonoBehaviour
{
    public float waveDistance = 3f;

    private Transform player;
    private HandUIController handUI;

    void Start()
    {
        handUI = FindObjectOfType<HandUIController>();
    }

    void Update()
    {
        // SAFE PLAYER ACQUISITION (build-safe)
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                return;
        }

        if (handUI == null)
        {
            handUI = FindObjectOfType<HandUIController>();
            if (handUI == null) return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= waveDistance)
        {
            handUI.SetNPCNearby(true);
        }
    }

    void LateUpdate()
    {
        // reset every frame so only nearby NPCs keep it active
        if (handUI != null)
        {
            handUI.SetNPCNearby(false);
        }
    }
}