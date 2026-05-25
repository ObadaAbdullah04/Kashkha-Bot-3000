using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;


/// <summary>
/// PHASE 13: Manages the interaction HUD lifecycle for standalone gameplay moments.
/// 
/// RESPONSIBILITIES:
/// 1. Shows interaction HUD with prompt, timer, and progress indicator
/// 2. Monitors player input via InputManager (shake, hold, tap, draw)
/// 3. Evaluates success based on threshold and duration
/// 4. Provides feedback (green/red flash) and updates meters
/// 5. Calls onComplete callback when finished
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
    
    [Tooltip("Icon RectTransform for shake animation")]
    [SerializeField] private RectTransform iconRectTransform;

    [Tooltip("Prompt text (Arabic instruction)")]
    [SerializeField] private RTLTextMeshPro promptText;

    [Tooltip("Timer progress bar (0-1)")]
    [SerializeField] private Image timerBar;

    [Tooltip("Progress counter text")]
    [SerializeField] private RTLTextMeshPro counterText;

    [Tooltip("Icon sprites folder in Resources")]
    [SerializeField] private string iconSpritesPath = "InteractionIcons";

    [Header("Timing")]
    [Tooltip("Default duration if CSV has 0")]
    [SerializeField] private float defaultDuration = 5f;

    [Tooltip("Warning threshold - bar turns red")]
    [SerializeField] private float warningThreshold = 3.5f;

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

    #endregion

    #region State

    private InteractionData currentInteraction;
    private Action<bool, float, int> onCompleteCallback;
    private float elapsed = 0f;
    private bool isActive = false;
    private Sequence entranceTween;
    private Sequence exitTween;
    private bool hasInteracted = false;
    private bool hasTriggeredInactivityFeedback = false;
    
    // GC optimization
    private int lastCounterValue = -1;
    private Tweener iconShakeTween;
    private float lastShakeCount = 0f;

    public static Action<InteractionData, bool> OnInteractionFinished;
    public static Action<int> OnEidiaEarned;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (hudPanel != null)
        {
            hudPanel.gameObject.SetActive(false);
            
            // Register for tutorials
            if (TutorialOverlayManager.Instance != null)
            {
                TutorialOverlayManager.Instance.RegisterTarget("InteractionZone", hudPanel);
            }
        }
    }

    private void Update()
    {
        if (!isActive || currentInteraction == null) return;

        bool tutorialActive = TutorialOverlayManager.Instance != null && TutorialOverlayManager.Instance.IsTutorialActive;
        
        // If tutorial is active, we only check for interaction to advance it, but pause the timer
        if (tutorialActive)
        {
            CheckForInteractionAdvance(tutorialActive);
            return;
        }

        UpdateTimer();
        UpdateProgress();
        CheckCompletion();
    }

    private void OnDestroy()
    {
        entranceTween?.Kill();
        exitTween?.Kill();
        iconShakeTween?.Kill();
    }
    
    #endregion
    
    #region Public API
    
    public void RunInteraction(InteractionData data, Action<bool, float, int> onComplete)
    {
        if (data == null) { onComplete?.Invoke(false, 0, 0); return; }

        currentInteraction = data;
        onCompleteCallback = onComplete;
        elapsed = 0f;
        isActive = true;
        hasInteracted = false; // Reset interaction flag
        hasTriggeredInactivityFeedback = false; // Reset feedback flag
        lastCounterValue = -1;
        lastShakeCount = 0f;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetInteractionState();
            switch (data.InteractionType)
            {
                case InteractionType.Shake: InputManager.Instance.EnableAction("Acceleration"); break;
                case InteractionType.Hold:
                case InteractionType.Tap:
                    InputManager.Instance.EnableAction("Hold");
                    InputManager.Instance.EnableAction("Tap");
                    InputManager.Instance.EnableAction("TouchStart");
                    break;
                case InteractionType.Draw: InputManager.Instance.EnableAction("Draw"); break;
            }
        }

        UpdateUI();
        
        // --- IMPROVED TUTORIAL FLOW ---
        bool isHouse1 = GameManager.Instance != null && GameManager.Instance.CurrentHouseLevel == 1;
        string interactionName = data.InteractionType.ToString().ToUpper();
        string tutorialKey = $"TUT_{interactionName}";

        if (isHouse1 && !SaveManager.Instance.HasSeenTutorial(tutorialKey))
        {
            // HIDE panel and speaker name initially so the tutorial can introduce it cleanly
            if (hudPanel != null) hudPanel.gameObject.SetActive(false);
            if (CinematicController.Instance != null) CinematicController.Instance.ToggleSpeakerName(false);

            if (DataManager.Instance != null && DataManager.Instance.tutorialStepsByID.ContainsKey(tutorialKey))
            {
                TutorialOverlayManager.Instance.PlayTutorial(tutorialKey, () =>
                {
                    SaveManager.Instance.MarkTutorialAsComplete(tutorialKey);
                    BeginActualInteraction(); 
                });
            }
            else
            {
                SaveManager.Instance.MarkTutorialAsComplete(tutorialKey);
                BeginActualInteraction();
            }
        }
        else
        {
            BeginActualInteraction();
        }
    }

    /// <summary>
    /// Starts the actual interaction UI (Panel + Speaker Name BG).
    /// </summary>
    private void BeginActualInteraction()
    {
        ShowPanel();
        if (CinematicController.Instance != null)
        {
            CinematicController.Instance.ToggleSpeakerName(true);
        }
    }

    private string GetInteractionInstruction(InteractionType type)
    {
        return type switch
        {
            InteractionType.Shake => "SHAKE!",
            InteractionType.Hold => "HOLD!",
            InteractionType.Tap => "TAP!",
            InteractionType.Draw => "DRAW!",
            _ => "GO!"
        };
    }

    public void HideHUD()
    {
        isActive = false;
        entranceTween?.Kill();
        exitTween?.Kill();
        iconShakeTween?.Kill();

        if (hudPanel != null)
        {
            hudPanel.gameObject.SetActive(false);
            CanvasGroup canvasGroup = hudPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            hudPanel.localScale = Vector3.one;
        }

        // HIDE Speaker Name Panel (BG) as well
        if (CinematicController.Instance != null)
        {
            CinematicController.Instance.ToggleSpeakerName(false);
        }
    }

    private void UpdateUI()
    {
        if (currentInteraction == null) return;

        if (promptText != null)
        {
            promptText.text = !string.IsNullOrEmpty(currentInteraction.PromptTextAR)
                ? currentInteraction.PromptTextAR
                : currentInteraction.InteractionType.GetArabicLabel();
        }

        if (iconImage != null)
        {
            string spriteName = currentInteraction.InteractionType.GetIconSpriteName();
            Sprite sprite = Resources.Load<Sprite>($"{iconSpritesPath}/{spriteName}");
            if (sprite != null) { iconImage.sprite = sprite; iconImage.enabled = true; }
            else iconImage.enabled = false;
        }

        UpdateCounterText(0);
        if (timerBar != null) { timerBar.fillAmount = 1f; timerBar.color = normalColor; }
    }

    private void CheckForInteractionAdvance(bool tutorialActive)
    {
        float currentValue = GetCurrentValue();
        // Increased threshold to 0.05f to avoid noise triggers
        if (!hasInteracted && currentValue > 0.05f)
        {
            hasInteracted = true;
            
            // DYNAMIC: If we were in a "Sad" state, restore expression to Default
            if (hasTriggeredInactivityFeedback)
            {
                RestoreCharacterToDefaultExpression();
            }

            // Advance tutorial dynamically if the player starts interacting!
            if (tutorialActive)
            {
                TutorialOverlayManager.Instance.AdvanceTutorial();
            }
        }
    }

    private void RestoreCharacterToDefaultExpression()
    {
        if (currentInteraction == null || CinematicController.Instance == null) return;

        var speaker = DataManager.Instance?.GetSpeakerByName(currentInteraction.SpeakerName);
        if (speaker != null)
        {
            CinematicController.Instance.UpdateExpressionExternally(speaker, "Default");
        }
    }

    private void UpdateTimer()
    {
        if (currentInteraction == null || currentInteraction.Duration <= 0) return;

        bool tutorialActive = TutorialOverlayManager.Instance != null && TutorialOverlayManager.Instance.IsTutorialActive;
        
        CheckForInteractionAdvance(tutorialActive);

        // Removed the hardcoded return so that Time.deltaTime respects the CSV's TimeScale setting.
        elapsed += Time.deltaTime;
        
        float duration = currentInteraction.Duration > 0 ? currentInteraction.Duration : defaultDuration;
        float remaining = Mathf.Max(0, duration - elapsed);
        float progress = remaining / duration;

        // Removed inactivity timer to restore original logic
        /*
        if (!hasInteracted && !hasTriggeredInactivityFeedback && elapsed >= 2.0f)
        {
            hasTriggeredInactivityFeedback = true;
            UpdateCharacterToWarningExpression();
        }
        */

        if (timerBar != null)
        {
            timerBar.fillAmount = progress;
            if (remaining <= warningThreshold) timerBar.color = dangerColor;
            else if (remaining <= warningThreshold * 2) timerBar.color = warningColor;
            else timerBar.color = normalColor;
        }
    }

    private void UpdateCharacterToWarningExpression()
    {
        if (currentInteraction == null || CinematicController.Instance == null) return;

        // Find speaker data from DataManager
        var speaker = DataManager.Instance?.GetSpeakerByName(currentInteraction.SpeakerName);
        if (speaker != null)
        {
            // Use "Sad" or "Warning" expression if available
            string expression = !string.IsNullOrEmpty(currentInteraction.FailureExpression) 
                ? currentInteraction.FailureExpression 
                : "Sad";
            
            // Explicitly pass false for showNamePanel
            CinematicController.Instance.UpdateExpressionExternally(speaker, expression, 0, false);
        }
    }

    private void UpdateProgress()
    {
        float currentValue = GetCurrentValue();
        int currentValueInt = Mathf.FloorToInt(currentValue);
        if (currentValueInt != lastCounterValue)
        {
            lastCounterValue = currentValueInt;
            UpdateCounterText(currentValue);
            
            // JUICE: Visual punch on any progress update
            if (iconImage != null)
            {
                iconImage.transform.DOKill();
                iconImage.transform.localScale = Vector3.one;
                iconImage.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f);
            }
        }
        UpdateIconStruggle(currentValue);
    }
    
    private void UpdateIconStruggle(float currentValue)
    {
        if (iconRectTransform == null && iconImage == null) return;
        RectTransform targetRect = iconRectTransform ?? iconImage.rectTransform;

        switch (currentInteraction.InteractionType)
        {
            case InteractionType.Shake:
                float shakeDelta = currentValue - lastShakeCount;
                lastShakeCount = currentValue;
                if (shakeDelta < 0.5f) return;
                
                float intensity = Mathf.Clamp01(currentValue / currentInteraction.Threshold);
                float amplitude = Mathf.Lerp(3f, 12f, intensity);
                float frequency = Mathf.Lerp(15f, 30f, intensity);
                
                iconShakeTween?.Kill();
                iconShakeTween = targetRect.DOShakePosition(0.15f, new Vector2(amplitude, amplitude * 0.6f), Mathf.RoundToInt(frequency), 90f, false);
                break;

            case InteractionType.Hold:
                if (InputManager.Instance != null && InputManager.Instance.IsTouching())
                {
                    // Pulse faster as we get closer to threshold
                    float holdIntensity = Mathf.Clamp01(currentValue / currentInteraction.Threshold);
                    float speed = Mathf.Lerp(1.5f, 0.4f, holdIntensity);
                    if (!DOTween.IsTweening(targetRect))
                    {
                        targetRect.DOScale(1.2f, speed).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                    }
                }
                else
                {
                    targetRect.DOKill();
                    targetRect.localScale = Vector3.one;
                }
                break;

            case InteractionType.Draw:
                if (InputManager.Instance != null && InputManager.Instance.IsTouching())
                {
                    // Rotate slightly while drawing
                    if (!DOTween.IsTweening(targetRect))
                    {
                        targetRect.DORotate(new Vector3(0, 0, 15f), 0.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                    }
                }
                else
                {
                    targetRect.DOKill();
                    targetRect.localRotation = Quaternion.identity;
                }
                break;
        }
    }

    private float GetCurrentValue()
    {
        if (InputManager.Instance == null) return 0;
        return currentInteraction.InteractionType switch
        {
            InteractionType.Shake => InputManager.Instance.GetShakeCount(),
            InteractionType.Hold => InputManager.Instance.GetHoldDuration(),
            InteractionType.Tap => InputManager.Instance.GetTapCount(),
            InteractionType.Draw => InputManager.Instance.IsTouching() ? 1f : 0f, // Use touch as proxy for Draw
            _ => 0
        };
    }

    private void UpdateCounterText(float currentValue)
    {
        if (counterText == null) return;
        string label = currentInteraction.InteractionType.ToString();
        counterText.text = $"{label}: {Mathf.FloorToInt(currentValue)}/{Mathf.FloorToInt(currentInteraction.Threshold)}";
        counterText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        if (iconImage != null) iconImage.transform.DOPunchScale(Vector3.one * 0.05f, 0.15f);
    }

    private void CheckCompletion()
    {
        float currentValue = GetCurrentValue();
        bool succeeded = currentInteraction.CheckThreshold(currentValue);
        bool timedOut = currentInteraction.Duration > 0 && elapsed >= currentInteraction.Duration + 0.2f;

        if (succeeded) CompleteInteraction(true);
        else if (timedOut) CompleteInteraction(currentValue >= currentInteraction.Threshold * 0.5f);
    }

    private void CompleteInteraction(bool succeeded)
    {
        if (!isActive) return;
        isActive = false;

        // SET expression based on result (PERSISTENT until next result)
        if (succeeded) RestoreCharacterToDefaultExpression();
        else UpdateCharacterToWarningExpression();

        float batteryDelta = currentInteraction.GetBatteryDelta(succeeded);
        float stomachDelta = currentInteraction.GetStomachDelta(succeeded);
        int eidiaReward = currentInteraction.GetEidReward(succeeded);

        AudioManager.Instance?.PlaySFX(succeeded ? AudioManager.SFXType.InteractionSuccess : AudioManager.SFXType.InteractionFail);
        if (!succeeded) HapticFeedback.Instance?.MediumTap();
        if (succeeded && currentInteraction.InteractionType == InteractionType.Shake) AudioManager.Instance?.PlaySFX(AudioManager.SFXType.InteractionShakeRumble);

        OnInteractionFinished?.Invoke(currentInteraction, succeeded);

        // HIDE HUD IMMEDIATELY to prevent "ghost" panel during feedback/tutorials
        HideHUD();

        // FTUE: If failed in House 1, show the "Sad" tutorial as a consequence
        // This is a blocking call - it will finish before we call the onCompleteCallback
        Action processFinalize = () =>
        {
            FlashResult(succeeded, () =>
            {
                MeterManager.Instance?.ModifyBattery(batteryDelta);
                MeterManager.Instance?.ModifyStomach(stomachDelta);
                if (eidiaReward > 0) OnEidiaEarned?.Invoke(eidiaReward);

                HidePanel(() =>
                {
                    onCompleteCallback?.Invoke(succeeded, batteryDelta, eidiaReward);
                    onCompleteCallback = null;
                    currentInteraction = null;
                });
            });
        };

        bool isHouse1 = GameManager.Instance != null && GameManager.Instance.CurrentHouseLevel == 1;
        bool hasSeenSadTutorial = SaveManager.Instance != null && SaveManager.Instance.HasSeenTutorial("TUT_SAD");

        if (!succeeded && isHouse1 && !hasSeenSadTutorial)
        {
            UpdateCharacterToWarningExpression();
            if (TutorialOverlayManager.Instance != null)
            {
                TutorialOverlayManager.Instance.PlayTutorial("TUT_SAD", () => {
                    SaveManager.Instance?.MarkTutorialAsComplete("TUT_SAD");
                    processFinalize();
                });
            }
            else processFinalize();
        }
        else
        {
            processFinalize();
        }
    }

    #endregion

    #region UI Animations

    private void ShowPanel()
    {
        if (hudPanel == null) return;
        hudPanel.gameObject.SetActive(true);
        entranceTween?.Kill();
        CanvasGroup canvasGroup = hudPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = hudPanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        hudPanel.localScale = Vector3.one * 0.5f;

        entranceTween = DOTween.Sequence();
        entranceTween.Append(canvasGroup.DOFade(1f, entranceDuration).SetEase(Ease.OutCubic));
        entranceTween.Join(hudPanel.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack));
    }

    private void HidePanel(Action onComplete)
    {
        if (hudPanel == null) { onComplete?.Invoke(); return; }
        exitTween?.Kill();
        CanvasGroup canvasGroup = hudPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = hudPanel.gameObject.AddComponent<CanvasGroup>();

        exitTween = DOTween.Sequence();
        exitTween.Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));
        exitTween.Join(hudPanel.DOScale(Vector3.one * 0.5f, exitDuration).SetEase(Ease.InBack));
        exitTween.OnComplete(() => { hudPanel.gameObject.SetActive(false); onComplete?.Invoke(); });
    }

    private void FlashResult(bool succeeded, Action onComplete)
    {
        if (timerBar == null) { onComplete?.Invoke(); return; }
        timerBar.DOKill();
        Color targetColor = succeeded ? successColor : failureColor;
        Sequence flashSeq = DOTween.Sequence();
        flashSeq.Append(timerBar.DOColor(targetColor, 0.15f).SetEase(Ease.OutQuad));
        flashSeq.Append(timerBar.DOColor(normalColor, 0.3f).SetEase(Ease.InQuad));
        flashSeq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion
}
