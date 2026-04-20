using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SlopeSlideController : MonoBehaviour
{
    [Header("Momentum")]
    public float acceleration = 12f;
    public float airAcceleration = 4f;
    public float friction = 6f;

    [Header("Speed Caps")]
    public float maxSpeed = 12f;
    public float downhillMaxSpeed = 25f;
    public float speedReturnLerp = 6f;

    [Header("Slope")]
    public float downhillForce = 20f;
    public float uphillDrag = 25f;
    public float minSlopeAngle = 5f;
    public float rayLength = 1.5f;
    public float slopeSmooth = 8f;

    [Header("Grind")]
    public float grindSpeed = 18f;
    public float grindSpeedLerp = 8f;
    public float grindRotationSpeed = 12f;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftShift;

    [Header("Visuals")]
    public GameObject skateboardObject;

    [Header("Skateboard Rotation")]
    public float boardRotationLerp = 10f;
    public float maxTurnAngle = 90f;

    private CharacterController controller;

    private Vector3 momentum;
    private Vector3 smoothSlopeDir;

    private float currentSpeedCap;
    private bool isOnDownhill;
    private bool wasSlidingLastFrame;

    // ---------------- GRIND ----------------
    private bool isGrinding;
    private Vector3 grindDirection;
    private float currentGrindSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentSpeedCap = maxSpeed;

        if (skateboardObject != null)
            skateboardObject.SetActive(false);
    }

    // =========================================================
    // GRIND ENTRY / EXIT
    // =========================================================

    public void StartGrind(Vector3 start, Vector3 end)
    {
        // Require Shift
        if (!Input.GetKey(slideKey)) return;

        isGrinding = true;

        Vector3 railDir = (end - start).normalized;

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        float dotForward = Vector3.Dot(velocity, railDir);
        float dotBackward = Vector3.Dot(velocity, -railDir);

        grindDirection = (dotForward > dotBackward) ? railDir : -railDir;

        momentum = Vector3.Project(velocity, grindDirection);
        currentGrindSpeed = momentum.magnitude;
    }

    public void EndGrind()
    {
        if (!isGrinding) return;

        isGrinding = false;

        // 🔥 THIS IS THE IMPORTANT PART
        // Use current speed as temporary cap
        Vector3 flat = new Vector3(momentum.x, 0f, momentum.z);
        currentSpeedCap = Mathf.Max(flat.magnitude, maxSpeed);
    }

    // =========================================================
    // SKATEBOARD VISUAL
    // =========================================================

    private void UpdateSkateboardRotation()
    {
        if (skateboardObject == null) return;

        float a = Input.GetKey(KeyCode.A) ? -1f : 0f;
        float d = Input.GetKey(KeyCode.D) ? 1f : 0f;
        float w = Input.GetKey(KeyCode.W) ? 1f : 0f;

        float turnInput = a + d;
        float forwardBias = 1f - (w * 0.5f);
        turnInput *= forwardBias;

        turnInput = Mathf.Clamp(turnInput, -1f, 1f);

        float targetYaw = turnInput * maxTurnAngle;

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);

        skateboardObject.transform.localRotation = Quaternion.Slerp(
            skateboardObject.transform.localRotation,
            targetRotation,
            boardRotationLerp * Time.deltaTime
        );
    }

    // =========================================================
    // MAIN MOVEMENT
    // =========================================================

    public Vector3 ModifyMovement(Vector3 baseMove, float verticalVelocity, bool grounded)
    {
        bool isSliding = Input.GetKey(slideKey);

        // Exit grind if Shift released
        if (isGrinding && !isSliding)
        {
            EndGrind();
        }

        // ---------------- VISUAL ----------------
        if (skateboardObject != null)
            skateboardObject.SetActive(isSliding);

        UpdateSkateboardRotation();

        // ---------------- GRIND ----------------
        if (isGrinding)
        {
            currentGrindSpeed = Mathf.Lerp(
                currentGrindSpeed,
                grindSpeed,
                grindSpeedLerp * Time.deltaTime
            );

            momentum = grindDirection * currentGrindSpeed;

            Quaternion targetRot = Quaternion.LookRotation(grindDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                grindRotationSpeed * Time.deltaTime
            );

            Vector3 move = momentum;
            move.y = verticalVelocity;
            return move;
        }

        // ---------------- WALK ----------------
        if (!isSliding)
        {
            // instantly return to walk cap
            currentSpeedCap = maxSpeed;

            momentum = Vector3.Lerp(momentum, Vector3.zero, friction * Time.deltaTime);

            Vector3 move = baseMove;
            move.y = verticalVelocity;

            wasSlidingLastFrame = false;
            return move;
        }

        // ---------------- SLIDE START ----------------
        if (isSliding && !wasSlidingLastFrame)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            momentum = v;
        }

        // ---------------- SLOPE DETECTION ----------------
        RaycastHit hit;
        Vector3 slopeDir = Vector3.zero;
        float slopeAngle = 0f;

        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (Physics.Raycast(origin, Vector3.down, out hit, rayLength))
        {
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
        }

        smoothSlopeDir = Vector3.Lerp(smoothSlopeDir, slopeDir, Time.deltaTime * slopeSmooth);

        isOnDownhill =
            slopeAngle > minSlopeAngle &&
            Vector3.Dot(smoothSlopeDir, transform.forward) > 0.2f;

        float targetCap = isOnDownhill ? downhillMaxSpeed : maxSpeed;

        // 🔥 KEY LOGIC: only reduce cap if above target
        if (currentSpeedCap > targetCap)
        {
            currentSpeedCap = Mathf.Lerp(currentSpeedCap, targetCap, speedReturnLerp * Time.deltaTime);
        }
        else
        {
            currentSpeedCap = targetCap;
        }

        // ---------------- MOMENTUM ----------------
        if (grounded)
        {
            momentum += baseMove * acceleration * Time.deltaTime;

            if (slopeAngle > minSlopeAngle)
            {
                float dot = Vector3.Dot(smoothSlopeDir, transform.forward);

                if (dot > 0)
                    momentum += smoothSlopeDir * downhillForce * Time.deltaTime;
                else
                    momentum += smoothSlopeDir * uphillDrag * Time.deltaTime;
            }
        }
        else
        {
            momentum += baseMove * airAcceleration * Time.deltaTime;
        }

        // ---------------- CLAMP ----------------
        Vector3 horizontal = new Vector3(momentum.x, 0, momentum.z);
        horizontal = Vector3.ClampMagnitude(horizontal, currentSpeedCap);

        momentum.x = horizontal.x;
        momentum.z = horizontal.z;

        Vector3 finalMove = momentum;
        finalMove.y = verticalVelocity;

        wasSlidingLastFrame = true;

        return finalMove;
    }
}