using UnityEngine;
using UnityEngine.UI;

public class VolumeQualitySlider : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;

    [Header("Volumes (3 options)")]
    public GameObject lowQualityVolume;
    public GameObject midQualityVolume;
    public GameObject highQualityVolume;

    void Start()
    {
        slider.minValue = 0;
        slider.maxValue = 2;
        slider.wholeNumbers = true;

        slider.onValueChanged.AddListener(OnSliderChanged);

        // default state
        ApplyQuality((int)slider.value);
    }

    void OnSliderChanged(float value)
    {
        ApplyQuality((int)value);
    }

    void ApplyQuality(int index)
    {
        // disable all first
        lowQualityVolume.SetActive(false);
        midQualityVolume.SetActive(false);
        highQualityVolume.SetActive(false);

        // enable selected
        switch (index)
        {
            case 0:
                lowQualityVolume.SetActive(true);
                break;

            case 1:
                midQualityVolume.SetActive(true);
                break;

            case 2:
                highQualityVolume.SetActive(true);
                break;
        }
    }
}