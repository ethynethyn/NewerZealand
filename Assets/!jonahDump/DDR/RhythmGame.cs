using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // If you get an error here, import TMP Essentials, or swap to UnityEngine.UI.Text

[RequireComponent(typeof(AudioSource))]
public class RhythmGame : MonoBehaviour
{
    [System.Serializable]
    public class Lane
    {
        public NoteDirection direction;
        public KeyCode key = KeyCode.UpArrow;
        [Tooltip("Empty object placed where notes should LAND (a receptor near the bottom).")]
        public Transform target;
        [Tooltip("Coloured sprite shown on the player's turn.")]
        public Sprite colorSprite;
        [Tooltip("Optional grey sprite for the showcase. If empty, the coloured sprite is just dimmed instead.")]
        public Sprite graySprite;

        // --- runtime only (filled in automatically) ---
        [System.NonSerialized] public SpriteRenderer targetRenderer; // the sprite on the target object
        [System.NonSerialized] public Sprite targetRestSprite;       // its normal (grey) look
        [System.NonSerialized] public Color targetRestColor;         // its normal colour
        [System.NonSerialized] public Vector3 targetRestScale;       // its normal size
        [System.NonSerialized] public float flashUntil;              // Time.time the colour flash ends
        [System.NonSerialized] public float popUntil;                // Time.time the hit pop ends
    }

    [Header("Lanes (one per arrow)")]
    public Lane[] lanes;

    [Header("Patterns")]
    [Tooltip("Drag in as many patterns as you like (e.g. 25).")]
    public List<PatternData> patterns = new List<PatternData>();
    [Tooltip("How many patterns you must clear to win.")]
    public int rounds = 4;
    [Tooltip("On = pick the round's patterns randomly from the list. Off = use the first ones in order.")]
    public bool randomizeOrder = true;
    [Tooltip("Allow the same pattern to appear more than once in one game.")]
    public bool allowRepeats = false;

    [Header("Note movement")]
    [Tooltip("How far above the target a note spawns (world units).")]
    public float fallDistance = 5f;
    [Tooltip("Seconds a note takes to fall from spawn to target.")]
    public float fallDuration = 1.2f;
    [Tooltip("How close (seconds) to the target a press must be to count as a hit.")]
    public float hitWindow = 0.15f;
    [Tooltip("Optional prefab for a note (needs a SpriteRenderer). Leave empty to auto-create plain ones.")]
    public GameObject notePrefab;
    [Tooltip("Dim colour used for the showcase when a lane has no grey sprite.")]
    public Color grayTint = new Color(0.55f, 0.55f, 0.55f, 1f);
    public int noteSortingOrder = 10;

    [Header("Targets")]
    [Tooltip("Targets are hidden during the showcase and only shown on the player's turn. When you press a key, that target briefly flashes its coloured sprite. This sets how long (seconds) the flash/pop lasts.")]
    public float targetFlashTime = 0.12f;
    [Tooltip("How big a target pops when you actually HIT a note. 1.3 = grows 30% then shrinks back.")]
    public float popScale = 1.3f;

    [Header("Sounds")]
    [Tooltip("Played when you miss a note (the buzzer). The turn fails instantly and restarts from the demo.")]
    public AudioClip missSound;

    [Header("Rules")]
    [Tooltip("On = pressing a key when no note is there also fails the round.")]
    public bool punishStrayPresses = false;
    [Tooltip("On a wrong answer, show the grey demo again before retrying.")]
    public bool showShowcaseOnRetry = true;
    [Tooltip("Seconds to wait on WRONG before replaying. It loops forever until you get it right.")]
    public float retryDelay = 1.5f;

    [Header("UI")]
    public TMP_Text statusText;
    public string startMessage = "Press Space to start";
    public string watchMessage = "Watch...";
    public string yourTurnMessage = "It's your turn!";
    public string niceMessage = "NICE";
    public string wrongMessage = "WRONG";
    public string winMessage = "YOU WIN";
    [Tooltip("How long 'It's your turn!' shows before the notes start.")]
    public float yourTurnDisplayTime = 1f;

    [Header("Start")]
    public bool autoStart = false;
    public KeyCode startKey = KeyCode.Space;

    private AudioSource source;
    private bool turnPassed;
    private const float endPadding = 0.5f;
    private const float missFallTime = 0.4f;

    // Auto-fills 4 lanes when you first add the component (or right-click > Reset).
    void Reset()
    {
        lanes = new Lane[]
        {
            new Lane { direction = NoteDirection.Left,  key = KeyCode.LeftArrow },
            new Lane { direction = NoteDirection.Down,  key = KeyCode.DownArrow },
            new Lane { direction = NoteDirection.Up,    key = KeyCode.UpArrow },
            new Lane { direction = NoteDirection.Right, key = KeyCode.RightArrow },
        };
    }

