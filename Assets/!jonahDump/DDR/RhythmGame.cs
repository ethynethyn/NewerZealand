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

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
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
        int hitCount = 0;
        int strayPresses = 0;
        List<FallingNote> active = new List<FallingNote>();

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

            // Move notes; mark misses; clean up notes that fell past.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                FallingNote n = active[i];
                n.Tick(songTime);

                if (!n.judged && songTime > n.hitTime + hitWindow)
                    n.judged = true; // missed (only matters on the player's turn)

                if (n.judged && !n.hit && songTime > n.hitTime + missFallTime)
                {
                    Destroy(n.gameObject);
                    active.RemoveAt(i);
                }
            }

            // Player input.
            if (interactive)
            {
                foreach (Lane lane in lanes)
                {
                    if (lane == null) continue;
                    if (!Input.GetKeyDown(lane.key)) continue;

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
                        best.hit = true;
                        best.judged = true;
                        hitCount++;
                        active.Remove(best);
                        Destroy(best.gameObject);
                    }
                    else
                    {
                        strayPresses++;
                    }
                }
            }

            if (songTime >= endTime) break;
            yield return null;
        }

        // Clear anything still on screen.
        foreach (FallingNote n in active)
            if (n != null) Destroy(n.gameObject);
        active.Clear();

        if (source.isPlaying) source.Stop();

        // Passed only if every note was hit (and no stray presses, if you punish those).
        turnPassed = (hitCount == total) && (!punishStrayPresses || strayPresses == 0);
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