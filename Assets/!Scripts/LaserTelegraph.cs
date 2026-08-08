using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Juiced ground telegraph. One marker (at the strike point) can be hit by SEVERAL
/// beams at once — pass two origins for eye-lasers that converge on the target.
/// Shows a spinning/strobing marker, an optional reticle sprite that snaps inward,
/// the charge beams, and rising rumble. Fire() unleashes a fat blast + screen chaos.
///
/// Put this on a prefab with a SpriteRenderer (your marker art). Optionally assign your
/// own Reticle Sprite, Impact (spark) Sprite, and Blast Sprite in the Inspector.
///
/// Boss flow:  Instantiate(prefab, target)  ->  Begin(windup, origins[])  ->  Fire()
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LaserTelegraph : MonoBehaviour
{
    [Header("Ground marker – spin & strobe")]
    public float spinSpeed = 540f;
    public Color baseColor   = new Color(1f, 0.4f, 0.1f, 0.5f);
    public Color flashColor  = new Color(1f, 1f, 0.3f, 1f);
    public Color dangerColor = new Color(1f, 0.05f, 0.05f, 1f);
    public float minFlashesPerSecond = 2f;
    public float maxFlashesPerSecond = 22f;
    public float rampExponent = 2.2f;
    public float markerGrowth = 0.35f;
    public float markerPulse = 0.15f;

    [Header("Reticle (assign your own sprite)")]
    public Sprite reticleSprite;
    public Color reticleTint = Color.white;
    public float reticleStartScale = 3f;
    public float reticleEndScale = 0.5f;
    public float reticleSpinSpeed = -220f;

    [Header("Charge beams")]
    public Color beamChargeColor = new Color(1f, 0.3f, 0.2f, 1f);
    public Color beamFireColor   = new Color(1f, 1f, 1f, 1f);
    public float beamStartWidth   = 0.05f;
    public float beamChargedWidth = 0.4f;
    public float beamFireWidth    = 1.3f;
    public float beamJitter = 0.1f;

    [Header("Impact spark burst")]
    public Sprite impactSprite;
    public Color impactColor = new Color(1f, 0.85f, 0.3f, 1f);
    public int impactParticles = 45;
    public float impactSpeed = 13f;
    public float impactSize = 0.25f;

    [Header("Impact blast (flat flash on the ground)")]
    [Tooltip("e.g. a flat fire image. Leave empty to fall back to the default expanding ring.")]
    public Sprite blastSprite;
    public Color blastColor = new Color(1f, 0.9f, 0.4f, 1f);
    public float blastStartScale = 0.5f;
    public float blastEndScale = 5f;
    public float blastDuration = 0.4f;

    [Header("Feel")]
    public float chargeRumble = 0.35f;

    SpriteRenderer sr;
    readonly List<LineRenderer> beams = new List<LineRenderer>();
    Vector3[] origins = new Vector3[0];
    Vector3 target, baseScale;
    float duration = 3f, elapsed, phase;
    bool firing;

    GameObject reticleGO;
    SpriteRenderer reticleSr;
    Transform reticleT;
    float reticleAngle;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        baseScale = transform.localScale;
        sr.color = baseColor;

        if (reticleSprite != null)
        {
            reticleGO = new GameObject("Reticle");
            reticleGO.transform.position = transform.position;
            reticleSr = reticleGO.AddComponent<SpriteRenderer>();
            reticleSr.sprite = reticleSprite;
            reticleSr.color = reticleTint;
            reticleT = reticleGO.transform;
        }
    }

    public void Begin(float windupDuration, Vector3[] beamOrigins)
    {
        target = transform.position;
        origins = beamOrigins ?? new Vector3[0];
        duration = Mathf.Max(0.01f, windupDuration);
        elapsed = 0f; phase = 0f; firing = false;
        foreach (var o in origins) beams.Add(MakeBeam());
    }

    LineRenderer MakeBeam()
    {
        var go = new GameObject("Beam");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = beamStartWidth;
        lr.numCapVertices = 3;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        return lr;
    }

    void Update()
    {
        if (firing) return;

        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / duration);

        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
        float fps = Mathf.Lerp(minFlashesPerSecond, maxFlashesPerSecond, Mathf.Pow(p, rampExponent));
        phase += fps * Time.deltaTime;
        float tri = Mathf.PingPong(phase, 1f);
        Color hot = Color.Lerp(flashColor, dangerColor, p);
        sr.color = Color.Lerp(baseColor, hot, tri);
        transform.localScale = baseScale * (1f + markerGrowth * p + markerPulse * p * tri);

        for (int i = 0; i < beams.Count; i++)
        {
            var lr = beams[i];
            float w = Mathf.Lerp(beamStartWidth, beamChargedWidth, p) * (1f + Random.Range(-0.25f, 0.25f) * (1f - p * 0.5f));
            lr.widthMultiplier = w;
            Color bc = Color.Lerp(beamChargeColor, beamFireColor, p * p);
            lr.startColor = bc; lr.endColor = bc;
            Vector3 jitter = (Vector3)(Random.insideUnitCircle * beamJitter * (1f - p));
            lr.SetPosition(0, origins[i]);
            lr.SetPosition(1, target + jitter);
        }

        if (reticleT != null)
        {
            reticleAngle += reticleSpinSpeed * Time.deltaTime;
            reticleT.rotation = Quaternion.AngleAxis(reticleAngle, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            reticleT.localScale = Vector3.one * Mathf.Lerp(reticleStartScale, reticleEndScale, p);
            Color rc = reticleTint; rc.a = reticleTint.a * (0.3f + 0.7f * p);
            reticleSr.color = rc;
        }

        if (chargeRumble > 0f) Juice.Shake(chargeRumble * p * p * Time.deltaTime);
    }

    public void Fire()
    {
        if (firing) return;
        firing = true;
        StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        if (reticleSr != null) reticleSr.enabled = false;

        foreach (var lr in beams)
        {
            lr.widthMultiplier = beamFireWidth;
            lr.startColor = beamFireColor; lr.endColor = beamFireColor;
            lr.SetPosition(1, target);
        }

        Juice.HitStop(0.06f);
        Juice.Shake(0.9f);
        Juice.Flash(new Color(1f, 1f, 1f, 0.85f), 0.12f);

        // Configurable ground blast: your sprite if set, otherwise the default ring.
        if (blastSprite != null)
            Juice.SpritePop(target, blastSprite, blastColor, blastStartScale, blastEndScale, blastDuration);
        else
            Juice.Shockwave(target, blastColor, blastEndScale, blastDuration);

        Juice.Burst(target, impactColor, impactParticles, impactSpeed, impactSprite, impactSize);

        float t = 0f;
        while (t < 0.05f) { t += Time.unscaledDeltaTime; yield return null; }

        float fade = 0.18f;
        t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float f = 1f - t / fade;
            foreach (var lr in beams)
            {
                lr.widthMultiplier = beamFireWidth * f * f;
                Color c = beamFireColor; c.a = f;
                lr.startColor = c; lr.endColor = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (reticleGO != null) Destroy(reticleGO);
    }
}
