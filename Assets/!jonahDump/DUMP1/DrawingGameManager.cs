using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the mini-game: shows a reference to copy, waits until the player has
/// drawn enough, shows a "press enter to submit" prompt, then checks whether the
/// drawing vaguely resembles the reference. If it does, it advances; if not, it
/// wipes the drawing, shows a "not accurate" message, and stays on the same
/// reference. After the set number of accepted drawings it activates the end object.
/// </summary>
public class DrawingGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrawingCanvas drawingCanvas;
    [Tooltip("The Image on the SIDE that the player copies (not over the draw space).")]
    [SerializeField] private Image referenceImage;
    [Tooltip("One sprite per drawing. Cycles if there are fewer than 'Total Drawings'.")]
    [SerializeField] private Sprite[] referenceSprites;
    [Tooltip("GameObject holding your 'press enter to submit' TextMeshPro. Gets toggled on/off.")]
    [SerializeField] private GameObject submitPrompt;
    [Tooltip("GameObject holding your 'NOT ACCURATE ENOUGH' TextMeshPro. Gets toggled on/off.")]
    [SerializeField] private GameObject notAccurateObject;
    [Tooltip("Activated once all drawings are finished.")]
    [SerializeField] private GameObject gameEndObject;

    [Header("Flow")]
    [Tooltip("How much of the canvas must be painted before the player can submit.")]
    [SerializeField, Range(0f, 100f)] private float requiredCoverage = 15f;
    [SerializeField] private int totalDrawings = 3;
    [SerializeField] private KeyCode submitKey = KeyCode.Return;

    [Header("Minigame Return")]
    [Tooltip("After finishing all drawings, how long the end screen shows before returning to the class. 0 = return instantly.")]
    [SerializeField] private float returnDelayAfterWin = 2f;

    [Header("Resemblance Check")]
    [Tooltip("0..1. Higher = must match more closely. Keep LOW for 'very vaguely resembles'.")]
    [SerializeField, Range(0f, 1f)] private float similarityThreshold = 0.1f;
    [Tooltip("Comparison grid size. LOWER = more forgiving / vaguer.")]
    [SerializeField, Range(2, 32)] private int gridResolution = 8;
    [Tooltip("Fraction of a cell that must be filled for it to count as occupied.")]
    [SerializeField, Range(0f, 1f)] private float cellFillThreshold = 0.05f;
    [Tooltip("OPAQUE references only: how far from white a pixel must be to count as content. Ignored for transparent PNGs (those use whatever isn't transparent).")]
    [SerializeField, Range(0f, 1f)] private float referenceBackgroundTolerance = 0.25f;

    [Header("Colour Matching")]
    [Tooltip("How much colour affects the score. 0 = ignore colour (shape only). 1 = colour matters a lot.")]
    [SerializeField, Range(0f, 1f)] private float colorImportance = 0.5f;
    [Tooltip("How close a colour must be to count as 'similar'. Lower = stricter, higher = more forgiving.")]
    [SerializeField, Range(0f, 1f)] private float colorTolerance = 0.5f;

    [Header("Debug")]
    [Tooltip("Logs the resemblance score to the Console on each submit (handy for tuning).")]
    [SerializeField] private bool debugLogScore = true;

    private int currentDrawing;        // 0-based index of the drawing in progress
    private bool canSubmit;
    private CellData[,] currentRefGrid; // cached grid (occupancy + colour) of the current reference

    private void Start()
    {
        if (gameEndObject != null) gameEndObject.SetActive(false);
        if (submitPrompt != null) submitPrompt.SetActive(false);
        if (notAccurateObject != null) notAccurateObject.SetActive(false);
        ShowReference(0);
    }

    private void Update()
    {
        if (drawingCanvas == null) return;

        // Clear the "not accurate" message as soon as the player starts redrawing.
        if (notAccurateObject != null && notAccurateObject.activeSelf && drawingCanvas.CoveragePercent > 0f)
            notAccurateObject.SetActive(false);

        // Reveal the submit prompt once the player has drawn enough.
        if (!canSubmit && drawingCanvas.CoveragePercent >= requiredCoverage)
        {
            canSubmit = true;
            if (submitPrompt != null) submitPrompt.SetActive(true);
        }

        if (canSubmit && Input.GetKeyDown(submitKey))
            Submit();
    }

    private void Submit()
    {
        canSubmit = false;
        if (submitPrompt != null) submitPrompt.SetActive(false);

        if (!ResemblesReference())
        {
            // Doesn't resemble it: wipe the drawing, keep the SAME reference.
            drawingCanvas.ClearTexture();
            if (notAccurateObject != null) notAccurateObject.SetActive(true);
            return;
        }

        currentDrawing++;
        if (currentDrawing >= totalDrawings)
        {
            EndGame();
            return;
        }

        drawingCanvas.ClearTexture();
        ShowReference(currentDrawing);
    }

    private bool ResemblesReference()
    {
        if (currentRefGrid == null) return true; // nothing to compare against -> don't block

        CellData[,] drawnGrid = drawingCanvas.GetDrawnGrid(gridResolution, cellFillThreshold);
        float score = DrawingSimilarity.Similarity(drawnGrid, currentRefGrid, colorImportance, colorTolerance);

        if (debugLogScore)
            Debug.Log($"[DrawingGame] Resemblance {score:F3} (need {similarityThreshold:F2}) -> " +
                      (score >= similarityThreshold ? "PASS" : "FAIL"));

        return score >= similarityThreshold;
    }

    private void ShowReference(int index)
    {
        if (referenceImage == null || referenceSprites == null || referenceSprites.Length == 0) return;

        Sprite sprite = referenceSprites[index % referenceSprites.Length];
        referenceImage.sprite = sprite;

        currentRefGrid = DrawingSimilarity.GetReferenceGrid(sprite, gridResolution, cellFillThreshold, referenceBackgroundTolerance);
        if (DrawingSimilarity.IsEmpty(currentRefGrid))
            Debug.LogWarning($"[DrawingGame] No ink detected in reference '{sprite.name}'. " +
                             "Every drawing will be rejected. Check 'Read/Write Enabled' and the ink threshold.");
    }

    private void EndGame()
    {
        if (drawingCanvas != null) drawingCanvas.SetInteractable(false); // stop drawing on the end screen
        if (notAccurateObject != null) notAccurateObject.SetActive(false);
        if (gameEndObject != null) gameEndObject.SetActive(true);

        // Tell the class minigame system the player finished, after a beat so the
        // end screen is visible before we return to the class.
        StartCoroutine(ReturnToClassAfterWin());
    }

    private IEnumerator ReturnToClassAfterWin()
    {
        yield return new WaitForSeconds(returnDelayAfterWin);
        ClassMinigameBridge.Finish();
    }
}