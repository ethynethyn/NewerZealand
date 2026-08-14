// TypeRacer.cs
// A fully 2D, TypeRacer-style typing scene for a Unity (2D or 3D) project.
//
// HOW IT PLAYS:
//   - The game STARTS ON ITS OWN. When this object becomes active you get a short "GET READY"
//     beat (Start Delay) with the first paragraph visible but dimmed, then typing goes live.
//   - Just start typing - no clicking, no input field. Letters, commas, symbols, etc. all work.
//   - Every character is checked THE MOMENT you type it:
//       * Right -> it turns GREEN and the caret moves on.
//       * Wrong -> it flashes RED, you LOSE A HEART, the line RESETS, and typing is LOCKED
//         for about a second so you can register what happened. Mashing keys during the lock
//         costs you nothing - those keystrokes are thrown away, not queued up.
//   - HEARTS (default 3) show at the bottom. Lose them all -> run over.
//   - A counter at the bottom shows how many paragraphs you've cleared, e.g. "3 / 10".
//   - Finish 'Paragraphs To Win' paragraphs -> you pass.
//   - Either way the GRADE SCREEN appears, showing a grade based on how many hearts you lost:
//       0 lost -> A+   PASSED   +2 GOLD STARS
//       1 lost -> B-   PASSED   +2 GOLD STARS
//       2 lost -> C-   PASSED   +1 GOLD STAR
//       all    -> FAILED / F- / NO GOLD STARS OBTAINED
//     (every one of those strings, colours, thresholds and star counts is editable in the Inspector)
//   - After the grade screen it tells the main scene it's done (ClassMinigameBridge.Finish),
//     which resumes the main scene, lerps the camera back and re-enables player input.
//
// THE MISTAKE LOCK, IN ORDER:
//   wrong key -> [Mistake Flash Duration] the wrong letter sits there in red
//             -> line resets
//             -> [Mistake Lockout Duration] paragraph dims, caret hides, input ignored
//             -> back to typing
//   Total lock = Flash + Lockout (default 0.3 + 0.9 = 1.2s). Set either to 0 to skip that phase.
//
// SETUP:
//   1. Attach this to an empty GameObject.
//   2. Assign Paragraph Text, Input Text, Status Text.
//   3. Assign Progress Text and Hearts Text - two more TMP objects at the bottom of the screen.
//   4. (Optional but recommended) make two parent GameObjects: one holding all the gameplay UI,
//      one holding the grade UI. Drag them into Gameplay Root / Grade Root. The script swaps
//      them for you, so the grade screen feels like a separate screen without a scene load.
//   5. Assign Grade Text (big), and optionally Grade Result Text and Grade Stars Text.
//      If you only assign Grade Text, all three lines get stacked into it automatically.
//
// STARTING IT YOURSELF: leave Auto Start ON and the run fires the moment this GameObject is
//   enabled, which is usually exactly what you want for a minigame the trigger switches on.
//   If you'd rather control it, turn Auto Start OFF and call BeginRun() from your trigger or
//   from a UnityEvent.
//
// HEART GLYPHS: the default symbols are the unicode hearts. If your TMP font asset doesn't
//   contain them you'll see boxes - either add the glyphs to the font asset, or just change
//   Full/Empty Heart Symbol to something your font has, like "<3" and "x", or a TMP
//   <sprite=0> tag if you'd rather use a sprite atlas.
//
// REQUIRES: ClassMinigameTrigger.cs must also be in the project, because this calls
//           ClassMinigameBridge.Finish(). If you want to test this scene on its own, turn
//           OFF 'Return To Main Scene When Done' and it'll loop straight into another run.
//
// NOTE on input: this auto-detects the Input System. If you're on the NEW Input System,
// keyboard text is captured via Keyboard.onTextInput; on the OLD one it uses Input.inputString.
// Either way it "just works" - nothing for you to configure.

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TypeRacer : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Mistake, Locked, Ending, Grade }

    // ---------------------------------------------------------------- grade tiers

    [System.Serializable]
    public class GradeTier
    {
        [Tooltip("Used when the run ended with THIS many hearts lost.\n" +
                 "If no tier matches exactly, the closest one below is used, so you can leave gaps.")]
        [Min(0)] public int heartsLost = 0;

        [Tooltip("ON  = a run that reached the target paragraph count (a pass tier).\n" +
                 "OFF = a run that ran out of hearts (a fail tier).")]
        public bool passed = true;

        [Tooltip("The big letter, e.g. A+ / B- / C- / F-")]
        public string grade = "A+";

        [Tooltip("The line that says PASSED or FAILED.")]
        public string result = "PASSED";

        [Tooltip("The reward line, e.g. +2 GOLD STARS")]
        public string stars = "+2 GOLD STARS";

        [Tooltip("Numeric reward. Sent to the On Graded event and stored in TypeRacer.LastGoldStars " +
                 "so your save system / main scene can read it.")]
        [Min(0)] public int goldStars = 2;

        [Tooltip("ON  = print the result line ABOVE the grade  (FAILED / F- / NO GOLD STARS)\n" +
                 "OFF = print the grade first                  (A+ / PASSED / +2 GOLD STARS)\n" +
                 "Only affects the stacked layout (when you only assigned one grade text).")]
        public bool resultAboveGrade = false;

        [Tooltip("Colour for this tier's grade text.")]
        public Color color = new Color(1.00f, 0.85f, 0.20f);
    }

    [System.Serializable] public class IntEvent : UnityEvent<int> { }

    // ---------------------------------------------------------------- paragraphs

    [Header("Paragraphs (write as many as you want)")]
    [Tooltip("Each entry is one paragraph. One is picked at random each round.\n" +
             "Don't press Enter inside a paragraph - line breaks are converted to spaces, " +
             "and don't leave any entry blank.")]
    [TextArea(2, 6)]
    public string[] paragraphs = new string[]
    {
        "The quick brown fox jumps over the lazy dog.",
        "Pack my box with five dozen liquor jugs, please.",
        "Sphinx of black quartz, judge my vow!"
    };

    // ---------------------------------------------------------------- start

    [Header("Start")]
    [Tooltip("ON (default) = the run begins by itself as soon as this GameObject is enabled.\n" +
             "OFF = nothing happens until something calls BeginRun() on this script.")]
    public bool autoStart = true;

    [Tooltip("Seconds of 'get ready' before typing goes live. Set to 0 to start instantly.")]
    [Min(0f)] public float startDelay = 1.2f;

    [Tooltip("ON (default) = the first paragraph is already on screen (dimmed) during the " +
             "countdown so you can read ahead.\nOFF = the screen stays empty until GO.")]
    public bool showParagraphDuringCountdown = true;

    // ---------------------------------------------------------------- rules

    [Header("Rules")]
    [Tooltip("How many paragraphs you must clear to pass. This is the '10' in '3 / 10'.")]
    [Min(1)] public int paragraphsToWin = 10;

    [Tooltip("How many hearts you start with. Add matching Grade Tiers if you raise this.")]
    [Min(1)] public int maxHearts = 3;

    [Tooltip("ON (default) = typing a wrong letter instantly costs a heart and resets the line.\n" +
             "OFF = wrong letters just sit there in red until you press Enter.")]
    public bool loseHeartOnWrongLetter = true;

    [Tooltip("ON (default) = a mistake wipes the whole line and you retype it from the start.\n" +
             "OFF = only the offending character is removed, so you keep your progress in the line.")]
    public bool clearTypingOnMistake = true;

    [Tooltip("ON = a mistake also rolls a brand new paragraph.\n" +
             "OFF (default) = you retry the same paragraph.")]
    public bool newParagraphOnMistake = false;

    [Tooltip("ON = a mistake also knocks your paragraph counter back to 0.\n" +
             "OFF (default) = your cleared paragraphs are safe; only hearts are lost.")]
    public bool mistakeResetsProgress = false;

    [Tooltip("ON (default) = finishing the line correctly auto-advances, no Enter needed.\n" +
             "OFF = you press Enter to submit. (With letter-perfect typing ON, Enter only " +
             "does anything once the line is fully typed.)")]
    public bool autoSubmitWhenComplete = true;

    [Tooltip("Only matters when Lose Heart On Wrong Letter is OFF.\n" +
             "ON (default) = pressing Enter on a wrong line costs a heart.")]
    public bool wrongSubmitCostsHeart = true;

    [Tooltip("ON (default) = Backspace works. OFF = no takebacks, hardcore mode.")]
    public bool allowBackspace = true;

    [Tooltip("ON (default) = every paragraph is shown once before any repeats (a shuffled deck).\n" +
             "OFF = paragraphs are picked purely at random and can repeat.")]
    public bool noRepeatsUntilCycled = true;

    // ---------------------------------------------------------------- gameplay UI

    [Header("Gameplay UI (TextMeshPro)")]
    [Tooltip("The prompt paragraph that colours green/red and shows the underline caret.")]
    public TMP_Text paragraphText;
    [Tooltip("The bottom text that shows exactly what you've typed.")]
    public TMP_Text inputText;
    [Tooltip("Shows GET READY / CORRECT / MISTAKE / TRY AGAIN.")]
    public TMP_Text statusText;
    [Tooltip("Bottom-of-screen counter, e.g. '3 / 10'.")]
    public TMP_Text progressText;
    [Tooltip("Bottom-of-screen hearts, e.g. '♥ ♥ ♡'.")]
    public TMP_Text heartsText;

    [Tooltip("OPTIONAL. A parent object holding all the gameplay UI. It gets hidden when the " +
             "grade screen appears. Put the hearts/counter inside it if you DON'T want them " +
             "visible on the grade screen; leave them outside it if you do.")]
    public GameObject gameplayRoot;

    // ---------------------------------------------------------------- grade UI

    [Header("Grade Screen UI (TextMeshPro)")]
    [Tooltip("OPTIONAL. A parent object holding the grade screen. Leave it DISABLED in the scene; " +
             "the script switches it on when the run ends.")]
    public GameObject gradeRoot;

    [Tooltip("The big letter grade (A+ / B- / C- / F-).\n" +
             "If you leave Result and Stars empty below, all three lines get stacked into this one.")]
    public TMP_Text gradeText;
    [Tooltip("OPTIONAL. The PASSED / FAILED line on its own object.")]
    public TMP_Text gradeResultText;
    [Tooltip("OPTIONAL. The GOLD STARS line on its own object.")]
    public TMP_Text gradeStarsText;

    [Header("Grades (matched by how many hearts you LOST)")]
    [Tooltip("Edit freely - add tiers, change letters, change star counts, change colours.")]
    public GradeTier[] gradeTiers = new GradeTier[]
    {
        new GradeTier { heartsLost = 0, passed = true,  grade = "A+", result = "PASSED", stars = "+2 GOLD STARS",           goldStars = 2, resultAboveGrade = false, color = new Color(1.00f, 0.85f, 0.20f) },
        new GradeTier { heartsLost = 1, passed = true,  grade = "B-", result = "PASSED", stars = "+2 GOLD STARS",           goldStars = 2, resultAboveGrade = false, color = new Color(0.55f, 0.90f, 0.45f) },
        new GradeTier { heartsLost = 2, passed = true,  grade = "C-", result = "PASSED", stars = "+1 GOLD STAR",            goldStars = 1, resultAboveGrade = false, color = new Color(0.55f, 0.80f, 0.95f) },
        new GradeTier { heartsLost = 3, passed = false, grade = "F-", result = "FAILED", stars = "NO GOLD STARS OBTAINED",  goldStars = 0, resultAboveGrade = true,  color = new Color(0.90f, 0.30f, 0.30f) }
    };

    // ---------------------------------------------------------------- hud look

    [Header("Progress Counter Look")]
    [Tooltip("{0} = paragraphs cleared, {1} = paragraphs needed. e.g. \"{0} / {1}\" or \"{0} of {1} DONE\".")]
    public string progressFormat = "{0} / {1}";
    public Color progressColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Hearts Look")]
    [Tooltip("Symbol for a heart you still have.")]
    public string fullHeartSymbol = "♥";
    [Tooltip("Symbol for a heart you've lost.")]
    public string emptyHeartSymbol = "♡";
    [Tooltip("ON (default) = lost hearts stay on screen as empty outlines.\n" +
             "OFF = they just disappear.")]
    public bool showEmptyHearts = true;
    [Tooltip("Text placed between hearts. A space works; try \"  \" for a wider row.")]
    public string heartSeparator = " ";
    public Color fullHeartColor = new Color(0.95f, 0.30f, 0.40f);
    public Color emptyHeartColor = new Color(0.35f, 0.35f, 0.35f);

    // ---------------------------------------------------------------- colours

    [Header("Paragraph Colours")]
    public Color pendingColor = new Color(0.60f, 0.60f, 0.60f); // not typed yet
    public Color correctColor = new Color(0.30f, 0.85f, 0.40f); // typed correctly
    public Color incorrectColor = new Color(0.90f, 0.30f, 0.30f); // typed wrong
    public Color hintColor = Color.white;
    public Color winColor = new Color(1.00f, 0.85f, 0.20f);

    [Tooltip("How faded the paragraph goes while you're locked out of typing (start countdown and " +
             "post-mistake lockout). 1 = no fade, 0 = invisible. This is your 'you can't type " +
             "yet' tell.")]
    [Range(0f, 1f)] public float lockedParagraphAlpha = 0.40f;

    [Tooltip("ON (default) = the underline caret disappears while input is locked, so it's " +
             "obvious the game isn't listening yet.")]
    public bool hideCaretWhileLocked = true;

    // ---------------------------------------------------------------- messages

    [Header("Messages")]
    [Tooltip("Shown during the start countdown. Put {0} in it for a live seconds counter, " +
             "e.g. \"STARTING IN {0}\". Leave blank for no message.")]
    public string startMessage = "GET READY";
    public string correctMessage = "CORRECT";
    public string incorrectMessage = "INCORRECT";
    [Tooltip("Flashed the instant you type a wrong letter.")]
    public string mistakeMessage = "MISTAKE!";
    [Tooltip("Shown during the lockout after a mistake. {0} works here too for a countdown.")]
    public string lockoutMessage = "TRY AGAIN...";
    [Tooltip("Shown briefly after clearing the last paragraph, before the grade screen.")]
    public string winMessage = "YOU WIN";
    [Tooltip("Shown briefly after losing your last heart, before the grade screen.")]
    public string failMessage = "OUT OF HEARTS";
    [Tooltip("Only used when Require Key Press To Return is ON.")]
    public string gradeContinueHint = "Press ENTER to continue";

    // ---------------------------------------------------------------- feel

    [Header("Feel")]
    [Tooltip("How long CORRECT / INCORRECT stays on screen (seconds).")]
    public float flashDuration = 0.6f;
    [Tooltip("PHASE 1 of a mistake: how long the wrong letter stays visible in red before the " +
             "line resets. Set to 0 to skip straight to the lockout.")]
    [Min(0f)] public float mistakeFlashDuration = 0.30f;
    [Tooltip("PHASE 2 of a mistake: how long input is IGNORED after the line resets, so mashing " +
             "keys can't chain-drain your hearts. Set to 0 for no lockout.")]
    [Min(0f)] public float mistakeLockoutDuration = 0.90f;
    [Tooltip("Delay before a held Backspace starts repeating.")]
    public float backspaceRepeatDelay = 0.4f;
    [Tooltip("Time between deletes while Backspace is held.")]
    public float backspaceRepeatRate = 0.04f;

    // ---------------------------------------------------------------- ending / return

    [Header("Grade Screen Timing")]
    [Tooltip("How long YOU WIN shows before the grade screen appears.")]
    public float winRevealDelay = 1.2f;
    [Tooltip("How long OUT OF HEARTS shows before the grade screen appears.")]
    public float failRevealDelay = 1.2f;
    [Tooltip("How long the grade screen stays up before returning to the main scene.")]
    public float returnDelayAfterGrade = 3f;
    [Tooltip("ON = after the delay above, the grade screen waits for ENTER or SPACE instead of " +
             "leaving on its own.")]
    public bool requireKeyPressToReturn = false;

    [Header("Minigame Return")]
    [Tooltip("ON (default) = calls ClassMinigameBridge.Finish() to hand control back to the main scene.\n" +
             "OFF = starts a fresh run instead, handy for testing this scene alone.")]
    public bool returnToMainSceneWhenDone = true;

    [Tooltip("ADVANCED / OPTIONAL. Leave EMPTY for the built-in grade screen (recommended).\n" +
             "If you fill this in, the script loads that Unity scene instead and does NOT call " +
             "ClassMinigameBridge.Finish() - your grade scene becomes responsible for getting " +
             "back. Read the results from the static TypeRacer.Last... fields.")]
    public string gradeSceneName = "";

    // ---------------------------------------------------------------- events

    [Header("Events (hook up SFX / VFX / screen shake here)")]
    public UnityEvent onCountdownStarted;
    public UnityEvent onGameStarted;
    public UnityEvent onParagraphCleared;
    public UnityEvent onHeartLost;
    [Tooltip("Fires when the grade is decided. Passes the gold stars earned.")]
    public IntEvent onGraded;

    // ---------------------------------------------------------------- static results
    // Anything in your project (save system, main scene, a separate grade scene) can read these.

    public static string LastGrade = "";
    public static string LastResult = "";
    public static string LastStars = "";
    public static int LastGoldStars = 0;
    public static bool LastPassed = false;
    public static int LastHeartsLost = 0;
    public static int LastParagraphsCleared = 0;

    // ---------------------------------------------------------------- runtime state

    private GameState state;
    private string target = "";
    private string typed = "";
    private int correctCount;
    private int hearts;
    private int heartsLost;
    private int lastIndex = -1;
    private readonly List<int> bag = new List<int>(); // shuffled deck of paragraph indices
    private bool dirty;
    private bool setupError;

    private float statusTimer;
    private bool statusPersistent;
    private float backspaceTimer;

    private float countdownTimer;
    private float mistakeTimer;
    private float lockTimer;
    private float endingTimer;
    private bool endingPassed;

    private bool returning;
    private float gradeTimer;
    private bool gradeHintShown;