    private AudioSource sfxSource;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        // A second, separate source for sound effects, so the buzzer keeps playing
        // even when we stop the pattern audio on a miss.
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        CacheTargets();
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    // Find the sprite on each target object and remember its normal look, then hide them.
    void CacheTargets()
    {
        if (lanes == null) return;
        foreach (Lane lane in lanes)
        {
            if (lane == null || lane.target == null) continue;
            lane.targetRenderer = lane.target.GetComponentInChildren<SpriteRenderer>(true);
            if (lane.targetRenderer != null)
            {
                lane.targetRestSprite = lane.targetRenderer.sprite;
                lane.targetRestColor = lane.targetRenderer.color;
                lane.targetRestScale = lane.targetRenderer.transform.localScale;
            }
        }
        SetTargetsVisible(false);
    }

    // Show/hide the target sprites. They're only visible on the player's turn.
    void SetTargetsVisible(bool show)
    {
        if (lanes == null) return;
        foreach (Lane lane in lanes)
        {
            if (lane == null || lane.targetRenderer == null) continue;
            lane.targetRenderer.enabled = show;
            // Always reset to the normal grey look + size.
            lane.targetRenderer.sprite = lane.targetRestSprite;
            lane.targetRenderer.color = lane.targetRestColor;
            lane.targetRenderer.transform.localScale = lane.targetRestScale;
            lane.flashUntil = 0f;
            lane.popUntil = 0f;
        }
    }

    // Called every frame during the player's turn: handles the colour flash (on press)
    // and the grow-and-shrink pop (on a successful hit), returning to normal afterward.
    void UpdateTargetVisuals()
    {
        if (lanes == null) return;
        foreach (Lane lane in lanes)
        {
            if (lane == null || lane.targetRenderer == null || !lane.targetRenderer.enabled) continue;
            Transform t = lane.targetRenderer.transform;

            // Colour: coloured sprite while flashing, otherwise grey.
            if (Time.time < lane.flashUntil && lane.colorSprite != null)
                lane.targetRenderer.sprite = lane.colorSprite;
            else
                lane.targetRenderer.sprite = lane.targetRestSprite;

            // Size: pop up on a hit, then ease back to normal.
            if (Time.time < lane.popUntil && targetFlashTime > 0f)
            {
                float k = (lane.popUntil - Time.time) / targetFlashTime; // 1 -> 0
                t.localScale = lane.targetRestScale * (1f + (popScale - 1f) * k);
            }
            else
            {
                t.localScale = lane.targetRestScale;
            }
        }
    }

    void Start()
    {
        if (autoStart) StartGame();
        else StartCoroutine(WaitToStart());
    }

    IEnumerator WaitToStart()
    {
        SetStatus(startMessage);
        while (!Input.GetKeyDown(startKey)) yield return null;
        StartGame();
    }

    public void StartGame()
    {
        StopAllCoroutines();
        StartCoroutine(RunGame());
    }

    IEnumerator RunGame()
    {
        List<PatternData> queue = BuildQueue();
        if (queue.Count == 0)
        {
            SetStatus("No patterns assigned.");
            yield break;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            PatternData pattern = queue[i];
            bool passed = false;
            bool firstTry = true;

            // Loops forever on this pattern until the player clears it.
            while (!passed)
            {
                if (firstTry || showShowcaseOnRetry)
                {
                    SetStatus(watchMessage);
                    yield return RunPhase(pattern, false); // grey showcase, audio plays
                }
                firstTry = false;

                SetStatus(yourTurnMessage);
                yield return new WaitForSeconds(yourTurnDisplayTime);

                SetStatus("");
                yield return RunPhase(pattern, true);      // coloured, player hits, audio plays
                passed = turnPassed;

                if (!passed)
                {
                    SetStatus(wrongMessage);
                    yield return new WaitForSeconds(retryDelay);
                }
            }
        }

        SetStatus(winMessage);
    }

