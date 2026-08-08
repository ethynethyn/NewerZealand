using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// ============================================================
// RUN (flash game clone) — drop this on an empty GameObject in
// an empty scene and press play. Builds everything itself.
// All tuning lives here in one inspector.
// ============================================================
public class run_GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    [System.Serializable]
    public class TunnelSettings
    {
        [Tooltip("Sides of the tunnel. 8 = octagon (classic Run), 6 = hexagon.")]
        [Min(3)] public int faces = 8;
        [Min(1)] public int tilesPerFace = 4;
        [Min(0.1f)] public float tileSize = 1f;
        [Range(0f, 0.4f)] public float tileGap = 0.08f;
        [Min(20)] public int levelRows = 250;
        [Min(0)] public int startSafeRows = 12;
        [Min(1)] public int finishBandRows = 6;

        [Header("Holes")]
        [Range(0f, 1f)] public float holeDensity = 0.5f;
        [Min(1)] public int maxHoleLength = 3;
        [Min(1)] public int maxHoleWidth = 3;
        [Min(0)] public int minSolidTilesPerRow = 8;
        public bool rampDifficulty = true;
        [Tooltip("0 = random layout every build")] public int seed = 0;

        [Header("World Rotation")]
        [Tooltip("Degrees per second the tunnel spins when you change faces")]
        public float rotateSpeed = 400f;

        [Header("Performance")]
        public bool cullRings = true;
        public int ringsVisibleAhead = 70;
        public int ringsVisibleBehind = 8;
    }

    [System.Serializable]
    public class PlayerSettings
    {
        [Header("Movement")]
        public float runSpeed = 8f;
        public float maxRunSpeed = 14f;
        [Tooltip("Rows travelled to reach max speed")] public int speedRampRows = 160;
        public float strafeSpeed = 6f;

        [Header("Jump")]
        public float jumpForce = 8.5f;
        public float gravity = 25f;
        public bool variableJumpHeight = true;
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        [Tooltip("How close to a tile edge still counts as standing on it")]
        public float edgeForgiveness = 0.18f;
        [Tooltip("How fast the tiny position pop at face seams smooths out")]
        public float seamSmoothing = 10f;

        [Header("Death / Respawn")]
        public float fallDeathDepth = 7f;
        public float respawnDelay = 0.8f;
        public bool useCheckpoints = true;
        [Min(5)] public int checkpointEveryRows = 50;
        public float respawnBlinkTime = 0.9f;

        [Header("Visuals")]
        [Tooltip("Your 3-frame run animation goes here")]
        public Sprite[] runFrames;
        public Sprite jumpFrame;
        public float animFPS = 10f;
        public bool scaleAnimWithSpeed = true;
        public float spriteScale = 1f;
        public float spriteYOffset = 0.45f;
        public float strafeLeanAngle = 12f;
        public float fallSpinSpeed = 480f;

        [Header("Keys (new Input System)")]
        public Key leftKey = Key.A;
        public Key leftKeyAlt = Key.LeftArrow;
        public Key rightKey = Key.D;
        public Key rightKeyAlt = Key.RightArrow;
        public Key jumpKey = Key.Space;
        public Key jumpKeyAlt = Key.W;
        public Key jumpKeyAlt2 = Key.UpArrow;
    }

    [System.Serializable]
    public class CameraSettings
    {
        public bool createCamera = true;
        public bool addAudioListener = true;
        public float fov = 75f;
        public float height = 2.3f;
        public float distanceBehind = 5.5f;
        public float lookAhead = 7f;
        public float lookHeight = 1.2f;
        public float followSmoothing = 12f;
        public bool shakeOnDeath = true;
        public float shakeAmount = 0.3f;
        public float shakeTime = 0.25f;
    }

    [System.Serializable]
    public class LivesSettings
    {
        [Min(1)] public int maxLives = 5;
        public float heartSize = 48f;
        public float heartSpacing = 8f;
        public float screenPadding = 18f;
        public Color fullColor = new Color(1f, 0.2f, 0.25f);
        public Color emptyColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);
        public bool hideEmptyHearts = false;
        [Tooltip("Optional — leave empty to use a generated heart sprite")]
        public Sprite fullHeartSprite;
        public Sprite emptyHeartSprite;
    }

    [System.Serializable]
    public class UISettings
    {
        public string winText = "youWIN";
        public string loseText = "YOU LOSE";
        public Color winColor = new Color(0.4f, 1f, 0.5f);
        public Color loseColor = new Color(1f, 0.25f, 0.25f);
        public float bigFontSize = 110f;
        public Color screenDimColor = new Color(0f, 0f, 0f, 0.55f);
        public bool showRestartHint = true;
        public string restartHintText = "press R to restart";
        public float hintFontSize = 30f;
        public bool showProgress = true;
        public float fadeInSpeed = 4f;
        public Key restartKey = Key.R;
        public bool allowRestartAnytime = true;
    }

    [System.Serializable]
    public class ColorSettings
    {
        public Color tileColor = new Color(0.16f, 0.2f, 0.34f);
        public Color tileAltColor = new Color(0.21f, 0.26f, 0.42f);
        [Range(0f, 0.3f)] public float tileColorVariation = 0.06f;
        public Color finishColor = new Color(0.35f, 0.95f, 0.45f);
        public Color backgroundColor = new Color(0.01f, 0.01f, 0.03f);

        [Header("Stars")]
        public bool spawnStars = true;
        public int starCount = 350;
        public Color starColor = Color.white;
    }

    [System.Serializable]
    public class SoundSettings
    {
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        public AudioClip music;
        public AudioClip jumpSfx;
        public AudioClip landSfx;
        public AudioClip fallSfx;
        public AudioClip winSfx;
        public AudioClip loseSfx;
    }

    [Header("=== TUNNEL ===")] public TunnelSettings tunnel = new TunnelSettings();
    [Header("=== PLAYER ===")] public PlayerSettings player = new PlayerSettings();
    [Header("=== CAMERA ===")] public CameraSettings cam = new CameraSettings();
    [Header("=== LIVES ===")] public LivesSettings lives = new LivesSettings();
    [Header("=== UI ===")] public UISettings ui = new UISettings();
    [Header("=== COLORS ===")] public ColorSettings colors = new ColorSettings();
    [Header("=== AUDIO (all optional) ===")] public SoundSettings sound = new SoundSettings();

    [Header("=== EVENTS (hook your main game here) ===")]
    public UnityEvent onWin;
    public UnityEvent onLose;
    public UnityEvent onLifeLost;

    [Header("Restart")]
    public bool regenerateLayoutOnRestart = false;

    public GameState State { get; private set; } = GameState.Playing;
    public int CurrentLives { get; private set; }

    [HideInInspector] public run_TunnelGenerator Tunnel;
    [HideInInspector] public run_PlayerController Player;
    [HideInInspector] public run_CameraRig CameraRig;
    [HideInInspector] public run_HeartsUI Hearts;
    [HideInInspector] public run_UIScreens Screens;

    AudioSource _sfx, _music;

    void Awake()
    {
        Build();
    }

    void Build()
    {
        CurrentLives = lives.maxLives;

        // tunnel (this transform rotates — player lives inside it)
        var tunnelGO = new GameObject("run_Tunnel");
        tunnelGO.transform.SetParent(transform, false);
        Tunnel = tunnelGO.AddComponent<run_TunnelGenerator>();
        Tunnel.Build(this);

        // player
        var playerGO = new GameObject("run_Player");
        playerGO.transform.SetParent(tunnelGO.transform, false);
        Player = playerGO.AddComponent<run_PlayerController>();

        var visualGO = new GameObject("Visual");
        visualGO.transform.SetParent(playerGO.transform, false);
        visualGO.AddComponent<SpriteRenderer>();
        var anim = visualGO.AddComponent<run_PlayerAnimator>();

        Player.Init(this);
        anim.Init(this, Player);

        // camera
        var camGO = new GameObject("run_CameraRig");
        camGO.transform.SetParent(transform, false);
        CameraRig = camGO.AddComponent<run_CameraRig>();
        CameraRig.Init(this);

        // UI
        var canvasGO = new GameObject("run_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Hearts = canvasGO.AddComponent<run_HeartsUI>();
        Hearts.Init(this, canvas.transform as RectTransform);

        Screens = canvasGO.AddComponent<run_UIScreens>();
        Screens.Init(this, canvas.transform as RectTransform);

        // audio
        if (HasAnySfx())
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
        }
        if (sound.music != null)
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = sound.music;
            _music.loop = true;
            _music.volume = sound.musicVolume;
            _music.Play();
        }
    }

    bool HasAnySfx()
    {
        return sound.jumpSfx || sound.landSfx || sound.fallSfx || sound.winSfx || sound.loseSfx;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null && _sfx != null) _sfx.PlayOneShot(clip, sound.sfxVolume);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[ui.restartKey].wasPressedThisFrame && (State != GameState.Playing || ui.allowRestartAnytime))
            Restart();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        PlaySfx(sound.winSfx);
        Screens.ShowEnd(ui.winText, ui.winColor);
        onWin?.Invoke();
    }

    public void Lose()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        PlaySfx(sound.loseSfx);
        Screens.ShowEnd(ui.loseText, ui.loseColor);
        onLose?.Invoke();
    }

    // called by the player when it has fallen out into space
    public void PlayerFell()
    {
        if (State != GameState.Playing) return;
        CurrentLives--;
        Hearts.SetLives(CurrentLives);
        onLifeLost?.Invoke();
        if (cam.shakeOnDeath && CameraRig != null) CameraRig.Shake();

        if (CurrentLives <= 0)
        {
            Lose();
            return;
        }
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(player.respawnDelay);
        if (State != GameState.Playing) yield break;
        Player.Respawn();
        CameraRig.SnapToPlayer();
    }

    public void Restart()
    {
        StopAllCoroutines();
        State = GameState.Playing;
        CurrentLives = lives.maxLives;
        Hearts.SetLives(CurrentLives);
        Screens.HideEnd();
        if (regenerateLayoutOnRestart) Tunnel.Rebuild();
        Player.ResetToStart();
        CameraRig.SnapToPlayer();
    }
}
