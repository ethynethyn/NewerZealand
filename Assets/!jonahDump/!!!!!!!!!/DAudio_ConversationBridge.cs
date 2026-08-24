using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// If your copy of Dialogue Editor lives in a different namespace, change this one line.
using DialogueEditor;

/// <summary>
/// Read-only window into Dialogue Editor's ConversationManager.
///
/// Everything past ConversationManager.Instance is found by reflection rather than by
/// hard-coded field names, so this keeps working across plugin versions, works whether the
/// UI uses UnityEngine.UI.Text or TextMeshPro, and never requires touching a plugin script.
/// </summary>
public class DAudio_ConversationBridge
{
    private const BindingFlags kInstanceAny =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<Type, PropertyInfo> s_textProperties = new Dictionary<Type, PropertyInfo>();
    private static readonly Dictionary<Type, PropertyInfo> s_maxVisibleProperties = new Dictionary<Type, PropertyInfo>();

    private ConversationManager m_manager;

    private readonly List<Component> m_dialogueCandidates = new List<Component>();
    private readonly List<Component> m_nameCandidates = new List<Component>();
    private readonly List<MemberInfo> m_nodeMembers = new List<MemberInfo>();

    private Component m_dialogueText;
    private Component m_nameText;
    private MemberInfo m_bestNodeMember;
    private float m_nextRetryTime;

    public Component DialogueTextComponent { get { return m_dialogueText; } }
    public Component NameTextComponent { get { return m_nameText; } }

    // ------------------------------------------------------------------ lifecycle

    /// <summary>Call once per frame. Returns false when there's nothing to read.</summary>
    public bool Refresh(Component dialogueOverride, Component nameOverride)
    {
        ConversationManager cm = ConversationManager.Instance;

        if (cm == null)
        {
            m_manager = null;
            m_dialogueText = null;
            return false;
        }

        if (!ReferenceEquals(cm, m_manager))
        {
            m_manager = cm;
            ResolveMembers();
        }
        else if (m_dialogueCandidates.Count == 0 && Time.unscaledTime >= m_nextRetryTime)
        {
            m_nextRetryTime = Time.unscaledTime + 0.5f;
            ResolveMembers();
        }

        m_dialogueText = dialogueOverride != null ? dialogueOverride : PickBest(m_dialogueCandidates);
        m_nameText = nameOverride != null ? nameOverride : PickBest(m_nameCandidates);

        return m_dialogueText != null;
    }

    public bool IsDialogueVisible()
    {
        return m_dialogueText != null && m_dialogueText.gameObject.activeInHierarchy;
    }

    // ------------------------------------------------------------------ text reading

    /// <summary>The current contents of the dialogue label, tags and all.</summary>
    public string GetRawDialogueText()
    {
        return GetTextValue(m_dialogueText);
    }

    /// <summary>
    /// How many characters are actually on screen. Dialogue Editor reveals text either by
    /// growing the string or (with TMP) by raising maxVisibleCharacters, so handle both.
    /// </summary>
    public int GetVisibleCount(int strippedLength)
    {
        PropertyInfo prop = GetMaxVisibleProperty(m_dialogueText);
        if (prop != null)
        {
            try
            {
                int maxVisible = (int)prop.GetValue(m_dialogueText, null);
                if (maxVisible >= 0 && maxVisible < strippedLength) return maxVisible;
            }
            catch { /* fall through */ }
        }

        return strippedLength;
    }

    // ------------------------------------------------------------------ node reading

    /// <summary>The SpeechNode instance currently being displayed, as a plain object.</summary>
    public object GetCurrentNode()
    {
        if (m_manager == null) return null;

        if (m_bestNodeMember != null)
        {
            object cached = ReadMember(m_bestNodeMember, m_manager);
            if (cached != null) return cached;
        }

        for (int i = 0; i < m_nodeMembers.Count; i++)
        {
            object value = ReadMember(m_nodeMembers[i], m_manager);
            if (value == null) continue;
            if (!HasMember(value.GetType(), "Name")) continue;

            m_bestNodeMember = m_nodeMembers[i];
            return value;
        }

        return null;
    }

    /// <summary>The speaker's name for the current node, or the name label's text as a fallback.</summary>
    public string GetSpeakerName()
    {
        string fromNode = ReadStringMember(GetCurrentNode(), "Name");
        if (!string.IsNullOrEmpty(fromNode)) return fromNode;

        string fromLabel = GetTextValue(m_nameText);
        if (!string.IsNullOrEmpty(fromLabel)) return fromLabel;

        return string.Empty;
    }

    /// <summary>The AudioClip assigned to the current node in the Dialogue Editor window, if any.</summary>
    public AudioClip GetNodeAudio()
    {
        object node = GetCurrentNode();
        if (node == null) return null;

        Type type = node.GetType();

        FieldInfo field = type.GetField("Audio", kInstanceAny);
        if (field != null && typeof(AudioClip).IsAssignableFrom(field.FieldType))
            return field.GetValue(node) as AudioClip;

        PropertyInfo prop = type.GetProperty("Audio", kInstanceAny);
        if (prop != null && typeof(AudioClip).IsAssignableFrom(prop.PropertyType))
            return prop.GetValue(node, null) as AudioClip;

        return null;
    }

    // ------------------------------------------------------------------ resolution

