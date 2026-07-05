using UnityEngine;
using UnityEngine.InputSystem;

// The alien. Auto-runs forward (+Z) while custom gravity pulls it toward the
// current floor. Running into a side wall wraps you onto it (it becomes the new
// floor) - no jump needed. Jumping still lets you cross to a wall mid-air.
// Kinematic body moved by hand; grounding + fall-out done with raycasts.
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class R_PlayerController : MonoBehaviour
{
    [Header("Run")]
    public float runSpeed = 12f;          // constant forward speed along +Z

    [Header("Strafe")]
    public float strafeSpeed = 9f;
    public float groundStrafeAccel = 120f;
    public float airStrafeAccel = 40f;

    [Header("Jump / Gravity")]
    public float jumpSpeed = 12f;
    public float gravity = 34f;
    public float terminalFall = 60f;
    public float switchLock = 0.12f;      // brief lock after a switch (stops corner flip-flop)

    [Header("Size / Bounds")]
    public float playerRadius = 0.5f;
    public float tunnelRadius = 3f;       // kept in sync by R_GameManager / generator
    public float fallOutMargin = 1.5f;    // how far past a gap before you die

    [Header("Start (set by GameManager or the scene)")]
    public Vector3 startPosition;

    // events / state the sprite animator reads
    public event System.Action Jumped;
    public bool IsGrounded => grounded;

    // runtime
    Rigidbody rb;
    Vector3 gravityDir = Vector3.down;    // points toward the current floor
    Vector3 sideDir = Vector3.right;   // screen-right, perpendicular to gravity
    float gravVel;                        // speed along gravityDir (+ = falling)
    float strafeVel;                      // speed along sideDir
    float switchTimer;
    bool grounded;

    public Vector3 GravityDir => gravityDir;   // camera reads this to roll

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.radius = playerRadius;
        col.isTrigger = false;

        gravityDir = Vector3.down;
        sideDir = SideFromGravity(gravityDir);
        startPosition = transform.position;
    }

    void Start()
    {
        Respawn();
    }

    public void Respawn()
    {
        transform.position = startPosition;
        gravityDir = Vector3.down;
        sideDir = SideFromGravity(gravityDir);
        gravVel = 0f;
        strafeVel = 0f;
        switchTimer = 0f;
        grounded = true;
    }

    void Update()
    {
        if (R_GameManager.IsGameOver) return;   // freeze on win/lose

        float dt = Time.deltaTime;
        if (switchTimer > 0f) switchTimer -= dt;

        // --- input (new Input System) ---
        float strafeInput = 0f;
        bool jumpPressed = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) strafeInput -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) strafeInput += 1f;
            jumpPressed = kb.spaceKey.wasPressedThisFrame
                       || kb.wKey.wasPressedThisFrame
                       || kb.upArrowKey.wasPressedThisFrame;
        }

        // --- strafe (snappy on ground, floaty in air) ---
        float accel = grounded ? groundStrafeAccel : airStrafeAccel;
        strafeVel = Mathf.MoveTowards(strafeVel, strafeInput * strafeSpeed, accel * dt);

        // --- jump ---
        if (grounded && jumpPressed)
        {
            gravVel = -jumpSpeed;
            grounded = false;
            Jumped?.Invoke();
        }

        // --- gravity ---
        gravVel = Mathf.Min(gravVel + gravity * dt, terminalFall);

        // --- integrate ---
        Vector3 delta = Vector3.forward * (runSpeed * dt)
                      + sideDir * (strafeVel * dt)
                      + gravityDir * (gravVel * dt);
        transform.position += delta;

        // airborne (from a jump): the region you drift into decides the floor
        if (!grounded && switchTimer <= 0f) UpdateGravityRegion();

        // grounded: wrap onto a side wall the moment you touch it
        if (grounded) GroundedWalls();

        GroundAndFallCheck();
    }

    // Run into a side wall -> that wall becomes the floor. If you're not pushing
    // into the wall, just clamp so you stay on the current surface.
    void GroundedWalls()
    {
        float limit = tunnelRadius - playerRadius;
        float sidePos = Vector3.Dot(transform.position, sideDir);
        const float edgeEps = 0.02f;
        const float velEps = 0.1f;

        if (sidePos >= limit - edgeEps && strafeVel > velEps)
        {
            SwitchGravity(sideDir);       // +side wall becomes the new floor
            grounded = false;
        }
        else if (sidePos <= -limit + edgeEps && strafeVel < -velEps)
        {
            SwitchGravity(-sideDir);      // -side wall becomes the new floor
            grounded = false;
        }
        else
        {
            float clamped = Mathf.Clamp(sidePos, -limit, limit);
            if (!Mathf.Approximately(clamped, sidePos))
            {
                transform.position += sideDir * (clamped - sidePos);
                strafeVel = 0f;
            }
        }
    }

    // Divide the square cross-section by its diagonals into 4 wall regions.
    void UpdateGravityRegion()
    {
        Vector3 g = RegionGravity(transform.position.x, transform.position.y);
        if (g != gravityDir) SwitchGravity(g);
    }

    Vector3 RegionGravity(float x, float y)
    {
        if (y <= -Mathf.Abs(x)) return Vector3.down;   // floor
        if (y >= Mathf.Abs(x)) return Vector3.up;     // ceiling
        if (x >= Mathf.Abs(y)) return Vector3.right;  // right wall
        return Vector3.left;                           // left wall
    }

    // Swap gravity but keep world-space momentum so switches feel smooth.
    void SwitchGravity(Vector3 newG)
    {
        Vector3 worldVel = sideDir * strafeVel + gravityDir * gravVel;
        gravityDir = newG;
        sideDir = SideFromGravity(newG);
        strafeVel = Vector3.Dot(worldVel, sideDir);
        gravVel = Vector3.Dot(worldVel, gravityDir);
        switchTimer = switchLock;
    }

    void GroundAndFallCheck()
    {
        // fell out through a gap?
        float depth = Vector3.Dot(transform.position, gravityDir);
        if (depth > tunnelRadius + fallOutMargin)
        {
            grounded = false;
            R_GameManager.PlayerDied();
            return;
        }

        // look for floor under the feet (length grows with fall speed to beat tunneling)
        float castLen = playerRadius + 0.2f + Mathf.Max(0f, gravVel) * Time.deltaTime;
        if (Physics.Raycast(transform.position, gravityDir, out RaycastHit hit,
                            castLen, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.attachedRigidbody != rb && gravVel >= 0f)
            {
                transform.position = hit.point - gravityDir * playerRadius; // rest on surface
                gravVel = 0f;
                grounded = true;
                return;
            }
        }

        if (grounded) grounded = false;   // ran off a ledge
    }

    // Screen-right for a given "down": rotate gravity 90 in the XY plane.
    static Vector3 SideFromGravity(Vector3 g)
    {
        return new Vector3(-g.y, g.x, 0f);
    }
}