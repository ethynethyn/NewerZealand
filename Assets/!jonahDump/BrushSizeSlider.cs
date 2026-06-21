using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the active tool's brush size from a UI Slider. The same slider works
/// for both the pencil and the eraser: it adjusts whichever tool is currently
/// selected, and snaps to show that tool's size when you switch tools (each tool
/// remembers its own size). Attach to a UI Slider and point it at the DrawingCanvas.
/// </summary>
[RequireComponent(typeof(Slider))]
public class BrushSizeSlider : MonoBehaviour
{
    [SerializeField] private DrawingCanvas drawingCanvas;
    [SerializeField] private Slider slider;

    [Header("Range (pixels)")]
    [SerializeField] private int minSize = 1;
    [SerializeField] private int maxSize = 40;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // Configure the slider so you don't have to set these in the inspector.
        slider.wholeNumbers = true;
        slider.minValue = minSize;
        slider.maxValue = maxSize;

        if (drawingCanvas != null)
        {
            int startSize = Mathf.Clamp(drawingCanvas.ActiveToolSize, minSize, maxSize);
            slider.SetValueWithoutNotify(startSize);
            drawingCanvas.SetActiveToolSize(startSize); // sync pencil + eraser to one starting size
            drawingCanvas.ActiveToolChanged += RefreshFromCanvas;
        }

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (drawingCanvas != null) drawingCanvas.ActiveToolChanged -= RefreshFromCanvas;
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        if (drawingCanvas != null) drawingCanvas.SetActiveToolSize((int)value);
    }

    // Slider follows the selected tool's size when you switch pencil <-> eraser.
    private void RefreshFromCanvas()
    {
        if (drawingCanvas != null)
            slider.SetValueWithoutNotify(Mathf.Clamp(drawingCanvas.ActiveToolSize, minSize, maxSize));
    }
}