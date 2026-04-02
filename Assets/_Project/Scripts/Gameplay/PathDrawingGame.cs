using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;

/// <summary>
/// Obstacle spawn patterns for predictable difficulty.
/// </summary>
public enum ObstacleSpawnPattern
{
    Diagonal,     // Diagonal line from start to end
    ZigZag,       // Zig-zag pattern across the path
    Cluster,      // Clustered in the middle
    Spread,       // Evenly spread across the map
    Custom        // Custom positions (set in editor)
}

/// <summary>
/// PHASE 5C: Path-Drawing Maze Mini-Game Manager.
/// 
/// GAMEPLAY:
/// - Player draws a path from Start to End using touch/mouse
/// - Line follows finger in real-time (LineRenderer)
/// - Avoid obstacle colliders or lose time
/// - Reach goal before time runs out to win
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create Canvas → Add this script to empty GameObject
/// 2. Add LineRenderer component to empty GameObject (child of Canvas)
/// 3. Assign UI references (TimerText, ResultText)
/// 4. Create 3 obstacle prefabs and assign to array
/// 5. Set StartPoint and EndPoint positions
/// </summary>
public class PathDrawingGame : MonoBehaviour
{
    public static PathDrawingGame Instance { get; private set; }

    #region Inspector Fields

    [Header("Map Configuration")]
    [Tooltip("Background sprite for the map (placeholder: colored Image)")]
    [SerializeField] private Sprite mapBackground;
    
    [Tooltip("Obstacle prefabs (create 3: NosyNeighbor, Traffic, Cat)")]
    [SerializeField] private GameObject[] obstaclePrefabs = new GameObject[3];
    
    [Header("Line Settings")]
    [Tooltip("LineRenderer component for drawing path")]
    [SerializeField] private LineRenderer pathLine;
    
    [Tooltip("Width of the drawn line")]
    [SerializeField] private float lineWidth = 0.15f;
    
    [Tooltip("Color of the path line")]
    [SerializeField] private Color lineColor = Color.green;
    
    [Tooltip("Maximum points in the line (performance)")]
    [SerializeField] private int maxLinePoints = 200;
    
    [Header("Game Settings")]
    [Tooltip("Base time limit in seconds")]
    [SerializeField] private float timeLimit = 30f;
    
    [Tooltip("Time penalty for hitting obstacle")]
    [SerializeField] private float collisionPenalty = 5f;
    
    [Tooltip("Battery hits allowed before game over")]
    [SerializeField] private int maxHits = 4;
    
    [Tooltip("Cooldown duration after hitting obstacle (seconds)")]
    [SerializeField] private float collisionCooldown = 1f;
    
    [Tooltip("Number of obstacles to spawn")]
    [SerializeField] private int obstacleCount = 5;
    
    [Tooltip("Obstacle spawn pattern")]
    [SerializeField] private ObstacleSpawnPattern spawnPattern = ObstacleSpawnPattern.Diagonal;
    
    [Tooltip("Starting position (player/robot) - MUST start drawing from here")]
    [SerializeField] private Vector2 startPoint = new Vector2(-4f, -4f);
    
    [Tooltip("Ending position (house door) - MUST reach here to win")]
    [SerializeField] private Vector2 endPoint = new Vector2(4f, 4f);
    
    [Tooltip("Distance to goal for win detection")]
    [SerializeField] private float goalDistance = 1.5f;
    
    [Tooltip("Radius around start point where drawing must begin")]
    [SerializeField] private float startRadius = 1.5f;
    
    [Tooltip("Layer mask for obstacle collision detection")]
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    
    [Tooltip("Distance between collision check points (smaller = more accurate but slower)")]
    [SerializeField] private float collisionCheckInterval = 0.2f;
    
    [Header("Visual Markers")]
    [Tooltip("Show start/end markers in editor")]
    [SerializeField] private bool showGizmos = true;
    
    [Tooltip("Start point marker sprite (optional)")]
    [SerializeField] private Sprite startMarkerSprite;
    
    [Tooltip("End point marker sprite (optional)")]
    [SerializeField] private Sprite endMarkerSprite;
    
    [Header("UI References")]
    [SerializeField] private RTLTextMeshPro timerText;
    
    [SerializeField] private RTLTextMeshPro resultText;
    
    [SerializeField] private GameObject resultPanel;
    
    [SerializeField] private RTLTextMeshPro hitsRemainingText; // NEW: Show battery/hits

    #endregion

    #region Private Fields

