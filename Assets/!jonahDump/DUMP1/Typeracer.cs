// TypeRacer.cs
// A fully 2D, TypeRacer-style typing scene for a Unity (2D or 3D) project.
//
// HOW IT PLAYS:
//   - Press 1 to start. A random paragraph (from the Inspector) appears.
//   - Just start typing - no clicking, no input field. Letters, commas, symbols, etc. all work.
//   - Each character of the paragraph turns GREEN if you typed it right, RED if wrong.
//   - The current character (your caret) is underlined.
//   - Backspace deletes (hold to repeat). Your typed text shows at the bottom.
//   - Press ENTER to check:
//       * Exact match  -> flash CORRECT, jump to next random paragraph.
//       * Not a match  -> flash INCORRECT, clear your typing, retry (or full reset - see toggle).
//   - After completing 'Paragraphs To Win' paragraphs (default 7) -> YOU WIN.
//   - On win, "YOU WIN" stays up for 'Return Delay After Win' seconds, then this scene tells
//     the main scene it's done (via ClassMinigameBridge.Finish), which resumes the main scene,
//     lerps the camera back and re-enables player input.
//
// SETUP: attach this to an empty GameObject and assign the three TextMeshPro texts.
//        (Detailed steps are in the chat message.)
//
// REQUIRES: ClassMinigameTrigger.cs must also be in the project, because this calls
//           ClassMinigameBridge.Finish(). Don't use this typing script in a project without it.
//
// NOTE on input: this auto-detects the Input System. If you're on the NEW Input System,
// keyboard text is captured via Keyboard.onTextInput; on the OLD one it uses Input.inputString.
// Either way it "just works" - nothing for you to configure.

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TypeRacer : MonoBehaviour
{
    public enum GameState { Idle, Playing, Won }

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

    [Header("Rules")]
    [Tooltip("How many paragraphs you must complete correctly to win.")]
    [Min(1)] public int paragraphsToWin = 7;

    [Tooltip("If ON, finishing the text correctly auto-advances - no Enter needed.\n" +
             "If OFF (default), you press Enter to submit.")]
    public bool autoSubmitWhenComplete = false;

    [Tooltip("If ON, a wrong Enter resets the WHOLE game back to 0 paragraphs.\n" +
             "If OFF (default), a wrong Enter just clears your typing so you retry the same paragraph.")]
    public bool wrongResetsEntireGame = false;

    [Tooltip("If ON (default), every paragraph is shown once before any repeats (a shuffled deck).\n" +
             "If OFF, paragraphs are picked purely at random and can repeat.")]
    public bool noRepeatsUntilCycled = true;

    [Header("UI References (TextMeshPro)")]
    [Tooltip("The prompt paragraph that colours green/red and shows the underline caret.")]
    public TMP_Text paragraphText;
    [Tooltip("The bottom text that shows exactly what you've typed.")]
    public TMP_Text inputText;
    [Tooltip("Shows CORRECT / INCORRECT / YOU WIN and the 'Press 1 to start' hint.")]
    public TMP_Text statusText;

    [Header("Colours")]
    public Color pendingColor = new Color(0.60f, 0.60f, 0.60f); // not typed yet
    public Color correctColor = new Color(0.30f, 0.85f, 0.40f); // typed correctly
    public Color incorrectColor = new Color(0.90f, 0.30f, 0.30f); // typed wrong
    public Color hintColor = Color.white;
    public Color winColor = new Color(1.00f, 0.85f, 0.20f);

    [Header("Messages")]
    public string startMessage = "Press 1 to start";
    public string correctMessage = "CORRECT";
    public string incorrectMessage = "INCORRECT";
    public string winMessage = "YOU WIN";

    [Header("Feel")]
    [Tooltip("How long CORRECT / INCORRECT stays on screen (seconds).")]
    public float flashDuration = 0.6f;
    [Tooltip("Delay before a held Backspace starts repeating.")]
    public float backspaceRepeatDelay = 0.4f;
    [Tooltip("Time between deletes while Backspace is held.")]
    public float backspaceRepeatRate = 0.04f;

    [Header("Minigame Return")]
    [Tooltip("After winning, how long YOU WIN stays on screen before returning to the main scene.\n" +
             "Set to 0 to return instantly.")]
    public float returnDelayAfterWin = 2f;

    // ---- runtime state ----
    private GameState state;
    private string target = "";
    private string typed = "";
    private int correctCount;
    private int lastIndex = -1;
    private readonly List<int> bag = new List<int>(); // shuffled deck of paragraph indices
    private bool dirty;

    private float statusTimer;
    private bool statusPersistent;
    private float backspaceTimer;

    private bool returning;
    private float returnTimer;

#if ENABLE_INPUT_SYSTEM
    private Keyboard subscribedKeyboard;
#endif

    void Awake()
    {
        if (paragraphText != null) paragraphText.richText = true;
        EnterIdle();
    }

#if ENABLE_INPUT_SYSTEM
    void OnDisable()
    {
        if (subscribedKeyboard != null)
        {
            subscribedKeyboard.onTextInput -= OnTextInput;
            subscribedKeyboard = null;
        }
    }

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
        if (state != GameState.Playing) return;
        if (IsControlChar(c)) return;        // Enter/Backspace handled separately
        if (typed.Length >= target.Length) return; // paragraph is full: only backspace/enter now
        typed += c;
        dirty = true;
        if (autoSubmitWhenComplete) TryAutoSubmit();
    }
