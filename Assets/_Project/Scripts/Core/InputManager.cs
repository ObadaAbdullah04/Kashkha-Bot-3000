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

            // Initialize DeviceControls programmatically (uses embedded JSON)
            if (deviceControls == null)
            {
                deviceControls = new DeviceControls();
            }

            // AUTO-DISABLE simulation on mobile devices
#if !UNITY_EDITOR
            useEditorSimulation = false;
#endif

            // Enable Touch input on mobile
#if !UNITY_EDITOR
            if (Touchscreen.current != null)
            {
                InputSystem.EnableDevice(Touchscreen.current);
            }
#endif

            // Ensure Accelerometer is enabled (required on some mobile platforms)
            if (Accelerometer.current != null)
            {
                InputSystem.EnableDevice(Accelerometer.current);
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
        // Debug.Log("[InputManager] DeviceControls enabled.");
#endif
    }

    private void OnDisable()
    {
        // Disable DeviceControls asset
        deviceControls?.Disable();
#if UNITY_EDITOR
        // Debug.Log("[InputManager] DeviceControls disabled.");
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
            // Debug.LogWarning("[InputManager] DeviceControls not initialized!");
            return;
        }

        var device = deviceControls.Device;
        switch (actionName)
        {
            case "MoveHorizontal": device.MoveHorizontal?.Enable(); break;
            case "Draw": device.Draw?.Enable(); break;
            case "TouchPosition": device.TouchPosition?.Enable(); break;
            case "TouchStart": device.TouchStart?.Enable(); break;
            case "Hold": device.Hold?.Enable(); break;
            case "Acceleration": 
                device.Acceleration?.Enable();
                if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
                break;
            case "Tap": device.Tap?.Enable(); break;
            default:
                // Debug.LogWarning($"[InputManager] Unknown action: {actionName}");
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
            // Debug.LogWarning("[InputManager] DeviceControls not initialized!");
            return;
        }

        var device = deviceControls.Device;
        switch (actionName)
        {
            case "MoveHorizontal": device.MoveHorizontal?.Disable(); break;
            case "Draw": device.Draw?.Disable(); break;
            case "TouchPosition": device.TouchPosition?.Disable(); break;
            case "TouchStart": device.TouchStart?.Disable(); break;
            case "Hold": device.Hold?.Disable(); break;
            case "Acceleration": device.Acceleration?.Disable(); break;
            case "Tap": device.Tap?.Disable(); break;
            default:
                // Debug.LogWarning($"[InputManager] Unknown action: {actionName}");
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
        // Try DeviceControls TouchPosition action first
        if (deviceControls?.Device.TouchPosition != null)
            return deviceControls.Device.TouchPosition.ReadValue<Vector2>();
            
        // Fallback: Direct Touchscreen position (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
            
        // Fallback: Mouse position
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
            
        return Vector2.zero;
    }

    /// <summary>
    /// Checks if the screen is currently being touched.
    /// </summary>
    public bool IsTouching()
    {
        // Try DeviceControls TouchStart action first
        if (deviceControls?.Device.TouchStart != null && deviceControls.Device.TouchStart.IsPressed())
            return true;
            
        // Fallback: Direct Touchscreen check (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;
            
        // Fallback: Pointer check
        if (Pointer.current != null && Pointer.current.press.isPressed)
            return true;
            
        return false;
    }

    /// <summary>
    /// Gets the current device acceleration (for shake detection on mobile).
    /// </summary>
    public Vector3 GetAcceleration()
    {
        if (deviceControls?.Device.Acceleration != null && deviceControls.Device.Acceleration.enabled)
            return deviceControls.Device.Acceleration.ReadValue<Vector3>();
        
        // Fallback to direct reading if action is disabled but device exists
        if (Accelerometer.current != null)
            return Accelerometer.current.acceleration.ReadValue();
            
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
        // Check tap action
        if (deviceControls?.Device.Tap != null && deviceControls.Device.Tap.WasPressedThisFrame())
            return true;
            
        // Fallback: TouchStart action
        if (deviceControls?.Device.TouchStart != null && deviceControls.Device.TouchStart.WasPressedThisFrame())
            return true;

        // Fallback: Direct touchscreen check (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
            
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
        
        // Ensure acceleration action is enabled for shake
        EnableAction("Acceleration");
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
#if UNITY_EDITOR
        if (useEditorSimulation)
        {
            // Editor simulation: Space key presses (New Input System)
            if (Keyboard.current != null && Keyboard.current[shakeKey].wasPressedThisFrame)
            {
                if (Time.time - _lastShakeTime >= SHAKE_COOLDOWN)
                {
                    _shakeCount++;
                    _lastShakeTime = Time.time;
                }
            }
            return; // Don't run real accelerometer logic in editor if simulation is on
        }
#endif

        // Mobile/Standalone: accelerometer magnitude threshold
        Vector3 accel = GetAcceleration();
        float magnitude = accel.magnitude;
        
        // Threshold should be higher than gravity to count as a "shake"
        // 13.0f means roughly 1.3g (gravity is 1g = ~9.81)
        // Some devices report higher values, so we also check for delta changes
        const float SHAKE_THRESHOLD = 13.0f; 
        
        // Check if acceleration exceeds threshold and cooldown has passed
        if (magnitude >= SHAKE_THRESHOLD && Time.time - _lastShakeTime >= SHAKE_COOLDOWN)
        {
            _shakeCount++;
            _lastShakeTime = Time.time;
        }
    }

    private void UpdateHoldInput()
    {
#if UNITY_EDITOR
        if (useEditorSimulation)
        {
            // Editor simulation: H key held (New Input System)
            bool hPressed = Keyboard.current != null && Keyboard.current[holdKey].isPressed;
            
            if (hPressed && !_isHolding)
            {
                _isHolding = true;
                _holdStartTime = Time.time;
            }
            else if (!hPressed && _isHolding)
            {
                _isHolding = false;
            }
            return;
        }
#endif

        // Mobile: check touch via Hold action or direct touch detection
        bool isTouching = false;
        
        // Try Hold action first
        if (deviceControls?.Device.Hold != null)
        {
            isTouching = deviceControls.Device.Hold.IsPressed();
        }
        
        // Fallback: direct touch detection
        if (!isTouching)
        {
            isTouching = IsTouching();
        }
        
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

    private void UpdateTapInput()
    {
#if UNITY_EDITOR
        if (useEditorSimulation)
        {
            if (Keyboard.current != null && Keyboard.current[tapKey].wasPressedThisFrame)
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
            return;
        }
#endif

        bool tapped = false;
        
        if (deviceControls?.Device.Tap != null)
        {
            tapped = deviceControls.Device.Tap.WasPressedThisFrame();
        }
        
        if (!tapped)
        {
            tapped = IsTapPressed();
        }
        
        if (tapped)
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

    #endregion

    #region Inspector Test Buttons

    [Button("Test: Print All Actions")]
    private void PrintAllActions()
    {
        if (deviceControls?.Device == null)
        {
            // Debug.LogWarning("[InputManager] DeviceControls not assigned!");
            return;
        }

        var device = deviceControls.Device;
        // Debug.Log("=== [InputManager] All DeviceControls Actions ===");
        // Debug.Log($"  - MoveHorizontal: {device.MoveHorizontal}");
        // Debug.Log($"  - Draw: {device.Draw}");
        // Debug.Log($"  - TouchPosition: {device.TouchPosition}");
        // Debug.Log($"  - TouchStart: {device.TouchStart}");
        // Debug.Log($"  - Acceleration: {device.Acceleration}");
        // Debug.Log($"  - Tap: {device.Tap}");
        // Debug.Log("===============================================");
    }

    [Button("Test: Check Touch Input")]
    private void TestTouchInput()
    {
        // Debug.Log($"[InputManager] IsTouching: {IsTouching()}, TouchPosition: {GetTouchPosition()}");
    }

    [Button("Test: Check MoveHorizontal Input")]
    private void TestMoveInput()
    {
        // Debug.Log($"[InputManager] MoveHorizontal: {GetMoveHorizontalValue()}");
    }

    #endregion
}
