using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

public class CameraSensitivitySlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the FirstPersonController script on your player")]
    public FirstPersonController playerController;

    [Tooltip("The UI Slider controlling camera rotation speed")]
    public Slider sensitivitySlider;

    [Header("Sensitivity Range")]
    [Tooltip("Minimum allowed rotation speed")]
    public float minSensitivity = 0.1f;
    [Tooltip("Maximum allowed rotation speed")]
    public float maxSensitivity = 10f;

    private const string SensitivityPrefKey = "CameraSensitivity";

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        if (sensitivitySlider != null && playerController != null)
        {
            // Setup slider range
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;

            // Load saved sensitivity or use current value
            float savedSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, playerController.RotationSpeed);
            playerController.RotationSpeed = savedSensitivity;
            sensitivitySlider.value = savedSensitivity;

            // Add listener for real-time updates
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
        }
    }

    private void UpdateSensitivity(float value)
    {
        if (playerController != null)
        {
            playerController.RotationSpeed = value;

            // Save to PlayerPrefs
            PlayerPrefs.SetFloat(SensitivityPrefKey, value);
            PlayerPrefs.Save();
        }
    }
}
