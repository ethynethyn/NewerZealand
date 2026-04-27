using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ResolutionDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private List<Resolution> finalResolutions = new List<Resolution>();

    void Start()
    {
        PopulateDropdown();

        // When opening dropdown → unlock cursor
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void PopulateDropdown()
    {
        var all = Screen.resolutions;

        finalResolutions = all
            .Where(r => r.width >= 1280)
            .Where(r => IsCommonAspectRatio(r))
            .GroupBy(r => new { r.width, r.height })
            .Select(g => g.OrderByDescending(r => GetRefreshRate(r)).First())
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToList();

        dropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < finalResolutions.Count; i++)
        {
            var r = finalResolutions[i];
            int hz = GetRefreshRate(r);

            options.Add($"{r.width} x {r.height} @ {hz}Hz");

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        dropdown.AddOptions(options);
        dropdown.value = currentIndex;
        dropdown.RefreshShownValue();
    }

    public void OnDropdownChanged(int index)
    {
        var r = finalResolutions[index];
        int hz = GetRefreshRate(r);

        Screen.SetResolution(r.width, r.height, true, hz);

        // Delay fix (IMPORTANT)
        StartCoroutine(ReleaseUIFocus());
    }

    IEnumerator ReleaseUIFocus()
    {
        // Wait until dropdown fully closes
        yield return null;
        yield return new WaitForEndOfFrame();

        // Clear selection
        EventSystem.current.SetSelectedGameObject(null);

        // Relock cursor (for FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    bool IsCommonAspectRatio(Resolution r)
    {
        float ratio = (float)r.width / r.height;

        return Mathf.Abs(ratio - (16f / 9f)) < 0.01f ||
               Mathf.Abs(ratio - (16f / 10f)) < 0.01f;
    }

    int GetRefreshRate(Resolution r)
    {
#if UNITY_2022_1_OR_NEWER
        return (int)r.refreshRateRatio.value;
#else
        return r.refreshRate;
#endif
    }

    // OPTIONAL: Call this when opening your settings menu
    public void OnMenuOpened()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}