    private void ResolveMembers()
    {
        m_dialogueCandidates.Clear();
        m_nameCandidates.Clear();
        m_nodeMembers.Clear();
        m_bestNodeMember = null;

        if (m_manager == null) return;

        List<Component> unlabelled = new List<Component>();
        Type type = m_manager.GetType();

        while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(UnityEngine.Object))
        {
            FieldInfo[] fields = type.GetFields(kInstanceAny);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                object value;
                try { value = field.GetValue(m_manager); }
                catch { continue; }

                Component comp = value as Component;
                if (comp != null && GetTextProperty(comp) != null)
                {
                    string lower = field.Name.ToLowerInvariant();

                    if (lower.Contains("dialogue") || lower.Contains("speech") || lower.Contains("body") || lower.Contains("message"))
                        AddUnique(m_dialogueCandidates, comp);
                    else if (lower.Contains("name") || lower.Contains("speaker") || lower.Contains("title"))
                        AddUnique(m_nameCandidates, comp);
                    else
                        AddUnique(unlabelled, comp);
                }

                if (LooksLikeNodeType(field.FieldType))
                    m_nodeMembers.Add(field);
            }

            PropertyInfo[] props = type.GetProperties(kInstanceAny);
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].GetIndexParameters().Length != 0) continue;
                if (!props[i].CanRead) continue;
                if (LooksLikeNodeType(props[i].PropertyType))
                    m_nodeMembers.Add(props[i]);
            }

            type = type.BaseType;
        }

        // Nothing obviously named "dialogue"? Fall back to whatever text components we found.
        if (m_dialogueCandidates.Count == 0)
            for (int i = 0; i < unlabelled.Count; i++)
                AddUnique(m_dialogueCandidates, unlabelled[i]);

        // Members named like "currentSpeech" are far more likely to be the live node.
        m_nodeMembers.Sort(CompareNodeMembers);

        if (m_dialogueCandidates.Count == 0)
            Debug.LogWarning("[DAudio] Couldn't find the dialogue text component on ConversationManager. " +
                             "Assign it manually in the Dialogue Text Override field on DAudio_DialogueVoice.");
    }

    private static int CompareNodeMembers(MemberInfo a, MemberInfo b)
    {
        int scoreA = a.Name.ToLowerInvariant().Contains("current") ? 0 : 1;
        int scoreB = b.Name.ToLowerInvariant().Contains("current") ? 0 : 1;
        return scoreA.CompareTo(scoreB);
    }

    private static void AddUnique(List<Component> list, Component comp)
    {
        if (comp == null) return;
        for (int i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i], comp)) return;
        list.Add(comp);
    }

    /// <summary>Prefers an active component that actually has text in it right now.</summary>
    private static Component PickBest(List<Component> list)
    {
        Component firstAlive = null;
        Component firstActive = null;

        for (int i = 0; i < list.Count; i++)
        {
            Component comp = list[i];
            if (comp == null) continue;

            if (firstAlive == null) firstAlive = comp;
            if (!comp.gameObject.activeInHierarchy) continue;
            if (firstActive == null) firstActive = comp;

            if (!string.IsNullOrEmpty(GetTextValue(comp))) return comp;
        }

        return firstActive != null ? firstActive : firstAlive;
    }

    // ------------------------------------------------------------------ reflection plumbing

    private static bool LooksLikeNodeType(Type type)
    {
        if (type == null || type.IsPrimitive || type == typeof(string)) return false;

        string n = type.Name;
        return n == "SpeechNode" || n == "ConversationNode" || n == "EditableSpeechNode";
    }

    private static object ReadMember(MemberInfo member, object target)
    {
        try
        {
            FieldInfo field = member as FieldInfo;
            if (field != null) return field.GetValue(target);

            PropertyInfo prop = member as PropertyInfo;
            if (prop != null) return prop.GetValue(target, null);
        }
        catch { /* ignore */ }

        return null;
    }

    private static bool HasMember(Type type, string name)
    {
        return type.GetField(name, kInstanceAny) != null || type.GetProperty(name, kInstanceAny) != null;
    }

    private static string ReadStringMember(object target, string name)
    {
        if (target == null) return null;

        Type type = target.GetType();

        FieldInfo field = type.GetField(name, kInstanceAny);
        if (field != null && field.FieldType == typeof(string))
            return field.GetValue(target) as string;

        PropertyInfo prop = type.GetProperty(name, kInstanceAny);
        if (prop != null && prop.PropertyType == typeof(string))
            return prop.GetValue(target, null) as string;

        return null;
    }

    /// <summary>Works for UnityEngine.UI.Text, TMP_Text, or anything else with a string "text" property.</summary>
    private static string GetTextValue(Component comp)
    {
        if (comp == null) return string.Empty;

        PropertyInfo prop = GetTextProperty(comp);
        if (prop == null) return string.Empty;

        try { return prop.GetValue(comp, null) as string ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static PropertyInfo GetTextProperty(Component comp)
    {
        Type type = comp.GetType();

        PropertyInfo cached;
        if (s_textProperties.TryGetValue(type, out cached)) return cached;

        PropertyInfo prop = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && (prop.PropertyType != typeof(string) || !prop.CanRead)) prop = null;

        s_textProperties[type] = prop;
        return prop;
    }

    private static PropertyInfo GetMaxVisibleProperty(Component comp)
    {
        if (comp == null) return null;

        Type type = comp.GetType();

        PropertyInfo cached;
        if (s_maxVisibleProperties.TryGetValue(type, out cached)) return cached;

        PropertyInfo prop = type.GetProperty("maxVisibleCharacters", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && (prop.PropertyType != typeof(int) || !prop.CanRead)) prop = null;

        s_maxVisibleProperties[type] = prop;
        return prop;
    }
}
