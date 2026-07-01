using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Math-quiz minigame with direct keyboard capture (no InputField).
/// The player just types and the characters appear on screen; backspace
/// deletes, Enter submits.
///
/// SETUP:
///  1. Canvas (Screen Space - Overlay) in your scene/panel.
///  2. Add 3 TextMeshPro UI texts: one for the question, one for the typed
///     answer, one for feedback. Drag them into the slots below.
///  3. Fill the Problems list. Done.
///
/// NOTE: this reads Input.inputString (the old Input Manager). If your project
/// is set to the NEW Input System only, go to Project Settings > Player > Other
/// Settings > Active Input Handling and set it to "Both" (or "Input Manager").
/// </summary>
public class MathQuiz : MonoBehaviour
{
    [System.Serializable]
    public class MathProblem
    {
        public string question = "2 + 5";   // what's shown on screen
        public string answer = "7";       // the correct answer
    }

    [Header("Problems (add up to 100+ here)")]
    public List<MathProblem> problems = new List<MathProblem>();

    [Header("Settings")]
    [Tooltip("How many you need to get right to win.")]
    public int problemsToWin = 7;
    [Tooltip("If true, problems are pulled in random order from the list.")]
    public bool randomizeOrder = true;
    [Tooltip("Only allow digits, minus, and a decimal point to be typed.")]
    public bool numbersOnly = true;

    [Header("Minigame Return")]
    [Tooltip("After winning, how long YOU WIN shows before returning to the class. 0 = return instantly.")]
    public float returnDelayAfterWin = 2f;

    [Header("UI References")]
    public TMP_Text questionText;   // shows the current problem
    public TMP_Text answerText;     // shows what the player is typing
    public TMP_Text feedbackText;   // shows INCORRECT / YOU WIN

    [Header("Messages")]
    public string incorrectMessage = "INCORRECT";
    public string winMessage = "YOU WIN";

    [Header("Typing Cursor")]
    public bool showCursor = true;
    public string cursorChar = "_";
    public float cursorBlinkRate = 0.5f;

    [Header("Events (optional)")]
    [Tooltip("Fires when the player wins. Hook up returning to your 3D scene, etc.")]
    public UnityEvent onWin;

    private readonly List<MathProblem> queue = new List<MathProblem>();
    private MathProblem current;
    private string typed = "";
    private int solvedCount = 0;
    private bool finished = false;

    private bool cursorOn = true;
    private float cursorTimer = 0f;

    void OnEnable()
    {
        StartQuiz();
    }

    public void StartQuiz()
    {
        finished = false;
        solvedCount = 0;
        typed = "";
        if (feedbackText != null) feedbackText.text = "";

        queue.Clear();
        queue.AddRange(problems);
        if (randomizeOrder) Shuffle(queue);

        ShowNextProblem();
    }

    void Update()
    {
        if (finished) return;

        // Grab everything typed this frame.
        foreach (char c in Input.inputString)
        {
            if (c == '\b')                       // backspace
            {
                if (typed.Length > 0)
                    typed = typed.Substring(0, typed.Length - 1);
            }
            else if (c == '\n' || c == '\r')     // enter / return
            {
                Submit();
                return;                          // problem may have changed; bail
            }
            else if (IsAllowed(c))
            {
                typed += c;
            }
        }

        // Blink the fake cursor.
        if (showCursor)
        {
            cursorTimer += Time.deltaTime;
            if (cursorTimer >= cursorBlinkRate)
            {
                cursorTimer = 0f;
                cursorOn = !cursorOn;
            }
        }

        RefreshAnswer();
    }

    bool IsAllowed(char c)
    {
        if (!numbersOnly) return !char.IsControl(c);
        return char.IsDigit(c) || c == '-' || c == '.';
    }

    void ShowNextProblem()
    {
        // Refill if we run out before winning so it never dead-ends.
        if (queue.Count == 0)
        {
            if (problems.Count == 0)
            {
                if (questionText != null) questionText.text = "(add problems in the Inspector)";
                return;
            }
            queue.AddRange(problems);
            if (randomizeOrder) Shuffle(queue);
        }

        current = queue[0];
        queue.RemoveAt(0);

        typed = "";
        if (questionText != null) questionText.text = current.question;
        RefreshAnswer();
    }

    void Submit()
    {
        if (current == null) return;

        if (IsCorrect(typed, current.answer))
        {
            solvedCount++;
            if (feedbackText != null) feedbackText.text = "";

            if (solvedCount >= problemsToWin)
                Win();
            else
                ShowNextProblem();   // instantly move on
        }
        else
        {
            if (feedbackText != null) feedbackText.text = incorrectMessage;
            typed = "";              // wipe their wrong answer
            RefreshAnswer();
        }
    }

    void Win()
    {
        finished = true;
        typed = "";
        if (questionText != null) questionText.text = "";
        if (answerText != null) answerText.text = "";
        if (feedbackText != null) feedbackText.text = winMessage;
        onWin?.Invoke();

        // Tell the class minigame system the player finished, after a beat so YOU WIN shows.
        StartCoroutine(ReturnToClassAfterWin());
    }

    IEnumerator ReturnToClassAfterWin()
    {
        yield return new WaitForSeconds(returnDelayAfterWin);
        ClassMinigameBridge.Finish();
    }

    void RefreshAnswer()
    {
        if (answerText == null) return;
        string cursor = (showCursor && cursorOn && !finished) ? cursorChar : "";
        answerText.text = typed + cursor;
    }

    // Numeric-aware so "7", " 7 ", and "07" all count. Falls back to text match.
    bool IsCorrect(string a, string b)
    {
        a = (a ?? "").Trim();
        b = (b ?? "").Trim();
        if (float.TryParse(a, out float fa) && float.TryParse(b, out float fb))
            return Mathf.Approximately(fa, fb);
        return a.Equals(b, System.StringComparison.OrdinalIgnoreCase);
    }

    void Shuffle(List<MathProblem> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}