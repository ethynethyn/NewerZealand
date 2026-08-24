using System.Collections;
using UnityEngine;

/// <summary>
/// Tuning helper. Put this anywhere in a scene alongside a DAudio_BlipPlayer, enter play mode,
/// then right-click the component header and choose "Preview Voice" to hear a profile
/// without having to trigger a real conversation.
/// </summary>
[AddComponentMenu("DAudio/Voice Preview")]
[RequireComponent(typeof(DAudio_BlipPlayer))]
public class DAudio_VoicePreview : MonoBehaviour
{
    [SerializeField] private DAudio_VoiceProfile m_profile;

    [TextArea(2, 4)]
    [SerializeField] private string m_sampleText = "hey. you look like you could use a break, kid.";

    [Tooltip("Characters revealed per second. Match this to your ConversationManager's Scroll Speed.")]
    [SerializeField, Range(5f, 120f)] private float m_charactersPerSecond = 30f;

    private DAudio_BlipPlayer m_player;
    private Coroutine m_routine;

    private void Awake()
    {
        m_player = GetComponent<DAudio_BlipPlayer>();
    }

    [ContextMenu("Preview Voice")]
    public void Preview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DAudio] Voice preview only works in play mode.", this);
            return;
        }

        if (m_profile == null)
        {
            Debug.LogWarning("[DAudio] Assign a voice profile first.", this);
            return;
        }

        if (m_routine != null) StopCoroutine(m_routine);
        m_routine = StartCoroutine(PreviewRoutine());
    }

    private IEnumerator PreviewRoutine()
    {
        if (m_player == null) m_player = GetComponent<DAudio_BlipPlayer>();

        string text = DAudio_TextUtils.StripRichText(m_sampleText);
        float interval = 1f / Mathf.Max(1f, m_charactersPerSecond);
        int counter = 0;
        float lastBlip = -999f;

        for (int i = 0; i < text.Length; i++)
        {
            if (m_profile.CountsAsCharacter(text[i]))
            {
                counter++;
                if (counter >= Mathf.Max(1, m_profile.charactersPerBlip))
                {
                    counter = 0;
                    if (Time.unscaledTime - lastBlip >= m_profile.minSecondsBetweenBlips)
                    {
                        m_player.Play(m_profile.NextClip(), m_profile.volume, m_profile.NextPitch(), m_profile.mixerGroup);
                        lastBlip = Time.unscaledTime;
                    }
                }
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        m_routine = null;
    }
}
