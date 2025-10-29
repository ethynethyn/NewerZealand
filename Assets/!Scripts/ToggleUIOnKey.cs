using UnityEngine;
using System.Collections.Generic;

public class ToggleUIOnKey : MonoBehaviour
{
    [Header("Key Settings")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Objects To Toggle")]
    public List<GameObject> objectsToToggle;

    [Header("Optional")]
    public bool startDeactivated = false;

    private bool isVisible = false;

    void Start()
    {
        SetVisibility(!startDeactivated);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisibility(!isVisible);
        }
    }

    // Use this from your Resume button
    public void SetVisibility(bool state)
    {
        isVisible = state;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
                obj.SetActive(isVisible);
        }
    }

    // Optional: toggle externally via a button
    public void Toggle()
    {
        SetVisibility(!isVisible);
    }
}
