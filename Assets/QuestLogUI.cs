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
    public GameObject bookAnimation;    // Optional book opening animation GameObject
    public float bookDisplayTime = 1f;  // Time to show book before UI

    [Header("Paging")]
    public int maxLinesPerPage = 10;    // Lines per page for overflow

    private bool isOpen = false;
    private List<string> pages = new List<string>();
    private int currentPage = 0;
    private Coroutine openCoroutine;

    private void Start()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);

        if (bookAnimation != null)
            bookAnimation.SetActive(false);
    }

    private void Update()
    {
        // Toggle quest log with Q
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
                    if (bookAnimation != null)
                        bookAnimation.SetActive(false);
                }
            }
            else
            {
                // Open with book animation
                isOpen = true;
                if (openCoroutine != null) StopCoroutine(openCoroutine);
                openCoroutine = StartCoroutine(OpenBookSequence());
            }
        }

        if (!isOpen) return;

        // Scroll wheel paging
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) ShowPage(currentPage - 1); // scroll up = previous page
        else if (scroll < 0f) ShowPage(currentPage + 1); // scroll down = next page
    }

    /// <summary>
    /// Coroutine to show the book animation before displaying UI
    /// </summary>
    private IEnumerator OpenBookSequence()
    {
        if (bookAnimation != null)
            bookAnimation.SetActive(true);

        yield return new WaitForSeconds(bookDisplayTime);

        if (bookAnimation != null)
            bookAnimation.SetActive(false);

        if (uiPanel != null)
            uiPanel.SetActive(true);

        // Reset to first page
        currentPage = 0;
        BuildPages();
        ShowPage(currentPage);

        openCoroutine = null;
    }

    /// <summary>
    /// Public method so QuestActivator / ObjectiveCompleter can refresh UI
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

        // Split incomplete & completed
        var incomplete = activeQuests.Where(q => !q.IsQuestFullyComplete()).ToList();
        var completed = activeQuests.Where(q => q.IsQuestFullyComplete()).ToList();

        List<Quest> orderedQuests = new List<Quest>();
        orderedQuests.AddRange(incomplete);

        // Active quests may share pages
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

            // If adding this quest exceeds page limit, start new page
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

        // Completed quests: each on its own page, green + italic
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
