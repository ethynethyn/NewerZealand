using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Lets the player draw with a "pencil" inside a RawImage. The RawImage's
/// rect is the only space drawing can happen in. Tracks coverage as the
/// percentage of unique pixels that have been painted. Supports swapping the
/// pen colour and an eraser mode.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Texture")]
    [Tooltip("Resolution of the drawing surface. Higher = smoother but heavier.")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private Color backgroundColor = Color.white;
    [Tooltip("Starting pen colour (a ColorPalette will override this at runtime).")]
    [SerializeField] private Color penColor = Color.black;

    [Header("Brush")]
    [Tooltip("Pencil radius in pixels.")]
    [SerializeField] private int brushSize = 6;
    [Tooltip("Eraser radius in pixels (usually a bit bigger than the pencil).")]
    [SerializeField] private int eraserSize = 12;

    private RawImage rawImage;
    private RectTransform rectTransform;
    private Texture2D drawTexture;

    private Color32[] pixels;     // working pixel buffer
    private bool[] drawn;         // which pixels are currently painted (prevents double counting)
    private int drawnCount;       // unique painted pixels
    private int totalPixels;

    private bool eraseMode;       // true = eraser, false = pencil
    private Vector2? lastPixel;   // last painted pixel, for stroke interpolation
    private bool dirty;           // texture needs re-upload this frame

    /// <summary>0..100 percentage of the canvas that is currently painted.</summary>
    public float CoveragePercent => totalPixels == 0 ? 0f : (float)drawnCount / totalPixels * 100f;
    public bool IsErasing => eraseMode;

    /// <summary>Radius (px) of whichever tool is currently selected.</summary>
    public int ActiveToolSize => eraseMode ? eraserSize : brushSize;

    /// <summary>Raised when the active tool changes (pencil &lt;-&gt; eraser), so UI can refresh.</summary>
    public event System.Action ActiveToolChanged;

    private int CurrentBrushSize => eraseMode ? eraserSize : brushSize;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        InitTexture();
    }

    private void InitTexture()
    {
        drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point // crisp, MS-Paint-style pixels
        };
        totalPixels = textureWidth * textureHeight;
        pixels = new Color32[totalPixels];
        drawn = new bool[totalPixels];
        rawImage.texture = drawTexture;
        ClearTexture();
    }

    // ---- Tool selection (called by ColorPalette) -------------------------

    public void SetPenColor(Color color)
    {
        penColor = color;
        eraseMode = false;
        ActiveToolChanged?.Invoke();
    }

    public void SetEraser()
    {
        eraseMode = true;
        ActiveToolChanged?.Invoke();
    }

    /// <summary>Sets the shared brush size used by both the pencil and the eraser.</summary>
    public void SetActiveToolSize(int size)
    {
        size = Mathf.Max(1, size);
        brushSize = size;
        eraserSize = size;
    }

    // ---- Canvas state ----------------------------------------------------

    /// <summary>Wipes the canvas back to the background colour and resets coverage.</summary>
    public void ClearTexture()
    {
        Color32 bg = backgroundColor;
        for (int i = 0; i < totalPixels; i++)
        {
            pixels[i] = bg;
            drawn[i] = false;
        }
        drawnCount = 0;
        lastPixel = null;
        drawTexture.SetPixels32(pixels);
        drawTexture.Apply();
    }

    /// <summary>Turn drawing on/off (e.g. lock it once the game is over).</summary>
    public void SetInteractable(bool value)
    {
        rawImage.raycastTarget = value;
        if (!value) lastPixel = null;
    }

    /// <summary>
    /// Builds a coarse grid of what's been drawn (occupancy + average colour per
    /// cell), for the resemblance check. A cell counts as occupied when enough of
    /// its pixels are painted. grid[0,0] is the bottom-left, matching texture order.
    /// </summary>
    public CellData[,] GetDrawnGrid(int resolution, float cellFillThreshold)
    {
        int[,] counts = new int[resolution, resolution];
        float[,] sumR = new float[resolution, resolution];
        float[,] sumG = new float[resolution, resolution];
        float[,] sumB = new float[resolution, resolution];

        for (int py = 0; py < textureHeight; py++)
        {
            int gy = Mathf.Min(py * resolution / textureHeight, resolution - 1);
            int rowBase = py * textureWidth;
            for (int px = 0; px < textureWidth; px++)
            {
                int idx = rowBase + px;
                if (!drawn[idx]) continue;
                int gx = Mathf.Min(px * resolution / textureWidth, resolution - 1);
                Color32 c = pixels[idx];
                sumR[gy, gx] += c.r / 255f;
                sumG[gy, gx] += c.g / 255f;
                sumB[gy, gx] += c.b / 255f;
                counts[gy, gx]++;
            }
        }

        float cellPixels = ((float)textureWidth / resolution) * ((float)textureHeight / resolution);
        int minPixels = Mathf.Max(1, Mathf.CeilToInt(cellPixels * cellFillThreshold));

        CellData[,] grid = new CellData[resolution, resolution];
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int n = counts[y, x];
                if (n < minPixels) continue;
                float inv = 1f / n;
                grid[y, x].Occupied = true;
                grid[y, x].Color = new Color(sumR[y, x] * inv, sumG[y, x] * inv, sumB[y, x] * inv);
            }
        return grid;
    }

    // ---- Drawing ---------------------------------------------------------

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("canvas pointer down");   // 
        lastPixel = null; // start a fresh stroke
        PaintAt(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PaintAt(eventData);
    }

    private void PaintAt(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            lastPixel = null;
            return;
        }

        // Local point -> normalised 0..1 across the rect.
        Rect rect = rectTransform.rect;
        float nx = (localPoint.x - rect.x) / rect.width;
        float ny = (localPoint.y - rect.y) / rect.height;

        // Outside the draw space: ignore and break the stroke so we don't
        // draw a straight line across the gap when the pen comes back.
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
        {
            lastPixel = null;
            return;
        }

        int px = Mathf.Clamp((int)(nx * textureWidth), 0, textureWidth - 1);
        int py = Mathf.Clamp((int)(ny * textureHeight), 0, textureHeight - 1);
        Vector2 current = new Vector2(px, py);

        if (lastPixel.HasValue)
            PaintLine(lastPixel.Value, current); // fill gaps on fast drags
        else
            PaintDot(px, py);

        lastPixel = current;
    }

    private void PaintLine(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.CeilToInt(distance);
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : (float)i / steps;
            Vector2 p = Vector2.Lerp(from, to, t);
            PaintDot(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }
    }

    private void PaintDot(int cx, int cy)
    {
        int r = CurrentBrushSize;
        int rSq = r * r;
        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y > rSq) continue; // round brush
                int px = cx + x;
                int py = cy + y;
                if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight) continue;
                SetPixel(px, py);
            }
        }
        dirty = true;
    }

    private void SetPixel(int px, int py)
    {
        int idx = py * textureWidth + px;
        if (eraseMode)
        {
            if (drawn[idx]) { drawn[idx] = false; drawnCount--; } // erased area no longer counts
            pixels[idx] = backgroundColor;
        }
        else
        {
            if (!drawn[idx]) { drawn[idx] = true; drawnCount++; } // only count newly covered area
            pixels[idx] = penColor;
        }
    }

    private void LateUpdate()
    {
        // Batch all of this frame's painting into a single upload.
        if (!dirty) return;
        drawTexture.SetPixels32(pixels);
        drawTexture.Apply();
        dirty = false;
    }
}