#if ENABLE_INPUT_SYSTEM
    private Keyboard subscribedKeyboard;
#endif

    // True whenever typed characters should be thrown on the floor.
    private bool InputLocked
    {
        get { return state != GameState.Playing; }
    }

    // True when the paragraph should render faded (the "not your turn yet" look).
    // Deliberately NOT during Mistake - you want to clearly see the red letter you fumbled.
    private bool DimParagraph
    {
        get { return state == GameState.Countdown || state == GameState.Locked; }
    }

    void Awake()
    {
        if (paragraphText != null) paragraphText.richText = true;
        if (heartsText != null) heartsText.richText = true;
        if (gradeRoot != null) gradeRoot.SetActive(false);
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
    }

    void OnEnable()
    {
        if (autoStart) BeginRun();
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    void EnsureTextSubscription()
    {
        var kb = Keyboard.current;
        if (kb == subscribedKeyboard) return;
        if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= OnTextInput;
        subscribedKeyboard = kb;
        if (subscribedKeyboard != null) subscribedKeyboard.onTextInput += OnTextInput;
    }

    void OnTextInput(char c)
    {
        PushChar(c);
    }
#endif

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureTextSubscription();
#endif
        TickStatus();

        switch (state)
        {
            case GameState.Countdown:
                if (setupError) break;
                countdownTimer -= Time.deltaTime;
                TickCountdownStatus(startMessage, hintColor, countdownTimer);
                if (countdownTimer <= 0f) BeginPlaying();
                break;

            case GameState.Playing:
                if (allowBackspace) HandleBackspace();
                if (EnterPressed()) EnterSubmit();
#if !ENABLE_INPUT_SYSTEM
                HandleLegacyTyping();
#endif
                break;

            case GameState.Mistake:
                mistakeTimer -= Time.deltaTime;
                if (mistakeTimer <= 0f) ResolveMistake();
                break;

            case GameState.Locked:
                lockTimer -= Time.deltaTime;
                TickCountdownStatus(lockoutMessage, incorrectColor, lockTimer);
                if (lockTimer <= 0f) EndLockout();
                break;

            case GameState.Ending:
                endingTimer -= Time.deltaTime;
                if (endingTimer <= 0f) EnterGrade(endingPassed);
                break;

            case GameState.Grade:
                TickGrade();
                break;
        }

        if (dirty)
        {
            RenderParagraph();
            dirty = false;
        }
    }

    // ---------------- input helpers ----------------

    bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    // Used only by the grade screen's "press a key to continue" option.
    bool ContinuePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame ||
                              kb.numpadEnterKey.wasPressedThisFrame ||
                              kb.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
               Input.GetKeyDown(KeyCode.Space);
