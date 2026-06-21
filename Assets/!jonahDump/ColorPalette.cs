using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a row of colour swatches (one per entry in 'colors') plus an optional
/// eraser button, and wires them to a DrawingCanvas. Change how many colours
/// appear just by resizing the array. Attach this to an empty UI GameObject
/// (a child of your Canvas) placed where you want the palette.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ColorPalette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrawingCanvas drawingCanvas;

    [Header("Colours")]
    [Tooltip("Add or remove entries to change how many colour options appear.")]
    [SerializeField]
    private Color[] colors =
    {
        Color.black,
        Color.red,
        new Color(0.1f, 0.4f, 1f),  // blue
        new Color(0.1f, 0.7f, 0.2f),// green
        Color.yellow
    };

    [Header("Eraser")]
    [SerializeField] private bool includeEraser = true;
    [Tooltip("Optional icon for the eraser button. If empty, it shows the colour below.")]
    [SerializeField] private Sprite eraserIcon;
    [SerializeField] private Color eraserButtonColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Layout")]
    [SerializeField] private float swatchSize = 48f;
    [SerializeField] private float spacing = 8f;

    [Header("Selection Highlight")]
    [SerializeField] private Color selectedOutlineColor = Color.black;
    [SerializeField] private float selectedOutlineThickness = 3f;

    private readonly List<UnityEngine.UI.Outline> outlines = new List<UnityEngine.UI.Outline>();

    private void Start()
    {
        BuildLayoutGroup();
        BuildButtons();
        if (colors.Length > 0) Select(0); // default to the first colour
    }

    private void BuildLayoutGroup()
    {
        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;
    }

    private void BuildButtons()
    {
        for (int i = 0; i < colors.Length; i++)
        {
            int index = i;
            CreateSwatch($"Colour_{i}", colors[i], null, () => Select(index));
        }

        if (includeEraser)
        {
            int eraserIndex = outlines.Count; // appended after the colours
            CreateSwatch("Eraser", eraserButtonColor, eraserIcon, () => SelectEraser(eraserIndex));
        }
    }

    private void CreateSwatch(string objectName, Color color, Sprite icon, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Image img = go.AddComponent<Image>();
        if (icon != null) { img.sprite = icon; img.color = Color.white; }
        else img.color = color;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = swatchSize;
        le.preferredHeight = swatchSize;
        le.minWidth = swatchSize;
        le.minHeight = swatchSize;

        UnityEngine.UI.Outline outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = selectedOutlineColor;
        outline.effectDistance = new Vector2(selectedOutlineThickness, selectedOutlineThickness);
        outline.enabled = false;
        outlines.Add(outline);

        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None; // show the true swatch colour, no hover tint
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    private void Select(int index)
    {
        if (drawingCanvas != null && index >= 0 && index < colors.Length)
            drawingCanvas.SetPenColor(colors[index]);
        Highlight(index);
    }

    private void SelectEraser(int index)
    {
        if (drawingCanvas != null) drawingCanvas.SetEraser();
        Highlight(index);
    }

    private void Highlight(int index)
    {
        for (int i = 0; i < outlines.Count; i++)
            if (outlines[i] != null) outlines[i].enabled = (i == index);
    }
}