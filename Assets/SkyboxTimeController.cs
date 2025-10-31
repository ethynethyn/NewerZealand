using UnityEngine;

[System.Serializable]
public struct SkyPeriod
{
    public string name;
    public float startHour; // inclusive
    public float endHour;   // exclusive
    public Color skyColor;
    public Color cloudColor;
}

public class SkyboxTimeController : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material skyboxMaterial;
    public string skyColorProperty = "_Sky_Color";
    public string cloudColorProperty = "_Cloud_Color";

    [Header("Time Settings")]
    public Character character;
    public string timeStatName = "Time";

    [Header("Sky Periods")]
    public SkyPeriod[] periods;

    [Header("Lerp Settings")]
    public float lerpSpeed = 2f; // controls smoothness

    private Color currentSkyColor;
    private Color currentCloudColor;
    private Color targetSkyColor;
    private Color targetCloudColor;

    private SkyPeriod lastActivePeriod;

    void Start()
    {
        if (skyboxMaterial != null)
        {
            currentSkyColor = skyboxMaterial.GetColor(skyColorProperty);
            currentCloudColor = skyboxMaterial.GetColor(cloudColorProperty);
        }

        if (periods.Length > 0)
        {
            lastActivePeriod = periods[0];
            targetSkyColor = lastActivePeriod.skyColor;
            targetCloudColor = lastActivePeriod.cloudColor;
        }
    }

    void Update()
    {
        if (character == null || skyboxMaterial == null || periods.Length == 0) return;

        float hour = character.GetStatValue(timeStatName) % 24f;

        // Determine the current period
        SkyPeriod activePeriod = periods[0];
        for (int i = 0; i < periods.Length; i++)
        {
            if (IsHourInPeriod(hour, periods[i]))
            {
                activePeriod = periods[i];
                break;
            }
        }

        // Only update target colors when the period changes
        if (activePeriod.startHour != lastActivePeriod.startHour)
        {
            targetSkyColor = activePeriod.skyColor;
            targetCloudColor = activePeriod.cloudColor;
            lastActivePeriod = activePeriod;
        }

        // Smoothly lerp current colors toward target colors every frame
        currentSkyColor = Color.Lerp(currentSkyColor, targetSkyColor, Time.deltaTime * lerpSpeed);
        currentCloudColor = Color.Lerp(currentCloudColor, targetCloudColor, Time.deltaTime * lerpSpeed);

        // Apply colors
        currentSkyColor.a = 1f;
        currentCloudColor.a = 1f;

        skyboxMaterial.SetColor(skyColorProperty, currentSkyColor);
        skyboxMaterial.SetColor(cloudColorProperty, currentCloudColor);
    }

    bool IsHourInPeriod(float hour, SkyPeriod period)
    {
        if (period.startHour < period.endHour)
            return hour >= period.startHour && hour < period.endHour;
        else
            return hour >= period.startHour || hour < period.endHour;
    }
}
