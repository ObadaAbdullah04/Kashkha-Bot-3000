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

    #endregion

    #region Inspector Fields

    [Header("Feedback UI")]
    [SerializeField] private RTLTextMeshPro feedbackText;
    [SerializeField] private GameObject feedbackPanel;

    [Header("Meter UI")]
    [Tooltip("Battery slider (shows current social battery)")]
    [SerializeField] private Slider batterySlider;
    [Tooltip("Stomach slider (shows current stomach fullness)")]
    [SerializeField] private Slider stomachSlider;
    [Tooltip("Timer slider (shows per-card timer countdown)")]
    [SerializeField] private Slider timerSlider;

    [Header("Game State Panels")]
    [Tooltip("Main menu start screen panel")]
    [SerializeField] private GameObject mainMenuPanel;
    [Tooltip("Start button on the main menu")]
    [SerializeField] private Button startBtn;
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

    [Header("Card Idle Animation")]

    public static Action OnPlayAgain;

    #endregion

    #region Private Fields

    private Sequence _feedbackSequence;
    private CanvasGroup _feedbackCanvasGroup;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
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
                AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
                if (GameManager.Instance != null)
                    GameManager.Instance.StartRun();
            });
        }

        // Handle initial state
        HandleInitialState();
    }

    /// <summary>
    /// Called when the player clicks Play Again on Game Over or Win panel.
    /// </summary>
    private void OnPlayAgainClicked()
    {
        Debug.Log("[UIManager] PLAY AGAIN CLICKED");
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
        OnPlayAgain?.Invoke();
    }

    /// <summary>
    /// Called when the player clicks Exit on Game Over or Win panel.
    /// Returns to main menu state.
    /// </summary>
    private void OnExitToMainMenu()
    {
        Debug.Log("[UIManager] EXIT TO MAIN MENU CLICKED");
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);

        if (GameManager.Instance != null)
        {
            // Reset everything and go back to main menu
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    public void QuitGame()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.ButtonClick);
        
        // Debug.Log("[UIManager] Quitting Game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Public wrapper for Inspector OnClick event assignment.
    /// </summary>
    public void ExitToMainMenu() => OnExitToMainMenu();

    /// <summary>
    /// Handles the initial game state on startup.
    /// </summary>
    private void HandleInitialState()
    {
        HideAllPanels();

        if (GameManager.Instance != null)
        {
            GameState initialState = GameManager.Instance.CurrentState;
            // Debug.Log($"[UIManager] Initial state: {initialState}");

            // Show appropriate panel based on initial state
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
        MeterManager.OnMetersChanged += HandleMetersChanged;
        MeterManager.OnBatteryModified += HandleBatteryModified;
        MeterManager.OnStomachModified += HandleStomachModified;

        // Exit buttons
        if (gameOverExitButton != null) gameOverExitButton.onClick.AddListener(OnExitToMainMenu);
        if (winExitButton != null) winExitButton.onClick.AddListener(OnExitToMainMenu);

        // Play Again buttons
        if (gameOverPlayAgainButton != null) gameOverPlayAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (winPlayAgainButton != null) winPlayAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
        GameManager.OnRunStarted -= HandleRunStarted;
        MeterManager.OnMetersChanged -= HandleMetersChanged;
        MeterManager.OnBatteryModified -= HandleBatteryModified;
        MeterManager.OnStomachModified -= HandleStomachModified;

        // Exit buttons
        if (gameOverExitButton != null) gameOverExitButton.onClick.RemoveListener(OnExitToMainMenu);
        if (winExitButton != null) winExitButton.onClick.RemoveListener(OnExitToMainMenu);

        // Play Again buttons
        if (gameOverPlayAgainButton != null) gameOverPlayAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        if (winPlayAgainButton != null) winPlayAgainButton.onClick.RemoveListener(OnPlayAgainClicked);

        // Kill all active tweens to prevent memory leaks
        _feedbackSequence?.Kill();

        if (mainPanel != null)
            mainPanel.DOKill();
    }

    private void HandleRunStarted()
    {
        // Play gameplay music on run start
        AudioManager.Instance?.PlayGameplayMusic();

        InitializeUI();
        // Feedback panel might be active from a previous game over
        feedbackPanel.SetActive(false);
        _feedbackSequence?.Kill();
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        HideAllPanels();
        swipeEncounterPanel.SetActive(true);
        feedbackPanel.SetActive(false);

        // Hide HUD meters by default - only shown during encounters
        SetHUDEnabled(false);

        // Set slider max values and refresh current values from MeterManager
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
            // Fallback if MeterManager not available
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

    public void ShowFeedback(string text, bool isCorrect, Action onComplete)
    {
        if (feedbackPanel == null)
        {
            // Debug.LogError("[UIManager] FeedbackPanel not assigned!");
            return;
        }

        EnsureFeedbackComponents();

        // KILL existing sequence and force reset
        _feedbackSequence?.Kill();
        
        Image feedbackImage = feedbackPanel.GetComponent<Image>();
        if (feedbackText != null)
            feedbackText.text = text;

        Color targetColor = isCorrect ? correctFeedbackColor : wrongFeedbackColor;

        // Force active and reset alpha
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
    /// Shows a neutral instruction text (blue color).
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
            .AppendInterval(2.5f) // Slightly longer for instructions
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
    /// Hides all UI panels during the inter-house mini-game.
    /// Called by MiniGameManager before instantiating the catch game prefab.
    /// </summary>
    public void HideAllPanelsForMiniGame()
    {
        HideAllPanels();
    }

    /// <summary>
    /// Hides the swipe encounter panel specifically.
    /// Called before house transitions to prevent stale content showing.
    /// </summary>
    public void HideSwipeEncounter()
    {
        if (swipeEncounterPanel != null)
            swipeEncounterPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the swipe encounter panel.
    /// Called during house transition midpoint (while screen is black).
    /// </summary>
    public void ShowSwipeEncounter()
    {
        if (swipeEncounterPanel != null)
            swipeEncounterPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the interaction HUD specifically.
    /// Called before house transitions to prevent stale content showing.
    /// </summary>
    public void HideInteractionHUD()
    {
        InteractionHUDController.Instance?.HideHUD();
    }

    public void ShakeSocialShutdown()
    {
        ShakePanel(socialShutdownShakeDuration, socialShutdownShakeAmplitude, socialShutdownShakeVibrato, socialShutdownShakeRandomness);
        CameraShakeManager.Instance?.ShakeSocialShutdown();
    }

    public void ShakeMaamoulExplosion()
    {
        ShakePanel(maamoulExplosionShakeDuration, maamoulExplosionShakeAmplitude, maamoulExplosionShakeVibrato, maamoulExplosionShakeRandomness);
        CameraShakeManager.Instance?.ShakeMaamoulExplosion();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Shakes the main panel with specified parameters.
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
    /// Ensures feedback panel has required components (CanvasGroup, Image).
    /// </summary>
    private void EnsureFeedbackComponents()
    {
        if (_feedbackCanvasGroup == null)
        {
            _feedbackCanvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (_feedbackCanvasGroup == null)
            {
                // Debug.LogWarning("[UIManager] feedbackPanel missing CanvasGroup — adding at runtime. Please add to prefab!");
                _feedbackCanvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
            }
        }

        if (feedbackPanel.GetComponent<Image>() == null)
        {
            // Debug.LogWarning("[UIManager] feedbackPanel missing Image component — adding at runtime. Please add to prefab!");
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

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        unifiedHubPanel.SetActive(false);
        swipeEncounterPanel.SetActive(false);
    }

    #endregion

    #region PHASE 10: Unified Hub UI

    /// <summary>
    /// PHASE 10: Shows the unified hub panel.
    /// UnifiedHubManager controls tab switching and UI updates.
    /// </summary>
    public void ShowUnifiedHub()
    {
        HideAllPanels();

        if (unifiedHubPanel != null)
        {
            unifiedHubPanel.SetActive(true);
        }

        // Hide HUD meters when in hub (no battery/stomach/timer visible)
        SetHUDEnabled(false);

#if UNITY_EDITOR
        // Debug.Log("[UIManager] Unified Hub panel shown.");
#endif
    }

    /// <summary>
    /// Shows the unified hub panel WITHOUT hiding other panels.
    /// Used for Game Over / Win states where both hub and result panel should show.
    /// </summary>
    private void ShowUnifiedHubWithoutHidingOthers()
    {
        if (unifiedHubPanel != null)
        {
            unifiedHubPanel.SetActive(true);
        }

        // Hide HUD meters when in hub (no battery/stomach/timer visible)
        SetHUDEnabled(false);

#if UNITY_EDITOR
        // Debug.Log("[UIManager] Unified Hub panel shown (overlay mode).");
#endif
    }

    /// <summary>
    /// PHASE 10: Hides the unified hub panel.
    /// </summary>
    public void HideUnifiedHub()
    {
        if (unifiedHubPanel != null)
            unifiedHubPanel.SetActive(false);
    }

    #endregion

    #region Event Handlers

    public void ShowGameOver(int totalEidia)
    {
        HideAllPanels();
        gameOverPanel.SetActive(true);
        if (gameOverEidiaText != null)
        {
            gameOverEidiaText.text = $"عيدية مجمعة: {totalEidia}";
        }
        SetHUDEnabled(false);
        HideUnifiedHub();
    }

    public void ShowWin(int totalEidia)
    {
        HideAllPanels();
        winPanel.SetActive(true);
        if (winEidiaText != null)
        {
            winEidiaText.text = $"عيدية مجمعة: {totalEidia}";
        }
        SetHUDEnabled(false);
        HideUnifiedHub();
    }

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
                // Both shown via unified hub - manager controls display
                // Hide HUD meters when in hub
                SetHUDEnabled(false);
                break;
            case GameState.Encounter:
                HideAllPanels();
                // CRITICAL FIX: Don't show encounter panel yet - it will be revealed
                // after the house transition completes (prevents showing behind fade)
                // HouseFlowController.ShowSingleCard will activate cards when ready
                // Show HUD meters and force refresh when entering a house
                SetHUDEnabled(true);
                RefreshMeters();
                break;
            case GameState.InterHouseMiniGame:
                HideAllPanels();
                // All panels hidden - mini-game prefab handles its own UI
                SetHUDEnabled(false);
                break;
            case GameState.GameOver:
                // Now handled by ShowGameOver
                break;
            case GameState.Win:
                // Now handled by ShowWin
                break;
        }
    }

    /// <summary>
    /// Shows or hides the HUD meters (battery/stomach/timer sliders).
    /// </summary>
    private void SetHUDEnabled(bool enabled)
    {
        if (batterySlider != null)
            batterySlider.gameObject.SetActive(enabled);

        if (stomachSlider != null)
            stomachSlider.gameObject.SetActive(enabled);

        if (timerSlider != null)
            timerSlider.gameObject.SetActive(enabled);
    }

    /// <summary>
    /// Force-refreshes meter sliders to current values.
    /// Called when entering a house to ensure UI is up to date.
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
    /// Public wrapper for RefreshMeters - called from GameManager when entering a house.
    /// </summary>
    public void RefreshMetersPublic() => RefreshMeters();

    private void HandleMetersChanged(float battery, float stomach)
    {
        // Fallback handler for initialization - sets values instantly
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
    /// Event handler for battery modification - animates slider with DOTween.
    /// Receives (currentValue 0-maxBattery, delta) from MeterManager.
    /// </summary>
    private void HandleBatteryModified(float currentValue, float delta)
    {
        if (batterySlider == null) return;

        // Kill any existing tween on this slider to prevent conflicts
        batterySlider.DOKill();

        // Get current max battery from MeterManager (can be >100 from upgrades)
        float maxBattery = MeterManager.Instance != null ? MeterManager.Instance.MaxBattery : 100f;

        // Update slider max value to match current max battery
        batterySlider.maxValue = maxBattery;

        // Set value directly (no normalization needed with correct maxValue)
        batterySlider.DOValue(currentValue, 0.3f).SetEase(Ease.OutQuad);
        
        // JUICE: Punch scale on modification
        batterySlider.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
    }

    /// <summary>
    /// Event handler for stomach modification - animates slider with DOTween.
    /// Receives (currentValue 0-100, delta) from MeterManager.
    /// </summary>
    private void HandleStomachModified(float currentValue, float delta)
    {
        if (stomachSlider == null) return;

        // Kill any existing tween on this slider to prevent conflicts
        stomachSlider.DOKill();

        // Stomach is always 0-100, so maxValue stays at 100
        stomachSlider.maxValue = 100f;

        // Set value directly (no normalization needed with correct maxValue)
        stomachSlider.DOValue(currentValue, 0.3f).SetEase(Ease.OutQuad);
        
        // JUICE: Punch scale on modification
        stomachSlider.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
    }

    #endregion

    #region Inspector Test Buttons

    #endregion
}