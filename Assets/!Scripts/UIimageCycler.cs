using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIImageCycler : MonoBehaviour
{
    [Header("Images")]
    public Image otherImage;

    [Header("Timing")]
    public float switchInterval = 0.5f;

    private Image thisImage;
    private bool showingThis = true;

    private void Start()
    {
        thisImage = GetComponent<Image>();

        if (thisImage == null || otherImage == null)
        {
            Debug.LogError("UIImageCycler: Missing Image reference.");
            enabled = false;
            return;
        }

        // Start with this image visible.
        thisImage.enabled = true;
        otherImage.enabled = false;

        StartCoroutine(CycleImages());
    }

    private IEnumerator CycleImages()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchInterval);

            showingThis = !showingThis;

            thisImage.enabled = showingThis;
            otherImage.enabled = !showingThis;
        }
    }
}