using System;
using System.Collections.Generic;
using UnityEngine;

// The four arrow lanes. (You don't have to use all four in a pattern.)
public enum NoteDirection { Left, Down, Up, Right }

// One note = a direction + the time it should be hit, measured in seconds
// from the START of the sound clip.
[Serializable]
public struct NoteEvent
{
    public NoteDirection direction;
    public float time;

    public NoteEvent(NoteDirection direction, float time)
    {
        this.direction = direction;
        this.time = time;
    }
}

// One pattern asset = a short sound + the arrow pattern timed to it.
// Create these with the PatternRecorder, or right-click in the Project:
// Create > Rhythm > Pattern.
[CreateAssetMenu(fileName = "Pattern", menuName = "Rhythm/Pattern")]
public class PatternData : ScriptableObject
{
    [Tooltip("The short audio snippet this pattern is timed to.")]
    public AudioClip clip;

    [Tooltip("Each note: a direction and the time (seconds from the start of the clip) it should be hit. You can hand-edit these.")]
    public List<NoteEvent> notes = new List<NoteEvent>();
}
