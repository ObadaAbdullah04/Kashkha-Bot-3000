using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 13: Manages the interaction HUD lifecycle for standalone gameplay moments.
/// 
/// RESPONSIBILITIES:
/// 1. Shows interaction HUD with prompt, timer, and progress indicator
/// 2. Monitors player input via InputManager (shake, hold, tap, draw)
/// 3. Evaluates success based on threshold and duration
/// 4. Provides feedback (green/red flash) and updates meters
/// 5. Calls onComplete callback when finished
/// 
/// USAGE:
/// - Attach to a Canvas GameObject in the scene
/// - Assign UI references in Inspector (prompt text, timer bar, counter text, icon image)
/// - HouseFlowController calls: interactionHUDController.RunInteraction(data, onComplete)
/// 
/// UI LAYOUT:
/// ┌─────────────────────┐
/// │  [Icon] هز الكوب!   │  ← Prompt text + sprite
/// │  ████████░░  3.2s    │  ← Timer progress bar
/// │  Shakes: 3/5         │  ← Progress counter
/// └─────────────────────┘
/// </summary>
public class InteractionHUDController : MonoBehaviour
{
    public static InteractionHUDController Instance { get; private set; }

    #region Inspector Fields

    [Header("UI References")]
    [Tooltip("Root panel of the interaction HUD")]
    [SerializeField] private RectTransform hudPanel;

    [Tooltip("Icon image for the interaction type")]
    [SerializeField] private Image iconImage;

    [Tooltip("Prompt text (Arabic instruction)")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("Timer progress bar (0-1)")]
    [SerializeField] private Image timerBar;

    [Tooltip("Progress counter text (e.g., 'Shakes: 3/5')")]
    [SerializeField] private TextMeshProUGUI counterText;

    [Tooltip("Icon sprites folder in Resources")]
    [SerializeField] private string iconSpritesPath = "InteractionIcons";

    [Header("Timing")]
    [Tooltip("Default duration if CSV has 0 (seconds)")]
    [SerializeField] private float defaultDuration = 5f;

    [Tooltip("Warning threshold - bar turns red (seconds)")]
    [SerializeField] private float warningThreshold = 2f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failureColor = Color.red;

    [Header("Animations")]
    [Tooltip("Entrance animation duration")]
    [SerializeField] private float entranceDuration = 0.3f;

    [Tooltip("Exit animation duration")]
    [SerializeField] private float exitDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    #endregion

    #region State

