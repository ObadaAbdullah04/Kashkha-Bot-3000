using System;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Manages encounter timer with panic mode.
/// All durations exposed to Inspector for designer tuning - NO HARDCODING.
/// </summary>
public class TimerController : MonoBehaviour
{
    public static TimerController Instance { get; private set; }

    #region Inspector Fields - Tunable Values

    [Header("Timer Durations (Seconds)")]
    [Tooltip("Base timer duration for House 1 encounters")]
    [SerializeField] private float house1Duration = 8f;
    [Tooltip("Base timer duration for House 2 encounters")]
    [SerializeField] private float house2Duration = 7f;
    [Tooltip("Base timer duration for House 3 encounters")]
    [SerializeField] private float house3Duration = 6f;
    [Tooltip("Base timer duration for House 4 (Boss Mode) - usually half of normal")]
    [SerializeField] private float house4Duration = 4f;

    [Header("Outfit Stat Bonuses (Applied at Runtime)")]
    [Tooltip("Timer extension bonus from outfit (e.g. +1 = +1 second to all timers)")]
    [ReadOnly] [SerializeField] private float outfitTimerBonus = 0f;

    [Header("Panic Mode Settings")]
    [Tooltip("When timer reaches this value (seconds), panic mode activates")]
    [SerializeField] private float panicThreshold = 3f;
    [Tooltip("Time between chromatic aberration pulses during panic mode")]
    [SerializeField] private float pulseCooldown = 0.3f;

    #endregion

    #region Timer State

    [Header("Timer State (Read-Only)")]
    [ReadOnly] public float timeRemaining;
    [ReadOnly] private bool isTimerRunning = false;
    private float _lastPulseTime = 0f;
    private bool _inPanicMode = false;

    #endregion

    #region Events

    public static Action OnTimeRanOut;
    public static event Action<bool> OnPanicModeChanged;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        timeRemaining -= Time.deltaTime;

        int currentSecond = Mathf.CeilToInt(timeRemaining);
        
        // Check panic mode threshold
        bool shouldPanic = currentSecond <= panicThreshold && currentSecond > 0;
        
        if (shouldPanic && !_inPanicMode)
        {
            _inPanicMode = true;
            OnPanicModeChanged?.Invoke(true);
            
            if (UIManager.Instance != null)
                UIManager.Instance.SetPanicMode(true);
        }
        else if (!shouldPanic && _inPanicMode)
        {
            _inPanicMode = false;
            OnPanicModeChanged?.Invoke(false);
            
            if (UIManager.Instance != null)
                UIManager.Instance.SetPanicMode(false);
        }

        // Pulse chromatic aberration during panic
        if (_inPanicMode)
        {
            if (Time.time - _lastPulseTime >= pulseCooldown)
            {
                if (URPPostProcessing.Instance != null)
                    URPPostProcessing.Instance.PulseChromaticAberration();
                _lastPulseTime = Time.time;
            }
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isTimerRunning = false;
            _inPanicMode = false;
            OnPanicModeChanged?.Invoke(false);
            Debug.LogWarning("[Timer] Time ran out!");
            OnTimeRanOut?.Invoke();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Applies outfit timer bonus from WardrobeManager. Call at start of run.
    /// </summary>
    public void ApplyOutfitBonus()
    {
        if (WardrobeManager.Instance == null) return;

        var (statType, value) = WardrobeManager.Instance.GetEquippedStatBonus();

        if (statType == OutfitStatType.TimerExtension)
        {
            outfitTimerBonus = value;
            Debug.Log($"[Timer] Outfit bonus applied: +{value}s to all timers");
        }
    }

    /// <summary>
    /// Starts timer with duration based on house level.
    /// Applies outfit timer bonus. House 4 uses separate duration field for boss mode tuning.
    /// </summary>
    public void StartTimer(int houseLevel, bool isHouse4 = false)
    {
        float baseDuration = isHouse4 ? house4Duration : houseLevel switch
        {
            1 => house1Duration,
            2 => house2Duration,
            3 => house3Duration,
            _ => house1Duration
        };

        // Apply outfit timer bonus
        float duration = baseDuration + outfitTimerBonus;
        timeRemaining = duration;
        isTimerRunning = true;
        _inPanicMode = false;

        Debug.Log($"[Timer] Started: {timeRemaining:F1}s (House {(isHouse4 ? "4 Boss" : houseLevel.ToString())}, base: {baseDuration} + outfit: {outfitTimerBonus})");
    }

    /// <summary>
    /// Starts timer with custom duration (for QTEs or special encounters).
    /// </summary>
    public void StartTimer(float customDuration)
    {
        timeRemaining = customDuration;
        isTimerRunning = true;
        _inPanicMode = false;
        
        Debug.Log($"[Timer] Started (custom): {timeRemaining:F1}s");
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        _inPanicMode = false;
        OnPanicModeChanged?.Invoke(false);
        Debug.Log("[Timer] Stopped.");
    }

    #endregion

    #region Inspector Test Buttons

    [Button("▶ House 1")]
    private void TestStart1() => StartTimer(1);

    [Button("▶ House 2")]
    private void TestStart2() => StartTimer(2);

    [Button("▶ House 3")]
    private void TestStart3() => StartTimer(3);

    [Button("☠️ House 4 (Boss)")]
    private void TestStartHouse4() => StartTimer(4, isHouse4: true);

    [Button("⏹ Stop")]
    private void TestStop() => StopTimer();

    #endregion
}
