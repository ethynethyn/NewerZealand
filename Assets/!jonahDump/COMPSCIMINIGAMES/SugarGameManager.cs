using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

// The brain of the Sugar, Sugar microgame.
// Pours sugar, tracks every cup, decides WIN / LOSE, drives the UI, and fires
// events you can hook your WarioWare framework into.
//
// WIN  = every cup reaches its target (can happen mid-pour).
// LOSE = the pour finished and the loose sugar has come to rest short of the goal,
//        OR (optional) the moment winning becomes mathematically impossible.
public class SugarGameManager : MonoBehaviour
{
    public static SugarGameManager Instance { get; private set; }

    [Header("── Sugar Amounts (all adjustable) ──")]
    [Min(0), Tooltip("Total grains that will pour out of the spawner this level.")]
    public int totalSugarToSpawn = 80;

    [Tooltip("Every cup in the level. Each cup has its own target.\nWIN = ALL cups reach their target.")]
    public List<SugarCup> cups = new List<SugarCup>();

    [Header("── Spawning ──")]
    [Tooltip("Empty GameObject at the top where the sugar pours from.")]
    public Transform spawnPoint;
    public GameObject grainPrefab;
    [Tooltip("Grains poured per second. Higher = faster pour.")]
    public float spawnRate = 40f;
    [Tooltip("Random horizontal spread so the stream isn't single-file (helps piling).")]
    public float spawnXJitter = 0.15f;
    [Tooltip("Optional tidy parent for spawned grains.")]
    public Transform grainContainer;

    [Header("── Sugar Physics / Speed ──")]
    [Tooltip("MAIN SPEED KNOB. Higher = faster-falling sugar = snappier game.")]
    public float grainGravityScale = 3f;
    public float grainMass = 0.05f;
    [Range(0f, 1f)] public float grainFriction = 0.2f;
    [Range(0f, 1f)] public float grainBounciness = 0f;
    [Tooltip("Optional extra downward speed at the instant each grain spawns.")]
    public float initialDownwardSpeed = 0f;
    [Tooltip("Stops fast grains tunnelling through thin lines. Leave this ON.")]
    public bool continuousCollision = true;

    [Header("── Win / Lose ──")]
    [Tooltip("Speed below which a grain is treated as 'settled'.")]
    public float settleVelocityThreshold = 0.05f;
    [Tooltip("How long all loose sugar must stay still before the level resolves.")]
    public float settleConfirmTime = 0.25f;
    [Tooltip("Call the loss the INSTANT a win becomes mathematically impossible (no dead time).")]
    public bool declareLossWhenImpossible = true;
    [Tooltip("Tiny pause before the result shows, so the last grain visibly lands.")]
    public float resultDelay = 0.1f;

    [Header("── UI (optional – drag TMP texts in) ──")]
    [Tooltip("Shows 'sugar left to come out' (grains still to pour).")]
    public TMP_Text sugarLeftText;
    public string sugarLeftFormat = "Sugar left: {0}";
    [Tooltip("Shows total in cups vs total needed to pass the level.")]
    public TMP_Text neededToPassText;
    public string neededToPassFormat = "In cups: {0} / {1}";
    [Tooltip("Shows YOU WIN / YOU LOSE. Hidden automatically until the level ends.")]
    public TMP_Text resultText;
    public string winMessage = "YOU WIN";
    public string loseMessage = "YOU LOSE";

    [Header("── Events (hook your WarioWare framework here) ──")]
    public UnityEvent onWin;
    public UnityEvent onLose;

    // ── runtime ──
    private int spawnedCount;
    private float spawnTimer;
    private float settleTimer;
    private bool gameOver;
    private readonly List<SugarGrain> aliveGrains = new List<SugarGrain>();
    private PhysicsMaterial2D grainPhysMat;

    public int RemainingToSpawn => Mathf.Max(0, totalSugarToSpawn - spawnedCount);
    public bool IsGameOver => gameOver;

