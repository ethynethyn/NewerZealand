using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop-in "juice" toolbox. Creates itself on first use — nothing to place in the scene.
///   Juice.Shake(0.8f);
///   Juice.Flash(Color.white, 0.12f);
///   Juice.HitStop(0.06f);
///   Juice.Burst(pos, Color.yellow, 40, 12f, mySprite);      // sparks (optional sprite)
///   Juice.SpritePop(pos, fireSprite, col, 0.5f, 5f, 0.4f);  // flat expanding flash
///   Juice.Shockwave(pos, Color.yellow, 5f);                 // fallback ring
/// All effects run on UNSCALED time so they keep playing during a HitStop.
/// </summary>
public class Juice : MonoBehaviour
{
    static Juice _i;
    static Juice I
    {
        get
        {
            if (_i == null) _i = new GameObject("~Juice (auto)").AddComponent<Juice>();
            return _i;
        }
    }

    [Header("Camera shake")]
    public float maxShakeOffset = 0.7f;
    public float traumaDecay = 1.6f;

    float trauma;
    Vector3 appliedOffset;
    Camera trackedCam;

    public static void Shake(float amount) => I.trauma = Mathf.Clamp01(I.trauma + amount);

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Transform t = cam.transform;
        if (cam != trackedCam) { trackedCam = cam; appliedOffset = Vector3.zero; }

        t.localPosition -= appliedOffset;
        if (trauma > 0f)
        {
            float shake = trauma * trauma;
            appliedOffset = new Vector3(Random.value * 2f - 1f, Random.value * 2f - 1f, 0f) * (maxShakeOffset * shake);
            t.localPosition += appliedOffset;
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
        }
        else appliedOffset = Vector3.zero;
    }

    // ---- SCREEN FLASH ----
    Image flashImage;
    Coroutine flashCo;

    void EnsureFlash()
    {
        if (flashImage != null) return;
        var canvasGO = new GameObject("~JuiceCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Flash");
        imgGO.transform.SetParent(canvasGO.transform, false);
        flashImage = imgGO.AddComponent<Image>();
        flashImage.raycastTarget = false;
        var rt = flashImage.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    public static void Flash(Color color, float duration)
    {
        I.EnsureFlash();
        if (I.flashCo != null) I.StopCoroutine(I.flashCo);
        I.flashCo = I.StartCoroutine(I.FlashRoutine(color, duration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float a = (1f - Mathf.Clamp01(e / duration)) * color.a;
            flashImage.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        flashImage.color = new Color(color.r, color.g, color.b, 0f);
    }

    // ---- HIT STOP ----
    bool hitStopping;
    public static void HitStop(float duration) => I.StartCoroutine(I.HitStopRoutine(duration));

    IEnumerator HitStopRoutine(float duration)
    {
        if (hitStopping) yield break;
        hitStopping = true;
        Time.timeScale = 0f;
        float e = 0f;
        while (e < duration) { e += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        hitStopping = false;
    }

    // ---- SPARK BURST (optional custom sprite) ----
    public static void Burst(Vector3 position, Color color, int count = 30, float speed = 10f,
                             Sprite sprite = null, float size = 0.15f, float lifetime = 0.6f)
    {
        var go = new GameObject("~Burst");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var mat = new Material(Shader.Find("Sprites/Default"));
        if (sprite != null) mat.mainTexture = sprite.texture;
        go.GetComponent<ParticleSystemRenderer>().material = mat;

        ps.Play();
        Destroy(go, lifetime + 0.3f);
    }

    // ---- SPRITE POP (a flat sprite that expands + fades, e.g. a fire flash) ----
    public static void SpritePop(Vector3 position, Sprite sprite, Color color,
                                 float startScale, float endScale, float duration)
    {
        if (sprite == null) return;
        var go = new GameObject("~SpritePop");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // lie flat; flip to -90 if needed
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        I.StartCoroutine(I.SpritePopRoutine(go, sr, color, startScale, endScale, duration));
    }

    IEnumerator SpritePopRoutine(GameObject go, SpriteRenderer sr, Color color, float startScale, float endScale, float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(e / duration);
            go.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);
            Color c = color; c.a = color.a * (1f - p);
            sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ---- SHOCKWAVE RING (fallback when no blast sprite is set) ----
    public static void Shockwave(Vector3 center, Color color, float maxRadius = 4f, float duration = 0.4f, float width = 0.18f)
    {
        var go = new GameObject("~Shockwave");
        go.transform.position = center;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = width;
        lr.numCapVertices = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        int segments = 48;
        lr.positionCount = segments;
        I.StartCoroutine(I.ShockwaveRoutine(go, lr, color, maxRadius, duration, segments));
    }

    IEnumerator ShockwaveRoutine(GameObject go, LineRenderer lr, Color color, float maxRadius, float duration, int segments)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(e / duration);
            float radius = Mathf.Lerp(0.2f, maxRadius, p);
            Color c = color; c.a = 1f - p;
            lr.startColor = c; lr.endColor = c;
            for (int i = 0; i < segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, 0.02f, Mathf.Sin(ang) * radius));
            }
            yield return null;
        }
        Destroy(go);
    }
}
