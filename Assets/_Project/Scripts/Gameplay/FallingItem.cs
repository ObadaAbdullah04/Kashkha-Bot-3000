using UnityEngine;

/// <summary>
/// Attached to falling Eidia/Ma'amoul prefabs (World Space 2D sprites).
/// Manages its own collision detection and falling movement.
/// 
/// WORLD SPACE EDITION:
/// - Uses Transform (not RectTransform)
/// - Falls via Translate in world space
/// - Self-destructs when below camera bounds
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class FallingItem : MonoBehaviour
{
    [Header("Item Type")]
    [Tooltip("True = Eidia (good, catch it). False = Ma'amoul (bad, avoid it)")]
    public bool isEidia = true;

    [Header("Movement")]
    [Tooltip("How fast this item falls (world units per second)")]
    public float fallSpeed = 8f;

    [Header("References")]
    [Tooltip("Cached BoxCollider2D for performance")]
    private BoxCollider2D _collider;

    private bool _isCaught = false;
    private float _bottomY = -10f;
    // private bool _hasBottomBound = false;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        // Cache bottom boundary once to avoid ViewportToWorldPoint every frame
        if (Camera.main != null)
        {
            _bottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.1f, 0)).y;
            // _hasBottomBound = true;
        }
    }

    /// <summary>
    /// Called by Unity Physics when this item enters a trigger collider.
    /// Checks if the collider belongs to the player basket.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCaught) return;

        // Check if we hit the player basket
        if (collision.CompareTag("Player"))
        {
            _isCaught = true;

            // Cache reference BEFORE destroying to avoid race condition
            CatchMiniGame catchGame = CatchMiniGame.Instance;
            bool isEidiaCapture = isEidia;

            // Disable collider and destroy this item
            if (_collider != null) _collider.enabled = false;
            Destroy(gameObject);

            // CRITICAL: Notify manager AFTER disabling collider but object still exists
            if (catchGame != null)
            {
                catchGame.OnItemCaught(isEidiaCapture);
            }
        }
    }

    /// <summary>
    /// Task 3: Update loop - handles falling movement and off-screen cleanup.
    /// Uses world space Transform movement.
    /// </summary>
    private void Update()
    {
        // Move item down in world space
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Check if item has fallen below camera bounds
        if (transform.position.y < _bottomY)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Optional: Visual debugging in Editor.
    /// Shows the collider bounds in Scene View when selected.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw collider bounds
        Gizmos.color = isEidia ? new Color(1f, 0.84f, 0f, 0.5f) : new Color(0.6f, 0.3f, 0.1f, 0.5f);

        if (_collider != null)
        {
            Gizmos.DrawWireCube(
                transform.position + (Vector3)_collider.offset,
                _collider.size
            );
        }
        else
        {
            // Fallback: draw a box at transform position
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
