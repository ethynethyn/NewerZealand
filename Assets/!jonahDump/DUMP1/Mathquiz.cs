using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Math-quiz minigame with direct keyboard capture (no InputField),
/// hearts, an "X / 10" progress counter, and a graded results screen.
///
/// FLOW:
///  - Wrong answer = lose 1 heart (adjustable).
///  - Lose every heart = instant FAILED screen.
///  - Complete the target number of problems (10 by default) = results
///    screen graded by how many hearts you lost (A+ / B- / C-).
///  - Grade sits on screen for Results Duration (3s), then loads the
///    morning tea scene that matches MorningTeaManager.morningTeaNumber.
///
/// SETUP:
///  1. Canvas (Screen Space - Overlay) in your scene/panel.
///  2. TMP texts: Question, Answer, Feedback (same as before).
///  3. A TMP text anchored to the BOTTOM of the screen -> Hearts Text.
///     (Or drag 3 heart images/objects into Heart Icons instead - or both.)
///  4. A TMP text for the "3 / 10" counter -> Progress Text.
///  5. Optional: a full-screen panel with one TMP text inside ->
///     Results Panel + Results Text. If you leave it empty, the grade just
///     shows on the Feedback text instead, so nothing breaks.
///  6. NEW: fill in Morning Tea Scenes under "After Results".
///     Element 0 = morning tea 1, element 1 = morning tea 2, and so on.
///     All those scenes need to be in File > Build Settings.
///  7. All grades, messages, and gold star amounts are editable under
///     the "Grading" header.
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
        public string answer = "7";         // the correct answer
    }

    [System.Serializable]
    public class GradeResult
    {
        [Tooltip("Exactly what the results screen says. Press Enter inside this box for new lines.")]
        [TextArea(2, 4)]
        public string screenText = "A+\nPASSED +2 GOLD STARS";

        [Tooltip("Gold stars this grade hands out. Sent through On Gold Stars Awarded and stored in MathQuiz.LastGoldStars.")]
        public int goldStars = 2;
    }

    [Header("Problems (add up to 100+ here)")]
    public List<MathProblem> problems = new List<MathProblem>();

    [Header("Quiz Settings")]
    [Tooltip("How many problems you must complete to finish the quiz. This is the 10 in '3 / 10'.")]
    public int problemsToComplete = 10;
    [Tooltip("If true, problems are pulled in random order from the list.")]
    public bool randomizeOrder = true;
    [Tooltip("Only allow digits, minus, and a decimal point to be typed.")]
    public bool numbersOnly = true;

    [Header("Hearts")]
    [Tooltip("How many hearts you start with.")]
    public int startingHearts = 3;
    [Tooltip("Hearts lost per wrong answer.")]
    public int heartsLostPerWrongAnswer = 1;
    [Tooltip("If true, a wrong answer ALSO moves on to the next problem and counts toward the X / 10. If false, you retry the same problem (old behaviour).")]
    public bool advanceOnWrongAnswer = false;
    [Tooltip("If true, losing every heart ends the quiz instantly with the FAILED screen.")]
    public bool failWhenOutOfHearts = true;

    [Header("Grading (element 0 = lost 0 hearts, element 1 = lost 1 heart, etc.)")]
    public List<GradeResult> passGrades = new List<GradeResult>
    {
        new GradeResult { screenText = "A+\nPASSED +2 GOLD STARS", goldStars = 2 },
        new GradeResult { screenText = "B-\nPASSED +2 GOLD STARS", goldStars = 2 },
        new GradeResult { screenText = "C-\nPASSED +1 GOLD STAR",  goldStars = 1 },
    };

    [Tooltip("Shown when you run out of hearts (or somehow finish with none left).")]
    public GradeResult failGrade = new GradeResult
    {
        screenText = "FAILED\nF-\nNO GOLD STARS OBTAINED",
        goldStars = 0
    };

    [Header("UI References")]
    public TMP_Text questionText;   // shows the current problem
    public TMP_Text answerText;     // shows what the player is typing
    public TMP_Text feedbackText;   // shows INCORRECT (and the grade, if no results panel is set)

    [Header("Hearts UI (use the text, the icons, or both)")]
    [Tooltip("Text version of the hearts. Anchor it to the bottom of the screen.")]
    public TMP_Text heartsText;
    [Tooltip("Character(s) for a heart you still have. If your font draws it as a box, swap it for something like 'O' or '<3', or use Heart Icons instead.")]
    public string fullHeart = "\u2665";     // ♥
    [Tooltip("Character(s) for a lost heart. Leave empty to make lost hearts vanish instead.")]
    public string emptyHeart = "\u2661";    // ♡
    [Tooltip("Placed between hearts.")]
    public string heartSpacing = " ";
    [Tooltip("Image version: drag heart objects here, left to right. They get switched off as you lose them.")]
    public List<GameObject> heartIcons = new List<GameObject>();

    [Header("Progress UI")]
    [Tooltip("Shows how many you've done, e.g. '3 / 10'.")]
    public TMP_Text progressText;
    [Tooltip("{0} = how many done, {1} = total needed.")]
    public string progressFormat = "{0} / {1}";

    [Header("Results Screen")]
    [Tooltip("Optional panel that pops up when the quiz ends. Leave empty and the grade shows on the Feedback text instead.")]
    public GameObject resultsPanel;
    [Tooltip("The TMP text inside the results panel that shows the grade.")]
    public TMP_Text resultsText;
    [Tooltip("Optional: parent object of the quiz UI, gets hidden while the results show.")]
    public GameObject quizPanel;
    [Tooltip("Advanced: load a dedicated results SCENE the instant the quiz ends, skipping the panel and the morning tea handoff entirely. That scene can read MathQuiz.LastResultText / LastGoldStars / LastPassed / LastHeartsLost.")]
    public bool loadResultsScene = false;
    public string resultsSceneName = "";

    [Header("Messages")]
    public string incorrectMessage = "INCORRECT";

    [Header("Typing Cursor")]
    public bool showCursor = true;
    public string cursorChar = "_";
    public float cursorBlinkRate = 0.5f;

    [Header("After Results")]
    [Tooltip("How long the grade stays on screen before moving on. 3 = three seconds.")]
    public float resultsDuration = 3f;
    [Tooltip("Load the scene matching MorningTeaManager.morningTeaNumber once the results are done. Happens on pass AND fail.")]
    public bool goToMorningTea = true;
    [Tooltip("Scene names, in order. Element 0 = morning tea 1, element 1 = morning tea 2, etc. All of these must be in File > Build Settings.")]
    public List<string> morningTeaScenes = new List<string>();
    [Tooltip("Fallback if Go To Morning Tea is off (or the scene name is missing): call ClassMinigameBridge.Finish() instead.")]
    public bool returnToClassWhenDone = true;

    [Header("Events (optional)")]
    [FormerlySerializedAs("onWin")]
    [Tooltip("Fires when the player passes (any grade that isn't the fail one).")]
    public UnityEvent onPass;
    [Tooltip("Fires when the player fails.")]
    public UnityEvent onFail;
    [Tooltip("Fires when the quiz ends, with how many gold stars were earned (0 on a fail). Hook your gold star counter up here.")]
    public UnityEvent<int> onGoldStarsAwarded;

    // Readable from anywhere afterwards (e.g. a results scene or your StaticManager).
    public static bool LastPassed;
    public static int LastHeartsLost;
    public static int LastGoldStars;
    public static string LastResultText = "";

    private readonly List<MathProblem> queue = new List<MathProblem>();
    private MathProblem current;
    private string typed = "";
    private int questionsDone = 0;
    private int hearts = 3;
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
        questionsDone = 0;
        hearts = startingHearts;
        typed = "";
        if (feedbackText != null) feedbackText.text = "";
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        queue.Clear();
        queue.AddRange(problems);
        if (randomizeOrder) Shuffle(queue);

        RefreshHearts();
        RefreshProgress();
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
        // Refill if we run out before finishing so it never dead-ends.
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
            questionsDone++;
            if (feedbackText != null) feedbackText.text = "";
            RefreshProgress();

            if (questionsDone >= problemsToComplete)
                FinishQuiz();
            else
                ShowNextProblem();   // instantly move on
        }
        else
        {
            hearts = Mathf.Max(0, hearts - heartsLostPerWrongAnswer);
            RefreshHearts();
            if (feedbackText != null) feedbackText.text = incorrectMessage;
            typed = "";              // wipe their wrong answer

            // Out of hearts -> instant fail screen.
            if (hearts <= 0 && failWhenOutOfHearts)
            {
                FinishQuiz();
                return;
            }

            if (advanceOnWrongAnswer)
            {
                questionsDone++;
                RefreshProgress();

                if (questionsDone >= problemsToComplete)
                    FinishQuiz();
                else
                    ShowNextProblem();
            }
            else
            {
                RefreshAnswer();     // retry the same problem
            }
        }
    }

    void FinishQuiz()
    {
        finished = true;
        typed = "";

        GradeResult result = PickGrade();
        bool passed = hearts > 0;

        // Stash the outcome somewhere any script/scene can read it.
        LastPassed = passed;
        LastHeartsLost = Mathf.Max(0, startingHearts - hearts);
        LastGoldStars = result.goldStars;
        LastResultText = result.screenText;

        if (passed) onPass?.Invoke();
        else onFail?.Invoke();
        onGoldStarsAwarded?.Invoke(result.goldStars);

        // Optional: skip everything and jump straight to a dedicated results scene.
        if (loadResultsScene && !string.IsNullOrEmpty(resultsSceneName))
        {
            SceneManager.LoadScene(resultsSceneName);
            return;
        }

        // Show the grade in this scene (results panel, or fall back to the feedback text).
        if (questionText != null) questionText.text = "";
        if (answerText != null) answerText.text = "";
        if (feedbackText != null) feedbackText.text = "";

        if (quizPanel != null) quizPanel.SetActive(false);

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            if (resultsText != null) resultsText.text = result.screenText;
        }
        else if (feedbackText != null)
        {
            feedbackText.text = result.screenText;
        }

        StartCoroutine(AfterResults());
    }

    GradeResult PickGrade()
    {
        if (hearts <= 0 || passGrades.Count == 0) return failGrade;
        int lost = Mathf.Clamp(startingHearts - hearts, 0, passGrades.Count - 1);
        return passGrades[lost];
    }

    // Waits out the results screen, then sends the player to morning tea.
    IEnumerator AfterResults()
    {
        yield return new WaitForSeconds(resultsDuration);

        if (goToMorningTea)
        {
            string scene = GetMorningTeaScene();
            if (!string.IsNullOrEmpty(scene))
            {
                SceneManager.LoadScene(scene);
                yield break;
            }

            Debug.LogWarning(
                $"[MathQuiz] No scene set for morning tea {MorningTeaManager.morningTeaNumber}. " +
                $"Add it to Morning Tea Scenes (element {MorningTeaManager.morningTeaNumber - 1}).", this);
        }

        if (returnToClassWhenDone)
            ClassMinigameBridge.Finish();
    }

    // morningTeaNumber 1 -> element 0, 2 -> element 1, etc.
    string GetMorningTeaScene()
    {
        int index = MorningTeaManager.morningTeaNumber - 1;
        if (index < 0 || index >= morningTeaScenes.Count) return null;
        return morningTeaScenes[index];
    }

    void RefreshHearts()
    {
        // Text hearts.
        if (heartsText != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < startingHearts; i++)
            {
                string h = (i < hearts) ? fullHeart : emptyHeart;
                if (string.IsNullOrEmpty(h)) continue;   // empty string = lost hearts vanish
                if (sb.Length > 0) sb.Append(heartSpacing);
                sb.Append(h);
            }
            heartsText.text = sb.ToString();
        }

        // Icon hearts.
        for (int i = 0; i < heartIcons.Count; i++)
        {
            if (heartIcons[i] != null)
                heartIcons[i].SetActive(i < hearts);
        }
    }

    void RefreshProgress()
    {
        if (progressText == null) return;
        try
        {
            progressText.text = string.Format(progressFormat, questionsDone, problemsToComplete);
        }
        catch
        {
            progressText.text = questionsDone + " / " + problemsToComplete;
        }
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