    // Runs one pass of a pattern. interactive=false is the grey showcase (watch only).
    // interactive=true is the player's turn (hit the arrows). Sets turnPassed.
    IEnumerator RunPhase(PatternData pattern, bool interactive)
    {
        List<NoteEvent> notes = new List<NoteEvent>(pattern.notes);
        notes.Sort((a, b) => a.time.CompareTo(b.time));
        int total = notes.Count;
        int spawnIndex = 0;
        bool failed = false;
        List<FallingNote> active = new List<FallingNote>();

        // Targets show only on the player's turn, hidden during the showcase.
        SetTargetsVisible(interactive);

        // Schedule audio to start after a lead-in so early notes have room to fall in.
        double dspStart = AudioSettings.dspTime + fallDuration;
        if (pattern.clip != null)
        {
            source.clip = pattern.clip;
            source.PlayScheduled(dspStart);
        }
        else
        {
            Debug.LogWarning("[RhythmGame] Pattern '" + pattern.name + "' has no clip; running silently.");
        }

        float lastNote = total > 0 ? notes[total - 1].time : 0f;
        float clipLen = pattern.clip != null ? pattern.clip.length : 0f;
        float endTime = Mathf.Max(lastNote + hitWindow + missFallTime, clipLen) + endPadding;

        while (true)
        {
            float songTime = (float)(AudioSettings.dspTime - dspStart);

            // Spawn notes as their fall-in time arrives.
            while (spawnIndex < total && songTime >= notes[spawnIndex].time - fallDuration)
            {
                SpawnNote(notes[spawnIndex], interactive, active);
                spawnIndex++;
            }

            // Move notes; detect a missed note (instant fail on the player's turn); clean up.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                FallingNote n = active[i];
                n.Tick(songTime);

                if (!n.judged && songTime > n.hitTime + hitWindow)
                {
                    n.judged = true; // its window has passed
                    if (interactive && !n.hit && !failed)
                    {
                        // A note slipped through — buzzer, and bail out back to the demo.
                        failed = true;
                        PlaySfx(missSound);
                    }
                }

                if (n.judged && !n.hit && songTime > n.hitTime + missFallTime)
                {
                    Destroy(n.gameObject);
                    active.RemoveAt(i);
                }
            }

            if (failed) break;

            // Player input.
            if (interactive)
            {
                foreach (Lane lane in lanes)
                {
                    if (lane == null) continue;
                    if (!Input.GetKeyDown(lane.key)) continue;

                    // Flash this target to its coloured sprite — but ONLY if it's active (visible).
                    if (lane.targetRenderer != null && lane.targetRenderer.enabled && lane.colorSprite != null)
                        lane.flashUntil = Time.time + targetFlashTime;

                    // Look for a note to hit in this lane.
                    FallingNote best = null;
                    float bestDelta = hitWindow + 1f;
                    foreach (FallingNote n in active)
                    {
                        if (n.judged || n.hit) continue;
                        if (n.direction != lane.direction) continue;
                        float d = Mathf.Abs(songTime - n.hitTime);
                        if (d <= hitWindow && d < bestDelta) { best = n; bestDelta = d; }
                    }

                    if (best != null)
                    {
                        // Good hit: pop the target, say NICE, remove the note.
                        best.hit = true;
                        best.judged = true;
                        active.Remove(best);
                        Destroy(best.gameObject);
                        if (lane.targetRenderer != null) lane.popUntil = Time.time + targetFlashTime;
                        SetStatus(niceMessage);
                    }
                    else if (punishStrayPresses && !failed)
                    {
                        // Optional: pressing with nothing there also fails.
                        failed = true;
                        PlaySfx(missSound);
                    }
                }

                UpdateTargetVisuals();
                if (failed) break;
            }

            if (songTime >= endTime) break;
            yield return null;
        }

        // Clear anything still on screen.
        foreach (FallingNote n in active)
            if (n != null) Destroy(n.gameObject);
        active.Clear();

        // Hide the targets again until the next turn.
        SetTargetsVisible(false);

        if (source.isPlaying) source.Stop();

        // Passed as long as nothing slipped through.
        turnPassed = !failed;
    }

    void SpawnNote(NoteEvent ev, bool interactive, List<FallingNote> active)
    {
        Lane lane = GetLane(ev.direction);
        if (lane == null || lane.target == null) return;

        Vector3 targetPos = lane.target.position;
        Vector3 spawnPos = targetPos + Vector3.up * fallDistance;

        GameObject go = notePrefab != null ? Instantiate(notePrefab) : new GameObject("Note");
        FallingNote note = go.GetComponent<FallingNote>();
        if (note == null) note = go.AddComponent<FallingNote>();

        Sprite sprite;
        Color color;
        if (interactive)
        {
            sprite = lane.colorSprite;
            color = Color.white;
        }
        else if (lane.graySprite != null)
        {
            sprite = lane.graySprite;
            color = Color.white;
        }
        else
        {
            sprite = lane.colorSprite;
            color = grayTint; // dim the coloured sprite when no grey art exists
        }

        note.Init(ev.direction, ev.time, spawnPos, targetPos, fallDuration, sprite, color, noteSortingOrder);
        active.Add(note);
    }

    Lane GetLane(NoteDirection dir)
    {
        foreach (Lane lane in lanes)
            if (lane != null && lane.direction == dir) return lane;
        return null;
    }

    List<PatternData> BuildQueue()
    {
        List<PatternData> pool = new List<PatternData>();
        foreach (PatternData p in patterns)
            if (p != null) pool.Add(p);

        List<PatternData> queue = new List<PatternData>();
        if (pool.Count == 0) return queue;

        if (randomizeOrder)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                PatternData tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }
        }

        if (allowRepeats)
        {
            for (int i = 0; i < rounds; i++)
                queue.Add(pool[Random.Range(0, pool.Count)]);
        }
        else
        {
            int count = Mathf.Min(rounds, pool.Count);
            for (int i = 0; i < count; i++)
                queue.Add(pool[i]);
        }
        return queue;
    }

    void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }
}