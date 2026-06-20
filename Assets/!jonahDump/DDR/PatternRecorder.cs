using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Add this to an empty GameObject. In Play mode, tick "Record", and tap your
// arrow keys in time with the sound. When the sound finishes it auto-saves a
// PatternData asset you can drag into RhythmGame's Patterns list.
[RequireComponent(typeof(AudioSource))]
public class PatternRecorder : MonoBehaviour
{
    [Header("Sound to record against")]
    public AudioClip clip;

    [Header("Keys (keep these identical in RhythmGame)")]
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    [Header("Controls — tick these in Play mode")]
    [Tooltip("Tick to start. Clears the last take, plays the sound, records your presses, and auto-saves when the sound ends.")]
    public bool record = false;
    [Tooltip("Tick to stop and save before the sound ends.")]
    public bool stopAndSave = false;

    [Header("Where to save")]
    [Tooltip("If set, overwrite this asset's notes. If empty, a NEW asset is created.")]
    public PatternData overwriteTarget;
    [Tooltip("Folder under Assets/ for new patterns.")]
    public string saveFolder = "Patterns";
    [Tooltip("File name for a newly created pattern.")]
    public string newPatternName = "NewPattern";

    private AudioSource source;
    private bool isRecording;
    private double dspStart;
    private readonly List<NoteEvent> take = new List<NoteEvent>();

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Update()
    {
        if (record && !isRecording) StartRecording();
        if (!isRecording) return;

        if (stopAndSave) { Finish(); return; }

        float t = (float)(AudioSettings.dspTime - dspStart);

        // t < 0 is the tiny lead-in before the sound actually starts.
        if (t >= 0f)
        {
            if (Input.GetKeyDown(leftKey))  take.Add(new NoteEvent(NoteDirection.Left,  t));
            if (Input.GetKeyDown(downKey))  take.Add(new NoteEvent(NoteDirection.Down,  t));
            if (Input.GetKeyDown(upKey))    take.Add(new NoteEvent(NoteDirection.Up,    t));
            if (Input.GetKeyDown(rightKey)) take.Add(new NoteEvent(NoteDirection.Right, t));
        }

        if (clip != null && t >= clip.length) Finish();
    }

    void StartRecording()
    {
        if (clip == null)
        {
            Debug.LogError("[PatternRecorder] Assign a Clip first.");
            record = false;
            return;
        }
        take.Clear();
        isRecording = true;
        stopAndSave = false;
        source.clip = clip;
        dspStart = AudioSettings.dspTime + 0.1; // small lead-in so timing is exact
        source.PlayScheduled(dspStart);
        Debug.Log("[PatternRecorder] Recording — tap your arrows in time with the sound.");
    }

    void Finish()
    {
        isRecording = false;
        record = false;
        stopAndSave = false;
        if (source.isPlaying) source.Stop();
        take.Sort((a, b) => a.time.CompareTo(b.time));
        Debug.Log("[PatternRecorder] Captured " + take.Count + " notes.");
        Save();
    }

    void Save()
    {
#if UNITY_EDITOR
        PatternData asset = overwriteTarget;
        if (asset == null)
        {
            string dir = "Assets/" + saveFolder;
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", saveFolder);
            asset = ScriptableObject.CreateInstance<PatternData>();
            string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + newPatternName + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("[PatternRecorder] Created " + path);
        }
        asset.clip = clip;
        asset.notes = new List<NoteEvent>(take);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PatternRecorder] Saved. Drag this asset into RhythmGame's Patterns list.");
#else
        Debug.LogWarning("[PatternRecorder] Saving only works inside the Unity Editor.");
#endif
    }
}
