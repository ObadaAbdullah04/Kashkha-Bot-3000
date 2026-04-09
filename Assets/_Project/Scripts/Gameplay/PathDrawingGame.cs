using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RTLTMPro;

/// <summary>
/// PHASE 5C (REVISED v2): Path-Drawing Maze Mini-Game Manager.
///
/// GAMEPLAY:
/// - Click AND hold ANYWHERE to start drawing a line
/// - Line follows finger in real-time from hold-start to hold-end position
/// - Release finger → Line clears immediately (like real pathfinding)
/// - To WIN: Line must PASS THROUGH StartPoint area AND reach EndPoint
/// - Hit obstacle → Time penalty + line cleared
///
/// SETUP INSTRUCTIONS:
/// 1. Create Canvas → Add this script to empty GameObject
/// 2. Add LineRenderer component to empty GameObject (child of Canvas)
/// 3. Assign UI references (TimerText, ResultText)
/// 4. Place StartPoint and EndPoint GameObjects in scene, assign Transforms
/// 5. Place obstacle GameObjects manually, assign to Obstacles[] array
/// </summary>
public class PathDrawingGame : MonoBehaviour
{
    public static PathDrawingGame Instance { get; private set; }

    #region Inspector Fields

    [Header("Map Configuration")]
    [Tooltip("Background sprite for the map (placeholder: colored Image)")]
    [SerializeField] private Sprite mapBackground;

    [Header("Line Settings")]
    [Tooltip("LineRenderer component for drawing path")]
    [SerializeField] private LineRenderer pathLine;

    [Tooltip("Width of the drawn line")]
    [SerializeField] private float lineWidth = 0.15f;

    [Tooltip("Color of the path line")]
    [SerializeField] private Color lineColor = Color.green;

    [Tooltip("Maximum points in the line (performance)")]
    [SerializeField] private int maxLinePoints = 200;

    [Header("Input")]
    [Tooltip("Input Action for drawing (assign Draw from DeviceControls)")]
    [SerializeField] private InputActionReference drawAction;

    [Header("Game Settings")]
    [Tooltip("Base time limit in seconds")]
    [SerializeField] private float timeLimit = 30f;

    [Tooltip("Time penalty for hitting obstacle")]
    [SerializeField] private float collisionPenalty = 5f;

    [Tooltip("Starting position (player/robot) - path must pass through here to win")]
    [SerializeField] private Transform startPoint;

    [Tooltip("Ending position (house door) - must reach here to win")]
    [SerializeField] private Transform endPoint;

    [Tooltip("Distance to goal for win detection")]
    [SerializeField] private float goalDistance = 1.5f;

    [Tooltip("Radius around start point that counts as 'passing through'")]
    [SerializeField] private float startRadius = 1.5f;

    [Tooltip("Distance between collision check points (smaller = more accurate but slower)")]
    [SerializeField] private float collisionCheckInterval = 0.2f;

    [Header("Obstacles (Manually Placed)")]
    [Tooltip("Array of obstacle Transforms (place manually in scene, then drag here)")]
    [SerializeField] private Transform[] obstacles = new Transform[0];

    [Header("UI References")]
    [SerializeField] private RTLTextMeshPro timerText;

    [SerializeField] private RTLTextMeshPro resultText;

    [SerializeField] private GameObject resultPanel;

    #endregion

    #region Private Fields

    private bool isHolding = false; // Is player currently holding?
    private List<Vector3> linePoints = new List<Vector3>();
    private List<Collider2D> alreadyHitObstacles = new List<Collider2D>(); // Prevent double penalty
    private float timeRemaining;
    private Camera mainCam;
    private bool gameEnded = false;
    private Canvas gameCanvas; // Cache canvas reference