    void Awake()
    {
        Instance = this;
        grainPhysMat = new PhysicsMaterial2D("SugarGrain")
        {
            friction = grainFriction,
            bounciness = grainBounciness
        };
        if (resultText != null) resultText.gameObject.SetActive(false);

        if (grainPrefab == null || spawnPoint == null)
            Debug.LogWarning("[SugarGameManager] Assign a grainPrefab and a spawnPoint or no sugar will pour.");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!gameOver) HandleSpawning();
        Evaluate();
    }

    // ───────────────────────────── Spawning ─────────────────────────────
    void HandleSpawning()
    {
        if (grainPrefab == null || spawnPoint == null) return;
        if (spawnedCount >= totalSugarToSpawn) return;

        float interval = spawnRate > 0f ? 1f / spawnRate : float.PositiveInfinity;
        spawnTimer -= Time.deltaTime;

        int safety = 0;
        while (spawnTimer <= 0f && spawnedCount < totalSugarToSpawn && safety < 2000)
        {
            SpawnGrain();
            spawnedCount++;
            spawnTimer += interval;
            safety++;
        }
    }

    void SpawnGrain()
    {
        Vector3 pos = spawnPoint.position + new Vector3(Random.Range(-spawnXJitter, spawnXJitter), 0f, 0f);
        GameObject go = Instantiate(grainPrefab, pos, Quaternion.identity, grainContainer);

        var grain = go.GetComponent<SugarGrain>();
        if (grain == null) grain = go.AddComponent<SugarGrain>();

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = grainGravityScale;
            rb.mass = grainMass;
            rb.collisionDetectionMode = continuousCollision
                ? CollisionDetectionMode2D.Continuous
                : CollisionDetectionMode2D.Discrete;
        }

        var col = go.GetComponent<Collider2D>();
        if (col != null) col.sharedMaterial = grainPhysMat;

        if (initialDownwardSpeed != 0f) grain.SetVelocity(new Vector2(0f, -initialDownwardSpeed));

        aliveGrains.Add(grain);
    }

    public void KillGrain(SugarGrain grain)
    {
        if (grain == null) return;
        aliveGrains.Remove(grain);
        Destroy(grain.gameObject);
    }

    // ──────────────────────────── Evaluation ────────────────────────────
    void Evaluate()
    {
        // Tally cups.
        int validCups = 0;
        bool anyUnfilled = false;
        int totalInCups = 0;
        int totalNeeded = 0;
        int totalDeficit = 0;
        foreach (var cup in cups)
        {
            if (cup == null) continue;
            validCups++;
            int c = cup.CurrentCount;
            totalInCups += c;
            totalNeeded += cup.target;
            if (c < cup.target) anyUnfilled = true;
            totalDeficit += Mathf.Max(0, cup.target - c);
        }
        bool allCupsFull = validCups > 0 && !anyUnfilled;

        // Tally loose (in-play) grains and whether they've all stopped.
        int inPlay = 0;
        bool allSettled = true;
        for (int i = aliveGrains.Count - 1; i >= 0; i--)
        {
            var g = aliveGrains[i];
            if (g == null) { aliveGrains.RemoveAt(i); continue; }
            if (g.currentCup != null) continue; // already counted by a cup
            inPlay++;
            if (!g.IsSettled(settleVelocityThreshold)) allSettled = false;
        }

        // UI.
        if (sugarLeftText != null)
            sugarLeftText.text = string.Format(sugarLeftFormat, RemainingToSpawn);
        if (neededToPassText != null)
            neededToPassText.text = string.Format(neededToPassFormat, totalInCups, totalNeeded);

        if (gameOver) return;

        // WIN — every cup is full.
        if (allCupsFull) { EndGame(true); return; }

        // The most sugar that could still possibly reach a cup.
        int stillComing = RemainingToSpawn + inPlay;

        // LOSE — nothing left in play and the cups aren't full.
        if (RemainingToSpawn == 0 && inPlay == 0) { EndGame(false); return; }

        // LOSE — even perfect placement of every remaining grain can't cover the deficit.
        // (Loose grains are counted optimistically, so this only fires when it's truly hopeless.)
        if (declareLossWhenImpossible && stillComing < totalDeficit) { EndGame(false); return; }

        // LOSE — pour finished and all the loose sugar has come to rest short of the goal.
        if (RemainingToSpawn == 0 && inPlay > 0 && allSettled)
        {
            settleTimer += Time.deltaTime;
            if (settleTimer >= settleConfirmTime) EndGame(false);
        }
        else
        {
            settleTimer = 0f;
        }
    }

    void EndGame(bool win)
    {
        if (gameOver) return;
        gameOver = true;
        StartCoroutine(ShowResult(win));
    }

    IEnumerator ShowResult(bool win)
    {
        if (resultDelay > 0f) yield return new WaitForSeconds(resultDelay);

        if (resultText != null)
        {
            resultText.text = win ? winMessage : loseMessage;
            resultText.gameObject.SetActive(true);
        }

        if (win) onWin?.Invoke();
        else onLose?.Invoke();
    }
}
