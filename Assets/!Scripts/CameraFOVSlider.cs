using UnityEngine;
using UnityEngine.UI;
using Cinemachine; // Needed for Cinemachine Virtual Camera support

public class CameraFOVSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: assign a Cinemachine Virtual Camera manually. If left empty, it will try to find one automatically.")]
    public CinemachineVirtualCamera virtualCamera;

    [Tooltip("UI Slider used to adjust the camera FOV")]
    public Slider fovSlider;

    [Header("FOV Range")]
    public float minFOV = 50f;
    public float maxFOV = 100f;

    private const string FovPrefKey = "CameraFOV";

    private void Start()
    {
        // Auto-detect the virtual camera if not assigned
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        if (fovSlider == null || virtualCamera == null)
        {
            Debug.LogWarning("CameraFOVSlider: Missing slider or Cinemachine Virtual Camera reference.");
            return;
        }

        // Configure slider range
        fovSlider.minValue = minFOV;
        fovSlider.maxValue = maxFOV;

        // Load saved FOV or use current FOV
        float savedFOV = PlayerPrefs.GetFloat(FovPrefKey, virtualCamera.m_Lens.FieldOfView);
        virtualCamera.m_Lens.FieldOfView = savedFOV;
        fovSlider.value = savedFOV;

        // Listen for changes
        fovSlider.onValueChanged.AddListener(UpdateFOV);
    }

    private void UpdateFOV(float value)
    {
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.FieldOfView = value;

            // Save to PlayerPrefs
            PlayerPrefs.SetFloat(FovPrefKey, value);
            PlayerPrefs.Save();
        }
    }
}
