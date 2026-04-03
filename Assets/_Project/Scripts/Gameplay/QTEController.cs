using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

/// <summary>
/// Manages Quick Time Event (QTE) sequences using Unity New Input System.
/// All thresholds, durations, and limits exposed to Inspector - NO HARDCODING.
/// House 4 Boss Mode uses multiplier fields for brutal difficulty scaling.
/// </summary>
public class QTEController : MonoBehaviour
{
    public static QTEController Instance { get; private set; }

    #region Inspector Fields - Tunable Values

    [Header("Shake QTE Settings")]
    [Tooltip("Minimum shake intensity to register as success")]
    [SerializeField] private float shakeThreshold = 15f;
    [Tooltip("Base time limit to complete shakes")]
    [SerializeField] private float shakeDuration = 3f;
    [Tooltip("Input cooldown after successful shake (prevents double-trigger)")]
    [SerializeField] private float shakeCooldown = 0.3f;

    [Header("Tap QTE Settings")]
    [Tooltip("Time window to complete all taps")]
    [SerializeField] private float tapTimeWindow = 2f;

    [Header("Swipe QTE Settings")]
    [Tooltip("Time limit to complete swipes")]
    [SerializeField] private float swipeTimeLimit = 2.5f;

    [Header("Hold QTE Settings")]
    [Tooltip("Duration to hold before release")]
    [SerializeField] private float holdDuration = 2f;

    [Header("House 4 Boss Mode Modifiers")]
    [Tooltip("Time limit multiplier for House 4 (0.5 = half time)")]
    [SerializeField] private float house4TimeMultiplier = 0.5f;
    [Tooltip("Additional inputs required for House 4")]
    [SerializeField] private int house4ExtraInputs = 1;
    [Tooltip("Increased threshold for shake QTEs in House 4")]
    [SerializeField] private float house4ShakeThresholdMultiplier = 1.5f;
    [Tooltip("Increased hold duration for House 4")]
    [SerializeField] private float house4HoldDurationMultiplier = 1.5f;

    [Header("Global Settings")]
    [Tooltip("Default time limit if none specified")]
    [SerializeField] private float defaultTimeLimit = 4f;
    [Tooltip("Input cooldown between all inputs (prevents spam)")]
    [SerializeField] private float globalInputCooldown = 0.15f;

    #endregion

    #region QTE State

    private QTEInputType activeInputType = QTEInputType.None;
    private SwipeDirection requiredDirection = SwipeDirection.Up;
    private bool isQTEActive = false;
    private float timeRemaining;
    private int inputsCompleted = 0;
    private int inputsRequired = 1;
    private float currentHoldDuration = 2f;
    
    private Vector2 touchStartPosition;
    private bool _inputOnCooldown = false;
    private float _inputCooldownTime = 0f;
    private bool _isHolding = false;
    private float _holdStartTime = 0f;

    // Input Actions
    private DeviceControls _inputActions;

    #endregion

    #region Events

    public static event Action<bool> OnQTEResolved;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Initialize Input Actions
            _inputActions = new DeviceControls();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    private void Update()
    {
        if (!isQTEActive) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            EndQTE(false);
            return;
        }

        // Input cooldown
        if (_inputOnCooldown)
        {
            if (Time.time >= _inputCooldownTime)
                _inputOnCooldown = false;
            else
                return;
        }

        // Process input based on active QTE type
        bool inputDetected = activeInputType switch
        {
            QTEInputType.Shake => CheckShakeInput(),
            QTEInputType.Tap => CheckTapInput(),
            QTEInputType.Swipe => CheckSwipeInput(),
            QTEInputType.Hold => CheckHoldInput(),
            _ => false
        };

        if (inputDetected)
        {
            inputsCompleted++;
            if (inputsCompleted >= inputsRequired)
            {
                EndQTE(true);
            }
        }

