using UnityEngine;
using System.Collections.Generic;

public class ToggleObjectsOnEnable : MonoBehaviour
{
    [Header("Objects to Enable")]
    public List<GameObject> objectsToEnable;

    [Header("Objects to Disable")]
    public List<GameObject> objectsToDisable;

    void OnEnable()
    {
        // Enable selected objects
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Disable selected objects
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    void OnDisable()
    {
        // Turn everything off when this object is disabled
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}