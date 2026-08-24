using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// A tiny pool of AudioSources so overlapping blips don't cut each other off
/// and each blip can have its own pitch.
/// </summary>
[AddComponentMenu("DAudio/Blip Player")]
public class DAudio_BlipPlayer : MonoBehaviour
{
    [Tooltip("How many blips can overlap. 3-4 is plenty.")]
    [SerializeField, Range(1, 16)] private int m_poolSize = 4;

    [Tooltip("Global volume for every voice, on top of each profile's own volume.")]
    [SerializeField, Range(0f, 1f)] private float m_masterVolume = 1f;

    [Tooltip("Keep blipping even when AudioListener.pause is true (useful if dialogue happens during a pause).")]
    [SerializeField] private bool m_ignoreListenerPause = true;

    private AudioSource[] m_sources;
    private int m_next;

    public float MasterVolume
    {
        get { return m_masterVolume; }
        set { m_masterVolume = Mathf.Clamp01(value); }
    }

    private void Awake()
    {
        BuildPool();
    }

    private void BuildPool()
    {
        int count = Mathf.Max(1, m_poolSize);
        m_sources = new AudioSource[count];

        for (int i = 0; i < count; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;              // 2D, always audible
            src.ignoreListenerPause = m_ignoreListenerPause;
            src.volume = 1f;
            m_sources[i] = src;
        }
    }

    public void Play(AudioClip clip, float volume, float pitch, AudioMixerGroup group)
    {
        if (clip == null) return;
        if (m_sources == null) BuildPool();

        AudioSource src = m_sources[m_next];
        m_next = (m_next + 1) % m_sources.Length;

        src.outputAudioMixerGroup = group;
        src.pitch = pitch;
        src.PlayOneShot(clip, Mathf.Clamp01(volume) * m_masterVolume);
    }

    public void StopAll()
    {
        if (m_sources == null) return;
        for (int i = 0; i < m_sources.Length; i++)
            if (m_sources[i] != null) m_sources[i].Stop();
    }
}
