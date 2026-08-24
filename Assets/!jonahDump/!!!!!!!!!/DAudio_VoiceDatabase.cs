using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps a Dialogue Editor speech node's Name field to a voice profile.
/// Create via Assets > Create > DAudio > Voice Database.
/// </summary>
[CreateAssetMenu(fileName = "DAudio_VoiceDatabase", menuName = "DAudio/Voice Database", order = 1)]
public class DAudio_VoiceDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Must match the Name field on the speech node in Dialogue Editor. Case-insensitive, spaces trimmed.")]
        public string speakerName = "";

        [Tooltip("Optional. Extra names that should use this same voice (e.g. \"Sans (tired)\", \"???\").")]
        public string[] aliases = new string[0];

        public DAudio_VoiceProfile profile;
    }

    [Tooltip("Used for any speaker that isn't listed below, and for nodes with a blank Name.")]
    public DAudio_VoiceProfile defaultProfile;

    public List<Entry> voices = new List<Entry>();

    [System.NonSerialized] private Dictionary<string, DAudio_VoiceProfile> m_lookup;

    /// <summary>Returns the profile for this speaker, falling back to the default profile.</summary>
    public DAudio_VoiceProfile GetProfile(string speakerName)
    {
        Build();

        string key = DAudio_TextUtils.NormaliseSpeaker(speakerName);
        if (key.Length > 0)
        {
            DAudio_VoiceProfile found;
            if (m_lookup.TryGetValue(key, out found) && found != null)
                return found;
        }

        return defaultProfile;
    }

    /// <summary>Call after editing the list at runtime.</summary>
    public void Rebuild()
    {
        m_lookup = null;
        Build();
    }

    private void Build()
    {
        if (m_lookup != null) return;

        m_lookup = new Dictionary<string, DAudio_VoiceProfile>();
        if (voices == null) return;

        for (int i = 0; i < voices.Count; i++)
        {
            Entry entry = voices[i];
            if (entry == null || entry.profile == null) continue;

            Register(entry.speakerName, entry.profile);

            if (entry.aliases == null) continue;
            for (int a = 0; a < entry.aliases.Length; a++)
                Register(entry.aliases[a], entry.profile);
        }
    }

    private void Register(string rawName, DAudio_VoiceProfile profile)
    {
        string key = DAudio_TextUtils.NormaliseSpeaker(rawName);
        if (key.Length == 0) return;

        if (m_lookup.ContainsKey(key))
        {
            Debug.LogWarning("[DAudio] Voice database '" + name + "' has a duplicate speaker key: '" + key + "'. The first entry wins.", this);
            return;
        }

        m_lookup[key] = profile;
    }

    private void OnValidate()
    {
        // Force a rebuild next lookup so edits in the inspector apply immediately in play mode.
        m_lookup = null;
    }
}
