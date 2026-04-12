using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QuestLogUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject uiPanel;          // Panel background
    public TMP_Text questText;          // Single TMP Text element
    public GameObject bookAnimation;    // Book opening animation GameObject
    public float bookDisplayTime = 1f;  // Time to show book before UI

    [Header("Toggle Object")]
    public GameObject objectToToggle;   // Object to disable when log is open

    [Header("Paging")]
    public int maxLinesPerPage = 10;    // Lines per page for overflow

    private bool isOpen = false;        // true = quest log is open
    private List<string> pages = new List<string>();
    private int currentPage = 0;
    private Coroutine openCoroutine;

    private void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        if (bookAnimation != null) bookAnimation.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isOpen)
            {
                // Close instantly
                isOpen = false;

                if (uiPanel != null)
                    uiPanel.SetActive(false);

                if (openCoroutine != null)
                {
                    StopCoroutine(openCoroutine);
                    openCoroutine = null;
                }

                if (bookAnimation != null)
                    bookAnimation.SetActive(false);

                //  Re-enable object
                if (objectToToggle != null)
                    objectToToggle.SetActive(true);
            }
            else
            {
                // Open
                isOpen = true; // immediately set true so Update continues live

                //  Disable object
                if (objectToToggle != null)
                    objectToToggle.SetActive(false);

                if (openCoroutine != null)
                    StopCoroutine(openCoroutine);

                openCoroutine = StartCoroutine(OpenBookSequence());
            }
        }

        if (!isOpen) return;

        // Live update while open
        BuildPages();
        ShowPage(currentPage);

        // Scroll wheel paging
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) ShowPage(currentPage - 1);
        else if (scroll < 0f) ShowPage(currentPage + 1);
    }

    private IEnumerator OpenBookSequence()
    {
        // Activate book animation
        if (bookAnimation != null)
            bookAnimation.SetActive(true);

        // Wait for animation duration
        yield return new WaitForSeconds(bookDisplayTime);

        // Deactivate animation
        if (bookAnimation != null)
            bookAnimation.SetActive(false);

        // Show UI panel
        if (uiPanel != null)
            uiPanel.SetActive(true);

        openCoroutine = null;
    }

    /// <summary>
    /// External scripts call this to refresh quest list UI
    /// </summary>
    public void UpdateQuestList()
    {
        BuildPages();
        ShowPage(currentPage);
    }

    private void BuildPages()
    {
        pages.Clear();
        if (QuestManager.Instance == null) return;

        List<Quest> activeQuests = QuestManager.Instance.GetActiveQuests();

        var incomplete = activeQuests.Where(q => !q.IsQuestFullyComplete()).ToList();
        var completed = activeQuests.Where(q => q.IsQuestFullyComplete()).ToList();

        // --- INCOMPLETE QUESTS ---
        List<string> currentPageLines = new List<string>();
        int lineCount = 0;

        foreach (Quest q in incomplete)
        {
            List<string> questLines = new List<string>();

            string title = $"<color=yellow>{q.questTitle}</color>";
            questLines.Add(title);

            for (int i = 0; i < q.objectives.Length; i++)
            {
                string status = (q.completed != null && i < q.completed.Length && q.completed[i])
                    ? "<color=green>COMPLETE</color>"
                    : "<color=red>X</color>";

                questLines.Add($"   {status} {q.objectives[i]}");
            }

            questLines.Add("");

            if (lineCount + questLines.Count > maxLinesPerPage && currentPageLines.Count > 0)
            {
                pages.Add(string.Join("\n", currentPageLines));
                currentPageLines.Clear();
                lineCount = 0;
            }

            currentPageLines.AddRange(questLines);
            lineCount += questLines.Count;
        }

        if (currentPageLines.Count > 0)
            pages.Add(string.Join("\n", currentPageLines));

        // --- COMPLETED QUESTS ---
        foreach (Quest q in completed)
        {
            List<string> questLines = new List<string>();

            string title = $"<color=green><i>{q.questTitle} COMPLETE</i></color>";
            questLines.Add(title);

            for (int i = 0; i < q.objectives.Length; i++)
            {
                string line = $"<color=green><i>{q.objectives[i]}</i></color>";
                questLines.Add($"   {line}");
            }

            pages.Add(string.Join("\n", questLines));
        }

        if (pages.Count == 0)
            pages.Add("No active quests");

        currentPage = Mathf.Clamp(currentPage, 0, pages.Count - 1);
    }

    private void ShowPage(int pageIndex)
    {
        if (pages.Count == 0) return;

        currentPage = Mathf.Clamp(pageIndex, 0, pages.Count - 1);
        questText.text = pages[currentPage];
    }
}