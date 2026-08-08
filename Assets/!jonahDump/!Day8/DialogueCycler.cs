using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using DialogueEditor;

/// <summary>
/// Undertale-style "the dialogue changes every time you talk to them" for Dialogue Editor,
/// WITHOUT touching any existing scripts. ConversationStarter still works exactly as before.
///
/// ONE-TIME SETUP:
///  1. In the Dialogue Editor window, make this NPC's conversation a STRAIGHT CHAIN of
///     speech nodes: root -> speech -> speech -> speech... The number of nodes = the most
///     text boxes any single interaction will ever need (4-6 is usually plenty).
///     The text you type into the nodes doesn't matter, it gets replaced at runtime.
///  2. Add this component next to the ConversationStarter (it auto-finds the
///     NPCConversation on the same object, or drag one in).
///  3. Fill in Entries (each element = one interaction, each LINE = one text box),
///     OR drop a .txt into Lines File (blank line = next interaction).
///
/// Per-node stuff you set in the editor (icon, font, audio, auto-advance, node events)
/// keeps working as the default - but chain nodes get re-used every interaction, so anything
/// that should only happen on ONE specific interaction goes in that Entry instead
/// (name / icon / audio / volume / font / sprite swap / functions).
/// </summary>
public class DialogueCycler : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [TextArea(2, 8)]
        [Tooltip("One interaction. Each line becomes its own text box. (Ignored if a Lines File is assigned - then this entry only supplies the extras below.)")]
        public string text;

        [Tooltip("Optional. Overrides the speaker name for this interaction.")]
        public string nameOverride;

        [Header("Dialogue-Editor-style overrides (leave empty = keep what the nodes have)")]
        [Tooltip("Optional. Portrait/icon shown during this interaction.")]
        public Sprite icon;
        [Tooltip("Optional. Audio played on each text box of this interaction (talk sound / voice blip).")]
        public AudioClip audio;
        [Tooltip("Optional. TMP font for this interaction's text.")]
        public TMPro.TMP_FontAsset font;
        [Tooltip("Tick this to override the audio volume for this interaction.")]
        public bool overrideVolume;
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Optional. This animator jumps to the state below when this interaction starts (for billboard sprite swaps).")]
        public Animator animator;
        [Tooltip("Animator state name to Play, i.e. which sprite/expression to show.")]
        public string animatorStateName;

        [Tooltip("Optional functions fired when this interaction starts.")]
        public UnityEvent onShown;
    }

    [Header("Conversation (the straight chain you made in Dialogue Editor)")]
    public NPCConversation conversation;

    [Header("Interactions - element = one interaction, line = one text box")]
    public List<Entry> entries = new List<Entry>();

    [Header("Optional bulk text file - blank line = next interaction")]
    [Tooltip("If assigned, this file supplies ALL the text. Entries above still add name/sprite/function extras by matching index (entry 0 = first block in the file, etc).")]
    public TextAsset linesFile;

    [Header("Optional prefix stuck on every text box, e.g. \"* \"")]
    public string boxPrefix = "";

    public enum EndBehaviour { RepeatLast, LoopBackToStart }
    [Header("After the last interaction has been seen")]
    public EndBehaviour whenOutOfLines = EndBehaviour.RepeatLast;

    [Header("Remember progress across scene loads (this play session)")]
    public bool persistAcrossScenes = false;
    [Tooltip("Unique id for this NPC. Blank = GameObject name.")]
    public string persistKey = "";

    /// <summary> Which interaction plays NEXT time the player talks to them (0-based). </summary>
    public int CurrentIndex { get { return _index; } }

    private static readonly Dictionary<string, int> s_progress = new Dictionary<string, int>();

    private EditableConversation _pristine;
    private List<string[]> _pages = new List<string[]>();
    private UnityEvent _rootEvent;
    private int _index;

    // Icon/audio/font are Unity assets so they DON'T live in the conversation's json -
    // Dialogue Editor stores them on hidden NodeEventHolder components (the
    // "ConversationEventInfo" child). We snapshot the originals so overrides can be
    // undone before every interaction.
    private class HolderOriginal
    {
        public NodeEventHolder holder;
        public Sprite icon;
        public AudioClip audio;
        public TMPro.TMP_FontAsset font;
        public float volume;
    }
    private readonly List<HolderOriginal> _holderOriginals = new List<HolderOriginal>();

    private static bool s_holderFieldsResolved;
    private static FieldInfo s_hIcon, s_hAudio, s_hFont, s_hVolume;

    // -------------------------------------------------- setup

    void Awake()
    {
        if (conversation == null) conversation = GetComponent<NPCConversation>();
        if (conversation == null)
        {
            Debug.LogError("[DialogueCycler] " + gameObject.name + ": no NPCConversation assigned or found.");
            enabled = false;
            return;
        }

        // Pull the authored conversation out ONCE and keep this copy pristine forever.
        _pristine = conversation.DeserializeForEditor();
        if (_pristine == null || _pristine.SpeechNodes == null || _pristine.SpeechNodes.Count == 0)
        {
            Debug.LogError("[DialogueCycler] " + gameObject.name + ": conversation has no speech nodes.");
            enabled = false;
            return;
        }
        if (_pristine.Options == null) _pristine.Options = new List<EditableOptionNode>();
        if (_pristine.Options.Count > 0)
            Debug.LogWarning("[DialogueCycler] " + gameObject.name + ": this conversation has option nodes. The cycler only follows the straight speech chain - keep this NPC's conversation branch-free.");
        if (conversation.ParameterList == null) conversation.ParameterList = new List<EditableParameter>();

        ParsePages();

        if (persistAcrossScenes)
        {
            if (string.IsNullOrEmpty(persistKey)) persistKey = gameObject.name;
            s_progress.TryGetValue(persistKey, out _index);
        }
        _index = Mathf.Clamp(_index, 0, LastIndex);

        // The root node's event fires every time this conversation starts -
        // that's how we know the player just talked to THIS npc.
        EditableSpeechNode root = FindRoot(_pristine);
        if (root == null)
        {
            Debug.LogError("[DialogueCycler] " + gameObject.name + ": couldn't find a root node.");
            enabled = false;
            return;
        }
        _rootEvent = conversation.GetNodeData(root.ID).Event;
        _rootEvent.AddListener(OnConversationStarted);

        SnapshotHolders();

        Bake(_index);
    }

    void OnDestroy()
    {
        if (_rootEvent != null) _rootEvent.RemoveListener(OnConversationStarted);
    }

    // -------------------------------------------------- interaction flow

    private void OnConversationStarted()
    {
        Entry extras = GetExtras(_index);
        if (extras != null)
        {
            if (extras.animator != null && !string.IsNullOrEmpty(extras.animatorStateName))
                extras.animator.Play(extras.animatorStateName, 0, 0f);
            if (extras.onShown != null) extras.onShown.Invoke();
        }

        // Queue up the NEXT interaction. Safe to do mid-conversation because the
        // ConversationManager already took its own copy when the conversation started.
        if (_index < LastIndex) _index++;
        else if (whenOutOfLines == EndBehaviour.LoopBackToStart) _index = 0;

        if (persistAcrossScenes) s_progress[persistKey] = _index;
        Bake(_index);
    }

    /// <summary> Jump to a specific interaction (0-based). Hook this up to UnityEvents / your StaticManager stuff if story flags should skip ahead. </summary>
    public void JumpTo(int interactionIndex)
    {
        _index = Mathf.Clamp(interactionIndex, 0, LastIndex);
        if (persistAcrossScenes && !string.IsNullOrEmpty(persistKey)) s_progress[persistKey] = _index;
        Bake(_index);
    }

    public void ResetToStart() { JumpTo(0); }

    private int LastIndex { get { return Mathf.Max(0, _pages.Count - 1); } }

    private Entry GetExtras(int i)
    {
        return (i >= 0 && i < entries.Count) ? entries[i] : null;
    }

    // -------------------------------------------------- text parsing

    private void ParsePages()
    {
        _pages.Clear();

        if (linesFile != null)
        {
            List<string> block = new List<string>();
            string[] raw = linesFile.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < raw.Length; i++)
            {
                string line = raw[i].Trim();
                if (line.Length == 0)
                {
                    if (block.Count > 0) { _pages.Add(block.ToArray()); block.Clear(); }
                }
                else
                {
                    block.Add(line);
                }
            }
            if (block.Count > 0) _pages.Add(block.ToArray());
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
                _pages.Add(SplitPages(entries[i].text));
        }

        if (_pages.Count == 0)
            Debug.LogWarning("[DialogueCycler] " + gameObject.name + ": no dialogue entries set up.");
    }

    private string[] SplitPages(string text)
    {
        List<string> pages = new List<string>();
        if (!string.IsNullOrEmpty(text))
        {
            string[] raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < raw.Length; i++)
            {
                string line = raw[i].Trim();
                if (line.Length > 0) pages.Add(line);
            }
        }
        if (pages.Count == 0) pages.Add("...");
        return pages.ToArray();
    }

    // -------------------------------------------------- rewriting the conversation

    private void Bake(int interactionIndex)
    {
        if (_pristine == null) return;

        string[] pages = (_pages.Count > 0)
            ? _pages[Mathf.Clamp(interactionIndex, 0, _pages.Count - 1)]
            : new string[] { "..." };

        // 1) put the untouched original back   2) pull out a fresh copy we're allowed to vandalise
        conversation.Serialize(_pristine);
        EditableConversation ec = conversation.DeserializeForEditor();
        if (ec == null) return;
        if (ec.Options == null) ec.Options = new List<EditableOptionNode>();
        if (conversation.ParameterList == null) conversation.ParameterList = new List<EditableParameter>();

        List<EditableSpeechNode> chain = GetChain(ec);
        if (chain.Count == 0)
        {
            Debug.LogError("[DialogueCycler] " + gameObject.name + ": couldn't walk the speech chain.");
            return;
        }

        if (pages.Length > chain.Count)
            Debug.LogWarning("[DialogueCycler] " + gameObject.name + ": interaction " + interactionIndex +
                " wants " + pages.Length + " boxes but the chain only has " + chain.Count +
                " nodes - extra lines got squished into the last box. Add more chained nodes in Dialogue Editor.");

        int used = Mathf.Min(pages.Length, chain.Count);
        Entry extras = GetExtras(interactionIndex);

        // Put every node's ORIGINAL icon/audio/font/volume back first, so overrides
        // from the previous interaction don't stick around.
        RestoreHolders();

        for (int i = 0; i < used; i++)
        {
            string text;
            if (i == used - 1 && pages.Length > used)
                text = boxPrefix + string.Join("\n" + boxPrefix, pages, i, pages.Length - i);
            else
                text = boxPrefix + pages[i];

            chain[i].Text = text;

            if (extras != null && !string.IsNullOrEmpty(extras.nameOverride))
                chain[i].Name = extras.nameOverride;
        }

        // Dialogue-Editor-style overrides for this interaction.
        if (extras != null)
        {
            if (extras.overrideVolume)
                for (int i = 0; i < chain.Count; i++)
                    chain[i].Volume = extras.volume;   // json side (also written to holders below)

            ApplyHolderOverrides(extras);
        }

        // Cut the chain so the conversation ends after the last box we actually used.
        chain[used - 1].Connections.Clear();

        conversation.Serialize(ec);
    }

    // -------------------------------------------------- icon / audio / font / volume overrides
    //
    // Text, name and volume live inside the conversation's json, but Unity assets can't,
    // so Dialogue Editor keeps icon/audio/font on NodeEventHolder components and re-reads
    // them every time the conversation starts. We write to those same holders through
    // reflection (name match first, type match as backup) so this compiles on any version
    // of the plugin. If a field can't be found it just skips that override and warns once.

    private static void ResolveHolderFields()
    {
        if (s_holderFieldsResolved) return;
        s_holderFieldsResolved = true;

        System.Type t = typeof(NodeEventHolder);
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        s_hIcon = FindField(t, flags, "Icon", typeof(Sprite), true);
        s_hAudio = FindField(t, flags, "Audio", typeof(AudioClip), true);
        s_hFont = FindField(t, flags, "TMPFont", typeof(TMPro.TMP_FontAsset), true);
        s_hVolume = FindField(t, flags, "Volume", typeof(float), false); // name-only; volume is also set via json as backup

        if (s_hIcon == null || s_hAudio == null)
            Debug.LogWarning("[DialogueCycler] Couldn't find the icon/audio fields on NodeEventHolder, so those per-interaction overrides will be skipped. (Probably a different plugin version.)");
    }

    private static FieldInfo FindField(System.Type t, BindingFlags flags, string name, System.Type fieldType, bool fallbackByType)
    {
        FieldInfo f = t.GetField(name, flags);
        if (f != null && f.FieldType == fieldType) return f;

        if (fallbackByType)
        {
            FieldInfo[] all = t.GetFields(flags);
            for (int i = 0; i < all.Length; i++)
                if (all[i].FieldType == fieldType) return all[i];
        }
        return null;
    }

    private void SnapshotHolders()
    {
        ResolveHolderFields();
        _holderOriginals.Clear();

        List<EditableSpeechNode> chain = GetChain(_pristine);
        for (int i = 0; i < chain.Count; i++)
        {
            NodeEventHolder h = conversation.GetNodeData(chain[i].ID);
            if (h == null) continue;

            HolderOriginal o = new HolderOriginal();
            o.holder = h;
            if (s_hIcon != null) o.icon = s_hIcon.GetValue(h) as Sprite;
            if (s_hAudio != null) o.audio = s_hAudio.GetValue(h) as AudioClip;
            if (s_hFont != null) o.font = s_hFont.GetValue(h) as TMPro.TMP_FontAsset;
            if (s_hVolume != null) o.volume = (float)s_hVolume.GetValue(h);
            _holderOriginals.Add(o);
        }
    }

    private void RestoreHolders()
    {
        for (int i = 0; i < _holderOriginals.Count; i++)
        {
            HolderOriginal o = _holderOriginals[i];
            if (o.holder == null) continue;
            if (s_hIcon != null) s_hIcon.SetValue(o.holder, o.icon);
            if (s_hAudio != null) s_hAudio.SetValue(o.holder, o.audio);
            if (s_hFont != null) s_hFont.SetValue(o.holder, o.font);
            if (s_hVolume != null) s_hVolume.SetValue(o.holder, o.volume);
        }
    }

    private void ApplyHolderOverrides(Entry extras)
    {
        for (int i = 0; i < _holderOriginals.Count; i++)
        {
            NodeEventHolder h = _holderOriginals[i].holder;
            if (h == null) continue;
            if (extras.icon != null && s_hIcon != null) s_hIcon.SetValue(h, extras.icon);
            if (extras.audio != null && s_hAudio != null) s_hAudio.SetValue(h, extras.audio);
            if (extras.font != null && s_hFont != null) s_hFont.SetValue(h, extras.font);
            if (extras.overrideVolume && s_hVolume != null) s_hVolume.SetValue(h, extras.volume);
        }
    }

    private EditableSpeechNode FindRoot(EditableConversation ec)
    {
        for (int i = 0; i < ec.SpeechNodes.Count; i++)
            if (ec.SpeechNodes[i].EditorInfo.isRoot)
                return ec.SpeechNodes[i];
        return null;
    }

    private List<EditableSpeechNode> GetChain(EditableConversation ec)
    {
        List<EditableSpeechNode> chain = new List<EditableSpeechNode>();
        EditableSpeechNode node = FindRoot(ec);
        while (node != null && !chain.Contains(node))
        {
            chain.Add(node);
            EditableSpeechNode next = null;
            for (int i = 0; i < node.Connections.Count; i++)
            {
                EditableSpeechConnection sc = node.Connections[i] as EditableSpeechConnection;
                if (sc != null) { next = sc.Speech; break; }
            }
            node = next;
        }
        return chain;
    }
}