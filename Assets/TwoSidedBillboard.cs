using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteBillboardTwoSided : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public NavMeshAgent agent; // IMPORTANT

    [Header("Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Settings")]
    public bool invertFacing = false;

    private SpriteRenderer spriteRenderer;
    private Vector3 lastMoveDir;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // -------------------------
        // BILLBOARD ROTATION
        // -------------------------
        Vector3 directionToCamera = cameraTransform.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(directionToCamera);
        transform.rotation = Quaternion.Euler(0f, lookRot.eulerAngles.y, 0f);

        // -------------------------
        // GET CHARACTER FORWARD (NOT BILLBOARD)
        // -------------------------
        Vector3 forward;

        if (agent != null && agent.velocity.sqrMagnitude > 0.01f)
        {
            // use movement direction
            lastMoveDir = agent.velocity.normalized;
        }

        forward = lastMoveDir.sqrMagnitude > 0.01f ? lastMoveDir : transform.parent.forward;

        forward.y = 0f;

        // -------------------------
        // FRONT / BACK CHECK
        // -------------------------
        Vector3 toCamera = (cameraTransform.position - transform.position).normalized;
        toCamera.y = 0f;

        float dot = Vector3.Dot(forward, toCamera);

        bool isFront = invertFacing ? dot < 0f : dot > 0f;

        // -------------------------
        // SPRITE SWITCH
        // -------------------------
        Sprite target = isFront ? frontSprite : backSprite;

        if (spriteRenderer.sprite != target)
        {
            spriteRenderer.sprite = target;
        }
    }
}