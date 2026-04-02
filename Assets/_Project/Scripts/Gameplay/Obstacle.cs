using UnityEngine;

/// <summary>
/// PHASE 5C: Individual obstacle component for Path-Drawing Maze.
/// Attach to obstacle prefabs (Nosy Neighbor, Traffic, Cat, etc.).
/// 
/// SETUP:
/// 1. Create empty GameObject or Sprite
/// 2. Add CircleCollider2D (isTrigger = true, radius = 0.5)
/// 3. Add this script
/// 4. Assign obstacle sprite (optional)
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Display name for debugging")]
    [SerializeField] private string obstacleName = "Obstacle";
    
    [Tooltip("Collision radius (must match CircleCollider2D)")]
    [SerializeField] private float collisionRadius = 0.5f;
    
    [Tooltip("Time penalty when player hits this obstacle")]
    [SerializeField] private float timePenalty = 5f;
    
    private void Start()
    {
        // Auto-add CircleCollider2D if not present
        if (GetComponent<CircleCollider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = collisionRadius;
            collider.isTrigger = false; // MUST be false for collision detection!
            Debug.Log($"[Obstacle] Added CircleCollider2D (radius: {collisionRadius}) to {obstacleName}");
        }
        else
        {
            // Ensure collider is NOT a trigger
            CircleCollider2D existing = GetComponent<CircleCollider2D>();
            existing.isTrigger = false;
            Debug.Log($"[Obstacle] Using existing CircleCollider2D on {obstacleName}");
        }
        
        // Auto-add SpriteRenderer if not present
        if (GetComponent<SpriteRenderer>() == null)
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = Color.magenta; // Purple placeholder
        }
    }
    
    /// <summary>
    /// Gets the time penalty for hitting this obstacle.
    /// </summary>
    public float GetTimePenalty() => timePenalty;
}
