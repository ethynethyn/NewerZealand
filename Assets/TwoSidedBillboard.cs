using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteBillboardTwoSided : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public NavMeshAgent agent;

    [Header("Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Settings")]
    public bool invertFacing = false;

    [Header("Optional Override (CLASS FOCUS)")]
    public Transform focusOverride;
    public bool useFocusOverride = false;

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
        // BILLBOARD FACING CAMERA
        // -------------------------
        Vector3 toCam = cameraTransform.position - transform.position;
        toCam.y = 0f;

        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(toCam).eulerAngles.y, 0f);

        // -------------------------
        // DETERMINE FORWARD DIRECTION
        // -------------------------
        Vector3 forward;

        if (useFocusOverride && focusOverride != null)
        {
            forward = focusOverride.position - transform.position;
        }
        else if (agent != null && agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveDir = agent.velocity.normalized;
            forward = lastMoveDir;
        }
        else
        {
            forward = transform.parent.forward;
        }

        forward.y = 0f;

        // -------------------------
        // FRONT / BACK CHECK
        // -------------------------
        Vector3 toCamera = (cameraTransform.position - transform.position).normalized;
        toCamera.y = 0f;

        float dot = Vector3.Dot(forward.normalized, toCamera);

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

    // 🔥 CALL THIS FROM NPC WHEN SITTING
    public void SetFocus(Transform focus)
    {
        focusOverride = focus;
        useFocusOverride = true;
    }

    public void ClearFocus()
    {
        useFocusOverride = false;
        focusOverride = null;
    }
}