using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// One character's "voice". Create via Assets > Create > DAudio > Voice Profile.
/// </summary>
[CreateAssetMenu(fileName = "DAudio_Voice_New", menuName = "DAudio/Voice Profile", order = 0)]
public class DAudio_VoiceProfile : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("One short blip (roughly 0.03 - 0.15s) is all you need. Add more for subtle variety.")]
    public AudioClip[] clips = new AudioClip[0];

    [Tooltip("On: picks a random clip each blip (never the same one twice in a row). Off: cycles in order.")]
    public bool randomiseClips = true;

    [Header("Mix")]
    [Range(0f, 1f)] public float volume = 0.7f;

    [Tooltip("Optional. Route this voice through a mixer group (handy for a Dialogue bus).")]
    public AudioMixerGroup mixerGroup;

    [Header("Pitch")]
    [Tooltip("1 = the clip's natural pitch. Lower = deeper character.")]
    [Range(0.1f, 3f)] public float pitch = 1f;

    [Tooltip("Random wobble applied per blip. 0 = perfectly flat and robotic.")]
    [Range(0f, 0.5f)] public float pitchVariance = 0.05f;

    [Header("Cadence")]
    [Tooltip("Play a blip every N spoken characters. 1 = every character (fastest).")]
    [Range(1, 12)] public int charactersPerBlip = 1;

    [Tooltip("Safety rate limit so fast scroll speeds don't machine-gun the sound.")]
    [Range(0f, 0.4f)] public float minSecondsBetweenBlips = 0.04f;

    [Header("Which characters make a sound")]
    public bool skipWhitespace = true;
    public bool skipPunctuation = false;

    [Tooltip("Only used when Skip Punctuation is on.")]
    public string punctuationCharacters = ".,!?;:'\"\u2019\u2026-\u2014";

    [System.NonSerialized] private int m_cursor = -1;

    public bool HasClips
    {
        get { return clips != null && clips.Length > 0; }
    }

    /// <summary>Next clip to play, honouring the randomise/cycle setting.</summary>
    public AudioClip NextClip()
    {
        if (!HasClips) return null;
        if (clips.Length == 1) return clips[0];

        int index;

        if (randomiseClips)
        {
            index = Random.Range(0, clips.Length);
            if (index == m_cursor) index = (index + 1) % clips.Length;
        }
        else
        {
            index = (m_cursor + 1) % clips.Length;
        }

        m_cursor = index;
        return clips[index];
    }

    /// <summary>Pitch for a single blip, including the random wobble.</summary>
    public float NextPitch()
    {
        if (pitchVariance <= 0f) return Mathf.Max(0.05f, pitch);
        return Mathf.Max(0.05f, pitch + Random.Range(-pitchVariance, pitchVariance));
    }

    /// <summary>Does this revealed character count towards the next blip?</summary>
    public bool CountsAsCharacter(char c)
    {
        if (skipWhitespace && char.IsWhiteSpace(c)) return false;

        if (skipPunctuation && !string.IsNullOrEmpty(punctuationCharacters)
            && punctuationCharacters.IndexOf(c) >= 0) return false;

        return true;
    }
}
