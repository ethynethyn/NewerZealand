using UnityEngine;
using UnityEngine.UI;

// Hearts in the top right. If you don't assign sprites it
// generates its own heart from the heart curve equation.
public class run_HeartsUI : MonoBehaviour
{
    run_GameManager gm;
    Image[] hearts;
    Sprite _full, _empty;
    static Sprite generatedHeart;

    public void Init(run_GameManager manager, RectTransform canvas)
    {
        gm = manager;
        var s = gm.lives;

        var holder = new GameObject("Hearts", typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(canvas, false);
        holder.anchorMin = holder.anchorMax = new Vector2(1f, 1f);
        holder.pivot = new Vector2(1f, 1f);
        holder.anchoredPosition = new Vector2(-s.screenPadding, -s.screenPadding);

        _full = s.fullHeartSprite != null ? s.fullHeartSprite : GetGeneratedHeart();
        _empty = s.emptyHeartSprite != null ? s.emptyHeartSprite : _full;

        hearts = new Image[s.maxLives];
        for (int i = 0; i < s.maxLives; i++)
        {
            var img = new GameObject("Heart_" + i, typeof(RectTransform)).AddComponent<Image>();
            var rt = img.rectTransform;
            rt.SetParent(holder, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(s.heartSize, s.heartSize);
            rt.anchoredPosition = new Vector2(-(s.maxLives - 1 - i) * (s.heartSize + s.heartSpacing), 0f);
            img.sprite = _full;
            img.preserveAspect = true;
            hearts[i] = img;
        }
        SetLives(gm.CurrentLives);
    }

    public void SetLives(int livesLeft)
    {
        if (hearts == null) return;
        var s = gm.lives;
        for (int i = 0; i < hearts.Length; i++)
        {
            bool full = i < livesLeft;
            if (!full && s.hideEmptyHearts) { hearts[i].enabled = false; continue; }
            hearts[i].enabled = true;
            hearts[i].sprite = full ? _full : _empty;
            hearts[i].color = full ? s.fullColor : s.emptyColor;
        }
    }

    // classic heart curve: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
    static Sprite GetGeneratedHeart()
    {
        if (generatedHeart != null) return generatedHeart;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    float fx = ((x + 0.25f + sx * 0.5f) / size) * 2.9f - 1.45f;
                    float fy = ((y + 0.25f + sy * 0.5f) / size) * 2.9f - 1.25f;
                    float f = Mathf.Pow(fx * fx + fy * fy - 1f, 3f) - fx * fx * fy * fy * fy;
                    if (f <= 0f) a += 0.25f;
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        generatedHeart = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return generatedHeart;
    }
}
