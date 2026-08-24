using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Undertale-style voice blips for the Dialogue Editor asset.
///
/// Drop this on the same GameObject as your ConversationManager (or anywhere in the scene),
/// point it at a DAudio_VoiceDatabase, and it will play a looping blip for each speaker while
/// their text scrolls out. Nothing in the Dialogue Editor plugin is modified or required to change.
/// </summary>
[DefaultExecutionOrder(1000)]
[AddComponentMenu("DAudio/Dialogue Voice")]
[RequireComponent(typeof(DAudio_BlipPlayer))]
public class DAudio_DialogueVoice : MonoBehaviour
{
    public static DAudio_DialogueVoice Instance { get; private set; }

    [Header("Voices")]
    [SerializeField] private DAudio_VoiceDatabase m_database;

    [Header("Behaviour")]
    [Tooltip("If the speech node has an Audio clip assigned in the Dialogue Editor window, use that " +
             "clip as the blip for that node instead of the profile's clips.")]
    [SerializeField] private bool m_useNodeAudioAsBlip = false;

    [Tooltip("Safety cap. Stops a burst of blips when text is revealed all at once (Scroll Text off).")]
    [SerializeField, Range(1, 8)] private int m_maxBlipsPerFrame = 1;

    [SerializeField] private bool m_dontDestroyOnLoad = false;

    [Header("Manual overrides (leave empty to auto-detect)")]
    [Tooltip("Only needed if auto-detection fails. Drag the Text / TextMeshProUGUI that shows the dialogue body.")]
    [SerializeField] private Component m_dialogueTextOverride;

    [Tooltip("Optional. The Text / TextMeshProUGUI that shows the speaker's name.")]
    [SerializeField] private Component m_nameTextOverride;

    [Header("Debug")]
    [Tooltip("Logs the speaker name and chosen profile every time a new node starts. Great for finding name typos.")]
    [SerializeField] private bool m_logSpeakerChanges = false;

    private DAudio_BlipPlayer m_player;
    private readonly DAudio_ConversationBridge m_bridge = new DAudio_ConversationBridge();
    private readonly Dictionary<string, DAudio_VoiceProfile> m_overrides = new Dictionary<string, DAudio_VoiceProfile>();

    private object m_lastNode;
    private string m_lastVisible = string.Empty;
    private int m_lastVisibleCount;
    private DAudio_VoiceProfile m_profile;
    private AudioClip m_forcedClip;
    private int m_charCounter;
    private float m_lastBlipTime;
    private bool m_warnedNoDatabase;

    public DAudio_VoiceDatabase Database
    {
        get { return m_database; }
        set { m_database = value; }
    }

    public DAudio_BlipPlayer Player
    {
        get { return m_player; }
    }

    // ------------------------------------------------------------------ lifecycle

