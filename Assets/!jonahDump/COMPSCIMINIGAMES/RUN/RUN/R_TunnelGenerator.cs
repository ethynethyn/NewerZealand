using UnityEngine;

// Builds a square tunnel (4 runnable surfaces) along +Z. Any surface can become
// the floor. The FLOOR and the two SIDE WALLS can have gaps; each is adjustable.
// Uses UNLIT flat-color materials (works in Built-in, URP 2D and 3D - no more
// magenta) with a checker shade per cell so you can read depth, like real Run.
[DisallowMultipleComponent]
public class R_TunnelGenerator : MonoBehaviour
{
    [Header("Tunnel Shape")]
    public float tunnelRadius = 3f;      // half-width of the square cross-section
    public float cellSize = 4f;          // length of one segment along Z
    public int cellCount = 60;           // how many segments long the tunnel is
    public float wallThickness = 0.5f;
    public bool snapToOrigin = true;     // force the tunnel onto the world axis (the player math needs this)
    public bool regenerateOnPlay = true; // rebuild on Play so radius/gap edits always apply

    [Header("Floor Gaps")]
    public int seed = 12345;
    [Range(0f, 1f)] public float holeChance = 0.35f;
    public int minGapCells = 1;
    public int maxGapCells = 3;
    public int minSolidBetween = 2;

    [Header("Side-Wall Gaps  (higher = fewer side tiles = harder)")]
    [Range(0f, 1f)] public float sideHoleChance = 0.30f;
    public int sideMinGapCells = 1;
    public int sideMaxGapCells = 2;
    public int sideMinSolidBetween = 2;
    public bool solidCeiling = true;     // untick to punch gaps in the ceiling too

    [Header("Safe Zones  (kept solid on every surface)")]
    public int safeStartCells = 4;       // runway at the start
    public int safeEndCells = 2;         // run-up to the finish

    [Header("Colors  (flat / unlit)")]
    public Color floorColor = new Color(0.10f, 0.65f, 0.75f);  // cyan track
    public Color ceilingColor = new Color(0.30f, 0.38f, 0.48f);  // steel gray-blue
    public Color sideColor = new Color(0.15f, 0.35f, 0.80f);  // blue
    [Range(0f, 0.5f)] public float checkerDarken = 0.18f;        // every 2nd cell this much darker
    public Color finishColor = new Color(0.20f, 0.95f, 0.45f, 0.55f);

    [Header("Auto-filled")]
    public Transform finishPoint;
    public float playerStartZ;

    void Awake()
    {
        ForceOrigin();   // runs before GameManager spawns the player
    }

    void Start()
    {
        if (regenerateOnPlay || transform.childCount == 0) Generate();
        else CacheFinishAndStart();
    }

    // The player treats the world Z axis (x=y=0) as the tunnel center, so the
    // tunnel object must sit at the origin with no rotation or scale.
    void ForceOrigin()
    {
        if (!snapToOrigin) return;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    [ContextMenu("Generate Tunnel")]
    public void Generate()
    {
        ForceOrigin();
        Clear();

        // two shades per surface -> checker pattern along the tunnel
        Material[] floorMats = MakePair(floorColor);
        Material[] ceilMats = MakePair(ceilingColor);
        Material[] sideMats = MakePair(sideColor);

        // separate deterministic layouts so surfaces don't gap identically
        bool[] floorSolid = BuildGapLayout(seed, holeChance, minGapCells, maxGapCells, minSolidBetween);
        bool[] leftSolid = BuildGapLayout(seed + 1, sideHoleChance, sideMinGapCells, sideMaxGapCells, sideMinSolidBetween);
        bool[] rightSolid = BuildGapLayout(seed + 2, sideHoleChance, sideMinGapCells, sideMaxGapCells, sideMinSolidBetween);
        bool[] ceilSolid = solidCeiling
            ? AllSolid()
            : BuildGapLayout(seed + 3, sideHoleChance, sideMinGapCells, sideMaxGapCells, sideMinSolidBetween);

        for (int i = 0; i < cellCount; i++)
        {
            float z = (i + 0.5f) * cellSize;
            int alt = i % 2;

            if (floorSolid[i])
                MakeTile("Floor_" + i,
                    new Vector3(0f, -tunnelRadius - wallThickness * 0.5f, z),
                    new Vector3(tunnelRadius * 2f, wallThickness, cellSize), floorMats[alt]);

            if (ceilSolid[i])
                MakeTile("Ceil_" + i,
                    new Vector3(0f, tunnelRadius + wallThickness * 0.5f, z),
                    new Vector3(tunnelRadius * 2f, wallThickness, cellSize), ceilMats[alt]);

            if (leftSolid[i])
                MakeTile("WallL_" + i,
                    new Vector3(-tunnelRadius - wallThickness * 0.5f, 0f, z),
                    new Vector3(wallThickness, tunnelRadius * 2f, cellSize), sideMats[alt]);

            if (rightSolid[i])
                MakeTile("WallR_" + i,
                    new Vector3(tunnelRadius + wallThickness * 0.5f, 0f, z),
                    new Vector3(wallThickness, tunnelRadius * 2f, cellSize), sideMats[alt]);
        }

        BuildFinish();
        playerStartZ = 1.5f * cellSize;
    }

    bool[] AllSolid()
    {
        bool[] s = new bool[cellCount];
        for (int k = 0; k < cellCount; k++) s[k] = true;
        return s;
    }

    // Deterministic gaps for one surface. Start/end runways stay solid.
    bool[] BuildGapLayout(int rngSeed, float chance, int minGap, int maxGap, int minSolid)
    {
        bool[] solid = AllSolid();

        System.Random rng = new System.Random(rngSeed);
        int i = safeStartCells;
        int lastGapEnd = safeStartCells;
        int limit = cellCount - safeEndCells;

        while (i < limit)
        {
            if (i - lastGapEnd >= minSolid && rng.NextDouble() < chance)
            {
                int gap = rng.Next(minGap, maxGap + 1);
                for (int g = 0; g < gap && i < limit; g++, i++)
                    solid[i] = false;
                lastGapEnd = i;
            }
            else i++;
        }
        return solid;
    }

    void BuildFinish()
    {
        float z = cellCount * cellSize + cellSize * 0.5f;

        GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fin.name = "R_Finish";
        fin.transform.SetParent(transform, false);
        fin.transform.localPosition = new Vector3(0f, 0f, z);
        fin.transform.localScale = new Vector3(tunnelRadius * 2f, tunnelRadius * 2f, 0.4f);

        fin.GetComponent<BoxCollider>().isTrigger = true;
        fin.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(finishColor);
        fin.AddComponent<R_FinishTrigger>();

        finishPoint = fin.transform;
    }

    void CacheFinishAndStart()
    {
        var f = transform.Find("R_Finish");
        if (f != null) finishPoint = f;
        playerStartZ = 1.5f * cellSize;
    }

    void MakeTile(string tileName, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = tileName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    Material[] MakePair(Color c)
    {
        Color dark = Color.Lerp(c, Color.black, checkerDarken);
        return new[] { MakeMat(c), MakeMat(dark) };
    }

    // UNLIT flat color. Sprites/Default exists in every pipeline (Built-in,
    // URP 2D, URP 3D, HDRP), writes correct color, and z-tests against the
    // world. No lighting = no magenta, no black walls.
    Material MakeMat(Color c)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");   // paranoia fallback
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        return m;
    }

    [ContextMenu("Clear Tunnel")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
        finishPoint = null;
    }
}