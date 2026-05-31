using System;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// Manages all UI elements, animations, and visual feedback.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnPlayAgain = null;
            Instance = null;
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Feedback UI")]
    [SerializeField] private RTLTextMeshPro feedbackText;
    [SerializeField] private GameObject feedbackPanel;

    [Header("Meter UI")]
    [Tooltip("Battery slider (shows current social battery)")]
    [SerializeField] private Slider batterySlider;
    public RectTransform BatterySliderRect => batterySlider != null ? batterySlider.GetComponent<RectTransform>() : null;

    [Tooltip("Stomach slider (shows current stomach fullness)")]
    [SerializeField] private Slider stomachSlider;
    public RectTransform StomachSliderRect => stomachSlider != null ? stomachSlider.GetComponent<RectTransform>() : null;

    [Tooltip("Timer text display (shows per-card numeric countdown)")]
    [SerializeField] private RTLTextMeshPro timerText;

    [Header("Resource HUD")]
    [SerializeField] private RTLTextMeshPro runEidiaText;
    [SerializeField] private RTLTextMeshPro totalScrapText;

    [Header("Game State Panels")]
    [Tooltip("Main menu start screen panel")]
    [SerializeField] private GameObject mainMenuPanel;
    [Tooltip("Start button on the main menu")]
    [SerializeField] private Button startBtn;
    [Tooltip("Toggle to reset progress and tutorial on start")]
    [SerializeField] private Toggle resetProgressToggle;
    [Tooltip("Swipe encounter panel (PHASE 16+ active swipe UI)")]
    [SerializeField] private GameObject swipeEncounterPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Result Displays")]
    [SerializeField] private RTLTextMeshPro gameOverEidiaText;
    [SerializeField] private RTLTextMeshPro winEidiaText;

    [Header("Panel Buttons")]
    [Tooltip("Exit button on the Game Over panel (returns to main menu)")]
    [SerializeField] private Button gameOverExitButton;
    [Tooltip("Play again button on the Game Over panel")]
    [SerializeField] private Button gameOverPlayAgainButton;
    [Tooltip("Exit button on the Win panel (returns to main menu)")]
    [SerializeField] private Button winExitButton;
    [Tooltip("Play again button on the Win panel")]
    [SerializeField] private Button winPlayAgainButton;

    [Header("Unified Hub Panel (PHASE 10)")]
    [Tooltip("Single unified hub panel with 3 tabs: Houses, Wardrobe, Upgrades")]
    [SerializeField] private GameObject unifiedHubPanel;

    [Header("Screen Shake Settings")]
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private float socialShutdownShakeDuration = 0.70f;
    [SerializeField] private Vector2 socialShutdownShakeAmplitude = new Vector2(40f, 22f);
    [SerializeField] private int socialShutdownShakeVibrato = 30;
    [SerializeField] private float socialShutdownShakeRandomness = 90f;

    [SerializeField] private float maamoulExplosionShakeDuration = 0.90f;
    [SerializeField] private Vector2 maamoulExplosionShakeAmplitude = new Vector2(55f, 30f);
    [SerializeField] private int maamoulExplosionShakeVibrato = 35;
    [SerializeField] private float maamoulExplosionShakeRandomness = 180f;

    [Header("Feedback Colors")]
    [SerializeField] private Color correctFeedbackColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color wrongFeedbackColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color instructionColor = new Color(0.2f, 0.6f, 0.9f, 1f); // Blue-ish

    [Header("Animation Settings")]
    [SerializeField] private float feedbackFadeInDuration = 0.22f;
    [SerializeField] private float feedbackDisplayDuration = 1.9f;
    [SerializeField] private float feedbackFadeOutDuration = 0.22f;

    public static Action OnPlayAgain;

    #endregion

    #region Private Fields

    private Sequence _feedbackSequence;
    private CanvasGroup _feedbackCanvasGroup;
    private Tween _startBtnPulse;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Register targets for tutorials
        if (TutorialOverlayManager.Instance != null)
        {
            if (batterySlider != null) TutorialOverlayManager.Instance.RegisterTarget("SocialBattery", batterySlider.GetComponent<RectTransform>());
            if (stomachSlider != null) TutorialOverlayManager.Instance.RegisterTarget("StomachMeter", stomachSlider.GetComponent<RectTransform>());
            if (timerText != null) TutorialOverlayManager.Instance.RegisterTarget("QuestionTimer", timerText.rectTransform);
            if (runEidiaText != null) TutorialOverlayManager.Instance.RegisterTarget("RunEidia", runEidiaText.rectTransform);
            if (totalScrapText != null) TutorialOverlayManager.Instance.RegisterTarget("TotalScrap", totalScrapText.rectTransform);
        }

        if (feedbackPanel != null)
        {
            _feedbackCanvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (_feedbackCanvasGroup == null)
                _feedbackCanvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
        }

        // Setup Start Button
        if (startBtn != null)
        {
            startBtn.onClick.AddListener(() => {
                StopStartButtonPulse();
                AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);

                if (resetProgressToggle != null && resetProgressToggle.isOn)
                {
                    SaveManager.Instance?.ClearData();
                }

                if (GameManager.Instance != null)
                    GameManager.Instance.StartRun();
            });

            StartButtonPulse();
        }

        // Handle initial state
        HandleInitialState();
    }

    private void StartButtonPulse()
    {
        if (startBtn == null) return;
        _startBtnPulse?.Kill();
        _startBtnPulse = startBtn.transform.DOScale(1.1f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopStartButtonPulse()
    {
        _startBtnPulse?.Kill();
        if (startBtn != null) startBtn.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Invoked when the player clicks the 'Play Again' button on a results panel.
    /// </summary>
    private void OnPlayAgainClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
        OnPlayAgain?.Invoke();
    }

    /// <summary>
    /// Invoked when the player clicks 'Exit' on a results panel.
    /// Triggers a clean scene reload to return to the main menu state.
    /// </summary>
    private void OnExitToMainMenu()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
        GameManager.StartRunOnLoad = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Gracefully exits the application or stops editor playback.
    /// </summary>
    public void QuitGame()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Public wrapper for Inspector button events.
    /// </summary>
    public void ExitToMainMenu() => OnExitToMainMenu();

    /// <summary>
    /// Determines which UI panels to show based on the current game state at startup.
    /// </summary>
    private void HandleInitialState()
    {
        HideAllPanels();

        if (GameManager.Instance != null)
        {
            GameState initialState = GameManager.Instance.CurrentState;

            if (initialState == GameState.MainMenu)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                AudioManager.Instance?.PlayMenuMusic();
            }
            else if (initialState == GameState.Wardrobe || initialState == GameState.HouseHub)
            {
                ShowUnifiedHub();
            }
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        GameManager.OnRunStarted += HandleRunStarted;
        GameManager.OnRunEidiaUpdated += HandleRunEidiaUpdated;
        SaveManager.OnScrapChanged += HandleTotalScrapUpdated;
        MeterManager.OnMetersChanged += HandleMetersChanged;
        MeterManager.OnBatteryModified += HandleBatteryModified;
        MeterManager.OnStomachModified += HandleStomachModified;

        if (gameOverExitButton != null) gameOverExitButton.onClick.AddListener(QuitGame);
        if (winExitButton != null) winExitButton.onClick.AddListener(QuitGame);

        if (gameOverPlayAgainButton != null) gameOverPlayAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (winPlayAgainButton != null) winPlayAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
        GameManager.OnRunStarted -= HandleRunStarted;
        GameManager.OnRunEidiaUpdated -= HandleRunEidiaUpdated;
        SaveManager.OnScrapChanged -= HandleTotalScrapUpdated;
        SaveManager.OnEidiaChanged -= HandleTotalScrapUpdated; 
        MeterManager.OnMetersChanged -= HandleMetersChanged;
        MeterManager.OnBatteryModified -= HandleBatteryModified;
        MeterManager.OnStomachModified -= HandleStomachModified;

        if (gameOverExitButton != null) gameOverExitButton.onClick.RemoveListener(QuitGame);
        if (winExitButton != null) winExitButton.onClick.RemoveListener(QuitGame);

        if (gameOverPlayAgainButton != null) gameOverPlayAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        if (winPlayAgainButton != null) winPlayAgainButton.onClick.RemoveListener(OnPlayAgainClicked);

        _feedbackSequence?.Kill();

        if (mainPanel != null)
            mainPanel.DOKill();
    }

    private void HandleRunStarted()
    {
        AudioManager.Instance?.PlayGameplayMusic();

        InitializeUI();
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        _feedbackSequence?.Kill();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Resets the UI for a fresh run, initializing meters and resource displays.
    /// </summary>
    private void InitializeUI()
    {
        HideAllPanels();
        if (swipeEncounterPanel != null) swipeEncounterPanel.SetActive(true);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        SetHUDEnabled(false);

        if (runEidiaText != null) runEidiaText.text = "0";
        if (totalScrapText != null && SaveManager.Instance != null)
            totalScrapText.text = SaveManager.Instance.CurrentData.TotalScrap.ToString();

        if (MeterManager.Instance != null)
        {
            if (batterySlider != null)
            {
                float maxBattery = MeterManager.Instance.MaxBattery;
                float currentBattery = MeterManager.Instance.CurrentBattery;

                batterySlider.minValue = 0f;
                batterySlider.maxValue = maxBattery;
                batterySlider.value = currentBattery;
            }

            if (stomachSlider != null)
            {
                float currentStomach = MeterManager.Instance.CurrentStomach;

                stomachSlider.minValue = 0f;
                stomachSlider.maxValue = 100f;
                stomachSlider.value = currentStomach;
            }
        }
        else
        {
            if (batterySlider != null)
            {
                batterySlider.minValue = 0f;
                batterySlider.maxValue = 100f;
                batterySlider.value = 100f;
            }

            if (stomachSlider != null)
            {
                stomachSlider.minValue = 0f;
                stomachSlider.maxValue = 100f;
                stomachSlider.value = 0f;
            }
        }
    }

    #endregion

    #region Public Display Methods

    /// <summary>
    /// Displays a temporary feedback popup for correct or incorrect actions.
    /// </summary>
    public void ShowFeedback(string text, bool isCorrect, Action onComplete)
    {
        if (feedbackPanel == null)
        {
            return;
        }

        EnsureFeedbackComponents();

        _feedbackSequence?.Kill();
        
        Image feedbackImage = feedbackPanel.GetComponent<Image>();
        if (feedbackText != null)
            feedbackText.text = text;

        Color targetColor = isCorrect ? correctFeedbackColor : wrongFeedbackColor;

        feedbackPanel.SetActive(true);
        _feedbackCanvasGroup.alpha = 0f;
        _feedbackCanvasGroup.interactable = false;
        _feedbackCanvasGroup.blocksRaycasts = false;
        
        if (feedbackImage != null)
            feedbackImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        _feedbackSequence = DOTween.Sequence()
            .Append(_feedbackCanvasGroup.DOFade(1f, feedbackFadeInDuration).SetUpdate(true))
            .Join(feedbackImage.DOFade(targetColor.a, feedbackFadeInDuration).SetUpdate(true))
            .AppendInterval(feedbackDisplayDuration)
            .Append(_feedbackCanvasGroup.DOFade(0f, feedbackFadeOutDuration).SetUpdate(true))
            .Join(feedbackImage.DOFade(0f, feedbackFadeOutDuration).SetUpdate(true))
            .OnComplete(() =>
            {
                feedbackPanel.SetActive(false);
                onComplete?.Invoke();
            })
            .SetUpdate(true);
    }

    public void ShowFeedback(string text, Action onComplete)
    {
        ShowFeedback(text, true, onComplete);
    }

    /// <summary>
    /// Displays a neutral instruction text overlay.
    /// </summary>
    public void ShowInstruction(string text, Action onComplete = null)
    {
        if (feedbackPanel == null) return;
        EnsureFeedbackComponents();

        _feedbackSequence?.Kill();
        
        Image feedbackImage = feedbackPanel.GetComponent<Image>();
        if (feedbackText != null)
            feedbackText.text = text;

        feedbackPanel.SetActive(true);
        _feedbackCanvasGroup.alpha = 0f;
        
        if (feedbackImage != null)
            feedbackImage.color = new Color(instructionColor.r, instructionColor.g, instructionColor.b, 0f);

        _feedbackSequence = DOTween.Sequence()
            .Append(_feedbackCanvasGroup.DOFade(1f, feedbackFadeInDuration).SetUpdate(true))
            .Join(feedbackImage.DOFade(instructionColor.a, feedbackFadeInDuration).SetUpdate(true))
            .AppendInterval(2.5f) 
            .Append(_feedbackCanvasGroup.DOFade(0f, feedbackFadeOutDuration).SetUpdate(true))
            .Join(feedbackImage.DOFade(0f, feedbackFadeOutDuration).SetUpdate(true))
            .OnComplete(() =>
            {
                feedbackPanel.SetActive(false);
                onComplete?.Invoke();
            })
            .SetUpdate(true);
    }

    /// <summary>
    /// Hides all active UI panels to prepare for a mini-game.
    /// </summary>
    public void HideAllPanelsForMiniGame()
    {
        HideAllPanels();
    }

    /// <summary>
    /// Explicitly hides the swipe encounter UI.
    /// </summary>
    public void HideSwipeEncounter()
    {
        if (swipeEncounterPanel != null)
            swipeEncounterPanel.SetActive(false);
    }

    /// <summary>
    /// Explicitly shows the swipe encounter UI and refreshes its background.
    /// </summary>
    public void ShowSwipeEncounter()
    {
        if (swipeEncounterPanel != null)
        {
            swipeEncounterPanel.SetActive(true);
            
            var bgController = swipeEncounterPanel.GetComponentInChildren<HouseBackgroundController>();
            if (bgController != null) bgController.RefreshBackground();
        }
    }

    /// <summary>
    /// Hides the interaction QTE HUD.
    /// </summary>
    public void HideInteractionHUD()
    {
        InteractionHUDController.Instance?.HideHUD();
    }

    /// <summary>
    /// Triggers the visual screen shake for a social battery depletion event.
    /// </summary>
    public void ShakeSocialShutdown()
    {
        ShakePanel(socialShutdownShakeDuration, socialShutdownShakeAmplitude, socialShutdownShakeVibrato, socialShutdownShakeRandomness);
        CameraShakeManager.Instance?.ShakeSocialShutdown();
    }

    /// <summary>
    /// Triggers the visual screen shake for a stomach fullness event.
    /// </summary>
    public void ShakeMaamoulExplosion()
    {
        ShakePanel(maamoulExplosionShakeDuration, maamoulExplosionShakeAmplitude, maamoulExplosionShakeVibrato, maamoulExplosionShakeRandomness);
        CameraShakeManager.Instance?.ShakeMaamoulExplosion();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Core logic for shaking a specific UI panel using DOTween.
    /// </summary>
    private void ShakePanel(float duration, Vector2 amplitude, int vibrato, float randomness)
    {
        if (mainPanel != null)
        {
            mainPanel.DOKill();
            mainPanel.DOShakeAnchorPos(duration, amplitude, vibrato, randomness, true).SetUpdate(true);
        }
    }

    /// <summary>
    /// Lazily initializes or verifies critical feedback components.
    /// </summary>
    private void EnsureFeedbackComponents()
    {
        if (_feedbackCanvasGroup == null)
        {
            _feedbackCanvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (_feedbackCanvasGroup == null)
            {
                _feedbackCanvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
            }
        }

        if (feedbackPanel.GetComponent<Image>() == null)
        {
            feedbackPanel.AddComponent<Image>();
        }
    }

    private void AnimateTextFadeIn(GameObject uiObject, float duration, float delay = 0f)
    {
        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = uiObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetDelay(delay).SetTarget(uiObject);
    }

    /// <summary>
    /// Deactivates all top-level game state panels.
    /// </summary>
    public void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (unifiedHubPanel != null) unifiedHubPanel.SetActive(false);
        if (swipeEncounterPanel != null) swipeEncounterPanel.SetActive(false);
    }

    #endregion

    #region Unified Hub UI

    /// <summary>
    /// Displays the unified hub and enables the base HUD with a smooth reveal animation.
    /// </summary>
    public void ShowUnifiedHub()
    {
        HideAllPanels();

        if (unifiedHubPanel != null)
        {
            unifiedHubPanel.SetActive(true);
            
            // JUICE: Smooth reveal animation
            CanvasGroup group = unifiedHubPanel.GetComponent<CanvasGroup>();
            if (group == null) group = unifiedHubPanel.AddComponent<CanvasGroup>();
            
            unifiedHubPanel.transform.DOKill();
            group.DOKill();
            
            unifiedHubPanel.transform.localScale = Vector3.one * 0.95f;
            group.alpha = 0f;
            
            unifiedHubPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            group.DOFade(1f, 0.2f).SetUpdate(true);
        }

        SetHUDEnabled(true);
        RefreshMeters();
    }

    /// <summary>
    /// Shows the unified hub without hiding already visible panels (e.g., results).
    /// </summary>
    private void ShowUnifiedHubWithoutHidingOthers()
    {
        if (unifiedHubPanel != null)
        {
            unifiedHubPanel.SetActive(true);
        }

        SetHUDEnabled(true);
        RefreshMeters();
    }

    /// <summary>
    /// Hides the unified hub panel.
    /// </summary>
    public void HideUnifiedHub()
    {
        if (unifiedHubPanel != null)
            unifiedHubPanel.SetActive(false);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Displays the Game Over panel with final eidia tally and isolates the view.
    /// </summary>
    public void ShowGameOver(int totalEidia)
    {
        HideAllPanels();
        
        AudioManager.Instance?.StopAllSFX();
        InteractionHUDController.Instance?.HideHUD();
        CinematicController.Instance?.StopCinematic();
        TutorialOverlayManager.Instance?.StopTutorial();
        
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverEidiaText != null)
        {
            gameOverEidiaText.text = $"عيدية مجمعة: {totalEidia}";
        }
        SetHUDEnabled(false);
        HideUnifiedHub();
    }

    /// <summary>
    /// Displays the Win panel with final eidia tally and isolates the view.
    /// </summary>
    public void ShowWin(int totalEidia)
    {
        HideAllPanels();

        AudioManager.Instance?.StopAllSFX();
        InteractionHUDController.Instance?.HideHUD();
        CinematicController.Instance?.StopCinematic();
        TutorialOverlayManager.Instance?.StopTutorial();

        if (winPanel != null) winPanel.SetActive(true);
        if (winEidiaText != null)
        {
            winEidiaText.text = $"عيدية مجمعة: {totalEidia}";
        }
        SetHUDEnabled(false);
        HideUnifiedHub();
    }

    /// <summary>
    /// Responds to global game state changes to toggle relevant UI elements.
    /// </summary>
    private void HandleStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                HideAllPanels();
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                AudioManager.Instance?.PlayMenuMusic();
                SetHUDEnabled(false);
                break;
            case GameState.Wardrobe:
            case GameState.HouseHub:
                HideAllPanels();
                SetHUDEnabled(true);
                RefreshMeters();
                break;
            case GameState.Encounter:
                HideAllPanels();
                SetHUDEnabled(true);
                RefreshMeters();
                break;
            case GameState.InterHouseMiniGame:
                HideAllPanels();
                SetHUDEnabled(true);
                RefreshMeters();
                break;
            case GameState.GameOver:
                break;
            case GameState.Win:
                break;
        }
    }

    /// <summary>
    /// Enables or disables the visibility of HUD sliders and numeric text.
    /// </summary>
    private void SetHUDEnabled(bool enabled)
    {
        if (batterySlider != null)
            batterySlider.gameObject.SetActive(enabled);

        if (stomachSlider != null)
            stomachSlider.gameObject.SetActive(enabled);

        if (timerText != null)
            timerText.gameObject.SetActive(enabled);

        if (runEidiaText != null)
            runEidiaText.gameObject.SetActive(enabled);

        if (totalScrapText != null)
            totalScrapText.gameObject.SetActive(enabled);
    }

    /// <summary>
    /// Syncs UI sliders with the latest values from MeterManager.
    /// </summary>
    private void RefreshMeters()
    {
        if (MeterManager.Instance == null) return;

        if (batterySlider != null)
        {
            float maxBattery = MeterManager.Instance.MaxBattery;
            float currentBattery = MeterManager.Instance.CurrentBattery;

            batterySlider.minValue = 0f;
            batterySlider.maxValue = maxBattery;
            batterySlider.value = currentBattery;
        }

        if (stomachSlider != null)
        {
            float currentStomach = MeterManager.Instance.CurrentStomach;

            stomachSlider.minValue = 0f;
            stomachSlider.maxValue = 100f;
            stomachSlider.value = currentStomach;
        }
    }

    /// <summary>
    /// Toggles the numeric countdown timer text.
    /// </summary>
    public void SetTimerVisibility(bool visible)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// External entry point to refresh meters during house transitions.
    /// </summary>
    public void RefreshMetersPublic() => RefreshMeters();

    private void HandleRunEidiaUpdated(int totalEidia)
    {
        if (runEidiaText != null)
        {
            runEidiaText.text = totalEidia.ToString();
            runEidiaText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }

    private void HandleTotalScrapUpdated(int totalScrap)
    {
        if (totalScrapText != null)
        {
            totalScrapText.text = totalScrap.ToString();
            totalScrapText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }

    private void HandleMetersChanged(float battery, float stomach)
    {
        if (batterySlider != null)
        {
            float maxBattery = MeterManager.Instance != null ? MeterManager.Instance.MaxBattery : 100f;
            batterySlider.maxValue = maxBattery;
            batterySlider.value = battery;
        }

        if (stomachSlider != null)
        {
            stomachSlider.maxValue = 100f;
            stomachSlider.value = stomach;
        }
    }

    /// <summary>
    /// Updates the battery slider with smooth animation and juice.
    /// </summary>
    private void HandleBatteryModified(float currentValue, float delta)
    {
        if (batterySlider == null) return;

        batterySlider.DOKill();
        float maxBattery = MeterManager.Instance != null ? MeterManager.Instance.MaxBattery : 100f;
        batterySlider.maxValue = maxBattery;
        batterySlider.DOValue(currentValue, 0.3f).SetEase(Ease.OutQuad);
        batterySlider.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);

        if (FloatingTextManager.Instance != null && Mathf.Abs(delta) >= 1f)
        {
            string sign = delta > 0 ? "+" : "";
            Color color = delta > 0 ? correctFeedbackColor : wrongFeedbackColor;
            FloatingTextManager.Instance.SpawnTextOverUI($"{sign}{Mathf.RoundToInt(delta)}", BatterySliderRect, color);
        }
    }

    /// <summary>
    /// Updates the stomach slider with smooth animation and juice.
    /// </summary>
    private void HandleStomachModified(float currentValue, float delta)
    {
        if (stomachSlider == null) return;

        stomachSlider.DOKill();
        stomachSlider.maxValue = 100f;
        stomachSlider.DOValue(currentValue, 0.3f).SetEase(Ease.OutQuad);
        stomachSlider.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);

        if (FloatingTextManager.Instance != null && Mathf.Abs(delta) >= 1f)
        {
            string sign = delta > 0 ? "+" : "";
            Color color = delta > 0 ? wrongFeedbackColor : correctFeedbackColor;
            FloatingTextManager.Instance.SpawnTextOverUI($"{sign}{Mathf.RoundToInt(delta)}", StomachSliderRect, color);
        }
    }

    #endregion

    #region Inspector Test Buttons

    #endregion
}
