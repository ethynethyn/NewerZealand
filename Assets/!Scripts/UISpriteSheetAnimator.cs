using UnityEngine;
using UnityEngine.UI;

public class UISpriteSheetAnimator : MonoBehaviour
{
    public Image targetImage;        // UI Image
    public Sprite[] frames;          // All frames from your sprite sheet
    public float fps = 12f;          // Frames per second

    private float timer;
    private int index;

    void Start()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer -= 1f / fps;
            index = (index + 1) % frames.Length;
            targetImage.sprite = frames[index];
        }
    }
}
