using UnityEngine;

/// <summary>
/// PHASE 5C (REVISED): Individual obstacle marker for Path-Drawing Maze.
/// 
/// This is now a SIMPLE marker component. The actual collision detection
/// is handled by PathDrawingGame.cs using the Collider2D on this GameObject.
///
/// SETUP:
/// 1. Create GameObject (Sprite or empty with Collider2D)
/// 2. Add CircleCollider2D or BoxCollider2D (isTrigger = false)
/// 3. Add this script (optional, for labeling/debugging)
/// 4. Place manually in scene, assign to PathDrawingGame.Obstacles[]
/// </summary>
public class Obstacle : MonoBehaviour
{
}
