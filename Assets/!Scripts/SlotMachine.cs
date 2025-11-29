using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SlotMachine : MonoBehaviour
{
    [System.Serializable]
    public class SlotOutcome
    {
        public string name = "Small Win";
        [Range(0f, 1f)] public float probability = 0.2f;
        public float payoutMultiplier = 1.2f;

        [Header("Outcome middle symbols")]
        public Sprite reel1Image;
        public Sprite reel2Image;
        public Sprite reel3Image;

        [Header("Outcome Sound")]
        public AudioClip soundEffect;
    }

    private const string MONEY_STAT = "Money";

    [Header("Stat System Reference")]
    public Character character;

    [Header("Slot Settings")]
    public float[] betOptions = { 5f, 25f, 100f };
    public float currentBet = 5f;
    public SlotOutcome[] outcomes;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI balanceText;

    [Header("UI Settings")]
    public bool fadeUI = true; // Toggle this in the Inspector


    [Header("Reels - assign 3 images per reel manually")]
    public Image[] reel1Symbols; // top, middle, bottom
    public Image[] reel2Symbols;
    public Image[] reel3Symbols;

    [Header("Spin Settings")]
    public float spinDuration = 2f;
    public float spinSpeed = 0.05f; // time between symbol shifts
    public Sprite[] possibleSymbols; // symbols that appear while spinning

    [Header("Reveal Settings")]
    public float middleRevealDelay = 0.1f; // delay between middle symbol reveals

    [Header("Handle")]
    public Animator handleAnimator; // assign the handle Animator
    public AudioClip handlePullSound;

    [Header("Audio")]
    public AudioClip spinningSound;
    public AudioSource audioSource;

    private bool isSpinning = false;
    private Coroutine fadeCoroutine;

    private float Balance
    {
        get => character != null ? character.GetStatValue(MONEY_STAT) : 0f;
        set
        {
            if (character == null) return;
            float current = character.GetStatValue(MONEY_STAT);
            character.ModifyStat(MONEY_STAT, value - current);
        }
    }

    public void SetBet(float amount)
    {
        currentBet = amount;
        UpdateUI("Bet set to $" + currentBet);
    }

    public void Spin()
    {
        if (isSpinning)
        {
            UpdateUI("Already spinning!");
            return;
        }

        if (Balance < currentBet)
        {
            UpdateUI("Not enough money!");
            return;
        }

        // Trigger handle animation and sound
        if (handleAnimator != null)
            handleAnimator.SetTrigger("Pull");

        if (handlePullSound != null)
            PlaySound(handlePullSound);

        Balance -= currentBet;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        UpdateUI("Spinning...");
        PlaySound(spinningSound);

        SlotOutcome chosen = DetermineOutcome();

        // Spin all reels simultaneously
        Coroutine spin1 = StartCoroutine(SpinReelVertical(reel1Symbols, spinDuration));
        Coroutine spin2 = StartCoroutine(SpinReelVertical(reel2Symbols, spinDuration));
        Coroutine spin3 = StartCoroutine(SpinReelVertical(reel3Symbols, spinDuration));

        yield return spin1;
        yield return spin2;
        yield return spin3;

        // Reveal middle symbols one by one with configurable delay
        if (chosen != null)
        {
            reel1Symbols[1].sprite = chosen.reel1Image;
            PlaySound(chosen.soundEffect);
            yield return new WaitForSeconds(middleRevealDelay);

            reel2Symbols[1].sprite = chosen.reel2Image;
            PlaySound(chosen.soundEffect);
            yield return new WaitForSeconds(middleRevealDelay);

            reel3Symbols[1].sprite = chosen.reel3Image;
            PlaySound(chosen.soundEffect);

            // Apply winnings
            float winnings = currentBet * chosen.payoutMultiplier;
            Balance += winnings;
            UpdateUI($"{chosen.name}! (+${winnings:0.00})");
        }
        else
        {
            UpdateUI("You lost!");
        }

        isSpinning = false;
    }

    private IEnumerator SpinReelVertical(Image[] symbols, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += spinSpeed;

            // Shift symbols down
            for (int i = symbols.Length - 1; i > 0; i--)
                symbols[i].sprite = symbols[i - 1].sprite;

            // Assign a random symbol to the top
            symbols[0].sprite = possibleSymbols[Random.Range(0, possibleSymbols.Length)];

            yield return new WaitForSeconds(spinSpeed);
        }
    }

    private SlotOutcome DetermineOutcome()
    {
        float roll = Random.value;
        float cumulative = 0f;

        foreach (var outcome in outcomes)
        {
            cumulative += outcome.probability;
            if (roll <= cumulative)
                return outcome;
        }

        return null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void UpdateUI(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            Color c = resultText.color;
            c.a = 1f;
            resultText.color = c;

            // Only fade if enabled
            if (fadeUI)
                fadeCoroutine = StartCoroutine(FadeOutResult());
        }

        if (balanceText != null)
            balanceText.text = $"Balance: ${Balance:0.00} | Bet: ${currentBet}";
    }


    private IEnumerator FadeOutResult()
    {
        yield return new WaitForSeconds(2f);

        float elapsed = 0f;
        float fadeDuration = 1f;
        Color c = resultText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            resultText.color = c;
            yield return null;
        }

        c.a = 0f;
        resultText.color = c;
    }
}
