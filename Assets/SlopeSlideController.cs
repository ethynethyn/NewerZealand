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

    // ---------------- GRIND EXIT STATE ----------------
    private bool justExitedGrind;
    private float exitSpeedCap;

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
        isGrinding = true;
        justExitedGrind = false;

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
        isGrinding = false;
        justExitedGrind = true;

        Vector3 flat = new Vector3(momentum.x, 0f, momentum.z);

        exitSpeedCap = Mathf.Max(flat.magnitude, maxSpeed);
        currentSpeedCap = exitSpeedCap;
    }

    // =========================================================
    // SKATEBOARD ROTATION VISUAL
    // =========================================================

    private void UpdateSkateboardRotation()
    {
        if (skateboardObject == null) return;

        float a = Input.GetKey(KeyCode.A) ? -1f : 0f;
        float d = Input.GetKey(KeyCode.D) ? 1f : 0f;
        float w = Input.GetKey(KeyCode.W) ? 1f : 0f;

        // Base lateral input
        float turnInput = a + d;

        // W pulls stance back toward forward (reduces extreme turning)
        float forwardBias = 1f - (w * 0.5f);

        // Apply bias so W+A / W+D naturally land in-between
        turnInput *= forwardBias;

        // Clamp so we stay stable
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);

        // Convert to angle:
        // 0 = forward
        // ±0.5 = half turn
        // ±1 = full turn
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

        // ---------------- SKATEBOARD VISUAL TOGGLE ----------------
        if (skateboardObject != null)
        {
            if (skateboardObject.activeSelf != isSliding)
                skateboardObject.SetActive(isSliding);
        }

        // rotation update (only visual)
        UpdateSkateboardRotation();

        // ---------------- GRIND MODE ----------------
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

            Vector3 flat = Vector3.Project(momentum, grindDirection);
            flat = Vector3.ClampMagnitude(flat, downhillMaxSpeed);
            momentum = flat;

            Vector3 move = momentum;
            move.y = verticalVelocity;
            return move;
        }

        // ---------------- SLIDE START ----------------
        if (isSliding && !wasSlidingLastFrame)
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            momentum = v;
        }

        // ---------------- SLIDE END ----------------
        if (!isSliding && !justExitedGrind)
        {
            momentum = Vector3.Lerp(momentum, Vector3.zero, friction * Time.deltaTime);
            currentSpeedCap = Mathf.Lerp(currentSpeedCap, maxSpeed, speedReturnLerp * Time.deltaTime);

            Vector3 move = baseMove;
            move.y = verticalVelocity;

            wasSlidingLastFrame = false;
            return move;
        }

        // ---------------- GRIND EXIT RECOVERY ----------------
        if (justExitedGrind)
        {
            Vector3 flat = new Vector3(momentum.x, 0f, momentum.z);

            float exitTargetCap = maxSpeed;
            currentSpeedCap = Mathf.Lerp(currentSpeedCap, exitTargetCap, speedReturnLerp * Time.deltaTime);

            float speed = flat.magnitude;
            speed = Mathf.Lerp(speed, currentSpeedCap, speedReturnLerp * Time.deltaTime);

            if (speed <= maxSpeed + 0.1f)
                justExitedGrind = false;

            momentum = flat.normalized * speed;
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
        currentSpeedCap = Mathf.Lerp(currentSpeedCap, targetCap, speedReturnLerp * Time.deltaTime);

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