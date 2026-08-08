using UnityEngine;

// Cycles your 3 run sprites, holds a jump frame in the air,
// spins when falling into space, leans while strafing,
// and billboards toward the camera so it always reads clean.
public class run_PlayerAnimator : MonoBehaviour
{
    run_GameManager gm;
    run_PlayerController pc;
    SpriteRenderer sr;
    float frameTimer;
    float fallSpin;

    public void Init(run_GameManager manager, run_PlayerController player)
    {
        gm = manager;
        pc = player;
        sr = GetComponent<SpriteRenderer>();
        transform.localPosition = new Vector3(0f, gm.player.spriteYOffset, 0f);
        transform.localScale = Vector3.one * gm.player.spriteScale;

        if (gm.player.runFrames != null && gm.player.runFrames.Length > 0)
            sr.sprite = gm.player.runFrames[0];
        else
            sr.sprite = MakePlaceholder();
    }

    void Update()
    {
        var p = gm.player;
        bool hasFrames = p.runFrames != null && p.runFrames.Length > 0;

        if (pc.State == run_PlayerController.PState.Run && gm.State == run_GameManager.GameState.Playing)
        {
            fallSpin = 0f;
            if (hasFrames)
            {
                float fps = p.animFPS * (p.scaleAnimWithSpeed ? pc.CurrentSpeed / Mathf.Max(0.01f, p.runSpeed) : 1f);
                frameTimer += Time.deltaTime * fps;
                sr.sprite = p.runFrames[(int)frameTimer % p.runFrames.Length];
            }
        }
        else if (pc.State == run_PlayerController.PState.Air)
        {
            fallSpin = 0f;
            if (p.jumpFrame != null) sr.sprite = p.jumpFrame;
            else if (hasFrames) sr.sprite = p.runFrames[0];
        }
        else if (pc.State == run_PlayerController.PState.FallingOut
              || pc.State == run_PlayerController.PState.Dead)
        {
            fallSpin += p.fallSpinSpeed * Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        Camera cam = (gm.CameraRig != null && gm.CameraRig.Cam != null) ? gm.CameraRig.Cam : Camera.main;
        if (cam == null) return;
        float lean = -pc.StrafeInput * gm.player.strafeLeanAngle;
        transform.rotation = Quaternion.LookRotation(cam.transform.forward, pc.transform.up)
                           * Quaternion.Euler(0f, 0f, lean + fallSpin);
    }

    Sprite MakePlaceholder()
    {
        var tex = new Texture2D(8, 8);
        var px = new Color[64];
        for (int i = 0; i < 64; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
    }
}
