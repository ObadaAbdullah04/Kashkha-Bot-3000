using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

/// <summary>
/// Centralized Input Manager for unified input handling across PC and mobile.
/// 
/// ARCHITECTURE:
/// - Single singleton managing all DeviceControls input actions
/// - Provides methods to enable/disable/query specific input actions
/// - Prevents input conflicts between systems (mini-games, swipe cards)
/// - Compatible with both PC (keyboard/mouse) and mobile (touch/accelerometer)
/// 
/// USAGE:
/// - Other systems call InputManager.Instance.GetAction("ActionName")
/// - Or use helper methods: EnableMoveInput(), DisableMoveInput(), etc.
/// - All input actions defined in DeviceControls.inputactions
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("DeviceControls Input Actions")]
    [Tooltip("DeviceControls input action asset (auto-generated from DeviceControls.inputactions)")]
    [SerializeField] private DeviceControls deviceControls;

    [Header("Editor Simulation")]
    [Tooltip("When true, maps keyboard keys to simulate mobile inputs (for Editor testing)")]
    [SerializeField] private bool useEditorSimulation = true;

    [Tooltip("Key to simulate shake input (New Input System)")]
    [SerializeField] private Key shakeKey = Key.Space;

    [Tooltip("Key to simulate hold input (New Input System)")]
    [SerializeField] private Key holdKey = Key.H;

    [Tooltip("Key to simulate tap input (New Input System)")]
    [SerializeField] private Key tapKey = Key.T;

    // Interaction state tracking
    private int _shakeCount = 0;
    private float _lastShakeTime = 0f;
    private float _holdStartTime = 0f;
    private bool _isHolding = false;
    private int _tapCount = 0;
    private float _lastTapTime = 0f;
    private const float SHAKE_COOLDOWN = 0.3f; // Minimum time between shake detections
    private const float TAP_COOLDOWN = 0.5f;  // Maximum time between taps to count as combo

    #region Properties

    /// <summary>
    /// Access to the DeviceControls input actions.
    /// </summary>
    public DeviceControls DeviceControls => deviceControls;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize DeviceControls if not assigned
            if (deviceControls == null)
            {
                deviceControls = new DeviceControls();
#if UNITY_EDITOR
                Debug.Log("[InputManager] DeviceControls created programmatically.");
#endif
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Enable DeviceControls asset
        deviceControls?.Enable();
#if UNITY_EDITOR
        Debug.Log("[InputManager] DeviceControls enabled.");
#endif
    }

    private void OnDisable()
    {
        // Disable DeviceControls asset
        deviceControls?.Disable();
#if UNITY_EDITOR
        Debug.Log("[InputManager] DeviceControls disabled.");
#endif
    }

    #endregion

    #region Public API - Input Action Access

    /// <summary>
    /// Enables a specific input action by name.
    /// Supported actions: MoveHorizontal, Draw, TouchPosition, TouchStart, Acceleration, Tap
    /// </summary>
    public void EnableAction(string actionName)
    {
        if (deviceControls?.Device == null)
        {
            Debug.LogWarning("[InputManager] DeviceControls not initialized!");
            return;
        }

        var device = deviceControls.Device;
        switch (actionName)
        {
            case "MoveHorizontal": device.MoveHorizontal?.Enable(); break;
            case "Draw": device.Draw?.Enable(); break;
            case "TouchPosition": device.TouchPosition?.Enable(); break;
            case "TouchStart": device.TouchStart?.Enable(); break;
            case "Acceleration": device.Acceleration?.Enable(); break;
            case "Tap": device.Tap?.Enable(); break;
            default:
                Debug.LogWarning($"[InputManager] Unknown action: {actionName}");
                break;
        }
    }

    /// <summary>
    /// Disables a specific input action by name.
    /// Supported actions: MoveHorizontal, Draw, TouchPosition, TouchStart, Acceleration, Tap
    /// </summary>
    public void DisableAction(string actionName)
    {
        if (deviceControls?.Device == null)
        {
            Debug.LogWarning("[InputManager] DeviceControls not initialized!");
            return;
        }

        var device = deviceControls.Device;
        switch (actionName)
        {
            case "MoveHorizontal": device.MoveHorizontal?.Disable(); break;
            case "Draw": device.Draw?.Disable(); break;
            case "TouchPosition": device.TouchPosition?.Disable(); break;
            case "TouchStart": device.TouchStart?.Disable(); break;
            case "Acceleration": device.Acceleration?.Disable(); break;
            case "Tap": device.Tap?.Disable(); break;
            default:
                Debug.LogWarning($"[InputManager] Unknown action: {actionName}");
                break;
        }
    }

    #endregion

    #region Public API - Helper Methods for Common Actions

    /// <summary>
    /// Gets the current horizontal movement input value (-1 to 1).
    /// Used by CatchMiniGame for player basket movement.
    /// </summary>
    public Vector2 GetMoveHorizontalValue()
    {
        if (deviceControls?.Device.MoveHorizontal != null)
            return deviceControls.Device.MoveHorizontal.ReadValue<Vector2>();
        return Vector2.zero;
    }

    /// <summary>
    /// Gets the current touch position on screen.
    /// </summary>
    public Vector2 GetTouchPosition()
    {
        if (deviceControls?.Device.TouchPosition != null)
            return deviceControls.Device.TouchPosition.ReadValue<Vector2>();
        return Vector2.zero;
    }

    /// <summary>
    /// Checks if the screen is currently being touched.
    /// </summary>
    public bool IsTouching()
    {
        if (deviceControls?.Device.TouchStart != null)
            return deviceControls.Device.TouchStart.IsPressed();
        return false;
    }

    /// <summary>
    /// Gets the current device acceleration (for shake detection on mobile).
    /// </summary>
    public Vector3 GetAcceleration()
    {
        if (deviceControls?.Device.Acceleration != null)
            return deviceControls.Device.Acceleration.ReadValue<Vector3>();
        return Vector3.zero;
    }

    /// <summary>
    /// Checks if the draw action is currently active.
    /// Used by PathDrawingGame.
    /// </summary>
    public bool IsDrawPressed()
    {
        if (deviceControls?.Device.Draw != null)
            return deviceControls.Device.Draw.IsPressed();
        return false;
    }

    /// <summary>
    /// Checks if the tap action was just performed.
    /// </summary>
    public bool IsTapPressed()
    {
        if (deviceControls?.Device.Tap != null)
            return deviceControls.Device.Tap.WasPressedThisFrame();
        return false;
    }

    #endregion

    #region Public API - Interaction Input Methods

    /// <summary>
    /// Resets all interaction counters. Call at the start of each interaction.
    /// </summary>
    public void ResetInteractionState()
    {
        _shakeCount = 0;
        _lastShakeTime = 0f;
        _holdStartTime = 0f;
        _isHolding = false;
        _tapCount = 0;
        _lastTapTime = 0f;
    }

    /// <summary>
    /// Gets the current shake count (mobile: accelerometer spikes, editor: Space key presses).
    /// </summary>
    public int GetShakeCount()
    {
        UpdateShakeInput();
        return _shakeCount;
    }

    /// <summary>
    /// Checks if currently holding (mobile: touch held, editor: H key held).
    /// Returns how long the hold has been active in seconds.
    /// </summary>
    public float GetHoldDuration()
    {
        UpdateHoldInput();
        return _isHolding ? Time.time - _holdStartTime : 0f;
    }

    /// <summary>
    /// Checks if currently holding.
    /// </summary>
    public bool IsHolding()
    {
        UpdateHoldInput();
        return _isHolding;
    }

    /// <summary>
    /// Gets the current tap count (mobile: rapid touches, editor: rapid T key presses).
    /// </summary>
    public int GetTapCount()
    {
        UpdateTapInput();
        return _tapCount;
    }

    /// <summary>
    /// Resets shake counter for next interaction.
    /// </summary>
    public void ResetShakeCount() => _shakeCount = 0;

    /// <summary>
    /// Resets tap counter for next interaction.
    /// </summary>
    public void ResetTapCount() => _tapCount = 0;

    #endregion

    #region Interaction Input Update Methods (Private)

    private void UpdateShakeInput()
    {
        if (useEditorSimulation)
        {
            // Editor simulation: Space key presses (New Input System)
            if (Keyboard.current != null && Keyboard.current[shakeKey].wasPressedThisFrame)
            {
                if (Time.time - _lastShakeTime >= SHAKE_COOLDOWN)
                {
                    _shakeCount++;
                    _lastShakeTime = Time.time;
#if UNITY_EDITOR
                    Debug.Log($"[InputManager] Shake detected! Count: {_shakeCount}");
#endif
                }
            }
        }
        else
        {
            // Mobile: accelerometer magnitude threshold
            Vector3 accel = GetAcceleration();
            float magnitude = accel.magnitude;
            const float SHAKE_THRESHOLD = 1.5f; // Adjust based on testing
            
            if (magnitude >= SHAKE_THRESHOLD && Time.time - _lastShakeTime >= SHAKE_COOLDOWN)
            {
                _shakeCount++;
                _lastShakeTime = Time.time;
            }
        }
    }

    private void UpdateHoldInput()
    {
        if (useEditorSimulation)
        {
            // Editor simulation: H key held (New Input System)
            bool isPressed = Keyboard.current != null && Keyboard.current[holdKey].isPressed;
            
            if (isPressed && !_isHolding)
            {
                _isHolding = true;
                _holdStartTime = Time.time;
            }
            else if (!isPressed && _isHolding)
            {
                _isHolding = false;
            }
        }
        else
        {
            // Mobile: touch held (use existing IsTouching)
            bool isTouching = IsTouching();
            
            if (isTouching && !_isHolding)
            {
                _isHolding = true;
                _holdStartTime = Time.time;
            }
            else if (!isTouching && _isHolding)
            {
                _isHolding = false;
            }
        }
    }

    private void UpdateTapInput()
    {
        if (useEditorSimulation)
        {
            // Editor simulation: T key presses (New Input System)
            if (Keyboard.current != null && Keyboard.current[tapKey].wasPressedThisFrame)
            {
                if (Time.time - _lastTapTime <= TAP_COOLDOWN || _tapCount == 0)
                {
                    _tapCount++;
                    _lastTapTime = Time.time;
#if UNITY_EDITOR
                    Debug.Log($"[InputManager] Tap detected! Count: {_tapCount}");
#endif
                }
                else
                {
                    // Too slow, reset counter
                    _tapCount = 1;
                    _lastTapTime = Time.time;
                }
            }
        }
        else
        {
            // Mobile: touch started
            if (deviceControls?.Device.TouchStart != null && deviceControls.Device.TouchStart.WasPressedThisFrame())
            {
                if (Time.time - _lastTapTime <= TAP_COOLDOWN || _tapCount == 0)
                {
                    _tapCount++;
                    _lastTapTime = Time.time;
                }
                else
                {
                    _tapCount = 1;
                    _lastTapTime = Time.time;
                }
            }
        }
    }

    #endregion

    #region Inspector Test Buttons

    [Button("Test: Print All Actions")]
    private void PrintAllActions()
    {
        if (deviceControls?.Device == null)
        {
            Debug.LogWarning("[InputManager] DeviceControls not assigned!");
            return;
        }

        var device = deviceControls.Device;
        Debug.Log("=== [InputManager] All DeviceControls Actions ===");
        Debug.Log($"  - MoveHorizontal: {device.MoveHorizontal}");
        Debug.Log($"  - Draw: {device.Draw}");
        Debug.Log($"  - TouchPosition: {device.TouchPosition}");
        Debug.Log($"  - TouchStart: {device.TouchStart}");
        Debug.Log($"  - Acceleration: {device.Acceleration}");
        Debug.Log($"  - Tap: {device.Tap}");
        Debug.Log("===============================================");
    }

    [Button("Test: Check Touch Input")]
    private void TestTouchInput()
    {
        Debug.Log($"[InputManager] IsTouching: {IsTouching()}, TouchPosition: {GetTouchPosition()}");
    }

    [Button("Test: Check MoveHorizontal Input")]
    private void TestMoveInput()
    {
        Debug.Log($"[InputManager] MoveHorizontal: {GetMoveHorizontalValue()}");
    }

    #endregion
}
