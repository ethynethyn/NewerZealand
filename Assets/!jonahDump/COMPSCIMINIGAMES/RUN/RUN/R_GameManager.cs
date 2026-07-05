using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

// One script to rule them all. On Play (autoSetup) it builds the tunnel, player,
// camera, a light, and the UI if they aren't already in the scene, then runs the
// game: 3 hearts, respawn-from-start on death, YOU WIN / YOU LOSE, press R to retry.
[DisallowMultipleComponent]
public class R_GameManager : MonoBehaviour
{
    public static R_GameManager Instance;
    public static bool IsGameOver;

    [Header("Auto Setup")]
    public bool autoSetup = true;
    public Sprite playerSprite;         // static fallback sprite (if no frames)
    public float spriteScale = 2f;

    [Header("Player Animation (optional - your drawings)")]
    public Sprite[] runFrames;          // played while running
    public Sprite[] jumpFrames;         // played while airborne
    public float animFps = 10f;

    [Header("Player Movement (pushed onto the player)")]
    public float runSpeed = 12f;        // forward speed
    public float jumpSpeed = 12f;       // jump strength

    [Header("Lives")]
    public int maxLives = 3;

    [Header("Shared Config (synced to generator + player)")]
    public float tunnelRadius = 3f;
    public float playerRadius = 0.5f;

    [Header("UI")]
    public Sprite heartSprite;          // optional; red square used if empty
    public float heartSize = 64f;
    public float heartSpacing = 8f;
    public float heartMargin = 24f;
    public int bigTextSize = 140;

    int lives;
    R_PlayerController player;
    Image[] hearts;
    TextMeshProUGUI bigText;
    TextMeshProUGUI hintText;
    Sprite whiteSquare;

    void Awake()
    {
        Instance = this;
        IsGameOver = false;           // reset (survives scene reloads otherwise)
        lives = maxLives;
    }

    void Start()
    {
        if (autoSetup) BuildEverything();
        else player = FindObjectOfType<R_PlayerController>();

        BuildUI();
        UpdateHearts();
    }

    void Update()
    {
        if (!IsGameOver) return;
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---- called by the player / finish trigger ----
    public static void PlayerDied()
    {
        if (Instance != null && !IsGameOver) Instance.HandleDeath();
    }

    public static void WinGame()
    {
        if (Instance != null && !IsGameOver) Instance.EndGame(true);
    }

    void HandleDeath()
    {
        lives--;
        UpdateHearts();
        if (lives <= 0) EndGame(false);
        else if (player != null) player.Respawn();
    }

    void EndGame(bool won)
    {
        IsGameOver = true;
        if (bigText != null)
        {
            bigText.text = won ? "YOU WIN" : "YOU LOSE";
            bigText.color = won ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            bigText.gameObject.SetActive(true);
        }
        if (hintText != null) hintText.gameObject.SetActive(true);
    }

    // ---------------- scene building ----------------
    void BuildEverything()
    {
        EnsureLight();

        // tunnel (adopt an existing generator's radius as the source of truth)
        R_TunnelGenerator gen = FindObjectOfType<R_TunnelGenerator>();
        if (gen == null)
        {
            gen = new GameObject("R_Tunnel").AddComponent<R_TunnelGenerator>();
            gen.tunnelRadius = tunnelRadius;
        }
        else tunnelRadius = gen.tunnelRadius;

        if (gen.transform.childCount == 0) gen.Generate();
        tunnelRadius = gen.tunnelRadius;

        // player
        player = FindObjectOfType<R_PlayerController>();
        bool createdPlayer = false;
        if (player == null)
        {
            var pgo = new GameObject("R_Player");
            pgo.AddComponent<SphereCollider>().radius = playerRadius;
            player = pgo.AddComponent<R_PlayerController>();
            createdPlayer = true;
        }
        player.tunnelRadius = tunnelRadius;
        player.playerRadius = playerRadius;
        player.runSpeed = runSpeed;
        player.jumpSpeed = jumpSpeed;

        if (createdPlayer)
        {
            Vector3 start = new Vector3(0f, -tunnelRadius + playerRadius, gen.playerStartZ);
            player.startPosition = start;
            player.transform.position = start;
            AddSprite(player.transform);
        }

        // camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = cgo.AddComponent<Camera>();
        }
        cam.orthographic = false;
        cam.fieldOfView = 60f;

        var camCtrl = cam.GetComponent<R_CameraController>();
        if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<R_CameraController>();
        camCtrl.player = player;

        var bb = player.GetComponentInChildren<R_Billboard>();
        if (bb != null) bb.cam = cam;
    }

    void AddSprite(Transform parent)
    {
        var sgo = new GameObject("Sprite");
        sgo.transform.SetParent(parent, false);
        sgo.transform.localScale = Vector3.one * spriteScale;

        var sr = sgo.AddComponent<SpriteRenderer>();
        sr.sprite = (runFrames != null && runFrames.Length > 0) ? runFrames[0] : playerSprite;
        sr.sortingOrder = 10;           // draw over tunnel tiles (they're order 0)

        var bill = sgo.AddComponent<R_Billboard>();
        bill.cam = Camera.main;
        bill.player = player;

        // wire up animation (flipbook if frames are supplied, else it's a no-op)
        var anim = sgo.AddComponent<R_PlayerAnimator>();
        anim.player = player;
        anim.runFrames = runFrames;
        anim.jumpFrames = jumpFrames;
        anim.runFps = animFps;
        anim.jumpFps = animFps + 2f;
    }

    void EnsureLight()
    {
        if (FindObjectOfType<Light>() == null)
        {
            var lgo = new GameObject("Directional Light");
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lgo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
        // keep the shaded sides of the tunnel from going pure black in a 2D project
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
    }

    // ---------------- UI ----------------
    void BuildUI()
    {
        var canvasGo = new GameObject("R_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // hearts, top-left
        hearts = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            var hgo = new GameObject("Heart_" + i);
            hgo.transform.SetParent(canvasGo.transform, false);
            var img = hgo.AddComponent<Image>();
            if (heartSprite != null) img.sprite = heartSprite;
            else { img.sprite = WhiteSquare(); img.color = new Color(0.95f, 0.25f, 0.3f); }

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(heartSize, heartSize);
            rt.anchoredPosition = new Vector2(heartMargin + i * (heartSize + heartSpacing), -heartMargin);
            hearts[i] = img;
        }

        bigText = MakeText("R_BigText", canvasGo.transform, "", bigTextSize);
        bigText.gameObject.SetActive(false);

        hintText = MakeText("R_Hint", canvasGo.transform, "Press R to restart", 44);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -120f);
        hintText.gameObject.SetActive(false);
    }

    TextMeshProUGUI MakeText(string goName, Transform parent, string text, int size)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;

        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600, 400);
        rt.anchoredPosition = Vector2.zero;
        return t;
    }

    void UpdateHearts()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null) hearts[i].enabled = i < lives;
    }

    Sprite WhiteSquare()
    {
        if (whiteSquare == null)
        {
            var tex = Texture2D.whiteTexture;
            whiteSquare = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f));
        }
        return whiteSquare;
    }
}