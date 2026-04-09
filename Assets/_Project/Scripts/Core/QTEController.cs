using System;
using System.Collections;
using UnityEngine;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 9.3: Modular QTE Controller for Timeline-driven house flow.
/// 
/// This system handles all QTE types (Shake, Swipe, Hold) triggered by HouseFlowController.
/// It reuses QTEInputController for input detection but adds data-driven configuration
/// from CSV (QTEs.csv) and proper integration with the Timeline flow.
/// 
/// ARCHITECTURE:
/// 1. HouseFlowController calls QTEController.StartQTE(qteData, onComplete)
/// 2. QTEController loads QTE config from QTEData (CSV)
/// 3. Shows QTE UI with instruction text
/// 4. Uses QTEInputController for input detection (Shake/Swipe/Hold)
/// 5. On success/fail: Applies battery effects, shows feedback
/// 6. Calls onComplete callback to signal HouseFlowController
/// 
/// USAGE:
/// 1. Add QTEController component to a GameObject in the scene
/// 2. Assign QTE UI prefabs (panel, instruction text, timer text)
/// 3. Assign QTEInputController reference (for input detection)
/// 4. HouseFlowController will call StartQTE() automatically from Timeline signals
/// </summary>
public class QTEController : MonoBehaviour
{
    public static QTEController Instance { get; private set; }

    #region Inspector Fields

    [Header("QTE UI")]
    [Tooltip("QTE prompt panel (shown when QTE is active)")]
    [SerializeField] private GameObject qtePromptPanel;

    [Tooltip("Instruction text (e.g., 'Shake to drink!')")]
    [SerializeField] private RTLTextMeshPro instructionText;

    [Tooltip("Timer text showing remaining time")]
    [SerializeField] private RTLTextMeshPro timerText;

    [Tooltip("Success feedback text panel")]
    [SerializeField] private GameObject successFeedbackPanel;

    [Tooltip("Failure feedback text panel")]
    [SerializeField] private GameObject failFeedbackPanel;

