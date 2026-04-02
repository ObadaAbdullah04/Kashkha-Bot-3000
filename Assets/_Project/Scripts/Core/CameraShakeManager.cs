using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Camera shake manager using Cinemachine Impulse with DOTween fallback.
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Impulse Source")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Shake Strengths")]
    [SerializeField] private float wrongAnswerStrength = 0.25f;
    [SerializeField] private float gameOverStrength = 0.80f;
    [SerializeField] private float explosionStrength = 1.10f;
    [SerializeField] private float qteFailStrength = 0.15f;

    [Header("Fallback")]
    [SerializeField] private Camera targetCamera;

    private Tween _shakeTween;
    private Vector3 _originalCameraPos;

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
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;
        
        if (targetCamera != null)
            _originalCameraPos = targetCamera.transform.position;
    }

    private void OnEnable()
    {
        // Re-validate camera position in case of scene changes (though we are single-scene)
        if (targetCamera != null)
            _originalCameraPos = targetCamera.transform.position;
    }

    [Button("Test Wrong Answer")]
    public void ShakeWrongAnswer() => Shake(wrongAnswerStrength, 0.5f);

    [Button("Test Game Over")]
    public void ShakeSocialShutdown() => Shake(gameOverStrength, 1.0f);

    [Button("Test Explosion")]
    public void ShakeMaamoulExplosion() => Shake(explosionStrength, 1.2f);

    [Button("Test QTE Fail")]
    public void ShakeQTEFail() => Shake(qteFailStrength, 0.3f);

    private void Shake(float strength, float duration)
    {
        if (impulseSource != null)
        {
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
            impulseSource.GenerateImpulse(strength);
            Debug.Log($"[CameraShake] Impulse: {strength}, {duration}s");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("[CameraShake] No camera!");
            return;
        }

        _shakeTween?.Kill();
        targetCamera.transform.position = _originalCameraPos;

        _shakeTween = targetCamera.transform
            .DOShakePosition(duration, strength * 0.5f, 20, 90, true)
            .SetUpdate(true)
            .OnComplete(() => targetCamera.transform.position = _originalCameraPos);

        Debug.Log($"[CameraShake] DOTween fallback");
    }
}
