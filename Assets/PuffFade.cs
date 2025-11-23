using UnityEngine;

public class PuffFade : MonoBehaviour
{
    public float fadeTime = 1.2f;
    private SpriteRenderer sr;
    private float timer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        if (timer >= fadeTime)
            Destroy(gameObject);
    }
}
