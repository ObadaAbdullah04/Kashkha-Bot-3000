using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// URP post-processing effects controller.
/// </summary>
public class URPPostProcessing : MonoBehaviour
{
    public static URPPostProcessing Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Volume globalVolume;

    [Header("Settings")]
    [Range(0f, 1f), SerializeField] private float panicChromaticAberration = 0.5f;
    [Range(0f, 1f), SerializeField] private float gameOverVignette = 0.6f;
    [SerializeField] private float pulseDuration = 0.15f;

    private VolumeProfile profile;
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    private bool isInitialized = false;
    private Tween _chromaticTween;
    private Tween _vignetteTween;

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

    private void OnEnable()
    {
        TimerController.OnPanicModeChanged += HandlePanicMode;
    }

    private void OnDisable()
    {
        TimerController.OnPanicModeChanged -= HandlePanicMode;
    }

    private void Start() => Initialize();

    private void HandlePanicMode(bool isPanic)
    {
        if (isPanic) EnablePanicMode();
        else DisablePanicMode();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        if (globalVolume == null)
        {
            Debug.LogWarning("[URP] No Global Volume assigned in Inspector! Searching...");
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("[URP] FATAL: No Global Volume found in scene! Post-processing disabled.");
            return;
        }

        profile = globalVolume.profile;
        if (profile == null)
        {
            Debug.LogError("[URP] Volume has no profile assigned!");
            return;
        }

        // Cache components to avoid repeated TryGet calls
        profile.TryGet(out chromaticAberration);
        profile.TryGet(out vignette);

        ResetEffects();
        isInitialized = true;
        Debug.Log($"[URP] Initialized successfully. Profile: {profile.name}");
    }

    public void EnablePanicMode()
    {
        if (!isInitialized || chromaticAberration == null) return;

        _chromaticTween?.Kill();
        _chromaticTween = DOTween.To(
            () => chromaticAberration.intensity.value,
            x => chromaticAberration.intensity.Override(x),
            panicChromaticAberration,
            0.3f
        ).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void DisablePanicMode()
    {
        if (!isInitialized || chromaticAberration == null) return;

        _chromaticTween?.Kill();
        _chromaticTween = DOTween.To(
            () => chromaticAberration.intensity.value,
            x => chromaticAberration.intensity.Override(x),
            0f,
            0.5f
        ).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void PulseChromaticAberration()
    {
        if (!isInitialized || chromaticAberration == null) return;

        _chromaticTween?.Kill();
        _chromaticTween = DOTween.Sequence()
            .Append(DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.Override(x),
                panicChromaticAberration,
                pulseDuration / 2
            ).SetEase(Ease.Linear))
            .Append(DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.Override(x),
                0f,
                pulseDuration / 2
            ).SetEase(Ease.Linear))
            .SetUpdate(true);
    }

    public void EnableGameOverEffect()
    {
        if (!isInitialized) return;

        _chromaticTween?.Kill();
        _vignetteTween?.Kill();

        if (chromaticAberration != null)
        {
            _chromaticTween = DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.Override(x),
                panicChromaticAberration,
                0.5f
            ).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (vignette != null)
        {
            _vignetteTween = DOTween.To(
                () => vignette.intensity.value,
                x => vignette.intensity.Override(x),
                gameOverVignette,
                0.5f
            ).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        Debug.Log("[URP] Game over effects enabled!");
    }

    public void ResetEffects()
    {
        if (!isInitialized) return;

        _chromaticTween?.Kill();
        _vignetteTween?.Kill();

        if (vignette != null)
        {
            DOTween.To(
                () => vignette.intensity.value,
                x => vignette.intensity.Override(x),
                0f,
                0.5f
            ).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (chromaticAberration != null)
        {
            DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.Override(x),
                0f,
                0.5f
            ).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    [Button("Test Panic")]
    private void TestPanic() => EnablePanicMode();

    [Button("Test Game Over")]
    private void TestGameOver() => EnableGameOverEffect();

    [Button("Test Pulse")]
    private void TestPulse() => PulseChromaticAberration();

    [Button("Reset")]
    private void TestReset() => ResetEffects();
}
