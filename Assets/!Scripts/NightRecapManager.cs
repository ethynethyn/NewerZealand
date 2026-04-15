using UnityEngine;
using TMPro;

public class NightRecapManager : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Character playerCharacter;

    [Header("Money Stat Tracking")]
    [SerializeField] private string moneyStatForTracking = "Money"; // Should match your money stat name

    [Header("Operating Hours")]
    [SerializeField] private int startHour = 18; // 6 PM
    [SerializeField] private int endHour = 5; // 5 AM

    [Header("Stat Names")]
    [SerializeField] private string moneyStatName = "Money";
    [SerializeField] private string strikesStatName = "Strikes";

    [Header("UI Text Elements")]
    [SerializeField] private TextMeshProUGUI moneyEarnedText;
    [SerializeField] private TextMeshProUGUI moneySpentText;
    [SerializeField] private TextMeshProUGUI strikesText;
    [SerializeField] private TextMeshProUGUI takeHomePayText;

    [Header("Recap Canvas")]
    [SerializeField] private Canvas recapCanvas;

    [Header("Exit Settings")]
    [SerializeField] private GameObject objectToEnableOnExit;
    [SerializeField] private KeyCode exitKey = KeyCode.Space;

    private float startingMoney = 0f;
    private float startingStrikes = 0f;
    private float totalMoneyEarned = 0f;
    private float totalMoneySpent = 0f;
    private bool recapActive = false;
    private bool hasTrackedStartValues = false;

    void Start()
    {
        if (recapCanvas != null)
            recapCanvas.gameObject.SetActive(false);

        CaptureStartingValues();
    }

    void Update()
    {
        if (recapActive && Input.GetKeyDown(exitKey))
        {
            ExitRecap();
        }
    }

    void CaptureStartingValues()
    {
        if (playerCharacter != null && !hasTrackedStartValues)
        {
            startingMoney = playerCharacter.GetStatValue(moneyStatName);
            startingStrikes = playerCharacter.GetStatValue(strikesStatName);
            totalMoneyEarned = 0f;
            totalMoneySpent = 0f;
            hasTrackedStartValues = true;
            Debug.Log($"Captured starting values - Money: {startingMoney}, Strikes: {startingStrikes}");
        }
    }

    /// <summary>
    /// Track money earned (e.g., from selling beer)
    /// </summary>
    public void AddEarnings(float amount)
    {
        if (amount > 0)
        {
            totalMoneyEarned += amount;
            Debug.Log($"RECAP MANAGER: Earnings tracked: +${amount:F2} (Total earned: ${totalMoneyEarned:F2})");
        }
        else
        {
            Debug.LogWarning($"RECAP MANAGER: Invalid earnings amount: ${amount:F2}");
        }
    }

    /// <summary>
    /// Track money spent (e.g., buying stock)
    /// </summary>
    public void AddExpense(float amount)
    {
        if (amount > 0)
        {
            totalMoneySpent += amount;
            Debug.Log($"RECAP MANAGER: Expense tracked: -${amount:F2} (Total spent: ${totalMoneySpent:F2})");
        }
        else
        {
            Debug.LogWarning($"RECAP MANAGER: Invalid expense amount: ${amount:F2}");
        }
    }

    public void TriggerRecap()
    {
        Debug.Log("TriggerRecap called!");

        if (recapActive)
        {
            Debug.Log("Recap already active, returning");
            return;
        }

        recapActive = true;
        DisplayRecap();

        if (recapCanvas != null)
        {
            recapCanvas.gameObject.SetActive(true);
            Debug.Log("Canvas enabled!");
        }
        else
        {
            Debug.LogError("RecapCanvas is NULL!");
        }

        Debug.Log("Night Recap triggered!");
    }

    void DisplayRecap()
    {
        if (playerCharacter == null)
        {
            Debug.LogError("NightRecapManager: Player Character not assigned!");
            return;
        }

        float currentMoney = playerCharacter.GetStatValue(moneyStatName);
        float currentStrikes = playerCharacter.GetStatValue(strikesStatName);

        float takeHomePay = currentMoney - startingMoney;
        float strikesReceived = startingStrikes - currentStrikes;

        // Calculate spent as the difference between earned and take home
        float moneySpent = totalMoneyEarned - takeHomePay;

        if (moneyEarnedText != null)
            moneyEarnedText.text = $"${totalMoneyEarned:F2}";

        if (moneySpentText != null)
            moneySpentText.text = $"${moneySpent:F2}";

        if (strikesText != null)
            strikesText.text = $"{strikesReceived}";

        if (takeHomePayText != null)
            takeHomePayText.text = $"${takeHomePay:F2}";

        Debug.Log($"=== NIGHT RECAP ===\nMoney Earned: ${totalMoneyEarned:F2}\nMoney Spent: ${moneySpent:F2}\nStrikes Received: {strikesReceived}\nTake Home Pay: ${takeHomePay:F2}");
    }

    void ExitRecap()
    {
        recapActive = false;

        if (recapCanvas != null)
            recapCanvas.gameObject.SetActive(false);

        if (objectToEnableOnExit != null)
        {
            objectToEnableOnExit.SetActive(true);
            Debug.Log($"Enabled: {objectToEnableOnExit.name}");
        }
    }
}