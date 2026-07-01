using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChemistryMixGame : MonoBehaviour
{
    // ---------------------------------------------------------------- Types

    public enum LiquidColor { Red, Yellow, Blue }
    public enum MixResult { Orange, Green, Purple, Invalid }

    [System.Serializable]
    public class Bottle
    {
        public string label = "Bottle";
        public LiquidColor color = LiquidColor.Red;

        [Tooltip("The bottle's UI image/object.")]
        public GameObject bottleObject;

        [Tooltip("This bottle's unique pour sprite/effect.")]
        public GameObject pourObject;
    }

    // ------------------------------------------------------------ Inspector

    [Header("Bottles (assign left -> right initially)")]
    public Bottle[] bottles = new Bottle[3];

    [Header("Selection Arrows (left -> right)")]
    public GameObject[] arrowObjects = new GameObject[3];

    [Header("Pour Positions (left -> right)")]
    [Tooltip("Assign 3 transforms representing the Left, Middle and Right pour locations.")]
    public Transform[] pourPositions = new Transform[3];

    [Header("Beaker")]
    public Image beakerLiquid;
    public float pourDuration = 1.0f;
    public float resultHoldDuration = 0.6f;

    [Header("Shuffle")]
    public int shuffleSwaps = 10;
    public float swapDuration = 0.22f;
    public float shuffleArcHeight = 30f;

    [Header("Prompt & Win")]
    public TMP_Text promptText;
    public string promptPrefix = "Make ";
    public GameObject winScreen;
    public int tasksToWin = 5;

    [Header("Minigame Return")]
    [Tooltip("After winning, how long the win screen shows before returning to the class. 0 = return instantly.")]
    public float returnDelayAfterWin = 2f;

    [Header("Input")]
    public KeyCode confirmKey = KeyCode.Space;
    public KeyCode confirmKeyAlt = KeyCode.Return;

    [Header("Liquid Colors")]
    public Color redColor = new Color(0.86f, 0.15f, 0.15f);
    public Color yellowColor = new Color(0.98f, 0.85f, 0.10f);
    public Color blueColor = new Color(0.15f, 0.35f, 0.90f);

    public Color orangeColor = new Color(0.97f, 0.55f, 0.10f);
    public Color greenColor = new Color(0.20f, 0.70f, 0.25f);
    public Color purpleColor = new Color(0.55f, 0.20f, 0.70f);

    public Color invalidMixColor = new Color(0.45f, 0.36f, 0.30f);
    public Color emptyColor = new Color(1, 1, 1, 0);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pourSound;
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip swapSound;

    // -------------------------------------------------------------- Runtime

    enum GameState
    {
        Selecting,
        Busy,
        Finished
    }

    GameState state = GameState.Selecting;

    int selectedIndex = 0;
    int selectionStep = 0;

    LiquidColor firstColor;
    MixResult currentTarget;

    int tasksCompleted = 0;

    Vector3[] slotPositions;
    bool slotsCaptured = false;

    // ----------------------------------------------------------- Unity

    void Start()
    {
        if (winScreen != null)
            winScreen.SetActive(false);

        HideAllPours();

        StartNewAttempt(true);
    }

    void Update()
    {
        if (state != GameState.Selecting)
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            MoveSelection(-1);

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            MoveSelection(1);

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmKeyAlt))
            TryPourSelected();
    }

    // --------------------------------------------------------- Selection

    void MoveSelection(int dir)
    {
        int count = bottles.Length;

        selectedIndex =
            (selectedIndex + dir + count) % count;

        ShowSelectedArrow();
    }

    void ShowSelectedArrow()
    {
        for (int i = 0; i < arrowObjects.Length; i++)
        {
            if (arrowObjects[i] != null)
                arrowObjects[i].SetActive(i == selectedIndex);
        }
    }

    void HideAllArrows()
    {
        foreach (var a in arrowObjects)
        {
            if (a != null)
                a.SetActive(false);
        }
    }

    void HideAllPours()
    {
        foreach (var bottle in bottles)
        {
            if (bottle != null &&
                bottle.pourObject != null)
            {
                bottle.pourObject.SetActive(false);
            }
        }
    }

    void TryPourSelected()
    {
        StartCoroutine(PourRoutine(selectedIndex));
    }

    // ------------------------------------------------------------- Pouring

    IEnumerator PourRoutine(int index)
    {
        state = GameState.Busy;

        Bottle bottle = bottles[index];

        if (bottle.bottleObject != null)
            bottle.bottleObject.SetActive(false);

        HideAllArrows();

        int slotIndex = GetBottleSlot(bottle);

        if (bottle.pourObject != null &&
            slotIndex < pourPositions.Length &&
            pourPositions[slotIndex] != null)
        {
            Transform anchor = pourPositions[slotIndex];

            // Position
            bottle.pourObject.transform.position =
                anchor.position;

            // Rotation
            bottle.pourObject.transform.rotation =
                anchor.rotation;

            // Scale (optional but useful)
            bottle.pourObject.transform.localScale =
                anchor.lossyScale;

            bottle.pourObject.SetActive(true);
        }

        PlaySound(pourSound);

        yield return new WaitForSeconds(pourDuration);

        if (bottle.pourObject != null)
            bottle.pourObject.SetActive(false);

        if (bottle.bottleObject != null)
            bottle.bottleObject.SetActive(true);

        if (selectionStep == 0)
        {
            firstColor = bottle.color;

            SetBeaker(ColorFor(firstColor));

            selectionStep = 1;

            ShowSelectedArrow();

            state = GameState.Selecting;
        }
        else
        {
            MixResult result =
                Mix(firstColor, bottle.color);

            SetBeaker(ColorForResult(result));

            yield return new WaitForSeconds(resultHoldDuration);

            bool success =
                result == currentTarget;

            if (success)
            {
                PlaySound(successSound);

                tasksCompleted++;

                if (tasksCompleted >= tasksToWin)
                {
                    Win();
                    yield break;
                }
            }
            else
            {
                PlaySound(failSound);
            }

            SetBeaker(emptyColor);

            yield return ShuffleRoutine();

            StartNewAttempt(success);
        }
    }

    // ------------------------------------------------------------- Shuffle

    IEnumerator ShuffleRoutine()
    {
        state = GameState.Busy;

        CaptureSlotsIfNeeded();

        Bottle[] current =
            (Bottle[])bottles.Clone();

        for (int s = 0; s < shuffleSwaps; s++)
        {
            int a =
                Random.Range(0, bottles.Length);

            int b =
                Random.Range(0, bottles.Length);

            if (a == b)
                continue;

            yield return AnimateSwap(
                current[a].bottleObject.transform,
                current[b].bottleObject.transform,
                slotPositions[a],
                slotPositions[b]
            );

            Bottle temp = current[a];
            current[a] = current[b];
            current[b] = temp;

            PlaySound(swapSound);
        }

        bottles = current;
    }

    IEnumerator AnimateSwap(
        Transform a,
        Transform b,
        Vector3 posA,
        Vector3 posB)
    {
        float t = 0f;

        while (t < swapDuration)
        {
            t += Time.deltaTime;

            float k =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t / swapDuration);

            a.position =
                Vector3.Lerp(posA, posB, k)
                + Vector3.up
                * Mathf.Sin(k * Mathf.PI)
                * shuffleArcHeight;

            b.position =
                Vector3.Lerp(posB, posA, k);

            yield return null;
        }

        a.position = posB;
        b.position = posA;
    }

    void CaptureSlotsIfNeeded()
    {
        if (slotsCaptured)
            return;

        slotPositions =
            new Vector3[bottles.Length];

        for (int i = 0; i < bottles.Length; i++)
        {
            slotPositions[i] =
                bottles[i]
                .bottleObject
                .transform
                .position;
        }

        slotsCaptured = true;
    }

    // ------------------------------------------------------------- Round

    void StartNewAttempt(bool newTarget)
    {
        selectionStep = 0;

        selectedIndex = 0;

        HideAllPours();

        SetBeaker(emptyColor);

        if (newTarget)
            currentTarget = PickTarget();

        if (promptText != null)
            promptText.text =
                promptPrefix + currentTarget;

        ShowSelectedArrow();

        state = GameState.Selecting;
    }

    MixResult PickTarget()
    {
        return (MixResult)Random.Range(0, 3);
    }

    MixResult Mix(
        LiquidColor a,
        LiquidColor b)
    {
        if ((a == LiquidColor.Red &&
             b == LiquidColor.Yellow)
            ||
            (a == LiquidColor.Yellow &&
             b == LiquidColor.Red))
            return MixResult.Orange;

        if ((a == LiquidColor.Yellow &&
             b == LiquidColor.Blue)
            ||
            (a == LiquidColor.Blue &&
             b == LiquidColor.Yellow))
            return MixResult.Green;

        if ((a == LiquidColor.Red &&
             b == LiquidColor.Blue)
            ||
            (a == LiquidColor.Blue &&
             b == LiquidColor.Red))
            return MixResult.Purple;

        return MixResult.Invalid;
    }

    void Win()
    {
        state = GameState.Finished;

        if (winScreen != null)
            winScreen.SetActive(true);

        // Tell the class minigame system the player finished, after a beat so the
        // win screen is visible before we return to the class.
        StartCoroutine(ReturnToClassAfterWin());
    }

    IEnumerator ReturnToClassAfterWin()
    {
        yield return new WaitForSeconds(returnDelayAfterWin);
        ClassMinigameBridge.Finish();
    }

    // ------------------------------------------------------------- Helpers

    int GetBottleSlot(Bottle bottle)
    {
        for (int i = 0; i < bottles.Length; i++)
        {
            if (bottles[i] == bottle)
                return i;
        }

        return 0;
    }

    void SetBeaker(Color c)
    {
        if (beakerLiquid == null)
            return;

        beakerLiquid.color = c;
        beakerLiquid.enabled = c.a > 0;
    }

    Color ColorFor(LiquidColor c)
    {
        switch (c)
        {
            case LiquidColor.Red:
                return redColor;

            case LiquidColor.Yellow:
                return yellowColor;

            case LiquidColor.Blue:
                return blueColor;
        }

        return emptyColor;
    }

    Color ColorForResult(MixResult r)
    {
        switch (r)
        {
            case MixResult.Orange:
                return orangeColor;

            case MixResult.Green:
                return greenColor;

            case MixResult.Purple:
                return purpleColor;
        }

        return invalidMixColor;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null &&
            clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}