    private void Awake()
    {
        Instance = this;
        m_player = GetComponent<DAudio_BlipPlayer>();

        if (m_dontDestroyOnLoad)
            DontDestroyOnLoad(transform.root.gameObject);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this)) Instance = null;
    }

    private void LateUpdate()
    {
        if (!m_bridge.Refresh(m_dialogueTextOverride, m_nameTextOverride)) { ResetState(); return; }
        if (!m_bridge.IsDialogueVisible()) { ResetState(); return; }

        string stripped = DAudio_TextUtils.StripRichText(m_bridge.GetRawDialogueText());
        int visibleCount = Mathf.Clamp(m_bridge.GetVisibleCount(stripped.Length), 0, stripped.Length);
        string visible = visibleCount == stripped.Length ? stripped : stripped.Substring(0, visibleCount);

        object node = m_bridge.GetCurrentNode();

        bool isNewNode =
            (node != null && !ReferenceEquals(node, m_lastNode)) ||
            visibleCount < m_lastVisibleCount ||
            (m_lastVisible.Length > 0 && !visible.StartsWith(m_lastVisible, StringComparison.Ordinal));

        if (isNewNode) BeginNode(node);

        if (m_profile != null && visible.Length > m_lastVisible.Length)
        {
            int blips = 0;
            int perBlip = Mathf.Max(1, m_profile.charactersPerBlip);

            for (int i = m_lastVisible.Length; i < visible.Length; i++)
            {
                if (!m_profile.CountsAsCharacter(visible[i])) continue;

                m_charCounter++;
                if (m_charCounter < perBlip) continue;

                m_charCounter = 0;
                blips++;
            }

            int toPlay = Mathf.Min(blips, Mathf.Max(1, m_maxBlipsPerFrame));
            for (int i = 0; i < toPlay; i++)
            {
                if (Time.unscaledTime - m_lastBlipTime < m_profile.minSecondsBetweenBlips) break;
                PlayBlip();
            }
        }

        m_lastVisible = visible;
        m_lastVisibleCount = visibleCount;
    }

    // ------------------------------------------------------------------ internals

    private void BeginNode(object node)
    {
        m_lastNode = node;
        m_lastVisible = string.Empty;
        m_lastVisibleCount = 0;
        m_charCounter = 0;
        m_forcedClip = null;

        string speaker = m_bridge.GetSpeakerName();
        m_profile = ResolveProfile(speaker);

        if (m_useNodeAudioAsBlip)
        {
            AudioClip nodeClip = m_bridge.GetNodeAudio();
            if (nodeClip != null) m_forcedClip = nodeClip;
        }

        if (m_logSpeakerChanges)
        {
            Debug.Log("[DAudio] Speaker: \"" + speaker + "\"  ->  " +
                      (m_profile != null ? m_profile.name : "NO PROFILE (silent)"), this);
        }
    }

    private DAudio_VoiceProfile ResolveProfile(string rawSpeaker)
    {
        string key = DAudio_TextUtils.NormaliseSpeaker(rawSpeaker);

        if (key.Length > 0)
        {
            DAudio_VoiceProfile overrideProfile;
            if (m_overrides.TryGetValue(key, out overrideProfile) && overrideProfile != null)
                return overrideProfile;
        }

        if (m_database == null)
        {
            if (!m_warnedNoDatabase)
            {
                m_warnedNoDatabase = true;
                Debug.LogWarning("[DAudio] No Voice Database assigned on " + name + ". No blips will play.", this);
            }
            return null;
        }

        return m_database.GetProfile(key);
    }

    private void PlayBlip()
    {
        AudioClip clip = m_forcedClip != null ? m_forcedClip : m_profile.NextClip();
        if (clip == null) return;

        m_player.Play(clip, m_profile.volume, m_profile.NextPitch(), m_profile.mixerGroup);
        m_lastBlipTime = Time.unscaledTime;
    }

    private void ResetState()
    {
        m_lastNode = null;
        m_lastVisible = string.Empty;
        m_lastVisibleCount = 0;
        m_charCounter = 0;
        m_profile = null;
        m_forcedClip = null;
    }

    // ------------------------------------------------------------------ runtime API

    /// <summary>Temporarily give a speaker a different voice (e.g. a character gets a cold, or a possessed variant).</summary>
    public void SetSpeakerOverride(string speakerName, DAudio_VoiceProfile profile)
    {
        string key = DAudio_TextUtils.NormaliseSpeaker(speakerName);
        if (key.Length == 0) return;

        m_overrides[key] = profile;
        m_lastNode = null; // force the current node to re-resolve
    }

    public void ClearSpeakerOverride(string speakerName)
    {
        string key = DAudio_TextUtils.NormaliseSpeaker(speakerName);
        if (key.Length == 0) return;

        m_overrides.Remove(key);
        m_lastNode = null;
    }

    public void ClearAllSpeakerOverrides()
    {
        m_overrides.Clear();
        m_lastNode = null;
    }

    /// <summary>Global volume for every voice. 0 mutes the whole system.</summary>
    public void SetMasterVolume(float volume)
    {
        if (m_player != null) m_player.MasterVolume = volume;
    }
}