#endif

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureTextSubscription();
#endif
        TickStatus();
        TickReturn();

        if (state == GameState.Playing)
        {
            HandleBackspace();
            if (EnterPressed()) Submit();
#if !ENABLE_INPUT_SYSTEM
            HandleLegacyTyping();
#endif
        }
        else // Idle or Won
        {
            if (!returning && StartPressed()) StartGame();
        }

        if (dirty)
        {
            RenderParagraph();
            dirty = false;
        }
    }

    // ---------------- input helpers ----------------

    bool StartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.digit1Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Alpha1);
#endif
    }

    bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
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
            char c = s[i];
            if (IsControlChar(c)) continue;  // skip backspace/enter/etc.
            if (typed.Length >= target.Length) break; // paragraph is full: only backspace/enter now
            typed += c;
            dirty = true;
        }
        if (autoSubmitWhenComplete) TryAutoSubmit();
    }
#endif

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

    void EnterIdle()
    {
        state = GameState.Idle;
        typed = "";
        target = "";
        if (paragraphText != null) paragraphText.text = "";
        if (inputText != null) inputText.text = "";
        ShowStatus(startMessage, hintColor, true);
    }

    void StartGame()
    {
        if (paragraphs == null || paragraphs.Length == 0)
        {
            ShowStatus("Add paragraphs in the Inspector", incorrectColor, true);
            return;
        }
        correctCount = 0;
        returning = false;
        lastIndex = -1;
        bag.Clear();
        state = GameState.Playing;
        typed = "";
        ClearStatus();
        LoadRandomParagraph();
        dirty = true;
    }

    void Submit()
    {
        if (typed == target)
        {
            correctCount++;
            if (correctCount >= paragraphsToWin)
            {
                Win();
            }
            else
            {
                ShowStatus(correctMessage, correctColor, false);
                typed = "";
                LoadRandomParagraph();
                dirty = true;
            }
        }
        else
        {
            ShowStatus(incorrectMessage, incorrectColor, false);
            typed = "";
            if (wrongResetsEntireGame)
            {
                correctCount = 0;
                LoadRandomParagraph();
            }
            dirty = true;
        }
    }

    void TryAutoSubmit()
    {
        if (state == GameState.Playing && target.Length > 0 && typed == target)
            Submit();
    }

    void Win()
    {
        state = GameState.Won;
        typed = "";
        target = "";
        if (paragraphText != null) paragraphText.text = "";
        if (inputText != null) inputText.text = "";
        ShowStatus(winMessage, winColor, true);

        // Start the countdown that returns control to the main scene.
        returning = true;
        returnTimer = returnDelayAfterWin;
    }

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
            var sb = new StringBuilder(target.Length * 24 + 32);
            int caret = typed.Length;

            for (int i = 0; i < target.Length; i++)
            {
                Color col;
                bool wrong;
                if (i < typed.Length) { wrong = typed[i] != target[i]; col = wrong ? incorrectColor : correctColor; }
                else { wrong = false; col = pendingColor; }
                // a wrong glyph already shows in red, but a wrong SPACE has nothing to colour,
                // so paint a red background block there instead.
                bool highlight = wrong && char.IsWhiteSpace(target[i]);
                AppendChar(sb, target[i], col, i == caret, highlight);
            }

            paragraphText.text = sb.ToString();
        }

        if (inputText != null) inputText.text = typed;
    }

    void AppendChar(StringBuilder sb, char c, Color col, bool underline, bool highlight)
    {
        sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(col)).Append('>');
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

    // ---------------- status text ----------------

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

    // ---------------- minigame return ----------------

    void TickReturn()
    {
        if (!returning) return;
        returnTimer -= Time.deltaTime;
        if (returnTimer <= 0f)
        {
            returning = false;
            ClassMinigameBridge.Finish(); // tells the main scene the minigame is done
        }
    }
}