#endif
    }

    bool BackspacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.backspaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Backspace);
#endif
    }

    bool BackspaceHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.backspaceKey.isPressed;
#else
        return Input.GetKey(KeyCode.Backspace);
#endif
    }

#if !ENABLE_INPUT_SYSTEM
    void HandleLegacyTyping()
    {
        string s = Input.inputString;
        if (string.IsNullOrEmpty(s)) return;
        for (int i = 0; i < s.Length; i++)
        {
            PushChar(s[i]);
            if (state != GameState.Playing) break; // a mistake ended the line; drop the rest
        }
    }
#endif

    // The single funnel every typed character goes through, old or new Input System.
    // Anything that arrives while input is locked is discarded right here - never buffered -
    // so panic-mashing during the mistake lockout can't cost you a second heart.
    void PushChar(char c)
    {
        if (InputLocked) return;
        if (IsControlChar(c)) return;              // Enter/Backspace handled separately
        if (typed.Length >= target.Length) return; // line is full: only backspace/enter now

        bool wrong = c != target[typed.Length];
        typed += c;
        dirty = true;

        if (wrong && loseHeartOnWrongLetter)
        {
            BeginMistake();
            return;
        }

        if (autoSubmitWhenComplete) TryAutoSubmit();
    }

    void HandleBackspace()
    {
        if (BackspacePressed())
        {
            DeleteOne();
            backspaceTimer = backspaceRepeatDelay;
        }
        else if (BackspaceHeld())
        {
            backspaceTimer -= Time.deltaTime;
            if (backspaceTimer <= 0f)
            {
                DeleteOne();
                backspaceTimer = backspaceRepeatRate;
            }
        }
    }

    void DeleteOne()
    {
        if (typed.Length == 0) return;
        typed = typed.Substring(0, typed.Length - 1);
        dirty = true;
    }

    static bool IsControlChar(char c)
    {
        return c == '\b' || c == '\n' || c == '\r' || c == '\t' || char.IsControl(c);
    }

    // ---------------- game flow ----------------

    /// <summary>
    /// Wipes everything and starts a fresh run (countdown first, then typing).
    /// Called automatically on enable when Auto Start is ON; call it yourself otherwise.
    /// </summary>
    public void BeginRun()
    {
        setupError = false;

        if (paragraphs == null || paragraphs.Length == 0)
        {
            state = GameState.Countdown;
            setupError = true;
            ShowStatus("Add paragraphs in the Inspector", incorrectColor, true);
            return;
        }

        correctCount = 0;
        heartsLost = 0;
        hearts = Mathf.Max(1, maxHearts);
        returning = false;
        gradeHintShown = false;
        typed = "";
        lastIndex = -1;
        bag.Clear();

        if (gradeRoot != null) gradeRoot.SetActive(false);
        if (gameplayRoot != null) gameplayRoot.SetActive(true);
        if (inputText != null) inputText.text = "";

        RefreshHud();
        LoadRandomParagraph();

        state = GameState.Countdown;
        countdownTimer = Mathf.Max(0f, startDelay);

        if (!showParagraphDuringCountdown && paragraphText != null) paragraphText.text = "";
        else dirty = true;

        ShowStatus(WithSeconds(startMessage, countdownTimer), hintColor, true);
        if (onCountdownStarted != null) onCountdownStarted.Invoke();

        if (countdownTimer <= 0f) BeginPlaying();
    }

    /// <summary>Skips the rest of the countdown and goes live immediately.</summary>
    public void BeginPlaying()
    {
        state = GameState.Playing;
        ClearStatus();
        dirty = true;
        if (onGameStarted != null) onGameStarted.Invoke();
    }

    // Enter key while playing.
    void EnterSubmit()
    {
        // In letter-perfect mode a half-typed line isn't "wrong", it's just unfinished,
        // so Enter only does anything once you've typed the whole thing.
        if (loseHeartOnWrongLetter && typed.Length < target.Length) return;
        Submit();
    }

    void Submit()
    {
        if (typed == target)
        {
            correctCount++;
            RefreshHud();
            if (onParagraphCleared != null) onParagraphCleared.Invoke();

            if (correctCount >= paragraphsToWin)
            {
                QueueEnding(true);
            }
            else
            {
                ShowStatus(correctMessage, correctColor, false);
                typed = "";
                LoadRandomParagraph();
                dirty = true;
            }
            return;
        }

        // Wrong on Enter. Only reachable when Lose Heart On Wrong Letter is OFF.
        ShowStatus(incorrectMessage, incorrectColor, false);
        typed = "";
        dirty = true;

        bool alive = true;
        if (wrongSubmitCostsHeart) alive = SpendHeart();
        if (mistakeResetsProgress) { correctCount = 0; RefreshHud(); }

        if (!alive) { QueueEnding(false); return; }
        if (newParagraphOnMistake) LoadRandomParagraph();
        BeginLockout();
    }

    void TryAutoSubmit()
    {
        if (state == GameState.Playing && target.Length > 0 && typed == target)
            Submit();
    }

    // ---------------- mistakes, hearts & the input lockout ----------------

    // Returns true if the player is still alive afterwards.
    bool SpendHeart()
    {
        hearts = Mathf.Max(0, hearts - 1);
        heartsLost++;
        RefreshHud();
        if (onHeartLost != null) onHeartLost.Invoke();
        return hearts > 0;
    }

    // PHASE 1: freeze on the red letter so the player can see what they fumbled.
    void BeginMistake()
    {
        SpendHeart();
        ShowStatus(mistakeMessage, incorrectColor, true);
        state = GameState.Mistake;
        mistakeTimer = Mathf.Max(0f, mistakeFlashDuration);
        dirty = true;
        if (mistakeTimer <= 0f) ResolveMistake();
    }

    // End of phase 1: reset the line, then hand over to the lockout.
    void ResolveMistake()
    {
        if (hearts <= 0) { QueueEnding(false); return; }

        if (clearTypingOnMistake) typed = "";
        else if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);

        if (mistakeResetsProgress) { correctCount = 0; RefreshHud(); }
        if (newParagraphOnMistake) LoadRandomParagraph();

        BeginLockout();
    }

    // PHASE 2: the line is clean and readable again, but keys do nothing for a beat.
    void BeginLockout()
    {
        lockTimer = Mathf.Max(0f, mistakeLockoutDuration);
        state = GameState.Locked;
        dirty = true;

        if (lockTimer <= 0f) { EndLockout(); return; }
        ShowStatus(WithSeconds(lockoutMessage, lockTimer), incorrectColor, true);
    }

    void EndLockout()
    {
        state = GameState.Playing;
        ClearStatus();
        dirty = true;
    }

    // ---------------- ending ----------------

    void QueueEnding(bool passed)
    {
        endingPassed = passed;
        endingTimer = Mathf.Max(0f, passed ? winRevealDelay : failRevealDelay);
        state = GameState.Ending;

        typed = "";
        target = "";
        if (paragraphText != null) paragraphText.text = "";
        if (inputText != null) inputText.text = "";

        ShowStatus(passed ? winMessage : failMessage,
                   passed ? winColor : incorrectColor, true);

        if (endingTimer <= 0f) EnterGrade(passed);
    }

    void EnterGrade(bool passed)
    {
        state = GameState.Grade;
        ClearStatus();

        GradeTier tier = FindTier(heartsLost, passed);

        LastGrade = tier.grade;
        LastResult = tier.result;
        LastStars = tier.stars;
        LastGoldStars = tier.goldStars;
        LastPassed = passed;
        LastHeartsLost = heartsLost;
        LastParagraphsCleared = correctCount;

        if (onGraded != null) onGraded.Invoke(tier.goldStars);

        // Optional: hand off to a real Unity scene instead of the built-in screen.
        if (!string.IsNullOrEmpty(gradeSceneName))
        {
            SceneManager.LoadScene(gradeSceneName);
            return;
        }

        if (gameplayRoot != null) gameplayRoot.SetActive(false);
        if (gradeRoot != null) gradeRoot.SetActive(true);
        ApplyGradeTexts(tier);

        returning = true;
        gradeHintShown = false;
        gradeTimer = Mathf.Max(0f, returnDelayAfterGrade);
    }

    // Picks the tier with the highest 'heartsLost' that isn't above what actually happened.
    GradeTier FindTier(int lost, bool passed)
    {
        GradeTier best = null;
        if (gradeTiers != null)
        {
            foreach (var t in gradeTiers)
            {
                if (t == null || t.passed != passed || t.heartsLost > lost) continue;
                if (best == null || t.heartsLost > best.heartsLost) best = t;
            }
            if (best == null) // nothing at or below: fall back to any tier on the right side
            {
                foreach (var t in gradeTiers)
                {
                    if (t == null || t.passed != passed) continue;
                    if (best == null || t.heartsLost < best.heartsLost) best = t;
                }
            }
        }

        // Last-ditch default so the screen is never blank if the array got emptied.
        if (best == null)
        {
            best = passed
                ? new GradeTier { grade = "A+", result = "PASSED", stars = "+2 GOLD STARS", goldStars = 2, color = winColor }
                : new GradeTier { grade = "F-", result = "FAILED", stars = "NO GOLD STARS OBTAINED", goldStars = 0, resultAboveGrade = true, color = incorrectColor };
        }
        return best;
    }

    void ApplyGradeTexts(GradeTier t)
    {
        bool split = (gradeResultText != null || gradeStarsText != null);
        TMP_Text host = gradeText != null ? gradeText : statusText;

        if (split)
        {
            if (host != null) { host.text = t.grade; host.color = t.color; }
            if (gradeResultText != null) { gradeResultText.text = t.result; gradeResultText.color = t.color; }
            if (gradeStarsText != null) { gradeStarsText.text = t.stars; gradeStarsText.color = t.color; }
        }
        else if (host != null)
        {
            host.text = BuildStackedGrade(t);
            host.color = t.color;
        }
    }

    string BuildStackedGrade(GradeTier t)
    {
        var sb = new StringBuilder(64);
        if (t.resultAboveGrade)
        {
            if (!string.IsNullOrEmpty(t.result)) sb.Append(t.result).Append('\n');
            if (!string.IsNullOrEmpty(t.grade)) sb.Append(t.grade).Append('\n');
        }
        else
        {
            if (!string.IsNullOrEmpty(t.grade)) sb.Append(t.grade).Append('\n');
            if (!string.IsNullOrEmpty(t.result)) sb.Append(t.result).Append('\n');
        }
        if (!string.IsNullOrEmpty(t.stars)) sb.Append(t.stars);
        return sb.ToString().TrimEnd('\n');
    }

    void TickGrade()
    {
        if (!returning) return;

        if (gradeTimer > 0f)
        {
            gradeTimer -= Time.deltaTime;
            if (gradeTimer > 0f) return;
        }

        if (requireKeyPressToReturn)
        {
            if (!gradeHintShown)
            {
                gradeHintShown = true;
                ShowStatus(gradeContinueHint, hintColor, true);
                return; // never accept the keypress on the same frame the hint appears
            }
            if (!ContinuePressed()) return;
        }

        returning = false;
        ClearStatus();
        FinishMinigame();
    }

    void FinishMinigame()
    {
        if (returnToMainSceneWhenDone)
        {
            ClassMinigameBridge.Finish(); // tells the main scene the minigame is done
        }
        else
        {
            BeginRun(); // testing loop: straight into another run
        }
    }

    // ---------------- paragraph picking ----------------

    void LoadRandomParagraph()
    {
        int idx = NextParagraphIndex();
        lastIndex = idx;
        target = Normalize(paragraphs[idx]);
    }

    // Deals indices from a shuffled deck so every paragraph appears once before any repeats.
    int NextParagraphIndex()
    {
        if (paragraphs.Length == 1) return 0;
        if (!noRepeatsUntilCycled) return Random.Range(0, paragraphs.Length);

        if (bag.Count == 0) RefillBag();
        int last = bag.Count - 1;
        int idx = bag[last];
        bag.RemoveAt(last);
        return idx;
    }

    void RefillBag()
    {
        bag.Clear();
        for (int i = 0; i < paragraphs.Length; i++) bag.Add(i);

        // Fisher-Yates shuffle.
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = bag[i]; bag[i] = bag[j]; bag[j] = t;
        }

        // Don't let the next deal repeat the paragraph we just showed (deck boundary).
        if (paragraphs.Length > 1 && bag[bag.Count - 1] == lastIndex)
        {
            int k = Random.Range(0, bag.Count - 1);
            int t = bag[bag.Count - 1]; bag[bag.Count - 1] = bag[k]; bag[k] = t;
        }
    }

    static string Normalize(string p)
    {
        if (string.IsNullOrEmpty(p)) return "";
        return p.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    // ---------------- rendering ----------------

    void RenderParagraph()
    {
        if (paragraphText != null)
        {
            float alpha = DimParagraph ? Mathf.Clamp01(lockedParagraphAlpha) : 1f;
            bool caretOn = !(DimParagraph && hideCaretWhileLocked);

            var sb = new StringBuilder(target.Length * 26 + 32);
            int caret = typed.Length;

            for (int i = 0; i < target.Length; i++)
            {
                Color col;
                bool wrong;
                if (i < typed.Length) { wrong = typed[i] != target[i]; col = wrong ? incorrectColor : correctColor; }
                else { wrong = false; col = pendingColor; }
                col.a = alpha;
                // a wrong glyph already shows in red, but a wrong SPACE has nothing to colour,
                // so paint a red background block there instead.
                bool highlight = wrong && char.IsWhiteSpace(target[i]);
                AppendChar(sb, target[i], col, caretOn && i == caret, highlight);
            }

            paragraphText.text = sb.ToString();
        }

        if (inputText != null) inputText.text = typed;
    }

    void AppendChar(StringBuilder sb, char c, Color col, bool underline, bool highlight)
    {
        sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(col)).Append('>');
        if (highlight) sb.Append("<mark=#").Append(ColorUtility.ToHtmlStringRGBA(incorrectColor)).Append('>');
        if (underline) sb.Append("<u>");
        // escape characters that TMP would otherwise read as rich-text tags
        if (c == '<') sb.Append("<noparse><</noparse>");
        else if (c == '>') sb.Append("<noparse>></noparse>");
        else sb.Append(c);
        if (underline) sb.Append("</u>");
        if (highlight) sb.Append("</mark>");
        sb.Append("</color>");
    }

    // ---------------- HUD (counter + hearts) ----------------

    void RefreshHud()
    {
        if (progressText != null)
        {
            progressText.text = FormatProgress(correctCount, paragraphsToWin);
            progressText.color = progressColor;
        }

        if (heartsText != null)
            heartsText.text = BuildHearts();
    }

    // Manual token swap instead of string.Format, so a typo'd format string in the
    // Inspector can never throw at runtime.
    string FormatProgress(int done, int total)
    {
        string s = string.IsNullOrEmpty(progressFormat) ? "{0} / {1}" : progressFormat;
        return s.Replace("{0}", done.ToString()).Replace("{1}", total.ToString());
    }

    string BuildHearts()
    {
        var sb = new StringBuilder(maxHearts * 24);
        for (int i = 0; i < maxHearts; i++)
        {
            bool alive = i < hearts;
            if (!alive && !showEmptyHearts) continue;

            if (sb.Length > 0) sb.Append(heartSeparator);
            sb.Append("<color=#")
              .Append(ColorUtility.ToHtmlStringRGB(alive ? fullHeartColor : emptyHeartColor))
              .Append('>')
              .Append(alive ? fullHeartSymbol : emptyHeartSymbol)
              .Append("</color>");
        }
        return sb.ToString();
    }

    // ---------------- status text ----------------

    static string WithSeconds(string msg, float remaining)
    {
        if (string.IsNullOrEmpty(msg)) return "";
        if (msg.IndexOf("{0}") < 0) return msg;
        return msg.Replace("{0}", Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString());
    }

    // Keeps a countdown message live without rebuilding the text mesh every single frame.
    void TickCountdownStatus(string msg, Color col, float remaining)
    {
        if (statusText == null) return;
        if (string.IsNullOrEmpty(msg) || msg.IndexOf("{0}") < 0) return; // static text, already set
        string s = WithSeconds(msg, remaining);
        if (statusText.text == s) return;
        statusText.text = s;
        statusText.color = col;
    }

    void ShowStatus(string msg, Color col, bool persistent)
    {
        statusPersistent = persistent;
        statusTimer = persistent ? 0f : flashDuration;
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = col;
        }
    }

    void ClearStatus()
    {
        statusPersistent = false;
        statusTimer = 0f;
        if (statusText != null) statusText.text = "";
    }

    void TickStatus()
    {
        if (statusPersistent || statusTimer <= 0f) return;
        statusTimer -= Time.deltaTime;
        if (statusTimer <= 0f && statusText != null) statusText.text = "";
    }
}