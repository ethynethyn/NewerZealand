using UnityEngine;
using UnityEngine.UI;

public class ApplyColor : MonoBehaviour
{
    public FlexibleColorPicker fcp;
    public Image targetImage;

    private void Update()
    {
        if (targetImage != null && fcp != null)
        {
            targetImage.color = fcp.color;
        }
    }
}