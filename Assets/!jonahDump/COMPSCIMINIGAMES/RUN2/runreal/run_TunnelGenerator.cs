using UnityEngine;

// Builds the polygon tunnel out of tile quads, punches holes in it,
// owns the coordinate math, spins the world when you change faces,
// and culls rings you can't see. This transform IS the rotating root.
public class run_TunnelGenerator : MonoBehaviour
{
    run_GameManager gm;
    bool[,] solid;      // [row, col]
    Transform[] rings;
    Transform starParent;
    int targetFace;
    System.Random rng;
    static MaterialPropertyBlock mpb;

    public int Faces { get; private set; }
    public int TilesPerFace { get; private set; }
    public int TotalCols { get; private set; }
    public int Rows { get; private set; }
    public float TileSize { get; private set; }
    public float FaceWidth { get; private set; }
    public float Perimeter { get; private set; }
    public float Apothem { get; private set; }
    public float FinishZ { get; private set; }

    Material tileMat;
    string colorProp = "_Color";

    public void Build(run_GameManager manager)
    {
        gm = manager;
        Faces = Mathf.Max(3, gm.tunnel.faces);
        TilesPerFace = Mathf.Max(1, gm.tunnel.tilesPerFace);
        TileSize = Mathf.Max(0.1f, gm.tunnel.tileSize);
        TotalCols = Faces * TilesPerFace;
        Rows = gm.tunnel.levelRows;
        FaceWidth = TilesPerFace * TileSize;
        Perimeter = FaceWidth * Faces;
        Apothem = FaceWidth * 0.5f / Mathf.Tan(Mathf.PI / Faces);
        FinishZ = (Rows - gm.tunnel.finishBandRows) * TileSize;

        MakeMaterial();
        GenerateLayout();
        SpawnTiles();
        if (gm.colors.spawnStars) SpawnStars();
        SnapToFace(0);
    }

    public void Rebuild()
    {
        if (rings != null)
            for (int r = 0; r < rings.Length; r++)
                if (rings[r]) Destroy(rings[r].gameObject);
        GenerateLayout();
        SpawnTiles();
    }

    // ---------------- material ----------------

    void MakeMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Standard");
        tileMat = new Material(s);
        colorProp = tileMat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        tileMat.SetColor(colorProp, Color.white);
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    // ---------------- level layout ----------------

    void GenerateLayout()
    {
        int seed = gm.tunnel.seed != 0 ? gm.tunnel.seed : Random.Range(int.MinValue, int.MaxValue);
        rng = new System.Random(seed);
        solid = new bool[Rows, TotalCols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < TotalCols; c++)
                solid[r, c] = true;

        var t = gm.tunnel;
        int firstHoleRow = Mathf.Max(1, t.startSafeRows);
        int lastHoleRow = Rows - t.finishBandRows - t.maxHoleLength - 2;
        int patches = Mathf.RoundToInt(t.holeDensity * Rows * 0.9f);

        for (int p = 0; p < patches; p++)
        {
            if (lastHoleRow <= firstHoleRow) break;
            int row = rng.Next(firstHoleRow, lastHoleRow);

            if (t.rampDifficulty)
            {
                float progress = (float)row / Rows;
                if (rng.NextDouble() > Mathf.Lerp(0.35f, 1f, progress)) continue;
            }

            int len = rng.Next(1, t.maxHoleLength + 1);
            int wid = rng.Next(1, t.maxHoleWidth + 1);
            int col = rng.Next(0, TotalCols);

            for (int dr = 0; dr < len; dr++)
                for (int dc = 0; dc < wid; dc++)
                    solid[Mathf.Min(row + dr, Rows - 1), (col + dc) % TotalCols] = false;
        }

        // keep every row survivable
        int minSolid = Mathf.Clamp(t.minSolidTilesPerRow, 0, TotalCols);
        for (int r = 0; r < Rows; r++)
        {
            int count = 0;
            for (int c = 0; c < TotalCols; c++) if (solid[r, c]) count++;
            int guard = 0;
            while (count < minSolid && guard++ < 500)
            {
                int c = rng.Next(0, TotalCols);
                if (!solid[r, c]) { solid[r, c] = true; count++; }
            }
        }

        // forced solid: start pad, finish band, checkpoint rows
        for (int r = 0; r < Mathf.Min(t.startSafeRows, Rows); r++) FillRow(r);
        for (int r = Rows - t.finishBandRows; r < Rows; r++) FillRow(r);
        if (gm.player.useCheckpoints)
        {
            for (int r = gm.player.checkpointEveryRows; r < Rows; r += gm.player.checkpointEveryRows)
            {
                FillRow(r);
                FillRow(r + 1);
            }
        }
    }

    void FillRow(int r)
    {
        if (r < 0 || r >= Rows) return;
        for (int c = 0; c < TotalCols; c++) solid[r, c] = true;
    }

    // ---------------- spawning ----------------

