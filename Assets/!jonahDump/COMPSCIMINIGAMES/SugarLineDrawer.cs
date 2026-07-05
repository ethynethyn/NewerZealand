using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Draw lines with the mouse for the sugar to slide down.
// Left-drag to draw a stroke, right-click to clear everything.
// Each stroke gets a LineRenderer (visual) + an EdgeCollider2D (physics).
[RequireComponent(typeof(Transform))]
public class SugarLineDrawer : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Camera used to turn mouse position into world position. Defaults to Camera.main.")]
    public Camera cam;

    [Header("Line Look")]
    public float lineWidth = 0.12f;
    public Color lineColor = new Color(0.4f, 0.9f, 1f);
    [Tooltip("Optional material for the visible line. If empty a default sprite material is used.")]
    public Material lineMaterial;
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    [Header("Drawing")]
    [Tooltip("Minimum world distance between sampled points. Smaller = smoother, more colliders.")]
    public float minPointDistance = 0.1f;
    [Tooltip("Collision THICKNESS of the line. Bump this up if fast sugar tunnels through.")]
    public float edgeRadius = 0.06f;
    [Range(0f, 1f)] public float lineFriction = 0.3f;
    [Range(0f, 1f)] public float lineBounciness = 0f;

    [Header("Ink (optional)")]
    public bool infiniteInk = true;
    [Tooltip("Total world length you can draw when ink is limited.")]
    public float inkBudget = 60f;

    [Header("Input")]
    public int drawButton = 0;   // 0 = left mouse
    public int clearButton = 1;  // 1 = right mouse, clears all lines
    public bool ignoreClicksOverUI = true;

    private float inkUsed;
    private Material runtimeLineMat;
    private PhysicsMaterial2D linePhysMat;

    private LineRenderer currentLine;
    private EdgeCollider2D currentCollider;
    private readonly List<Vector2> currentPoints = new List<Vector2>();
    private readonly List<GameObject> strokes = new List<GameObject>();

    public float InkRemaining => infiniteInk ? Mathf.Infinity : Mathf.Max(0f, inkBudget - inkUsed);

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        linePhysMat = new PhysicsMaterial2D("SugarLine") { friction = lineFriction, bounciness = lineBounciness };
        runtimeLineMat = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(clearButton)) ClearAll();

        if (Input.GetMouseButtonDown(drawButton) && !PointerOverUI()) BeginStroke();
        if (Input.GetMouseButton(drawButton) && currentLine != null) ExtendStroke();
        if (Input.GetMouseButtonUp(drawButton)) EndStroke();
    }

    bool PointerOverUI()
    {
        if (!ignoreClicksOverUI) return false;
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    Vector2 MouseWorld()
    {
        Vector3 m = Input.mousePosition;
        m.z = Mathf.Abs(cam.transform.position.z); // distance to the z=0 plane
        Vector3 w = cam.ScreenToWorldPoint(m);
        return new Vector2(w.x, w.y);
    }

    void BeginStroke()
    {
        if (InkRemaining <= 0f) return;

        var go = new GameObject("SugarLine");
        go.transform.SetParent(transform, false);
        go.transform.position = Vector3.zero; // keep local == world so collider points line up

        currentLine = go.AddComponent<LineRenderer>();
        currentLine.useWorldSpace = true;
        currentLine.widthMultiplier = lineWidth;
        currentLine.numCapVertices = 4;
        currentLine.numCornerVertices = 4;
        currentLine.textureMode = LineTextureMode.Stretch;
        currentLine.material = runtimeLineMat;
        currentLine.startColor = currentLine.endColor = lineColor;
        currentLine.sortingLayerName = sortingLayerName;
        currentLine.sortingOrder = sortingOrder;

        currentCollider = go.AddComponent<EdgeCollider2D>();
        currentCollider.edgeRadius = edgeRadius;
        currentCollider.sharedMaterial = linePhysMat;

        currentPoints.Clear();
        Vector2 p = MouseWorld();
        currentPoints.Add(p);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, p);
        // EdgeCollider2D needs >= 2 points; we set it once we have a second one.
    }

    void ExtendStroke()
    {
        if (currentPoints.Count == 0) return;

        Vector2 p = MouseWorld();
        Vector2 last = currentPoints[currentPoints.Count - 1];
        float d = Vector2.Distance(last, p);
        if (d < minPointDistance) return;
        if (InkRemaining <= 0f) return;

        // If ink is limited, clamp this segment to whatever ink is left.
        if (!infiniteInk && d > InkRemaining)
        {
            p = last + (p - last).normalized * InkRemaining;
            d = InkRemaining;
        }

        currentPoints.Add(p);
        inkUsed += d;

        currentLine.positionCount = currentPoints.Count;
        currentLine.SetPosition(currentPoints.Count - 1, p);

        if (currentPoints.Count >= 2) currentCollider.SetPoints(currentPoints);
    }

    void EndStroke()
    {
        if (currentLine == null) return;

        if (currentPoints.Count < 2)
            Destroy(currentLine.gameObject); // a single dot has no usable collider
        else
            strokes.Add(currentLine.gameObject);

        currentLine = null;
        currentCollider = null;
        currentPoints.Clear();
    }

    public void ClearAll()
    {
        foreach (var s in strokes)
            if (s != null) Destroy(s);
        strokes.Clear();

        if (currentLine != null) Destroy(currentLine.gameObject);
        currentLine = null;
        currentCollider = null;
        currentPoints.Clear();

        inkUsed = 0f;
    }
}