    [Tooltip("Success feedback text (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro successFeedbackText;

    [Tooltip("Failure feedback text (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro failFeedbackText;

    [Header("Input Detection")]
    [Tooltip("QTEController for shake/swipe detection (self-contained)")]
    [SerializeField] private QTEController inputController;

    [Header("Visual Feedback")]
    [Tooltip("Color when QTE is active")]
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0f); // Golden yellow

    [Tooltip("Color on QTE success")]
    [SerializeField] private Color successColor = new Color(0.2f, 0.9f, 0.2f); // Green

    [Tooltip("Color on QTE failure")]
    [SerializeField] private Color failColor = new Color(0.9f, 0.2f, 0.2f); // Red

    [Header("Animation Settings")]
    [Tooltip("Entrance animation duration")]
    [SerializeField] private float entranceDuration = 0.5f;

    [Tooltip("Exit animation duration")]
    [SerializeField] private float exitDuration = 0.3f;

    [Tooltip("Feedback display duration")]
    [SerializeField] private float feedbackDuration = 1.0f;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging")]
    [SerializeField] private bool debugLogging = false;

    #endregion

    #region State

    private QTEData currentQTEData;
    private Action<bool, float, string> onCompleteCallback;
    private bool isQTEActive = false;
    private float timeRemaining = 0f;
    private Coroutine qteTimerCoroutine;

    // Hold QTE specific
    private float holdProgress = 0f;

    #endregion

    #region Events

    /// <summary>
    /// Fires when a QTE starts.
    /// (qteID, qteType, duration)
    /// </summary>
    public static Action<string, QTEType, float> OnQTEStarted;

    /// <summary>
    /// Fires when a QTE completes.
    /// (wasSuccess, batteryDelta, qteID)
    /// </summary>
    public static Action<bool, float, string> OnQTECompleted;

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
            Debug.LogError("[QTEController] Duplicate instance! Destroying.");
            Destroy(gameObject);
            return;
        }

        // Ensure UI is hidden on start
        if (qtePromptPanel != null)
            qtePromptPanel.SetActive(false);
        if (successFeedbackPanel != null)
            successFeedbackPanel.SetActive(false);
        if (failFeedbackPanel != null)
            failFeedbackPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (qteTimerCoroutine != null)
            StopCoroutine(qteTimerCoroutine);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts a QTE with the given configuration.
    /// Called by HouseFlowController when a QTE element is triggered.
    /// </summary>
    /// <param name="qteData">QTE configuration from CSV</param>
    /// <param name="onComplete">Callback: (wasSuccess, batteryDelta, qteID)</param>
    public void StartQTE(QTEData qteData, Action<bool, float, string> onComplete)
    {
        if (isQTEActive)
        {
            Debug.LogWarning("[QTEController] QTE already active! Ignoring StartQTE call.");
            return;
        }

        currentQTEData = qteData;
        onCompleteCallback = onComplete;
        isQTEActive = true;
        timeRemaining = qteData.Duration;
        holdProgress = 0f;

        if (debugLogging)
            Debug.Log($"[QTEController] Starting QTE: {qteData.ID} | Type: {qteData.QTEType} | Duration: {qteData.Duration}s");

        // Fire event
        OnQTEStarted?.Invoke(qteData.ID, qteData.QTEType, qteData.Duration);

        // Show QTE UI
        ShowQTEUI();

        // Start timer
        if (qteTimerCoroutine != null)
            StopCoroutine(qteTimerCoroutine);
        qteTimerCoroutine = StartCoroutine(QTETimerCoroutine());

        // Configure input controller
        if (inputController != null)
        {
            ConfigureInputController(qteData);
        }
        else
        {
            Debug.LogWarning("[QTEController] InputController not assigned! Using fallback input detection.");
            // TODO: Implement fallback input detection directly in QTEController
        }
    }

    /// <summary>
    /// Cancels the active QTE (e.g., on house timeout).
    /// </summary>
    public void CancelActiveQTE()
    {
        if (!isQTEActive) return;

        if (debugLogging)
            Debug.Log("[QTEController] QTE cancelled externally.");

        isQTEActive = false;
        if (qteTimerCoroutine != null)
            StopCoroutine(qteTimerCoroutine);

        // Callback with failure
        onCompleteCallback?.Invoke(false, currentQTEData?.FailBatteryEffect ?? 0f, currentQTEData?.ID ?? "Unknown");
        OnQTECompleted?.Invoke(false, currentQTEData?.FailBatteryEffect ?? 0f, currentQTEData?.ID ?? "Unknown");

        HideQTEUI();
    }

    #endregion

    #region QTE Logic

    /// <summary>
    /// Timer coroutine for the QTE.
    /// </summary>
    private IEnumerator QTETimerCoroutine()
    {
        while (timeRemaining > 0f && isQTEActive)
        {
            timeRemaining -= Time.deltaTime;

            // Update timer UI
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(Mathf.Max(0, timeRemaining)).ToString();

            // Panic threshold (last 3 seconds)
            if (timeRemaining <= 3f && timeRemaining > 0f)
            {
                if (timerText != null)
                    timerText.color = failColor;
            }

            yield return null;
        }

        // Timeout = QTE fail
        if (isQTEActive)
        {
            if (debugLogging)
                Debug.Log("[QTEController] QTE timeout!");
            ResolveQTE(false);
        }
    }

    /// <summary>
    /// Resolves the QTE with success or failure.
    /// </summary>
    private void ResolveQTE(bool wasSuccess)
    {
        if (!isQTEActive) return;
        isQTEActive = false;

        // Stop timer
        if (qteTimerCoroutine != null)
            StopCoroutine(qteTimerCoroutine);

        float batteryDelta = wasSuccess ? currentQTEData.SuccessBatteryEffect : currentQTEData.FailBatteryEffect;
        string feedbackText = wasSuccess ? currentQTEData.SuccessTextAR : currentQTEData.FailTextAR;

        if (debugLogging)
            Debug.Log($"[QTEController] QTE resolved: Success={wasSuccess} | Battery={batteryDelta:+0;-#} | Feedback: {feedbackText}");

        // Apply battery effect
        if (MeterManager.Instance != null)
        {
            MeterManager.Instance.ModifyBattery(batteryDelta);
            if (debugLogging)
                Debug.Log($"[QTEController] Battery modified: {batteryDelta:+0;-#}");
        }

        // Show feedback
        ShowQTEFeedback(wasSuccess, feedbackText);

        // Fire event
        OnQTECompleted?.Invoke(wasSuccess, batteryDelta, currentQTEData.ID);

        // Call completion callback
        onCompleteCallback?.Invoke(wasSuccess, batteryDelta, currentQTEData.ID);
    }

    /// <summary>
    /// Called by QTEInputController when input is detected.
    /// </summary>
    public void OnInputDetected(bool wasSuccess)
    {
        if (!isQTEActive) return;

        if (debugLogging)
            Debug.Log($"[QTEController] Input detected: Success={wasSuccess}");

        ResolveQTE(wasSuccess);
    }

    /// <summary>
    /// Called by QTEInputController for hold-type QTEs (progress 0-1).
    /// </summary>
    public void OnHoldProgress(float progress)
    {
        if (!isQTEActive || currentQTEData.QTEType != QTEType.Hold) return;

        holdProgress = progress;

        // Update UI to show progress (e.g., fill bar)
        if (timerText != null)
            timerText.text = $"{Mathf.CeilToInt(progress * 100)}%";

        // Hold complete
        if (progress >= 1.0f)
        {
            if (debugLogging)
                Debug.Log("[QTEController] Hold complete!");
            ResolveQTE(true);
        }
    }

    #endregion

    #region Input Configuration

    /// <summary>
    /// Configures QTEInputController for the current QTE type.
    /// </summary>
    private void ConfigureInputController(QTEData qteData)
    {
        // For Shake and Swipe, we let QTEInputController handle it
        // For Hold, we'll implement directly in this controller
        // TODO: Implement Hold detection in QTEInputController or here

        if (qteData.QTEType == QTEType.Hold)
        {
            // For Hold type, we detect it directly in this controller
            StartCoroutine(HoldDetectionCoroutine());
        }
    }

    /// <summary>
    /// Coroutine for detecting hold input.
    /// Player must hold for the duration specified in QTEData.
    /// </summary>
    private IEnumerator HoldDetectionCoroutine()
    {
        bool isHoldingInput = false;

        while (isQTEActive)
        {
            // Check for hold start
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                isHoldingInput = true;
                holdProgress = 0f;
            }

            // Check for hold progress
            if (isHoldingInput)
            {
                if (Input.GetMouseButton(0) || Input.touchCount > 0)
                {
                    holdProgress += Time.deltaTime / currentQTEData.Duration;
                    OnHoldProgress(holdProgress);
                }
                else
                {
                    // Released before complete = fail
                    isHoldingInput = false;
                    holdProgress = 0f;
                    if (debugLogging)
                        Debug.Log("[QTEController] Hold released early!");
                    // Optional: You could fail immediately or just reset progress
                    // For now, we just reset progress (more forgiving)
                }
            }

            yield return null;
        }
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Shows the QTE prompt UI with entrance animation.
    /// </summary>
    private void ShowQTEUI()
    {
        if (qtePromptPanel == null)
        {
            Debug.LogError("[QTEController] QTE Prompt Panel is NULL!");
            return;
        }

        qtePromptPanel.SetActive(true);

        // Set instruction text based on QTE type
        if (instructionText != null)
        {
            string instruction = GetInstructionText(currentQTEData.QTEType);
            instructionText.text = instruction;
            instructionText.color = activeColor;
        }

        // Entrance animation
        RectTransform rectTransform = qtePromptPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack);
        }

        // Fade in
        CanvasGroup canvasGroup = qtePromptPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.3f);
        }
    }

    /// <summary>
    /// Gets instruction text based on QTE type.
    /// </summary>
    private string GetInstructionText(QTEType type)
    {
        return type switch
        {
            QTEType.Shake => "هز الهاتف! 📱", // Shake the phone!
            QTEType.Swipe => "اسحب بسرعة! 👆", // Swipe quickly!
            QTEType.Hold => "اضغط باستمرار! ✊", // Hold down!
            _ => "تفاعل الآن!" // Interact now!
        };
    }

    /// <summary>
    /// Hides the QTE UI with exit animation.
    /// </summary>
    private void HideQTEUI()
    {
        if (qtePromptPanel == null) return;

        CanvasGroup canvasGroup = qtePromptPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, exitDuration).OnComplete(() =>
            {
                qtePromptPanel.SetActive(false);
            });
        }
        else
        {
            qtePromptPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows success or failure feedback text.
    /// </summary>
    private void ShowQTEFeedback(bool wasSuccess, string feedbackText)
    {
        GameObject feedbackPanel = wasSuccess ? successFeedbackPanel : failFeedbackPanel;
        RTLTextMeshPro feedbackTextComponent = wasSuccess ? successFeedbackText : failFeedbackText;
        Color feedbackColor = wasSuccess ? successColor : failColor;

        if (feedbackPanel == null || feedbackTextComponent == null)
        {
            Debug.LogWarning("[QTEController] Feedback UI not set up! Skipping feedback.");
            HideQTEUI();
            return;
        }

        // Set text
        feedbackTextComponent.text = feedbackText;
        feedbackTextComponent.color = feedbackColor;

        // Show panel
        feedbackPanel.SetActive(true);

        // Entrance animation
        RectTransform rectTransform = feedbackTextComponent.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        // Hide after delay
        DOVirtual.DelayedCall(feedbackDuration, () =>
        {
            CanvasGroup canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
                {
                    feedbackPanel.SetActive(false);
                    HideQTEUI();
                });
            }
            else
            {
                feedbackPanel.SetActive(false);
                HideQTEUI();
            }
        });
    }

    #endregion

    #region Inspector Buttons

    [Button("🧪 Test Shake QTE")]
    private void TestShakeQTE()
    {
        if (Application.isPlaying)
        {
            QTEData testData = new QTEData
            {
                ID = "Test_Shake",
                HouseLevel = 1,
                QTEType = QTEType.Shake,
                Duration = 5f,
                SuccessTextAR = "أحسنت! ✅",
                FailTextAR = "فشلت! ❌",
                SuccessBatteryEffect = -5f,
                FailBatteryEffect = -15f
            };

            StartQTE(testData, (success, battery, id) =>
            {
                Debug.Log($"[QTEController Test] QTE completed: {id} | Success={success} | Battery={battery}");
            });
        }
        else
        {
            Debug.LogWarning("[QTEController] Enter Play mode to test.");
        }
    }

    [Button("🧪 Test Swipe QTE")]
    private void TestSwipeQTE()
    {
        if (Application.isPlaying)
        {
            QTEData testData = new QTEData
            {
                ID = "Test_Swipe",
                HouseLevel = 1,
                QTEType = QTEType.Swipe,
                Duration = 5f,
                SuccessTextAR = "سريع! ⚡",
                FailTextAR = "بطيء! 🐌",
                SuccessBatteryEffect = -5f,
                FailBatteryEffect = -15f
            };

            StartQTE(testData, (success, battery, id) =>
            {
                Debug.Log($"[QTEController Test] QTE completed: {id} | Success={success} | Battery={battery}");
            });
        }
        else
        {
            Debug.LogWarning("[QTEController] Enter Play mode to test.");
        }
    }

    [Button("🧪 Test Hold QTE")]
    private void TestHoldQTE()
    {
        if (Application.isPlaying)
        {
            QTEData testData = new QTEData
            {
                ID = "Test_Hold",
                HouseLevel = 1,
                QTEType = QTEType.Hold,
                Duration = 3f,
                SuccessTextAR = "مثبت! 💪",
                FailTextAR = "ما ثبت! 😞",
                SuccessBatteryEffect = -5f,
                FailBatteryEffect = -15f
            };

            StartQTE(testData, (success, battery, id) =>
            {
                Debug.Log($"[QTEController Test] QTE completed: {id} | Success={success} | Battery={battery}");
            });
        }
        else
        {
            Debug.LogWarning("[QTEController] Enter Play mode to test.");
        }
    }

    #endregion
}
