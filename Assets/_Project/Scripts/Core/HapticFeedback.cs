using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Simple haptic feedback for mobile using Handheld.Vibrate().
/// Note: Unity's Handheld.Vibrate() triggers device default vibration.
/// For more control, would need platform-specific implementations.
/// </summary>
public class HapticFeedback : MonoBehaviour
{
    public static HapticFeedback Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Master toggle for all haptic feedback")]
    [SerializeField] private bool enableHaptics = true;
    
    [Tooltip("Minimum time between vibrations to prevent spam")]
    [SerializeField] private float minVibrationInterval = 0.1f;

    private float lastVibrationTime = 0f;

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

    /// <summary>
    /// Light vibration - single short pulse
    /// </summary>
    public void LightTap()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        lastVibrationTime = Time.time;
    }

    /// <summary>
    /// Medium vibration - single pulse
    /// </summary>
    public void MediumTap()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        lastVibrationTime = Time.time;
    }

    /// <summary>
    /// Heavy vibration - double pulse with delay
    /// </summary>
    public void HeavyVibration()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        Invoke(nameof(VibratePulse), minVibrationInterval);
        lastVibrationTime = Time.time;
    }

    /// <summary>
    /// Explosion vibration - multiple rapid pulses
    /// </summary>
    public void ExplosionVibration()
    {
        if (!enableHaptics) return;
        CancelInvoke(nameof(VibratePulse));

        for (int i = 0; i < 5; i++)
        {
            Invoke(nameof(VibratePulse), i * minVibrationInterval);
        }

        lastVibrationTime = Time.time;
    }

    private bool CanVibrate() => Time.time - lastVibrationTime >= minVibrationInterval;

    private void VibratePulse()
    {
        Handheld.Vibrate();
    }

    [Button("Test Light")]
    private void TestLight() => LightTap();

    [Button("Test Medium")]
    private void TestMedium() => MediumTap();

    [Button("Test Heavy")]
    private void TestHeavy() => HeavyVibration();
    
    [Button("Test Explosion")]
    private void TestExplosion() => ExplosionVibration();
}