        // Skip input (Enter key for testing)
        if (CheckSkipInput()) EndQTE(true);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts a QTE sequence with the new input-based system.
    /// Automatically applies House 4 modifiers if active.
    /// </summary>
    /// <param name="inputType">Generic input type (Shake, Tap, Swipe, Hold)</param>
    /// <param name="count">Number of inputs required</param>
    /// <param name="timeLimit">Time to complete (seconds)</param>
    /// <param name="direction">Swipe direction (Up, Down, Left, Right) - for Swipe QTE</param>
    /// <param name="holdDur">Hold duration (seconds) - for Hold QTE</param>
    public void StartQTE(string inputType, int count, float timeLimit, string direction = "", float holdDur = 0f)
    {
        activeInputType = ParseInputType(inputType);

        // Reset input state
        touchStartPosition = Vector2.zero;
        _isHolding = false;

        // Set parameters from CSV
        inputsRequired = count > 0 ? count : 1;
        timeRemaining = timeLimit > 0 ? timeLimit : defaultTimeLimit;
        currentHoldDuration = holdDur > 0 ? holdDur : this.holdDuration;

        // Parse swipe direction
        if (!string.IsNullOrEmpty(direction) && direction != "_")
        {
            requiredDirection = ParseSwipeDirection(direction);
        }
        else
        {
            requiredDirection = SwipeDirection.Up; // Default
        }

        // Apply House 4 modifiers
        bool isHouse4 = GameManager.Instance != null && GameManager.Instance.IsHouse4Active;
        if (isHouse4)
        {
            timeRemaining *= house4TimeMultiplier;
            if (activeInputType != QTEInputType.Hold)
            {
                inputsRequired += house4ExtraInputs;
            }
            else
            {
                currentHoldDuration *= house4HoldDurationMultiplier;
            }
            
            if (activeInputType == QTEInputType.Shake)
            {
                shakeThreshold *= house4ShakeThresholdMultiplier;
            }
            
            Debug.Log($"[QTE] HOUSE 4 MODE: Type={activeInputType}, Time={timeRemaining:F1}s, Inputs={inputsRequired}, HoldDur={currentHoldDuration:F1}s");
        }
        else
        {
            Debug.Log($"[QTE] Started: Type={activeInputType}, Time={timeRemaining:F1}s, Inputs={inputsRequired}");
        }

        inputsCompleted = 0;
        isQTEActive = true;
        _inputOnCooldown = false;
        _isHolding = false;
    }

    /// <summary>
    /// Parses input type string from CSV.
    /// </summary>
    private QTEInputType ParseInputType(string type)
    {
        return type.ToLower() switch
        {
            "shake" => QTEInputType.Shake,
            "tap" => QTEInputType.Tap,
            "swipe" => QTEInputType.Swipe,
            "hold" => QTEInputType.Hold,
            _ => QTEInputType.None
        };
    }
    
    /// <summary>
    /// Parses swipe direction string from CSV.
    /// </summary>
    private SwipeDirection ParseSwipeDirection(string direction)
    {
        return direction.ToLower() switch
        {
            "up" => SwipeDirection.Up,
            "down" => SwipeDirection.Down,
            "left" => SwipeDirection.Left,
            "right" => SwipeDirection.Right,
            _ => SwipeDirection.Up
        };
    }

    #endregion

    #region Input Detection

    /// <summary>
    /// SHAKE QTE: Detect device shake via accelerometer.
    /// </summary>
    private bool CheckShakeInput()
    {
        Vector3 acceleration = _inputActions.Device.Acceleration.ReadValue<Vector3>();
        float shakeIntensity = new Vector3(
            Mathf.Abs(acceleration.x),
            Mathf.Abs(acceleration.y),
            Mathf.Abs(acceleration.z)
        ).magnitude;

        // Apply House 4 threshold multiplier
        float currentThreshold = (GameManager.Instance != null && GameManager.Instance.IsHouse4Active)
            ? shakeThreshold * house4ShakeThresholdMultiplier
            : shakeThreshold;

        if (shakeIntensity >= currentThreshold)
        {
            _inputOnCooldown = true;
            _inputCooldownTime = Time.time + shakeCooldown;
            return true;
        }

        // Fallback: Spacebar for Editor testing
        if (_inputActions.Device.ShakeSkip.ReadValue<float>() > 0.5f)
        {
            _inputOnCooldown = true;
            _inputCooldownTime = Time.time + shakeCooldown;
            return true;
        }

        return false;
    }

