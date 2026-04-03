using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles per-card animations for a dialogue choice card.
/// </summary>
public class ChoiceCard : MonoBehaviour
{
    [Header("Card Identity")]
    [SerializeField] private int cardIndex;
    private int _logicIndex;

    private Tween _idleTween;
    private Vector2 _originalAnchoredPosition;
    private Button _button;

    public void SetLogicIndex(int index) => _logicIndex = index;

    private void Awake()
    {
        if (transform is RectTransform rt)
            _originalAnchoredPosition = rt.anchoredPosition;

        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnCardClicked);
    }

    private void OnEnable()
    {
        // Ensure position is restored when re-enabled (for object pooling)
        if (transform is RectTransform rt)
            rt.anchoredPosition = _originalAnchoredPosition;
    }

    private void OnDisable()
    {
        StopIdle();
        if (transform is RectTransform rt)
            rt.anchoredPosition = _originalAnchoredPosition;
        transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_button != null)
            _button.onClick.RemoveListener(OnCardClicked);
    }

    public void SetIdleFloating(bool active)
    {
        StopIdle();

        if (!active)
        {
            if (transform is RectTransform rt)
                rt.anchoredPosition = _originalAnchoredPosition;
            return;
        }

        float targetY = _originalAnchoredPosition.y + 7f;
        float delay = cardIndex * 0.15f;

        if (transform is RectTransform rectTransform)
        {
            Vector2 targetAnchor = new Vector2(_originalAnchoredPosition.x, targetY);

            _idleTween = DOTween.To(
                () => rectTransform.anchoredPosition,
                x => rectTransform.anchoredPosition = x,
                targetAnchor,
                1.25f
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(delay)
            .SetUpdate(true);
        }
    }

    [Button("Test Correct")]
    public void AnimateCorrect()
    {
        StopIdle();
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.20f, 0.45f, 8, 0.6f).SetUpdate(true);
    }

    [Button("Test Wrong")]
    public void AnimateWrong()
    {
        StopIdle();
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOShakeScale(0.38f, 0.22f, 22, 90).SetUpdate(true);
    }

    public void ResetInstant()
    {
        StopIdle();
        transform.DOKill();
        if (transform is RectTransform rt)
            rt.anchoredPosition = _originalAnchoredPosition;
        transform.localScale = Vector3.one;
    }

    private void StopIdle()
    {
        _idleTween?.Kill(complete: false);
        _idleTween = null;
    }

    /// <summary>
    /// Public method to kill all tweens on this card (called by UIManager.OnDisable).
    /// </summary>
    public void KillTweens()
    {
        StopIdle();
        transform.DOKill();
    }

    private void OnCardClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ProcessChoice(_logicIndex);
    }
}
