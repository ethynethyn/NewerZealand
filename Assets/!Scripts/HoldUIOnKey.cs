using UnityEngine;
using System.Collections.Generic;

public class HoldUIOnKey : MonoBehaviour
{
    [Header("Key Settings")]
    public KeyCode holdKey = KeyCode.Escape;

    [Header("Objects To Toggle")]
    public List<GameObject> objectsToToggle;

    [Header("Optional")]
    public bool startDeactivated = false;

    void Start()
    {
        SetVisibility(!startDeactivated);
    }

    void Update()
    {
        // If the key is held down, show UI; otherwise hide
        if (Input.GetKey(holdKey))
        {
            SetVisibility(true);
        }
        else
        {
            SetVisibility(false);
        }
    }

    // Updates the visibility of all target objects
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
