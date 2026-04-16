using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;

/// <summary>
/// PHASE 4: Single floating text popup with CanvasGroup alpha fading.
/// 
/// PREFAB SETUP INSTRUCTIONS:
/// 1. Create empty GameObject under Canvas
/// 2. Add components:
///    - CanvasGroup (required for alpha fading)
///    - RTLTextMeshPro (for Arabic text)
///    - FloatingText (this script)
/// 3. Set CanvasGroup.alpha = 1, CanvasGroup.interactable = false, CanvasGroup.blocksRaycasts = false
/// 4. Assign RTLTextMeshPro reference in Inspector
/// 5. Set anchor to center, pivot to center
/// 6. Save as prefab: Assets/_Project/Prefabs/UI/FloatingText.prefab
/// 
/// PERFORMANCE NOTES:
/// - Uses CanvasGroup.alpha instead of textMesh.alpha (more efficient)
/// - All tweens use SetRecyclable(true) for DOTween optimization
/// - No allocations in Spawn() method
/// </summary>
public class FloatingText : MonoBehaviour
{
    #region Inspector Fields

    [Header("References")]
    [Tooltip("RTLTextMeshPro component (auto-find if not assigned)")]
    [SerializeField] private RTLTextMeshPro textMesh;

    [Tooltip("CanvasGroup for alpha fading (auto-find if not assigned)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float initialScaleDuration = 0.3f;
    [SerializeField] private Ease initialScaleEase = Ease.OutBack;
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private Ease fadeEase = Ease.InExpo;

    #endregion

    #region Private Fields

    private Sequence _activeTween;
    private Vector3 _startPosition;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Auto-find components if not assigned
        if (textMesh == null)
            textMesh = GetComponent<RTLTextMeshPro>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Ensure CanvasGroup is configured correctly
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        _startPosition = transform.localPosition;
    }

    private void OnDisable()
    {
        // Clean up tween when disabled
        KillTween();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Spawns the floating text with animation.
    /// Called by FloatingTextManager when spawning from pool.
    /// </summary>
    /// <param name="content">Text to display</param>
    /// <param name="color">Text color</param>
    /// <param name="floatDistance">How far to float up</param>
    /// <param name="totalDuration">Total animation duration</param>
    public void Spawn(string content, Color color, float floatDistance, float totalDuration)
    {
        if (textMesh == null || canvasGroup == null)
        {
            // Debug.LogError("[FloatingText] Missing references!");
            return;
        }

        // Kill any existing tween
        KillTween();

        // Reset state
        _startPosition = transform.localPosition;
        textMesh.text = content;
        textMesh.color = color;
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;

        // Create animation sequence
        _activeTween = DOTween.Sequence()
            .SetRecyclable(true) // Enable recycling for performance
            .SetUpdate(true)     // Update in unscaled time (works during pause)

            // Phase 1: Punch scale up
            .Append(transform.DOScale(Vector3.one, initialScaleDuration).SetEase(initialScaleEase))

            // Phase 2: Float up + fade out (parallel)
            .Join(
                transform.DOLocalMoveY(_startPosition.y + floatDistance, moveDuration)
                    .SetEase(moveEase)
                    .SetDelay(initialScaleDuration)
            )
            .Join(
                canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase).SetDelay(initialScaleDuration)
            )

            // Phase 3: Cleanup
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                transform.localScale = Vector3.one; // Reset for next spawn
                transform.localPosition = _startPosition;
                textMesh.alpha = 1f; // Reset alpha for next spawn
            });
    }

    /// <summary>
    /// Kills the active tween (called when returning to pool).
    /// </summary>
    public void KillTween()
    {
        if (_activeTween != null)
        {
            _activeTween.Kill();
            _activeTween = null;
        }
    }

    #endregion

    #region Inspector Test Buttons

    #if UNITY_EDITOR
    [UnityEngine.ContextMenu("Test Spawn")]
    private void TestSpawn()
    {
        Spawn("Test +25", Color.green, 80f, 1.5f);
    }

    [UnityEngine.ContextMenu("Kill Tween")]
    private void TestKill()
    {
        KillTween();
    }
    #endif

    #endregion
}
