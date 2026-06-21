using UnityEngine;

/// <summary>One coarse grid cell: whether it has content, and that content's average colour.</summary>
public struct CellData
{
    public bool Occupied;
    public Color Color;
}

/// <summary>
/// Loose "does the drawing vaguely resemble the reference" check. Both images are
/// reduced to a coarse grid of (occupied?, average colour), then compared. Overlap
/// gives the base score; colour similarity in overlapping cells adjusts it.
/// </summary>
public static class DrawingSimilarity
{
    /// <summary>
    /// Builds an occupancy + colour grid from a reference sprite.
    /// - Transparent images: content = whatever ISN'T transparent (any colour, even white).
    /// - Opaque images: content = pixels far enough from white (colour or dark ink).
    /// Requires the sprite's texture to have Read/Write Enabled.
    /// </summary>
    public static CellData[,] GetReferenceGrid(Sprite sprite, int resolution, float cellFillThreshold, float backgroundTolerance)
    {
        CellData[,] grid = new CellData[resolution, resolution];
        if (sprite == null) return grid;

        Texture2D tex = sprite.texture;
        Rect region = sprite.textureRect; // the part of the texture this sprite uses
        int rx = Mathf.RoundToInt(region.x);
        int ry = Mathf.RoundToInt(region.y);
        int rw = Mathf.RoundToInt(region.width);
        int rh = Mathf.RoundToInt(region.height);

        Color[] px;
        try
        {
            px = tex.GetPixels(rx, ry, rw, rh);
        }
        catch
        {
            Debug.LogError($"[DrawingSimilarity] Reference '{sprite.name}' texture is not readable. " +
                           "Select it in the Project window and tick 'Read/Write Enabled' in its import settings.");
            return grid;
        }

        // Does this image actually use transparency (any see-through pixels)?
        bool hasTransparency = false;
        for (int i = 0; i < px.Length; i++)
            if (px[i].a < 0.5f) { hasTransparency = true; break; }

        float[,] sumR = new float[resolution, resolution];
        float[,] sumG = new float[resolution, resolution];
        float[,] sumB = new float[resolution, resolution];
        int[,] counts = new int[resolution, resolution];

        for (int y = 0; y < rh; y++)
        {
            int gy = Mathf.Min(y * resolution / rh, resolution - 1);
            int rowBase = y * rw;
            for (int x = 0; x < rw; x++)
            {
                Color c = px[rowBase + x];

                bool isContent = hasTransparency
                    ? c.a >= 0.5f                                   // only what isn't transparent
                    : DistanceFromWhite(c) >= backgroundTolerance;  // non-white on an opaque image
                if (!isContent) continue;

                int gx = Mathf.Min(x * resolution / rw, resolution - 1);
                sumR[gy, gx] += c.r;
                sumG[gy, gx] += c.g;
                sumB[gy, gx] += c.b;
                counts[gy, gx]++;
            }
        }

        float cellPixels = ((float)rw / resolution) * ((float)rh / resolution);
        int minContent = Mathf.Max(1, Mathf.CeilToInt(cellPixels * cellFillThreshold));
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int n = counts[y, x];
                if (n < minContent) continue;
                float inv = 1f / n;
                grid[y, x].Occupied = true;
                grid[y, x].Color = new Color(sumR[y, x] * inv, sumG[y, x] * inv, sumB[y, x] * inv);
            }

        return grid;
    }

    /// <summary>
    /// Colour-aware resemblance, 0..1. Cells occupied in both contribute the base
    /// overlap; how much their colours match (via colorImportance + colorTolerance)
    /// scales that contribution. Cells in only one image count against the score.
    /// </summary>
    public static float Similarity(CellData[,] drawn, CellData[,] reference, float colorImportance, float colorTolerance)
    {
        int res = drawn.GetLength(0);
        float intersection = 0f;
        int union = 0;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                bool dv = drawn[y, x].Occupied;
                bool rv = reference[y, x].Occupied;
                if (dv && rv)
                {
                    float colorSim = ColorSimilarity(drawn[y, x].Color, reference[y, x].Color, colorTolerance);
                    // Right place always counts; matching colour adds the remainder.
                    intersection += (1f - colorImportance) + colorImportance * colorSim;
                    union++;
                }
                else if (dv || rv)
                {
                    union++;
                }
            }
        return union == 0 ? 0f : intersection / union;
    }

    public static bool IsEmpty(CellData[,] grid)
    {
        foreach (CellData cell in grid) if (cell.Occupied) return false;
        return true;
    }

    // 1 when colours match, falling to 0 as they diverge past the tolerance.
    private static float ColorSimilarity(Color a, Color b, float tolerance)
    {
        float dist = ColorDistance(a, b);
        if (tolerance <= 0f) return dist <= 0.001f ? 1f : 0f;
        return Mathf.Clamp01(1f - dist / tolerance);
    }

    private static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f; // normalised 0..1
    }

    private static float DistanceFromWhite(Color c)
    {
        float dr = 1f - c.r, dg = 1f - c.g, db = 1f - c.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f;
    }
}