using UnityEngine;
using TMPro;
using System.Collections;

public class RouletteMachine : MonoBehaviour
{
    private const string MONEY_STAT = "Money";

    [Header("Stat System Reference")]
    public Character character;

    [Header("Bet Settings")]
    public float currentBet = 5f;
    public float minBet = 5f;
    public float maxBet = 200f;
    public float betStep = 5f;

    public enum BetType { Red, Black, Green }
    public BetType currentBetType = BetType.Red;

    [Header("Red/Black/Green Layout")]
    public int[] redNumbers;
    public int[] blackNumbers;
    public int[] greenNumbers;

    [Header("Wheel Visual")]
    public Transform wheel;
    public Transform ball;

    [Header("Spin Settings")]
    public float wheelSpinSpeed = 200f;       // Wheel degrees per second
    public float ballSpinSpeed = 720f;        // Ball degrees per second while spinning
    public float landingDuration = 1f;        // Time for ball to settle on pocket

    [Header("Pocket Positions (Assign in Inspector)")]
    public Transform[] pocketPositions;       // Exact transforms of pockets
    public int[] wheelNumberOrder;            // Numbers in same order as pockets

    [Header("UI")]
    public TextMeshProUGUI betAndResultText;

    private bool isSpinning = false;
    private bool showBetInfo = true; // Controls whether to display bet info

    private float Balance
    {
        get => character != null ? character.GetStatValue(MONEY_STAT) : 0f;
        set
        {
            if (character == null) return;
            float diff = value - Balance;
            character.ModifyStat(MONEY_STAT, diff);
        }
    }

    private void Start()
    {
        UpdateBetUI("Place your bet!");
    }

    // ------------ INTERACTIONS ---------------
    public void RaiseBet()
    {
        currentBet = Mathf.Clamp(currentBet + betStep, minBet, maxBet);
        showBetInfo = true;
        UpdateBetUI("Bet raised!");
    }

    public void LowerBet()
    {
        currentBet = Mathf.Clamp(currentBet - betStep, minBet, maxBet);
        showBetInfo = true;
        UpdateBetUI("Bet lowered!");
    }

    public void SetBetTypeRed() => SetBetType(BetType.Red);
    public void SetBetTypeBlack() => SetBetType(BetType.Black);
    public void SetBetTypeGreen() => SetBetType(BetType.Green);

    private void SetBetType(BetType type)
    {
        currentBetType = type;
        showBetInfo = true;
        UpdateBetUI($"");
    }

    private bool CanSpin()
    {
        if (Balance < currentBet)
        {
            UpdateBetUI("Not enough money!");
            return false;
        }
        return true;
    }

    // ------------ SPIN ---------------
    public void Spin()
    {
        if (!CanSpin() || isSpinning) return;

        Balance -= currentBet;
        isSpinning = true;

        // During spin, hide bet info
        showBetInfo = false;
        UpdateBetUI("Spinning...");

        int resultIndex = Random.Range(0, wheelNumberOrder.Length);
        StartCoroutine(SpinRoutine(resultIndex));
    }

    IEnumerator SpinRoutine(int resultIndex)
    {
        float spinTime = 5f; // Ball rolling before final landing
        float elapsed = 0f;

        // Pick a random starting pocket for ball
        Transform startPocket = pocketPositions[Random.Range(0, pocketPositions.Length)];
        ball.position = startPocket.position;
        ball.LookAt(wheel.position + Vector3.up * 0.1f);

        // --- Main spin: rotate wheel + move ball around wheel ---
        while (elapsed < spinTime)
        {
            float deltaTime = Time.deltaTime;

            // Rotate wheel flat on table (Z-axis)
            wheel.Rotate(Vector3.forward, wheelSpinSpeed * deltaTime, Space.Self);

            // Rotate ball around wheel center (Vector3.up for rim)
            ball.RotateAround(wheel.position, Vector3.up, ballSpinSpeed * deltaTime);

            elapsed += deltaTime;
            yield return null;
        }

        // --- Smooth final landing on target pocket ---
        Transform targetPocket = pocketPositions[resultIndex];
        elapsed = 0f;
        Vector3 startPos = ball.position;
        Quaternion startRot = ball.rotation;

        while (elapsed < landingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / landingDuration);

            ball.position = Vector3.Lerp(startPos, targetPocket.position, t);
            ball.rotation = Quaternion.Slerp(startRot, targetPocket.rotation, t);

            // Keep wheel spinning
            wheel.Rotate(Vector3.forward, wheelSpinSpeed * Time.deltaTime, Space.Self);

            yield return null;
        }

        // Snap final position
        ball.position = targetPocket.position;
        ball.rotation = targetPocket.rotation;

        // Resolve result
        ResolveResult(wheelNumberOrder[resultIndex]);
        isSpinning = false;
    }

    // ------------ RESULT RESOLUTION ---------------
    void ResolveResult(int number)
    {
        bool won = false;
        float payout = 0f;

        switch (currentBetType)
        {
            case BetType.Red:
                if (System.Array.Exists(redNumbers, n => n == number))
                {
                    won = true;
                    payout = currentBet * 2f;
                }
                break;
            case BetType.Black:
                if (System.Array.Exists(blackNumbers, n => n == number))
                {
                    won = true;
                    payout = currentBet * 2f;
                }
                break;
            case BetType.Green:
                if (System.Array.Exists(greenNumbers, n => n == number))
                {
                    won = true;
                    payout = currentBet * 14f;
                }
                break;
        }

        if (won)
            Balance += payout;

        // After spin, only show result (bet info remains hidden)
        showBetInfo = false;
        UpdateBetUI(won ? $"WIN! Number {number} (+${payout})" : $"LOSS! Number {number}");
    }

    void UpdateBetUI(string message)
    {
        if (betAndResultText != null)
        {
            if (showBetInfo)
                betAndResultText.text = $"Bet: ${currentBet} ({currentBetType})\n{message}";
            else
                betAndResultText.text = message; // Only show result / spinning
        }
    }
}
