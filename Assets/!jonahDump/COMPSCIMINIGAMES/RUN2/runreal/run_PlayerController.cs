using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// All movement happens in unwrapped tunnel-surface space
// (surfaceX around the perimeter, z forward, h off the surface),
// then gets mapped onto the polygon. Crossing a face edge
// tells the tunnel to rotate that face down — the Run mechanic.
public class run_PlayerController : MonoBehaviour
{
    public enum PState { Run, Air, FallingOut, Dead }

    run_GameManager gm;
    run_TunnelGenerator gen;

    public PState State { get; private set; }
    public float Z { get; private set; }
    public float SurfaceX { get; private set; }
    public float H { get; private set; }
    public float CurrentSpeed { get; private set; }
    public float StrafeInput { get; private set; }
    public int CurrentFace { get; private set; }

    float vH;
    float coyote, buffer;
    float checkpointZ;
    Vector3 visOffset;
    SpriteRenderer visualRenderer;

    public void Init(run_GameManager manager)
    {
        gm = manager;
        gen = gm.Tunnel;
        visualRenderer = GetComponentInChildren<SpriteRenderer>();
        ResetToStart();
    }

    public void ResetToStart()
    {
        Z = gen.TileSize * 1.5f;
        checkpointZ = Z;
        PlaceAtSpawn();
    }

    void PlaceAtSpawn()
    {
        SurfaceX = gen.FaceWidth * 0.5f; // middle of the floor face
        H = 0f; vH = 0f;
        coyote = 0f; buffer = 0f;
        visOffset = Vector3.zero;
        CurrentFace = 0;
        State = PState.Run;
        gen.SnapToFace(0);
        ApplyTransform();
    }

    public void Respawn()
    {
        Z = checkpointZ;
        PlaceAtSpawn();
        StopAllCoroutines();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        float t = 0f;
        while (t < gm.player.respawnBlinkTime)
        {
            if (visualRenderer) visualRenderer.enabled = !visualRenderer.enabled;
            yield return new WaitForSeconds(0.08f);
            t += 0.08f;
        }
        if (visualRenderer) visualRenderer.enabled = true;
    }

    void Update()
    {
        if (gm.State != run_GameManager.GameState.Playing) return;
        if (State == PState.Dead) return;

        var p = gm.player;
        float dt = Time.deltaTime;
        ReadInput(out float strafe, out bool jumpPressed, out bool jumpReleased);
        StrafeInput = strafe;

        // forward speed ramps with distance
        float progress = Mathf.Clamp01((float)gen.RowFromZ(Z) / Mathf.Max(1, p.speedRampRows));
        CurrentSpeed = Mathf.Lerp(p.runSpeed, p.maxRunSpeed, progress);

        // fell through a hole — falling out into space
        if (State == PState.FallingOut)
        {
            Z += CurrentSpeed * 0.35f * dt;
            vH -= p.gravity * dt;
            H += vH * dt;
            if (H < -p.fallDeathDepth)
            {
                State = PState.Dead;
                gm.PlayerFell();
            }
            ApplyTransform();
            return;
        }

        Z += CurrentSpeed * dt;
        SurfaceX = gen.WrapSurfaceX(SurfaceX + strafe * p.strafeSpeed * dt);

        // jump buffer
        if (buffer > 0f) buffer -= dt;
        if (jumpPressed) buffer = p.jumpBufferTime;

        // walked off an edge?
        if (State == PState.Run && !SolidBelow())
        {
            State = PState.Air;
            coyote = p.coyoteTime;
            vH = 0f;
        }

        if (State == PState.Air && coyote > 0f) coyote -= dt;

        bool canJump = State == PState.Run || (State == PState.Air && coyote > 0f && vH <= 0f);
        if (canJump && buffer > 0f)
        {
            vH = p.jumpForce;
            State = PState.Air;
            coyote = 0f; buffer = 0f;
            gm.PlaySfx(gm.sound.jumpSfx);
        }

        if (State == PState.Air)
        {
            if (p.variableJumpHeight && jumpReleased && vH > 0f)
                vH *= p.jumpCutMultiplier;

            vH -= p.gravity * dt;
            H += vH * dt;

            if (H <= 0f && vH <= 0f)
            {
                if (SolidBelow())
                {
                    H = 0f; vH = 0f;
                    State = PState.Run;
                    gm.PlaySfx(gm.sound.landSfx);
                }
                else
                {
                    State = PState.FallingOut;
                    gm.PlaySfx(gm.sound.fallSfx);
                }
            }
        }

        // face change -> spin the world, smooth the tiny seam pop
        int face = gen.FaceFromSurfaceX(SurfaceX);
        if (face != CurrentFace)
        {
            Vector3 before = gen.LocalPosOnFace(CurrentFace, SurfaceX, H, Z);
            Vector3 after = gen.LocalPosOnFace(face, SurfaceX, H, Z);
            visOffset += before - after;
            CurrentFace = face;
            gen.SetTargetFace(face);
        }
        visOffset = Vector3.Lerp(visOffset, Vector3.zero, 1f - Mathf.Exp(-gm.player.seamSmoothing * dt));

        // checkpoints
        if (p.useCheckpoints)
        {
            int row = gen.RowFromZ(Z);
            int cpRow = (row / p.checkpointEveryRows) * p.checkpointEveryRows;
            float cpZ = cpRow * gen.TileSize + gen.TileSize * 0.5f;
            if (cpRow > 0 && cpZ > checkpointZ) checkpointZ = cpZ;
        }

        // win
        if (Z >= gen.FinishZ)
        {
            ApplyTransform();
            gm.Win();
            return;
        }

        if (gm.ui.showProgress && gm.Screens != null)
            gm.Screens.SetProgress(Z / gen.FinishZ);

        ApplyTransform();
    }

    bool SolidBelow()
    {
        var p = gm.player;
        if (gen.IsSolid(SurfaceX, Z)) return true;
        if (p.edgeForgiveness > 0f)
        {
            if (gen.IsSolid(SurfaceX + p.edgeForgiveness, Z)) return true;
            if (gen.IsSolid(SurfaceX - p.edgeForgiveness, Z)) return true;
        }
        return false;
    }

    void ReadInput(out float strafe, out bool jumpPressed, out bool jumpReleased)
    {
        strafe = 0f; jumpPressed = false; jumpReleased = false;
        var kb = Keyboard.current;
        if (kb == null) return;
        var p = gm.player;
        bool left = kb[p.leftKey].isPressed || kb[p.leftKeyAlt].isPressed;
        bool right = kb[p.rightKey].isPressed || kb[p.rightKeyAlt].isPressed;
        strafe = (right ? 1f : 0f) - (left ? 1f : 0f);
        jumpPressed = kb[p.jumpKey].wasPressedThisFrame
                   || kb[p.jumpKeyAlt].wasPressedThisFrame
                   || kb[p.jumpKeyAlt2].wasPressedThisFrame;
        jumpReleased = kb[p.jumpKey].wasReleasedThisFrame
                    || kb[p.jumpKeyAlt].wasReleasedThisFrame
                    || kb[p.jumpKeyAlt2].wasReleasedThisFrame;
    }

    void ApplyTransform()
    {
        transform.localPosition = gen.LocalPosOnFace(CurrentFace, SurfaceX, H, Z) + visOffset;
        transform.localRotation = Quaternion.LookRotation(Vector3.forward, gen.LocalInward(CurrentFace));
    }
}
