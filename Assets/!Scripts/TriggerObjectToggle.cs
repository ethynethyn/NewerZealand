using UnityEngine;
using System.Collections.Generic;

public class TriggerObjectToggle : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string playerTag = "Player";

    [Header("Objects To Enable")]
    public List<GameObject> objectsToEnable = new List<GameObject>();

    [Header("Objects To Disable")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Options")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;

            // Enable objects
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"Enabled: {obj.name}");
                }
            }

            // Disable objects
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"Disabled: {obj.name}");
                }
            }
        }
    }
}