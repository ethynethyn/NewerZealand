using UnityEngine;

// One falling arrow. RhythmGame creates these and drives their position via Tick().
[RequireComponent(typeof(SpriteRenderer))]
public class FallingNote : MonoBehaviour
{
    public NoteDirection direction;
    public float hitTime;     // song time (sec) when it should reach the target
    public bool hit;          // player hit it correctly
    public bool judged;       // already counted (as hit or as a miss)

    private Vector3 spawnPos;
    private Vector3 targetPos;
    private float spawnTime;   // song time when it appears at the top
    private float fallDuration;
    private SpriteRenderer sr;

    public void Init(NoteDirection direction, float hitTime, Vector3 spawnPos, Vector3 targetPos,
                     float fallDuration, Sprite sprite, Color color, int sortingOrder)
    {
        this.direction = direction;
        this.hitTime = hitTime;
        this.spawnPos = spawnPos;
        this.targetPos = targetPos;
        this.fallDuration = fallDuration;
        this.spawnTime = hitTime - fallDuration;
        hit = false;
        judged = false;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        transform.position = spawnPos;
    }

    // Called every frame with the current song time. Not clamped, so a missed
    // note keeps falling past the target and off screen.
    public void Tick(float songTime)
    {
        float denom = Mathf.Max(0.0001f, fallDuration);
        float p = (songTime - spawnTime) / denom;
        transform.position = Vector3.LerpUnclamped(spawnPos, targetPos, p);
    }
}