    private InteractionData currentInteraction;
    private Action<bool, float, int> onCompleteCallback;
    private float elapsed = 0f;
    private bool isActive = false;
    private Sequence entranceTween;
    private Sequence exitTween;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("[InteractionHUDController] Duplicate instance! Destroying.");
            Destroy(gameObject);
            return;
        }

        // Hide panel initially
        if (hudPanel != null)
        {
            hudPanel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isActive || currentInteraction == null) return;

        UpdateTimer();
        UpdateProgress();
        CheckCompletion();
    }

    private void OnDestroy()
    {
        // Kill tweens to prevent memory leaks
        entranceTween?.Kill();
        exitTween?.Kill();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts an interaction. Shows HUD, monitors input, calls onComplete when done.
    /// </summary>
    /// <param name="data">InteractionData from DataManager</param>
    /// <param name="onComplete">Callback: (succeeded, batteryDelta, eidiaReward)</param>
    public void RunInteraction(InteractionData data, Action<bool, float, int> onComplete)
    {
        if (data == null)
        {
            Debug.LogError("[InteractionHUDController] Null InteractionData!");
            onComplete?.Invoke(false, 0, 0);
            return;
        }

        currentInteraction = data;
        onCompleteCallback = onComplete;
        elapsed = 0f;
        isActive = true;

        // Reset input manager state
        InputManager.Instance?.ResetInteractionState();

        // Update UI
        UpdateUI();

        // Show panel with entrance animation
        ShowPanel();

        if (debugLogging)
            Debug.Log($"[InteractionHUDController] Starting: {data.ID} | Type:{data.InteractionType} | Duration:{data.Duration}s | Threshold:{data.Threshold}");
    }

    #endregion

    #region Private Methods

    private void UpdateUI()
    {
        if (currentInteraction == null) return;

        // Set prompt text
        if (promptText != null)
        {
            promptText.text = !string.IsNullOrEmpty(currentInteraction.PromptTextAR)
                ? currentInteraction.PromptTextAR
                : currentInteraction.InteractionType.GetArabicLabel();
        }

        // Set icon
        if (iconImage != null)
        {
            string spriteName = currentInteraction.InteractionType.GetIconSpriteName();
            Sprite sprite = Resources.Load<Sprite>($"{iconSpritesPath}/{spriteName}");
            
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[InteractionHUDController] Icon sprite not found: {spriteName}. Using fallback.");
                iconImage.enabled = false;
            }
        }

        // Set initial counter text
        UpdateCounterText(0);

        // Set timer bar to full
        if (timerBar != null)
        {
            timerBar.fillAmount = 1f;
            timerBar.color = normalColor;
        }
    }

    private void UpdateTimer()
    {
        // Skip timer if duration is 0 (unlimited)
        if (currentInteraction.Duration <= 0) return;

        elapsed += Time.deltaTime;

        float duration = currentInteraction.Duration > 0 ? currentInteraction.Duration : defaultDuration;
        float remaining = Mathf.Max(0, duration - elapsed);
        float progress = remaining / duration;

        // Update timer bar
        if (timerBar != null)
        {
            timerBar.fillAmount = progress;

            // Color changes based on remaining time
            if (remaining <= warningThreshold)
            {
                timerBar.color = dangerColor;
            }
            else if (remaining <= warningThreshold * 2)
            {
                timerBar.color = warningColor;
            }
            else
            {
                timerBar.color = normalColor;
            }
        }
    }

    private void UpdateProgress()
    {
        float currentValue = GetCurrentValue();
        UpdateCounterText(currentValue);
    }

    private float GetCurrentValue()
    {
        if (InputManager.Instance == null) return 0;

        return currentInteraction.InteractionType switch
        {
            InteractionType.Shake => InputManager.Instance.GetShakeCount(),
            InteractionType.Hold => InputManager.Instance.GetHoldDuration(),
            InteractionType.Tap => InputManager.Instance.GetTapCount(),
            InteractionType.Draw => 0, // Draw uses its own completion logic
            _ => 0
        };
    }

    private void UpdateCounterText(float currentValue)
    {
        if (counterText == null) return;

        string label = currentInteraction.InteractionType switch
        {
            InteractionType.Shake => $"Shakes",
            InteractionType.Hold => $"Hold",
            InteractionType.Tap => $"Taps",
            InteractionType.Draw => $"Draw",
            _ => "Progress"
        };

        // For Draw type, show "In Progress" instead of counter
        if (currentInteraction.InteractionType == InteractionType.Draw)
        {
            counterText.text = "In Progress...";
        }
        else
        {
            counterText.text = $"{label}: {Mathf.FloorToInt(currentValue)}/{Mathf.FloorToInt(currentInteraction.Threshold)}";
        }
    }

    private void CheckCompletion()
    {
        float currentValue = GetCurrentValue();
        bool succeeded = currentInteraction.CheckThreshold(currentValue);

        // Check timeout
        bool timedOut = currentInteraction.Duration > 0 && elapsed >= currentInteraction.Duration;

        if (succeeded)
        {
            // Threshold met - success!
            CompleteInteraction(true);
        }
        else if (timedOut)
        {
            // Time's up - check if close enough to threshold
            bool partialSuccess = currentValue >= currentInteraction.Threshold * 0.5f; // 50% threshold = partial credit
            CompleteInteraction(partialSuccess);
        }
    }

    private void CompleteInteraction(bool succeeded)
    {
        if (!isActive) return;
        isActive = false;

        float batteryDelta = currentInteraction.GetBatteryDelta(succeeded);
        int eidiaReward = currentInteraction.GetEidReward(succeeded);

        if (debugLogging)
            Debug.Log($"[InteractionHUDController] {(succeeded ? "SUCCESS" : "FAILED")}: {currentInteraction.ID} | Battery:{batteryDelta} | Eid:{eidiaReward}");

        // Show feedback flash
        FlashResult(succeeded, () =>
        {
            // Update meters
            MeterManager.Instance?.ModifyBattery(batteryDelta);
            SaveManager.Instance?.AddRunRewards(eidiaReward);

            // Hide panel
            HidePanel(() =>
            {
                // Call completion callback
                onCompleteCallback?.Invoke(succeeded, batteryDelta, eidiaReward);
                onCompleteCallback = null;
                currentInteraction = null;
            });
        });
    }

    #endregion

    #region UI Animations

    private void ShowPanel()
    {
        if (hudPanel == null) return;

        hudPanel.gameObject.SetActive(true);
        entranceTween?.Kill();

        // Start hidden and scaled down
        CanvasGroup canvasGroup = hudPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = hudPanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        hudPanel.localScale = Vector3.one * 0.5f;

        // Animate in
        entranceTween = DOTween.Sequence();
        entranceTween.Append(canvasGroup.DOFade(1f, entranceDuration).SetEase(Ease.OutCubic));
        entranceTween.Join(hudPanel.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack));
        entranceTween.Play();
    }

    private void HidePanel(Action onComplete)
    {
        if (hudPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        exitTween?.Kill();

        CanvasGroup canvasGroup = hudPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = hudPanel.gameObject.AddComponent<CanvasGroup>();

        exitTween = DOTween.Sequence();
        exitTween.Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));
        exitTween.Join(hudPanel.DOScale(Vector3.one * 0.5f, exitDuration).SetEase(Ease.InBack));
        exitTween.OnComplete(() =>
        {
            hudPanel.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
        exitTween.Play();
    }

    private void FlashResult(bool succeeded, Action onComplete)
    {
        if (timerBar == null)
        {
            onComplete?.Invoke();
            return;
        }

        Color targetColor = succeeded ? successColor : failureColor;
        Sequence flashSeq = DOTween.Sequence();
        flashSeq.Append(timerBar.DOColor(targetColor, 0.15f).SetEase(Ease.OutQuad));
        flashSeq.Append(timerBar.DOColor(normalColor, 0.3f).SetEase(Ease.InQuad));
        flashSeq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion
}
