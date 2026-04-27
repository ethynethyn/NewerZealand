using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using RetroShadersPro.URP;

public class PixelSizeController : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;

    [Header("Single Global Volume")]
    public Volume globalVolume;

    private CRTSettings crt;

    private const string PREF_KEY = "PIXEL_MODE";

    void Start()
    {
        if (globalVolume.profile.TryGet(out CRTSettings settings))
        {
            crt = settings;
        }

        slider.minValue = 0;
        slider.maxValue = 2;
        slider.wholeNumbers = true;

        slider.onValueChanged.AddListener(OnSliderChanged);

        // 🔥 Load saved value (default = 1 middle)
        int savedMode = PlayerPrefs.GetInt(PREF_KEY, 1);

        slider.SetValueWithoutNotify(savedMode);
        Apply(savedMode);
    }

    void OnSliderChanged(float value)
    {
        int mode = (int)value;

        Apply(mode);

        // 💾 Save selection
        PlayerPrefs.SetInt(PREF_KEY, mode);
        PlayerPrefs.Save();
    }

    void Apply(int mode)
    {
        if (crt == null) return;

        switch (mode)
        {
            case 2: // Low quality
                crt.pixelSize.value = 4;
                break;

            case 1: // Medium (default)
                crt.pixelSize.value = 3;
                break;

            case 0: // High quality
                crt.pixelSize.value = 1;
                break;
        }
    }
}