    // Pooled collision buffer to avoid GC allocation (OverlapCircleNonAlloc)
    private static readonly Collider2D[] _collisionBuffer = new Collider2D[16];

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
            mainCam = Camera.allCameras[0];
        }

        // FIX #1: Auto-configure Canvas for proper rendering
        ConfigureCanvas();

        // Enable input action
        if (drawAction != null && drawAction.action != null)
        {
            drawAction.action.Enable();
        }

        InitializeGame();
    }

    private void OnEnable()
    {
        if (drawAction != null && drawAction.action != null)
        {
            drawAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (drawAction != null && drawAction.action != null)
        {
            drawAction.action.Disable();
        }
    }

    private void Update()
    {
        if (gameEnded) return;

        // Update timer (use unscaled time to work during pause)
        timeRemaining -= Time.unscaledDeltaTime;

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

        // Get input state
        bool isPressing = GetInputState();

        // Input state changed: was not holding, now holding
        if (isPressing && !isHolding)
        {
            OnHoldStart();
        }

        // Input state changed: was holding, now released
        if (!isPressing && isHolding)
        {
            OnHoldEnd();
        }

        // Currently holding - continue drawing
        if (isHolding)
        {
            Vector3 inputPos = GetInputPosition();
            AddPointToLine(inputPos);
            CheckCollisions();
        }

        // Update holding state for next frame
        isHolding = isPressing;
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }

    #endregion

    #region Canvas Configuration

    /// <summary>
    /// FIX #1: Auto-configure Canvas render mode and camera assignment.
    /// </summary>
    private void ConfigureCanvas()
    {
        gameCanvas = GetComponentInChildren<Canvas>();
        if (gameCanvas == null)
        {
            Debug.LogError("[PathDrawingGame] No Canvas found on this GameObject or children!");
            return;
        }

        if (mainCam != null)
        {
            // Use ScreenSpaceCamera for world-space LineRenderer compatibility
            gameCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            gameCanvas.worldCamera = mainCam;
            gameCanvas.planeDistance = 100f;

#if UNITY_EDITOR
            Debug.Log($"[PathDrawingGame] Canvas configured with camera: {mainCam.gameObject.name}");
#endif
        }
        else
        {
            // Fallback to Overlay if no camera
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.LogWarning("[PathDrawingGame] No camera found! Using ScreenSpaceOverlay.");
        }

        // Fix RectTransform to fill screen
        RectTransform rectTransform = gameCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
        }
    }

    #endregion

    #region Input Handling

    private bool GetInputState()
    {
        // Check for input via Input Action
        if (drawAction != null && drawAction.action != null)
        {
            if (drawAction.action.ReadValue<float>() > 0.5f)
                return true;
        }

        // Fallback: Touchscreen (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        // Fallback: Mouse
        if (Pointer.current != null && Pointer.current.position.ReadValue() != Vector2.zero)
        {
            if (Input.GetMouseButton(0))
                return true;
        }

        return false;
    }

    private void OnHoldStart()
    {
        isHolding = true;
        Vector3 inputPos = GetInputPosition();

        // FIX #2: Start drawing from ANYWHERE - no validation
        linePoints.Clear();
        linePoints.Add(inputPos);

#if UNITY_EDITOR
        Debug.Log($"[PathGame] Drawing started at {inputPos}");
#endif
    }

    private void OnHoldEnd()
    {
        isHolding = false;
        // Clear the line immediately when released (real pathfinding behavior)
        linePoints.Clear();
        UpdateLineRenderer();
    }

    private Vector3 GetInputPosition()
    {
        Vector3 inputPos;

        // Touch input via New Input System (primary touch)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            inputPos = mainCam.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, 10f));
        }
        else
        {
            // Mouse fallback (editor) - use Input System Pointer
            if (Pointer.current != null && Pointer.current.position.ReadValue() != Vector2.zero)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                inputPos = mainCam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 10f));
            }
            else
            {
                // Ultimate fallback to old Input system
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 10f;
                inputPos = mainCam.ScreenToWorldPoint(mousePos);
            }
        }

        return inputPos;
    }

    #endregion

    #region Line Drawing

    private void AddPointToLine(Vector3 position)
    {
        // Only add point if far enough from last
        if (linePoints.Count > 0 &&
            Vector3.Distance(linePoints[linePoints.Count - 1], position) < 0.1f)
        {
            return;
        }

        // Check max points
        if (linePoints.Count >= maxLinePoints)
        {
            linePoints.RemoveAt(0);
        }

        linePoints.Add(position);
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        if (pathLine == null) return;

        pathLine.positionCount = linePoints.Count;
        if (linePoints.Count > 0)
        {
            pathLine.SetPositions(linePoints.ToArray());
        }
    }

    #endregion

    #region Collision Detection

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

                // Use NonAlloc version to avoid GC pressure on mobile
                int hitCount = Physics2D.OverlapCircleNonAlloc(checkPos, lineWidth, _collisionBuffer);

                for (int k = 0; k < hitCount; k++)
                {
                    Collider2D hitCollider = _collisionBuffer[k];

                    // FIX #3: Check if this collider belongs to our obstacles array
                    if (IsObstacleCollider(hitCollider) && !alreadyHitObstacles.Contains(hitCollider))
                    {
                        OnPathCollision(hitCollider);
                        alreadyHitObstacles.Add(hitCollider);
                        return; // Stop checking after first hit
                    }
                }
            }
        }

        // Check win condition: line passes through StartPoint AND reaches EndPoint
        if (CheckWinCondition())
        {
            EndGame(true);
        }
    }

    /// <summary>
    /// FIX #3: Check if a collider belongs to an obstacle in our array.
    /// </summary>
    private bool IsObstacleCollider(Collider2D collider)
    {
        if (obstacles == null || obstacles.Length == 0)
            return false;

        // Check if collider is on any obstacle Transform
        foreach (Transform obstacle in obstacles)
        {
            if (obstacle == null) continue;

            // Check if collider is on this obstacle or its children
            if (collider.transform == obstacle || collider.transform.IsChildOf(obstacle))
            {
                return true;
            }

            // Also check if obstacle has a collider matching this one
            Collider2D[] obstacleColliders = obstacle.GetComponents<Collider2D>();
            foreach (Collider2D obsCollider in obstacleColliders)
            {
                if (obsCollider == collider)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// FIX #2: Win condition - line must pass through StartPoint AND reach EndPoint.
    /// </summary>
    private bool CheckWinCondition()
    {
        if (linePoints.Count < 2) return false;

        bool passedThroughStart = false;
        bool reachedEnd = false;

        // Check if ANY point in the line passed through StartPoint radius
        foreach (Vector3 point in linePoints)
        {
            float distToStart = Vector2.Distance(point, startPoint.position);
            if (distToStart <= startRadius)
            {
                passedThroughStart = true;
                break;
            }
        }

        // Check if the LAST point (current position) is near EndPoint
        float distToEnd = Vector2.Distance(linePoints[linePoints.Count - 1], endPoint.position);
        if (distToEnd <= goalDistance)
        {
            reachedEnd = true;
        }

        // Must satisfy BOTH conditions to win
        bool winCondition = passedThroughStart && reachedEnd;

#if UNITY_EDITOR
        if (winCondition)
        {
            Debug.Log($"[PathGame] Win condition met! Start: {passedThroughStart}, End: {reachedEnd}");
        }
#endif

        return winCondition;
    }

    private void OnPathCollision(Collider2D hitCollider)
    {
        if (gameEnded) return;

        // Apply time penalty
        timeRemaining -= collisionPenalty;

        // Clear the ENTIRE line - player must start fresh
        linePoints.Clear();
        alreadyHitObstacles.Clear(); // Reset hit obstacles so they can hit again
        UpdateLineRenderer();

        // Visual feedback - flash line red briefly
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

#if UNITY_EDITOR
        Debug.Log($"[PathGame] HIT OBSTACLE! -{collisionPenalty}s - Line cleared!");
#endif
    }

    private void ResetLineColor()
    {
        if (pathLine != null)
        {
            pathLine.startColor = lineColor;
            pathLine.endColor = lineColor;
        }
    }

    #endregion

    #region Game Logic

    private void InitializeGame()
    {
        gameEnded = false;
        timeRemaining = timeLimit;
        linePoints.Clear();
        alreadyHitObstacles.Clear();

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

#if UNITY_EDITOR
        Debug.Log($"[PathGame] Game initialized! Time: {timeLimit}s, Obstacles: {obstacles.Length}");
        Debug.Log($"[PathGame] START: {startPoint.position} | END: {endPoint.position}");
        Debug.Log("[PathGame] Draw ANYWHERE but must pass through Start and reach End to win!");
#endif
    }

    private void EndGame(bool success)
    {
        if (gameEnded) return;

        gameEnded = true;
        isHolding = false;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = success ? "نجحت!" : "فشلت!";
            resultText.color = success ? Color.green : Color.red;
        }

        if (success)
        {
#if UNITY_EDITOR
            Debug.Log($"[PathGame] Success! Remaining time: {timeRemaining:F1}s");
#endif

            // Calculate rewards based on remaining time
            int eidiaReward = Mathf.CeilToInt(timeRemaining);
            int scrapReward = Mathf.Max(1, Mathf.CeilToInt(timeRemaining / 3));

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMiniGameComplete(eidiaReward);
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("[PathGame] Failed - Time ran out!");
#endif

            // Partial rewards (consolation)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMiniGameComplete(0);
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

    #region Editor Gizmos

    private void OnDrawGizmos()
    {
        // Draw start zone (green)
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, startRadius);
        }

        // Draw end zone (red)
        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, goalDistance);
        }

        // Draw direct path hint (yellow dashed)
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }

        // Draw obstacle positions (magenta)
        if (obstacles != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Transform obstacle in obstacles)
            {
                if (obstacle != null)
                {
                    Gizmos.DrawWireSphere(obstacle.position, 0.5f);
                }
            }
        }
    }

    #endregion
}
