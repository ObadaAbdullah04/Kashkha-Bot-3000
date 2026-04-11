using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using RTLTMPro;

/// <summary>
/// PHASE 7 REFACTORED: Tinder-style swipe card with DOTween animations and result feedback.
/// 
/// NEW FEATURES:
/// - Card name displayed as title above question
/// - Neutral drag tint (blue/gray) - NO green/red spoilers during drag!
/// - Result feedback flash AFTER swipe (green = correct, red = wrong)
/// - Elastic entrance animation, smooth drag tilt, fly-off on swipe
/// 
/// SETUP:
/// 1. Create UI card prefab with Image background + RTLTextMeshPro for text
/// 2. Add this script to the card GameObject
/// 3. Assign all text fields (cardNameText, speakerText, questionText, option texts)
/// 4. Card handles drag, tilt, and swipe-away animations
/// </summary>
public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region Inspector Fields

    [Header("Card Content")]
    [Tooltip("Character portrait image (loaded from Resources/CharacterSprites/)")]
    [SerializeField] private UnityEngine.UI.Image characterImage;

    [Tooltip("Default sprite if character sprite not found")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("Speaker name text (RTLTextMeshPro) - displayed below character sprite")]
    [SerializeField] private RTLTextMeshPro speakerText;

    [Tooltip("Question text (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro questionText;

    [Tooltip("Right side option text (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro rightOptionText;

    [Tooltip("Left side option text (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro leftOptionText;

    [Tooltip("Feedback text shown after swipe result (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro feedbackText;

    [Header("Card Visuals")]
    [Tooltip("Background image or panel (for color tinting on swipe)")]
    [SerializeField] private UnityEngine.UI.Image backgroundImage;

    [Tooltip("CanvasGroup for alpha fading")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Swipe Settings")]
    [Tooltip("Horizontal distance threshold to register a swipe (pixels)")]
    [SerializeField] private float swipeThreshold = 150f;

    [Tooltip("Maximum rotation angle during drag (degrees)")]
    [SerializeField] private float maxRotationAngle = 25f;

    [Tooltip("Swipe-away animation distance (pixels off-screen)")]
    [SerializeField] private float swipeAwayDistance = 800f;

    [Tooltip("Animation duration for swipe-away")]
    [SerializeField] private float swipeAnimDuration = 0.4f;

    [Tooltip("Animation duration for snap-back (if not swiped far enough)")]
    [SerializeField] private float snapBackDuration = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Neutral color tint during drag (blue/gray - no spoilers!)")]
    [SerializeField] private Color neutralDragColor = new Color(0.4f, 0.6f, 0.9f, 0.3f);

    [Tooltip("Green flash color for correct answer")]
    [SerializeField] private Color correctFlashColor = new Color(0.2f, 0.9f, 0.2f, 0.8f);

    [Tooltip("Red flash color for incorrect answer")]
    [SerializeField] private Color incorrectFlashColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);

    [Tooltip("Feedback text display duration (seconds)")]
    [SerializeField] private float feedbackTextDuration = 1.5f;

    #endregion

    #region Events

    /// <summary>
    /// Fires when card is swiped. Direction: -1 = left, +1 = right.
    /// </summary>
    public static System.Action<SwipeCard, int> OnCardSwiped;

    #endregion

    #region Private Fields

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isSwiped = false; // Prevents double-swipe
    private SwipeCardData cardData;
    private int cardIndex;
    private int totalCards;
    private Sequence _feedbackSequence;
    private bool _hasTriggeredHaptic = false; // Track if haptic already triggered

    #endregion

    #region Public Properties

    public SwipeCardData Data => cardData;
    public int CardIndex => cardIndex;
    public int TotalCards => totalCards;
    public bool IsSwiped => isSwiped;

    #endregion

    #region Initialization

    /// <summary>
    /// Sets up the card with data and display info.
    /// </summary>
    public void Setup(SwipeCardData data, int index, int total)
    {
        cardData = data;
        cardIndex = index;
        totalCards = total;

        // Load character sprite
        if (characterImage != null && !string.IsNullOrEmpty(data.SpriteName))
        {
            Sprite sprite = Resources.Load<Sprite>("CharacterSprites/" + data.SpriteName);

            if (sprite != null)
            {
                characterImage.sprite = sprite;
                characterImage.enabled = true;
            }
            else if (defaultSprite != null)
            {
                characterImage.sprite = defaultSprite;
                characterImage.enabled = true;
#if UNITY_EDITOR
                Debug.LogWarning($"[SwipeCard] Sprite not found: 'CharacterSprites/{data.SpriteName}'. Using default.");
#endif
            }
            else
            {
                characterImage.enabled = false;
            }
        }
        else if (characterImage != null)
        {
            // No sprite name provided, hide character image
            characterImage.enabled = false;
        }

        // Set speaker name text
        if (speakerText != null && !string.IsNullOrEmpty(data.Speaker))
        {
            speakerText.text = data.Speaker;
            speakerText.gameObject.SetActive(true);
        }
        else if (speakerText != null)
        {
            speakerText.gameObject.SetActive(false);
        }

        if (questionText != null)
            questionText.text = data.QuestionAR;

        // Set option texts based on which side is correct
        // Right side shows correct answer if RightIsCorrect, otherwise shows wrong answer
        if (rightOptionText != null)
            rightOptionText.text = data.RightIsCorrect ? data.OptionCorrectAR : data.OptionWrongAR;

        // Left side shows wrong answer if RightIsCorrect, otherwise shows correct answer
        if (leftOptionText != null)
            leftOptionText.text = data.RightIsCorrect ? data.OptionWrongAR : data.OptionCorrectAR;

        // Reset visual state
        ResetCard();

#if UNITY_EDITOR
        Debug.Log($"[SwipeCard] Setup: Card {index + 1}/{total} - Speaker: {data.Speaker}");
#endif
    }

    /// <summary>
    /// Resets the card to its original position and state.
    /// </summary>
    public void ResetCard()
    {
        isSwiped = false;
        _hasTriggeredHaptic = false; // Reset haptic flag
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (backgroundImage != null)
            backgroundImage.color = Color.white;

        // Reset option texts to full opacity
        if (rightOptionText != null)
            rightOptionText.alpha = 1f;

        if (leftOptionText != null)
            leftOptionText.alpha = 1f;

        // Hide feedback text
        if (feedbackText != null)
        {
            feedbackText.text = "";
            feedbackText.alpha = 0f;
        }
    }

    #endregion

    #region Drag Handling

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSwiped) return;
        // Disable raycast blocking during drag
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSwiped) return;

        // Calculate drag delta
        Vector3 dragDelta = new Vector3(eventData.delta.x, eventData.delta.y, 0f);
        transform.localPosition += dragDelta;

        // Calculate rotation based on horizontal drag
        float xDelta = transform.localPosition.x - originalPosition.x;
        float rotationZ = Mathf.Clamp(xDelta / swipeThreshold * maxRotationAngle, -maxRotationAngle, maxRotationAngle);
        transform.localRotation = Quaternion.Euler(0, 0, rotationZ);

        // Calculate swipe progress (-1 to 1)
        float swipeProgress = Mathf.Clamp(xDelta / swipeThreshold, -1f, 1f);

        // Update visual feedback (NEUTRAL tint - no spoilers!)
        UpdateSwipeFeedback(swipeProgress);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSwiped) return;

        // Re-enable raycast blocking
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // Check if swipe threshold was met
        float xDelta = transform.localPosition.x - originalPosition.x;

        if (Mathf.Abs(xDelta) >= swipeThreshold)
        {
            // Swipe committed!
            int direction = xDelta > 0 ? 1 : -1; // +1 = right, -1 = left
            SwipeAway(direction);
        }
        else
        {
            // Snap back to center
            SnapBack();
        }
    }

    #endregion

    #region Visual Feedback

    private void UpdateSwipeFeedback(float swipeProgress)
    {
        // Progress: -1 = fully left, 0 = center, +1 = fully right
        // NEUTRAL tint only - NO green/red (spoilers!)

        // Background tint (neutral blue/gray)
        if (backgroundImage != null)
        {
            float intensity = Mathf.Abs(swipeProgress);
            backgroundImage.color = Color.Lerp(Color.white, neutralDragColor, intensity);
        }

        // Fade option texts based on swipe direction
        if (rightOptionText != null)
            rightOptionText.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, swipeProgress));

        if (leftOptionText != null)
            leftOptionText.alpha = Mathf.Clamp01(1f - Mathf.Max(0f, -swipeProgress));

        // Haptic feedback when crossing threshold (commitment point)
        if (!_hasTriggeredHaptic && Mathf.Abs(swipeProgress) >= 0.95f)
        {
            _hasTriggeredHaptic = true;
            HapticFeedback.Instance?.LightTap();
        }
    }

    #endregion

    #region Result Feedback Flash

    /// <summary>
    /// Shows green/red flash AFTER swipe with feedback text.
    /// Called by SwipeEncounterManager after processing the swipe.
    /// </summary>
    public void ShowResultFeedback(bool wasCorrect, string feedback)
    {
        Color flashColor = wasCorrect ? correctFlashColor : incorrectFlashColor;

        // Flash background color
        if (backgroundImage != null)
        {
            backgroundImage.DOColor(flashColor, 0.2f)
                .OnComplete(() => backgroundImage.DOColor(Color.white, 0.4f));
        }

        // Show feedback text with fade-in and fade-out
        if (feedbackText != null)
        {
            feedbackText.text = feedback;
            feedbackText.color = flashColor;
            feedbackText.alpha = 0f;

            _feedbackSequence?.Kill();
            _feedbackSequence = DOTween.Sequence()
                .Append(feedbackText.DOFade(1f, 0.3f))
                .AppendInterval(feedbackTextDuration)
                .Append(feedbackText.DOFade(0f, 0.3f));
        }
    }

    #endregion

    #region Swipe Animations

    /// <summary>
    /// Animates the card flying off-screen in the given direction.
    /// </summary>
    private void SwipeAway(int direction)
    {
        if (isSwiped) return;
        isSwiped = true;

        // Calculate target position (off-screen)
        Vector3 targetPos = transform.localPosition;
        targetPos.x = originalPosition.x + (swipeAwayDistance * direction);
        targetPos.y += direction * 50f; // Slight arc

        // Animate swipe away
        Sequence swipeSeq = DOTween.Sequence();
        swipeSeq.Append(transform.DOLocalMove(targetPos, swipeAnimDuration).SetEase(Ease.OutBack));
        swipeSeq.Join(transform.DORotate(new Vector3(0, 0, direction * maxRotationAngle * 2), swipeAnimDuration).SetEase(Ease.OutBack));
        swipeSeq.Join(canvasGroup != null
            ? canvasGroup.DOFade(0f, swipeAnimDuration)
            : null);

        // Fire event
        swipeSeq.OnComplete(() =>
        {
            OnCardSwiped?.Invoke(this, direction);
        });
    }

    /// <summary>
    /// Snaps the card back to center (swipe not committed).
    /// </summary>
    private void SnapBack()
    {
        Sequence snapSeq = DOTween.Sequence();
        snapSeq.Append(transform.DOLocalMove(originalPosition, snapBackDuration).SetEase(Ease.OutBack));
        snapSeq.Join(transform.DOLocalRotateQuaternion(originalRotation, snapBackDuration));

        // Reset feedback
        if (backgroundImage != null)
            backgroundImage.DOColor(Color.white, snapBackDuration);

        // Reset option texts to full opacity
        if (rightOptionText != null)
            rightOptionText.DOFade(1f, snapBackDuration);

        if (leftOptionText != null)
            leftOptionText.DOFade(1f, snapBackDuration);
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        // Kill all tweens on this card to prevent memory leaks
        _feedbackSequence?.Kill();
        DOTween.Kill(transform);
        if (canvasGroup != null)
            DOTween.Kill(canvasGroup);
        if (backgroundImage != null)
            DOTween.Kill(backgroundImage);
    }

    #endregion
}
