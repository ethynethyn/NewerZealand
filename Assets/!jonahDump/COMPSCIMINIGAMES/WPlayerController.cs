using UnityEngine;

/// <summary>
/// The red square. Moves with the arrow keys, loses a heart and respawns when it
/// touches a blue ball, and wins when it reaches the green end square.
/// Detection is tag-based, so the balls and the goal just need a trigger collider
/// and the right tag — no script on them.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Move speed in world units per second.")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("References")]
    [Tooltip("The level's GameManager. Leave empty to auto-find it in the scene.")]
    [SerializeField] private GameManager gameManager;

    [Header("Tags it reacts to")]
    [Tooltip("Tag on the blue balls / obstacles.")]
    [SerializeField] private string obstacleTag = "Obstacle";
    [Tooltip("Tag on the green end square.")]
    [SerializeField] private string goalTag = "Goal";

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool inputLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;     // top-down: never fall
        rb.freezeRotation = true; // never spin when bumping a wall

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (inputLocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Arrow keys (WASD also work with Unity's default Input settings).
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = Vector2.ClampMagnitude(moveInput, 1f); // stop diagonals being faster
    }

    void FixedUpdate()
    {
        // MovePosition respects wall colliders, so the square is blocked by walls.
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null) return;

        if (other.CompareTag(obstacleTag))
            gameManager.PlayerHit();
        else if (other.CompareTag(goalTag))
            gameManager.ReachedGoal();
    }

    /// <summary>Teleport the player. Used for the initial spawn and every respawn.</summary>
    public void MoveTo(Vector2 position)
    {
        transform.position = position;
        rb.position = position;
        moveInput = Vector2.zero;
    }

    /// <summary>Freeze / unfreeze movement (called on win or loss).</summary>
    public void LockInput(bool locked) => inputLocked = locked;
}
