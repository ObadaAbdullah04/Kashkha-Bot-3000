using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Simple haptic feedback for mobile.
/// </summary>
public class HapticFeedback : MonoBehaviour
{
    public static HapticFeedback Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableHaptics = true;
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

    public void LightTap()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        lastVibrationTime = Time.time;
    }

    public void MediumTap()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        lastVibrationTime = Time.time;
    }

    public void HeavyVibration()
    {
        if (!enableHaptics || !CanVibrate()) return;
        Handheld.Vibrate();
        Invoke(nameof(VibrateAgain), minVibrationInterval);
        lastVibrationTime = Time.time;
    }

    public void ExplosionVibration()
    {
        if (!enableHaptics) return;
        CancelInvoke(nameof(VibrateAgain));

        for (int i = 0; i < 5; i++)
            Invoke(nameof(Handheld.Vibrate), i * minVibrationInterval);

        lastVibrationTime = Time.time;
    }

    private bool CanVibrate() => Time.time - lastVibrationTime >= minVibrationInterval;

    private void VibrateAgain() => Handheld.Vibrate();

    [Button("Test Light")]
    private void TestLight() => LightTap();

    [Button("Test Heavy")]
    private void TestHeavy() => HeavyVibration();
}
