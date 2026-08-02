using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionApplySelector : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public TMP_Text label;
    public Button applyButton;

    private List<ResolutionOption> options = new List<ResolutionOption>();
    private int selectedIndex = 0;

    void Start()
    {
        BuildOptions();

        slider.minValue = 0;
        slider.maxValue = options.Count - 1;
        slider.wholeNumbers = true;

        slider.onValueChanged.AddListener(OnSliderChanged);
        applyButton.onClick.AddListener(ApplyResolution);

        SetInitial();
    }

    void BuildOptions()
    {
        var all = Screen.resolutions;

        options = all
            .Where(r => r.width >= 1280)
            .Where(r => IsCommonAspectRatio(r))
            .GroupBy(r => new { r.width, r.height })
            .Select(g =>
            {
                var best = g.OrderByDescending(x => GetHz(x)).First();

                return new ResolutionOption
                {
                    width = best.width,
                    height = best.height,
                    refreshRate = GetHz(best)
                };
            })
            .OrderByDescending(o => o.width)
            .ThenByDescending(o => o.height)
            .ToList();
    }

    void SetInitial()
    {
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].width == Screen.currentResolution.width &&
                options[i].height == Screen.currentResolution.height)
            {
                selectedIndex = i;
                break;
            }
        }

        int sliderIndex = options.Count - 1 - selectedIndex;

        slider.SetValueWithoutNotify(sliderIndex);
        UpdateLabel(sliderIndex);
    }

    void OnSliderChanged(float value)
    {
        int sliderIndex = Mathf.RoundToInt(value);
        selectedIndex = options.Count - 1 - sliderIndex;

        UpdateLabel(sliderIndex);
    }

    void UpdateLabel(int sliderIndex)
    {
        int index = options.Count - 1 - Mathf.Clamp(sliderIndex, 0, options.Count - 1);

        var opt = options[index];

        if (label != null)
            label.text = $"{opt.width} x {opt.height} @ {opt.refreshRate}Hz";
    }

    void ApplyResolution()
    {
        int sliderIndex = Mathf.RoundToInt(slider.value);
        int index = options.Count - 1 - sliderIndex;

        var opt = options[index];

        Screen.SetResolution(
            opt.width,
            opt.height,
            true,
            opt.refreshRate
        );
    }

    bool IsCommonAspectRatio(Resolution r)
    {
        float ratio = (float)r.width / r.height;

        return Mathf.Abs(ratio - (16f / 9f)) < 0.01f ||
               Mathf.Abs(ratio - (16f / 10f)) < 0.01f;
    }

    int GetHz(Resolution r)
    {
#if UNITY_2022_1_OR_NEWER
        return (int)r.refreshRateRatio.value;
#else
        return r.refreshRate;
#endif
    }

    [System.Serializable]
    public class ResolutionOption
    {
        public int width;
        public int height;
        public int refreshRate;
    }
}