using UnityEngine;

// Drives the alien's animation from the player's state.
// TWO WAYS to use it (pick one):
//   A) Flipbook  - drop your hand-drawn Sprites into Run Frames / Jump Frames.
//   B) Animator  - leave Run Frames empty and add an Animator to this object;
//                  this sets a "Grounded" bool and fires a "Jump" trigger.
// Put this on the Sprite child (same object as the SpriteRenderer + R_Billboard).
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class R_PlayerAnimator : MonoBehaviour
{
    public R_PlayerController player;         // auto-found in a parent if left empty

    [Header("A) Flipbook frames (your drawings)")]
    public Sprite[] runFrames;
    public Sprite[] jumpFrames;
    public float runFps = 10f;
    public float jumpFps = 12f;
    public bool loopJump = false;             // off = hold the last jump frame while airborne

    [Header("B) Animator (only used if Run Frames is empty)")]
    public Animator animator;
    public string groundedBool = "Grounded";
    public string jumpTrigger = "Jump";

    SpriteRenderer sr;
    float timer;
    int frame;
    bool wasGrounded = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (player == null) player = GetComponentInParent<R_PlayerController>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void OnEnable() { if (player != null) player.Jumped += OnJump; }
    void OnDisable() { if (player != null) player.Jumped -= OnJump; }

    bool UseFrames() => runFrames != null && runFrames.Length > 0;

    void OnJump()
    {
        if (UseFrames())
        {
            frame = 0;   // restart the jump clip
            timer = 0f;
        }
        else if (animator != null && !string.IsNullOrEmpty(jumpTrigger))
        {
            animator.SetTrigger(jumpTrigger);
        }
    }

    void Update()
    {
        if (player == null) return;
        bool grounded = player.IsGrounded;

        if (UseFrames())
        {
            bool haveJump = jumpFrames != null && jumpFrames.Length > 0;
            Sprite[] set = (grounded || !haveJump) ? runFrames : jumpFrames;
            float fps = grounded ? runFps : jumpFps;

            if (grounded != wasGrounded) { frame = 0; timer = 0f; }   // reset on state change

            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, fps);
            while (timer >= step)
            {
                timer -= step;
                frame++;
                if (frame >= set.Length)
                    frame = (!grounded && !loopJump) ? set.Length - 1 : 0;  // hold or loop
            }

            if (set.Length > 0)
                sr.sprite = set[Mathf.Clamp(frame, 0, set.Length - 1)];
        }
        else if (animator != null)
        {
            if (!string.IsNullOrEmpty(groundedBool)) animator.SetBool(groundedBool, grounded);
        }

        wasGrounded = grounded;
    }
}