    private bool isDrawing = false;
    private bool hasStartedDrawing = false; // Must start from startPoint
    private bool isCooldown = false; // NEW: Cooldown after collision
    private float cooldownTimer = 0f;
    private List<Vector3> linePoints = new List<Vector3>();
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<Obstacle> alreadyHitObstacles = new List<Obstacle>(); // Prevent double penalty
    private int hitsRemaining; // NEW: Battery/hits system
    private float timeRemaining;
    private Camera mainCam;
    private bool gameEnded = false;
    
    // Visual markers
    private GameObject startMarker;
    private GameObject endMarker;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            // Fallback: find any enabled camera
            mainCam = Camera.allCameras[0];
        }
        
        InitializeGame();
    }

    private void Update()
    {
        if (!isDrawing || gameEnded) return;
        
        // Handle cooldown period after collision
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isCooldown = false;
                hasStartedDrawing = false; // Force player to start from green circle again
                Debug.Log("[PathGame] Cooldown ended - click on GREEN circle to continue!");
            }
            return; // Don't process input during cooldown
        }
        
        // Update timer
        timeRemaining -= Time.deltaTime;
        
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
        
        // Check time loss
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            EndGame(false);
            return;
        }
        
        // Panic mode: Red text when <5s
        if (timerText != null && timeRemaining < 5f)
        {
            timerText.color = Color.red;
        }
        
        // Get input position and draw
        if (Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved))
        {
            Vector3 inputPos = GetInputPosition();
            
            // Check if player hasn't started drawing yet
            if (!hasStartedDrawing)
            {
                // Check if clicking near start point
                float distToStart = Vector2.Distance(inputPos, startPoint);
                if (distToStart <= startRadius)
                {
                    // Valid start! Begin drawing
                    hasStartedDrawing = true;
                    linePoints.Clear();
                    linePoints.Add(inputPos);
                    Debug.Log("[PathGame] Drawing started from green zone!");
                }
                else
                {
                    // Show hint
                    if (Time.frameCount % 30 == 0) // Every ~0.5 seconds
                    {
                        Debug.Log("[PathGame] WARNING: Click on GREEN circle to start drawing!");
                    }
                    return; // Don't draw yet
                }
            }
            else
            {
                // Already drawing - add point if far enough from last
                if (linePoints.Count == 0 || 
                    Vector3.Distance(linePoints[linePoints.Count - 1], inputPos) > 0.1f)
                {
                    // Check max points
                    if (linePoints.Count >= maxLinePoints)
                    {
                        // Remove oldest point
                        linePoints.RemoveAt(0);
                    }
                    
                    linePoints.Add(inputPos);
                    UpdateLineRenderer();
                    CheckCollisions();
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup obstacles
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }
    }

    #endregion

    #region Game Logic

    private void InitializeGame()
    {
        isDrawing = true;
        hasStartedDrawing = false;
        gameEnded = false;
        timeRemaining = timeLimit;
        hitsRemaining = maxHits; // Reset hits
        linePoints.Clear();
        alreadyHitObstacles.Clear(); // Reset hit obstacles
        
        // Setup line renderer
        if (pathLine != null)
        {
            pathLine.startWidth = lineWidth;
            pathLine.endWidth = lineWidth;
            pathLine.startColor = lineColor;
            pathLine.endColor = lineColor;
            pathLine.positionCount = 0;
            pathLine.useWorldSpace = true;
        }
        
        // Hide result panel
        if (resultPanel != null)
            resultPanel.SetActive(false);
        
        // Update hits UI
        UpdateHitsUI();
        
        // Create visual markers for start and end points
        CreateStartEndMarkers();
        
        // Spawn obstacles
        SpawnObstacles();
        
        Debug.Log($"[PathGame] Game initialized! Time: {timeLimit}s, Obstacles: {obstacleCount}, Hits: {maxHits}");
    }
    
    /// <summary>
    /// Updates the hits remaining UI display.
    /// </summary>
    private void UpdateHitsUI()
    {
        if (hitsRemainingText != null)
        {
            // Show hits as + symbols or number
            string display = "";
            for (int i = 0; i < hitsRemaining; i++) display += "+ ";
            hitsRemainingText.text = display;
            hitsRemainingText.color = hitsRemaining <= 1 ? Color.red : Color.green;
        }
    }
    
    /// <summary>
    /// Creates visual markers for start and end points.
    /// </summary>
    private void CreateStartEndMarkers()
    {
        // Start marker (Green circle)
        startMarker = new GameObject("StartPoint");
        startMarker.transform.position = startPoint;
        SpriteRenderer startSR = startMarker.AddComponent<SpriteRenderer>();
        startSR.color = Color.green;
        
        // Add circle to show start radius
        CircleCollider2D startCircle = startMarker.AddComponent<CircleCollider2D>();
        startCircle.radius = startRadius;
        startCircle.isTrigger = true;
        
        // End marker (Red circle)
        endMarker = new GameObject("EndPoint");
        endMarker.transform.position = endPoint;
        SpriteRenderer endSR = endMarker.AddComponent<SpriteRenderer>();
        endSR.color = Color.red;
        
        CircleCollider2D endCircle = endMarker.AddComponent<CircleCollider2D>();
        endCircle.radius = goalDistance;
        endCircle.isTrigger = true;
        
        Debug.Log("[PathGame] ===== INSTRUCTIONS =====");
        Debug.Log($"[PathGame] START: Click on GREEN circle at ({startPoint.x:F1}, {startPoint.y:F1})");
        Debug.Log($"[PathGame] END: Draw to RED circle at ({endPoint.x:F1}, {endPoint.y:F1})");
        Debug.Log($"[PathGame] AVOID: Purple obstacles (you have {maxHits} hits)");
        Debug.Log("[PathGame] =========================");
    }

    private Vector3 GetInputPosition()
    {
        // Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPos = touch.position;
            touchPos.z = 10f; // Distance from camera
            return mainCam.ScreenToWorldPoint(touchPos);
        }
        
        // Mouse fallback (editor)
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        return mainCam.ScreenToWorldPoint(mousePos);
    }

    private void UpdateLineRenderer()
    {
        if (pathLine == null) return;
        
        pathLine.positionCount = linePoints.Count;
        pathLine.SetPositions(linePoints.ToArray());
    }

    private void CheckCollisions()
    {
        if (linePoints.Count < 2) return;
        
        // Check collision along the entire line
        for (int i = 0; i < linePoints.Count - 1; i++)
        {
            Vector3 start = linePoints[i];
            Vector3 end = linePoints[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            
            // Check multiple points along the segment
            int checkPoints = Mathf.CeilToInt(segmentLength / collisionCheckInterval);
            
            for (int j = 0; j <= checkPoints; j++)
            {
                float t = j / (float)checkPoints;
                Vector3 checkPos = Vector3.Lerp(start, end, t);
                
                // Simple circle overlap check - no layer mask needed
                Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, lineWidth);
                
                foreach (var hit in hits)
                {
                    Obstacle obstacle = hit.GetComponent<Obstacle>();
                    if (obstacle != null && !alreadyHitObstacles.Contains(obstacle))
                    {
                        OnPathCollision(obstacle.gameObject);
                        alreadyHitObstacles.Add(obstacle);
                        return;
                    }
                }
            }
        }
        
        // Check if reached goal
        if (linePoints.Count > 0)
        {
            float distanceToGoal = Vector2.Distance(linePoints[linePoints.Count - 1], endPoint);
            if (distanceToGoal <= goalDistance)
            {
                EndGame(true);
            }
        }
    }

    private void OnPathCollision(GameObject obstacle)
    {
        if (gameEnded || isCooldown) return;
        
        // Apply time penalty
        timeRemaining -= collisionPenalty;
        
        // Apply hit penalty
        hitsRemaining--;
        UpdateHitsUI();
        
        // REJECT: Clear the ENTIRE line - player must start over!
        linePoints.Clear();
        UpdateLineRenderer();
        
        // Start cooldown period - player cannot draw during this time
        isCooldown = true;
        cooldownTimer = collisionCooldown;
        
        // Visual feedback - flash line red
        if (pathLine != null)
        {
            Color originalColor = pathLine.startColor;
            pathLine.startColor = Color.red;
            pathLine.endColor = Color.red;
            Invoke(nameof(ResetLineColor), 0.3f);
        }
        
        // Screen shake
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeWrongAnswer();
        }
        
        // Show warning message
        Debug.Log($"[PathGame] HIT OBSTACLE! Cooldown: {collisionCooldown}s - Line cleared!");
        Debug.Log($"[PathGame] Collision! -{collisionPenalty}s, Hits: {hitsRemaining}/{maxHits}");
        
        // Check for game over (no hits remaining)
        if (hitsRemaining <= 0)
        {
            Debug.Log("[PathGame] Game Over - No hits remaining!");
            EndGame(false);
        }
    }

    private void ResetLineColor()
    {
        if (pathLine != null)
        {
            pathLine.startColor = lineColor;
            pathLine.endColor = lineColor;
        }
    }

    private void SpawnObstacles()
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 spawnPos = GetObstaclePositionByPattern(i, obstacleCount);
            
            // Pick random obstacle type
            if (obstaclePrefabs.Length > 0)
            {
                int prefabIndex = Random.Range(0, obstaclePrefabs.Length);
                GameObject obstacle = Instantiate(obstaclePrefabs[prefabIndex], spawnPos, Quaternion.identity);
                activeObstacles.Add(obstacle);
            }
        }
    }
    
    /// <summary>
    /// Gets obstacle position based on spawn pattern.
    /// </summary>
    private Vector3 GetObstaclePositionByPattern(int index, int total)
    {
        Vector3 pathDirection = (endPoint - startPoint).normalized;
        
        switch (spawnPattern)
        {
            case ObstacleSpawnPattern.Diagonal:
                // Diagonal line from start to end with alternating offset
                {
                    float t = index / (float)total;
                    Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                    float offset1 = (index % 2 == 0) ? 1.5f : -1.5f;
                    Vector3 perp1 = new Vector3(-pathDirection.y, pathDirection.x, 0);
                    return pos + perp1 * offset1;
                }
                
            case ObstacleSpawnPattern.ZigZag:
                // Zig-zag pattern across the path
                {
                    float t = index / (float)total;
                    Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                    float zigZag = Mathf.Sin(index * 1.5f) * 2f;
                    Vector3 perp2 = new Vector3(-pathDirection.y, pathDirection.x, 0);
                    return pos + perp2 * zigZag;
                }
                
            case ObstacleSpawnPattern.Cluster:
                // Clustered in the middle of the path
                {
                    float t = 0.3f + (index / (float)total) * 0.4f; // Middle 40% of path
                    Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                    float spread1 = (index - total / 2f) * 0.8f;
                    Vector3 perp3 = new Vector3(-pathDirection.y, pathDirection.x, 0);
                    return pos + perp3 * spread1;
                }
                
            case ObstacleSpawnPattern.Spread:
                // Evenly spread across the map
                {
                    float t = index / (float)total;
                    Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                    float spread2 = (index % 3 - 1) * 2f; // -2, 0, or 2
                    Vector3 perp4 = new Vector3(-pathDirection.y, pathDirection.x, 0);
                    return pos + perp4 * spread2;
                }
                
            default:
                // Random position between start and end
                Vector3 directPath = Vector3.Lerp(startPoint, endPoint, index / (float)total);
                Vector3 perp5 = new Vector3(-pathDirection.y, pathDirection.x, 0);
                float randomOffset = Random.Range(-2f, 2f);
                return directPath + perp5 * randomOffset;
        }
    }

    private void EndGame(bool success)
    {
        if (gameEnded) return;
        
        gameEnded = true;
        isDrawing = false;
        
        if (resultPanel != null)
            resultPanel.SetActive(true);
        
        if (resultText != null)
        {
            resultText.text = success ? "نجحت!" : "فشلت!";
            resultText.color = success ? Color.green : Color.red;
        }
        
        if (success)
        {
            Debug.Log($"[PathGame] Success! Remaining time: {timeRemaining:F1}s");
            
            // Calculate rewards based on remaining time
            int eidiaReward = Mathf.CeilToInt(timeRemaining);
            int scrapReward = Mathf.Max(1, Mathf.CeilToInt(timeRemaining / 3));
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMiniGameComplete(eidiaReward, scrapReward);
            }
        }
        else
        {
            Debug.Log("[PathGame] Failed - Time ran out!");
            
            // Partial rewards (consolation)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMiniGameComplete(0, 1);
            }
        }
        
        // Cleanup after delay
        Invoke(nameof(Cleanup), 2f);
    }

    private void Cleanup()
    {
        // Calculate final rewards for the manager call
        int eidiaReward = gameEnded && timeRemaining > 0 ? Mathf.CeilToInt(timeRemaining) : 0;
        int scrapReward = gameEnded && timeRemaining > 0 ? Mathf.Max(1, Mathf.CeilToInt(timeRemaining / 3)) : 1;

        // Return to MiniGameManager
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.EndMiniGame(eidiaReward, scrapReward);
        }
        
        Destroy(gameObject);
    }

    #endregion

    #region Inspector Test Buttons

    [UnityEditor.MenuItem("Tools/PathDrawingGame/Test Win")]
    private static void TestWin()
    {
        if (Instance != null)
        {
            Instance.timeRemaining = 100f;
            Debug.Log("[PathGame] Test: Set time to 100s");
        }
    }

    [UnityEditor.MenuItem("Tools/PathDrawingGame/Test Loss")]
    private static void TestLoss()
    {
        if (Instance != null)
        {
            Instance.timeRemaining = 1f;
            Debug.Log("[PathGame] Test: Set time to 1s");
        }
    }

    #endregion
    
    #region Editor Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw start zone (green)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPoint, startRadius);
        Gizmos.DrawIcon(startPoint, "start_point.png", true);
        
        // Draw end zone (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPoint, goalDistance);
        Gizmos.DrawIcon(endPoint, "end_point.png", true);
        
        // Draw direct path hint (yellow dashed)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint, endPoint);
    }
    
    #endregion
}
