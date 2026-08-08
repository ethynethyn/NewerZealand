using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// "youWIN" / "YOU LOSE" overlays + progress % in the top left.
public class run_UIScreens : MonoBehaviour
{
    run_GameManager gm;
    GameObject endRoot;
    TextMeshProUGUI bigText, hintText, progressText;
    CanvasGroup group;

    public void Init(run_GameManager manager, RectTransform canvas)
    {
        gm = manager;
        var u = gm.ui;

        endRoot = new GameObject("EndScreen", typeof(RectTransform));
        var root = endRoot.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        Stretch(root);
        group = endRoot.AddComponent<CanvasGroup>();

        var dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
        dim.rectTransform.SetParent(root, false);
        Stretch(dim.rectTransform);
        dim.color = u.screenDimColor;

        bigText = MakeText(root, "BigText", u.bigFontSize, FontStyles.Bold);
        bigText.rectTransform.anchoredPosition = new Vector2(0f, 40f);

        hintText = MakeText(root, "Hint", u.hintFontSize, FontStyles.Normal);
        hintText.rectTransform.anchoredPosition = new Vector2(0f, -80f);
        hintText.text = u.restartHintText;
        hintText.color = new Color(1f, 1f, 1f, 0.85f);
        hintText.gameObject.SetActive(u.showRestartHint);

        endRoot.SetActive(false);

        if (u.showProgress)
        {
            progressText = MakeText(canvas, "Progress", 34f, FontStyles.Bold);
            var rt = progressText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(300f, 50f);
            rt.anchoredPosition = new Vector2(18f, -14f);
            progressText.alignment = TextAlignmentOptions.TopLeft;
            progressText.color = new Color(1f, 1f, 1f, 0.75f);
            progressText.text = "0%";
        }
    }

    TextMeshProUGUI MakeText(RectTransform parent, string name, float size, FontStyles style)
    {
        var t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        var rt = t.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, 300f);
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public void SetProgress(float t)
    {
        if (progressText == null) return;
        progressText.text = Mathf.Clamp(Mathf.FloorToInt(t * 100f), 0, 100) + "%";
    }

    public void ShowEnd(string text, Color color)
    {
        endRoot.SetActive(true);
        bigText.text = text;
        bigText.color = color;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void HideEnd()
    {
        StopAllCoroutines();
        endRoot.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        group.alpha = 0f;
        while (group.alpha < 1f)
        {
            group.alpha += Time.deltaTime * gm.ui.fadeInSpeed;
            yield return null;
        }
        group.alpha = 1f;
    }
}
