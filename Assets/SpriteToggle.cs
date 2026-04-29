using UnityEngine;
using UnityEngine.UI;

public class UIImageToggle : MonoBehaviour
{
    public Sprite spriteA;
    public Sprite spriteB;
    public float switchTime = 0.5f;

    private Image img;
    private float timer;
    private bool showingA = true;

    void Start()
    {
        img = GetComponent<Image>();
        img.sprite = spriteA;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= switchTime)
        {
            timer = 0f;

            showingA = !showingA;
            img.sprite = showingA ? spriteA : spriteB;
        }
    }
}