    void SpawnTiles()
    {
        rings = new Transform[Rows];
        float gap = 1f - gm.tunnel.tileGap;
        int finishStart = Rows - gm.tunnel.finishBandRows;

        for (int r = 0; r < Rows; r++)
        {
            var ring = new GameObject("Ring_" + r).transform;
            ring.SetParent(transform, false);
            ring.localPosition = new Vector3(0f, 0f, (r + 0.5f) * TileSize);
            rings[r] = ring;

            for (int c = 0; c < TotalCols; c++)
            {
                if (!solid[r, c]) continue;

                int face = c / TilesPerFace;
                int i = c % TilesPerFace;
                float t = (i + 0.5f) * TileSize - FaceWidth * 0.5f;
                float phi = FaceAngleRad(face);
                Vector3 outward = new Vector3(Mathf.Cos(phi), Mathf.Sin(phi), 0f);
                Vector3 tangent = new Vector3(-Mathf.Sin(phi), Mathf.Cos(phi), 0f);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(quad.GetComponent<Collider>());
                quad.name = "T";
                var tr = quad.transform;
                tr.SetParent(ring, false);
                tr.localPosition = outward * Apothem + tangent * t;
                tr.localRotation = Quaternion.LookRotation(outward, Vector3.forward);
                tr.localScale = new Vector3(TileSize * gap, TileSize * gap, 1f);

                var rend = quad.GetComponent<MeshRenderer>();
                rend.sharedMaterial = tileMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

                Color col;
                if (r >= finishStart) col = gm.colors.finishColor;
                else col = ((r + c) % 2 == 0) ? gm.colors.tileColor : gm.colors.tileAltColor;
                float v = 1f + ((float)rng.NextDouble() * 2f - 1f) * gm.colors.tileColorVariation;
                col = new Color(Mathf.Clamp01(col.r * v), Mathf.Clamp01(col.g * v), Mathf.Clamp01(col.b * v), 1f);
                mpb.SetColor(colorProp, col);
                rend.SetPropertyBlock(mpb);
            }
        }
    }

    void SpawnStars()
    {
        starParent = new GameObject("run_Stars").transform;
        starParent.SetParent(gm.transform, false); // NOT under the rotating root
        float length = Rows * TileSize;

        for (int i = 0; i < gm.colors.starCount; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Apothem * Mathf.Lerp(2.5f, 9f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-30f, length + 60f, (float)rng.NextDouble());

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "star";
            var tr = quad.transform;
            tr.SetParent(starParent, false);
            Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            tr.localPosition = dir * rad + Vector3.forward * z;
            tr.localRotation = Quaternion.LookRotation(dir, Vector3.forward);
            float s = Mathf.Lerp(0.06f, 0.22f, (float)rng.NextDouble()) * (rad / (Apothem * 3f));
            tr.localScale = new Vector3(s, s, 1f);

            var rend = quad.GetComponent<MeshRenderer>();
            rend.sharedMaterial = tileMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            mpb.SetColor(colorProp, gm.colors.starColor);
            rend.SetPropertyBlock(mpb);
        }
    }

    // ---------------- rotation + culling ----------------

    public void SetTargetFace(int face)
    {
        targetFace = ((face % Faces) + Faces) % Faces;
    }

    public void SnapToFace(int face)
    {
        SetTargetFace(face);
        transform.localRotation = Quaternion.Euler(0f, 0f, TargetAngle());
    }

    float TargetAngle() { return -targetFace * (360f / Faces); }

    void Update()
    {
        var target = Quaternion.Euler(0f, 0f, TargetAngle());
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, target, gm.tunnel.rotateSpeed * Time.deltaTime);

        if (gm.tunnel.cullRings && gm.Player != null && rings != null)
        {
            int pr = RowFromZ(gm.Player.Z);
            int lo = pr - gm.tunnel.ringsVisibleBehind;
            int hi = pr + gm.tunnel.ringsVisibleAhead;
            for (int r = 0; r < Rows; r++)
            {
                bool active = r >= lo && r <= hi;
                if (rings[r] && rings[r].gameObject.activeSelf != active)
                    rings[r].gameObject.SetActive(active);
            }
        }
    }

    // ---------------- coordinate math ----------------
    // The tunnel surface is treated as an unwrapped 2D strip:
    // surfaceX = position around the perimeter, z = distance in, h = height off the surface.

    public float WrapSurfaceX(float sx) { return Mathf.Repeat(sx, Perimeter); }

    public int FaceFromSurfaceX(float sx)
    {
        return Mathf.Clamp((int)(WrapSurfaceX(sx) / FaceWidth), 0, Faces - 1);
    }

    public int ColFromSurfaceX(float sx)
    {
        return Mathf.Clamp((int)(WrapSurfaceX(sx) / TileSize), 0, TotalCols - 1);
    }

    public int RowFromZ(float z) { return Mathf.FloorToInt(z / TileSize); }

    public float FaceAngleRad(int face)
    {
        return (-90f + face * (360f / Faces)) * Mathf.Deg2Rad;
    }

    public bool IsSolid(float sx, float z)
    {
        int row = RowFromZ(z);
        if (row < 0) return true;      // ground before the start line
        if (row >= Rows) return false;
        return solid[row, ColFromSurfaceX(sx)];
    }

    public Vector3 LocalPosOnFace(int face, float sx, float h, float z)
    {
        float center = (face + 0.5f) * FaceWidth;
        float t = WrapSurfaceX(sx) - center;
        t = Mathf.Repeat(t + Perimeter * 0.5f, Perimeter) - Perimeter * 0.5f;
        float phi = FaceAngleRad(face);
        Vector3 outward = new Vector3(Mathf.Cos(phi), Mathf.Sin(phi), 0f);
        Vector3 tangent = new Vector3(-Mathf.Sin(phi), Mathf.Cos(phi), 0f);
        return outward * (Apothem - h) + tangent * t + Vector3.forward * z;
    }

    public Vector3 LocalInward(int face)
    {
        float phi = FaceAngleRad(face);
        return new Vector3(-Mathf.Cos(phi), -Mathf.Sin(phi), 0f);
    }
}