    /// <summary>
    /// TAP QTE: Detect tap/click inputs.
    /// </summary>
    private bool CheckTapInput()
    {
        // Use dedicated Tap action
        if (_inputActions.Device.Tap.ReadValue<float>() > 0.5f ||
            _inputActions.Device.ShakeSkip.ReadValue<float>() > 0.5f) // Spacebar fallback
        {
            _inputOnCooldown = true;
            _inputCooldownTime = Time.time + globalInputCooldown;
            return true;
        }
        return false;
    }

    /// <summary>
    /// SWIPE QTE: Detect swipe in specific direction.
    /// </summary>
    private bool CheckSwipeInput()
    {
        // Keyboard fallback for swipe up
        if (_inputActions.Device.SwipeUp.ReadValue<float>() > 0.5f)
        {
            _inputOnCooldown = true;
            _inputCooldownTime = Time.time + globalInputCooldown;
            return true;
        }

        // Get touch phase for swipe detection
        if (Touchscreen.current == null) return false;
        
        var touchPhase = Touchscreen.current.primaryTouch.phase.ReadValue();
        
        // Touch start - record position
        if (touchPhase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            touchStartPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        // Touch end - check direction
        if (touchPhase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            Vector2 currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector2 delta = currentPos - touchStartPosition;

            SwipeDirection detectedDirection = DetectSwipeDirection(delta);

            if (detectedDirection == requiredDirection)
            {
                _inputOnCooldown = true;
                _inputCooldownTime = Time.time + globalInputCooldown;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects swipe direction from delta vector.
    /// </summary>
    private SwipeDirection DetectSwipeDirection(Vector2 delta)
    {
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        if (angle > -45f && angle <= 45f) return SwipeDirection.Right;
        if (angle > 45f && angle <= 135f) return SwipeDirection.Up;
        if (angle > 135f || angle <= -135f) return SwipeDirection.Left;
        if (angle > -135f && angle <= -45f) return SwipeDirection.Down;

        return SwipeDirection.Up; // Default
    }

    /// <summary>
    /// HOLD QTE: Hold for duration, then release.
    /// </summary>
    private bool CheckHoldInput()
    {
        // Use dedicated Hold action for press detection
        bool isHoldingInput = _inputActions.Device.Hold.ReadValue<float>() > 0.5f;
        
        // Start hold
        if (isHoldingInput && !_isHolding)
        {
            _holdStartTime = Time.time;
            _isHolding = true;
        }

        // Continue holding
        if (_isHolding)
        {
            float holdTime = Time.time - _holdStartTime;

            // Check if held long enough
            if (holdTime >= currentHoldDuration)
            {
                // Wait for release
                if (!isHoldingInput)
                {
                    _isHolding = false;
                    return true; // Success
                }
            }
        }

        // Released too early
        if (_isHolding && !isHoldingInput)
        {
            _isHolding = false;
            return false; // Fail
        }

        return false;
    }

    private bool CheckSkipInput()
    {
        // Enter/Return for skip (editor testing)
        if (_inputActions.Device.SkipQTE.ReadValue<float>() > 0.5f)
        {
            return true;
        }
        return false;
    }

    #endregion

    #region QTE Resolution

    private void EndQTE(bool success)
    {
        if (!isQTEActive) return;

        isQTEActive = false;
        activeInputType = QTEInputType.None;
        inputsCompleted = 0;
        inputsRequired = 1;
        _isHolding = false;
        touchStartPosition = Vector2.zero; // Reset for next QTE

        Debug.Log($"[QTE] Result: {(success ? "Success" : "Failed")}");
        OnQTEResolved?.Invoke(success);
    }

    #endregion

    #region Inspector Test Buttons

    [Button("Test Shake QTE")]
    private void TestShake() => StartQTE("Shake", 3, shakeDuration);

    [Button("Test Tap QTE")]
    private void TestTap() => StartQTE("Tap", 2, tapTimeWindow);

    [Button("Test Swipe QTE (Up)")]
    private void TestSwipeUp() => StartQTE("Swipe", 1, swipeTimeLimit, "Up");

    [Button("Test Hold QTE")]
    private void TestHold() => StartQTE("Hold", 1, 0f, "", holdDuration);

    #endregion
}
