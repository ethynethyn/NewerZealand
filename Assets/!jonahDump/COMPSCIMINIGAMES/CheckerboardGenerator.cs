using UnityEngine;

/// <summary>
/// Builds a checkerboard texture procedurally and puts it on a SpriteRenderer.
/// No image asset needed — set the grid size and two colours in the inspector.
/// One square = one world unit, centered on this object's position.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class CheckerboardGenerator : MonoBehaviour
{
    [Header("Grid size (in squares)")]
    [SerializeField] private int columns = 16;
    [SerializeField] private int rows = 10;

    [Header("Colours")]
    [SerializeField] private Color colorA = new Color(0.45f, 0.45f, 0.95f);
    [SerializeField] private Color colorB = new Color(0.38f, 0.38f, 0.85f);

    [Header("Quality")]
    [Tooltip("Texture pixels per square. Higher = crisper.")]
    [SerializeField] private int pixelsPerSquare = 16;

    private SpriteRenderer sr;

    void OnEnable() => Generate();

#if UNITY_EDITOR
    void OnValidate()
    {
        // Rebuild a frame later so we aren't creating textures mid-edit.
        UnityEditor.EditorApplication.delayCall += DeferredGenerate;
    }

    private void DeferredGenerate()
    {
        UnityEditor.EditorApplication.delayCall -= DeferredGenerate;
        if (this == null) return;
        Generate();
    }
#endif

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        int cols = Mathf.Max(1, columns);
        int rws  = Mathf.Max(1, rows);
        int pps  = Mathf.Max(1, pixelsPerSquare);

        int w = cols * pps;
        int h = rws * pps;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool even = ((x / pps) + (y / pps)) % 2 == 0;
                tex.SetPixel(x, y, even ? colorA : colorB);
            }

        tex.Apply();

        // pixelsPerUnit = pixelsPerSquare  ->  one square = one world unit.
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pps);
    }
}
