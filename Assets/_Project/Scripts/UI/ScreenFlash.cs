using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// PHASE 4: Screen flash effect for correct/wrong answers.
/// Attach to a full-screen Image overlay in the Canvas.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create empty GameObject under Canvas
/// 2. Add Image component
/// 3. Set Image color to white (or any flash color)
/// 4. Set Image type to Simple Filled
/// 5. Stretch RectTransform to fill entire screen (anchor: stretch, pivot: 0.5,0.5)
/// 6. Add ScreenFlash component
/// 7. Set "flashImage" reference to the Image component
/// 8. Set CanvasGroup.alpha to 0 initially
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    #region Singleton

    public static ScreenFlash Instance { get; private set; }

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

    #endregion

    #region Inspector Fields

    [Header("References")]
    [Tooltip("Full-screen Image for flash effect")]
    [SerializeField] private Image flashImage;

    [Tooltip("CanvasGroup for alpha control")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Flash Settings")]
    [SerializeField] private Color correctFlashColor = new Color(0.2f, 1f, 0.4f, 0.3f); // Green tint
    [SerializeField] private Color wrongFlashColor = new Color(1f, 0.2f, 0.2f, 0.4f); // Red tint
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Ease flashEase = Ease.OutQuad;

    #endregion

    #region Private Fields

    private Sequence _flashTween;
    private Color _originalColor;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Validate references
        if (flashImage == null)
            flashImage = GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Ensure flash starts hidden
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (flashImage != null)
            _originalColor = flashImage.color;
    }

    private void OnDisable()
    {
        _flashTween?.Kill();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Triggers a flash effect for correct answer.
    /// </summary>
    public void FlashCorrect()
    {
        TriggerFlash(correctFlashColor);
    }

    /// <summary>
    /// Triggers a flash effect for wrong answer.
    /// </summary>
    public void FlashWrong()
    {
        TriggerFlash(wrongFlashColor);
    }

    /// <summary>
    /// Triggers a custom flash effect.
    /// </summary>
    public void TriggerFlash(Color flashColor, float duration = 0f)
    {
        if (flashImage == null || canvasGroup == null)
        {
            // Debug.LogWarning("[ScreenFlash] Missing references!");
            return;
        }

        // Kill existing flash
        _flashTween?.Kill();

        // Set flash color
        flashImage.color = flashColor;

        // Flash animation: 0 -> 1 -> 0 alpha
        float actualDuration = duration > 0 ? duration : flashDuration;

        _flashTween = DOTween.Sequence()
            .SetRecyclable(true)
            .SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, actualDuration / 2).SetEase(flashEase))
            .Append(canvasGroup.DOFade(0f, actualDuration / 2).SetEase(flashEase))
            .OnComplete(() =>
            {
                // Reset to original color
                flashImage.color = _originalColor;
            });
    }

    #endregion

    #region Inspector Test Buttons

    #if UNITY_EDITOR
    [UnityEngine.ContextMenu("Test Correct Flash")]
    private void TestCorrect()
    {
        FlashCorrect();
    }

    [UnityEngine.ContextMenu("Test Wrong Flash")]
    private void TestWrong()
    {
        FlashWrong();
    }
    #endif

    #endregion
}
