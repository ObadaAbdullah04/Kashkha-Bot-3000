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

    [Header("Encounter UI")]
    [SerializeField] private RTLTextMeshPro questionText;
    [SerializeField] private RTLTextMeshPro[] choiceTexts;
    [SerializeField] private ChoiceCard[] choiceCards;

    [Header("Feedback UI")]
    [SerializeField] private RTLTextMeshPro feedbackText;
    [SerializeField] private GameObject feedbackPanel;

    [Header("QTE Warning UI")]
    [SerializeField] private GameObject qteWarningPanel;
    [SerializeField] private RTLTextMeshPro qteWarningText;

    [Header("Meter UI")]
    [SerializeField] private Slider batterySlider;
    [SerializeField] private Slider stomachSlider;

    [Header("Game State Panels")]
    [SerializeField] private GameObject encounterPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject wardrobePanel;

    [Header("Wardrobe UI")]
    [SerializeField] private RTLTextMeshPro scrapCounterText;
    [SerializeField] private OutfitSlot[] outfitSlots;
    [SerializeField] private Button startRunButton;
    
    [Header("Crossroads UI (NEW)")]
    [SerializeField] private GameObject crossroadsPanel;
    [SerializeField] private RTLTextMeshPro crossroadsTitleText;
    [SerializeField] private RTLTextMeshPro crossroadsStatusText;
    [SerializeField] private Button escapeButton;
    [SerializeField] private Button riskButton;

    [Header("Screen Shake")]
    [SerializeField] private RectTransform mainPanel;

    [Header("Feedback Colors")]
    [SerializeField] private Color correctFeedbackColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color wrongFeedbackColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Header("Animation Settings")]
    [SerializeField] private float questionFadeDuration = 0.3f;
    [SerializeField] private float choiceStaggerDelay = 0.1f;

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

        // Initialize Crossroads UI buttons
        if (crossroadsPanel != null)
        {
            crossroadsPanel.SetActive(false);

            if (escapeButton != null)
                escapeButton.onClick.AddListener(() => GameManager.Instance.ChooseEscape());

            if (riskButton != null)
                riskButton.onClick.AddListener(() => GameManager.Instance.ChooseRiskHouse4());
        }

        // Initialize Wardrobe UI
        if (wardrobePanel != null)
        {
            wardrobePanel.SetActive(false);

            if (startRunButton != null)
                startRunButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartRun();
                });
        }

        // Don't call InitializeUI() here - it hides all panels
        // Instead, handle initial state directly
        HandleInitialState();
    }

    /// <summary>
    /// Handles the initial game state on startup (Wardrobe).
    /// </summary>
    private void HandleInitialState()
    {
        HideAllPanels();

        if (GameManager.Instance != null)
        {
            GameState initialState = GameManager.Instance.CurrentState;
            Debug.Log($"[UIManager] Initial state: {initialState}");

            if (initialState == GameState.Wardrobe)
            {
                wardrobePanel.SetActive(true);
                RefreshWardrobeUI();
            }
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        GameManager.OnRunStarted += HandleRunStarted;
        MeterManager.OnMetersChanged += HandleMetersChanged;
        WardrobeManager.OnScrapChanged += HandleWardrobeUpdated;
        WardrobeManager.OnOutfitPurchased += HandleWardrobeUpdated;
        WardrobeManager.OnOutfitEquipped += HandleWardrobeUpdated;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
        GameManager.OnRunStarted -= HandleRunStarted;
        MeterManager.OnMetersChanged -= HandleMetersChanged;
        WardrobeManager.OnScrapChanged -= HandleWardrobeUpdated;
        WardrobeManager.OnOutfitPurchased -= HandleWardrobeUpdated;
        WardrobeManager.OnOutfitEquipped -= HandleWardrobeUpdated;

        // Kill all active tweens to prevent memory leaks
        _feedbackSequence?.Kill();

        if (mainPanel != null)
            mainPanel.DOKill();

        if (qteWarningPanel != null)
            qteWarningPanel.transform.DOKill();

        foreach (var card in choiceCards)
        {
            if (card != null)
                card.KillTweens();
        }
    }

    private void HandleRunStarted()
    {
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
        encounterPanel.SetActive(true);
        feedbackPanel.SetActive(false);
        qteWarningPanel.SetActive(false);

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

    #endregion

    #region Public Display Methods

    public void DisplayEncounter(EncounterData data)
    {
        if (data == null)
        {
            Debug.LogError("[UIManager] Cannot display null EncounterData!");
            return;
        }

        // Ensure encounter panel is visible
        if (encounterPanel != null)
            encounterPanel.SetActive(true);

        feedbackPanel.SetActive(false);

        if (questionText != null)
        {
            questionText.text = data.QuestionAR;
            questionText.gameObject.SetActive(true);
            AnimateTextFadeIn(questionText.gameObject, questionFadeDuration);
        }

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            if (i < 3 && choiceTexts[i] != null && choiceCards[i] != null)
            {
                choiceCards[i].ResetInstant();
                choiceCards[i].SetLogicIndex(i); // FIX: Set the logic index for correct button mapping

                string choiceText = GetChoiceText(data, i);
                choiceTexts[i].text = choiceText;
                choiceTexts[i].gameObject.SetActive(true);

                choiceCards[i].gameObject.SetActive(true);
                AnimateTextFadeIn(choiceCards[i].gameObject, 0.3f, i * choiceStaggerDelay);
            }
        }

        // Start idle floating after cards are visible
        Invoke(nameof(StartIdle0), 0.5f);
        Invoke(nameof(StartIdle1), 0.65f);
        Invoke(nameof(StartIdle2), 0.8f);
    }

    private void StartIdle0() => choiceCards[0]?.SetIdleFloating(true);
    private void StartIdle1() => choiceCards[1]?.SetIdleFloating(true);
    private void StartIdle2() => choiceCards[2]?.SetIdleFloating(true);

    public void ShowFeedback(string text, bool isCorrect, Action onComplete)
    {
        if (feedbackPanel == null)
        {
            Debug.LogError("[UIManager] FeedbackPanel not assigned!");
            return;
        }

        if (_feedbackCanvasGroup == null)
        {
            _feedbackCanvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            if (_feedbackCanvasGroup == null)
                _feedbackCanvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
        }

        Image feedbackImage = feedbackPanel.GetComponent<Image>();
        if (feedbackImage == null)
            feedbackImage = feedbackPanel.AddComponent<Image>();

        if (feedbackText != null)
            feedbackText.text = text;

        Color targetColor = isCorrect ? correctFeedbackColor : wrongFeedbackColor;

        feedbackPanel.SetActive(true);

        _feedbackSequence?.Kill();
        _feedbackCanvasGroup.alpha = 0f;
        _feedbackCanvasGroup.interactable = false;
        _feedbackCanvasGroup.blocksRaycasts = false;
        feedbackImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        _feedbackSequence = DOTween.Sequence()
            .Append(_feedbackCanvasGroup.DOFade(1f, 0.22f).SetUpdate(true))
            .Join(DOTween.To(
                () => feedbackImage.color,
                x => feedbackImage.color = x,
                targetColor,
                0.22f
            ).SetUpdate(true))
            .AppendInterval(1.9f)
            .Append(_feedbackCanvasGroup.DOFade(0f, 0.22f).SetUpdate(true))
            .OnComplete(() =>
            {
                feedbackPanel.SetActive(false);
                _feedbackCanvasGroup.interactable = false;
                _feedbackCanvasGroup.blocksRaycasts = false;
                onComplete?.Invoke();
            })
            .SetUpdate(true);
    }

    public void ShowFeedback(string text, Action onComplete)
    {
        ShowFeedback(text, true, onComplete);
    }

    public void PlayCardAnimation(int index, bool isCorrect)
    {
        if (choiceCards.Length > index && choiceCards[index] != null)
        {
            if (isCorrect)
                choiceCards[index].AnimateCorrect();
            else
                choiceCards[index].AnimateWrong();
        }
    }

    public void ShowQTEWarning(string instructionText)
    {
        if (qteWarningPanel == null || qteWarningText == null)
        {
            Debug.LogWarning("[UIManager] QTE Warning UI not assigned!");
            return;
        }

        // Hide any existing QTE warning first to prevent stacking
        qteWarningPanel.SetActive(false);

        // Directly use the Arabic instruction text passed from GameManager
        // (GetQTEInstructionAR already converts input type to Arabic)
        qteWarningText.text = instructionText;
        qteWarningPanel.SetActive(true);
        qteWarningPanel.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 1f);
    }

    public void HideQTEWarning()
    {
        if (qteWarningPanel != null)
            qteWarningPanel.SetActive(false);
    }

    /// <summary>
    /// Hides all UI panels during the inter-house mini-game.
    /// Called by MiniGameManager before instantiating the catch game prefab.
    /// </summary>
    public void HideAllPanelsForMiniGame()
    {
        HideAllPanels();
    }

    public void ShakeSocialShutdown()
    {
        // Impact both UI and Camera
        if (mainPanel != null)
        {
            mainPanel.DOKill();
            mainPanel.DOShakeAnchorPos(0.70f, new Vector2(40f, 22f), 30, 90, true).SetUpdate(true);
        }

        CameraShakeManager.Instance?.ShakeSocialShutdown();
    }

    public void ShakeMaamoulExplosion()
    {
        // Impact both UI and Camera
        if (mainPanel != null)
        {
            mainPanel.DOKill();
            mainPanel.DOShakeAnchorPos(0.90f, new Vector2(55f, 30f), 35, 180, true).SetUpdate(true);
        }

        CameraShakeManager.Instance?.ShakeMaamoulExplosion();
    }

    public void ShakeQTEFail()
    {
        // Impact both UI and Camera
        if (mainPanel != null)
        {
            mainPanel.DOKill();
            mainPanel.DOShakeAnchorPos(0.35f, new Vector2(12f, 6f), 15, 80, true).SetUpdate(true);
        }

        CameraShakeManager.Instance?.ShakeQTEFail();
    }

    public void SetPanicMode(bool active)
    {
        foreach (var card in choiceCards)
        {
            if (card != null)
                card.SetIdleFloating(!active);
        }
    }

    #endregion

    #region Private Helper Methods

    private string GetChoiceText(EncounterData data, int index)
    {
        return index switch
        {
            0 => data.Choice1AR,
            1 => data.Choice2AR,
            2 => data.Choice3AR,
            _ => ""
        };
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
        encounterPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        qteWarningPanel.SetActive(false);
        crossroadsPanel.SetActive(false);
        wardrobePanel.SetActive(false);
    }

    #endregion

    #region Crossroads UI (NEW)

    /// <summary>
    /// Shows the Crossroads decision panel after House 3.
    /// Player can choose to Escape (Win) or Risk House 4.
    /// </summary>
    /// <param name="canEscape">If true, player has enough Eidia to escape/win</param>
    public void ShowCrossroadsPanel(bool canEscape)
    {
        if (crossroadsPanel == null)
        {
            Debug.LogError("[UIManager] CrossroadsPanel not assigned in Inspector!");
            return;
        }

        crossroadsPanel.SetActive(true);

        if (crossroadsTitleText != null)
            crossroadsTitleText.text = canEscape ? "المفترق!" : "طريق المسدود";

        if (crossroadsStatusText != null)
        {
            crossroadsStatusText.text = canEscape
                ? $"جمعت {GameManager.Instance.AccumulatedEidia} دينار!\nاهرب الآن أو خاطِر ببيت رابع؟"
                : "ما جمعتش كفاية من العيديا...\nلازم تكمل لبيت رابع!";
        }

        // Enable/disable buttons based on canEscape
        if (escapeButton != null)
            escapeButton.interactable = canEscape;

        if (riskButton != null)
            riskButton.interactable = true;
    }

    #endregion

    #region Wardrobe UI

    /// <summary>
    /// Refreshes all Wardrobe UI elements (called on scrap change, purchase, equip).
    /// </summary>
    private void RefreshWardrobeUI()
    {
        if (WardrobeManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] WardrobeManager not available!");
            return;
        }

        // Validate references
        if (wardrobePanel == null)
        {
            Debug.LogError("[UIManager] wardrobePanel is NULL! Assign it in Inspector.");
            return;
        }

        if (scrapCounterText == null)
        {
            Debug.LogError("[UIManager] scrapCounterText is NULL! Assign it in Inspector.");
        }

        if (outfitSlots == null || outfitSlots.Length == 0)
        {
            Debug.LogError("[UIManager] outfitSlots array is NULL or EMPTY! Assign at least 1 slot in Inspector.");
        }

        // Read values directly (sync first to ensure we have latest from save)
        WardrobeManager.Instance.SyncScrap();
        int playerScrap = WardrobeManager.Instance.CurrentScrap;
        int equippedID = WardrobeManager.Instance.EquippedOutfitID;

        Debug.Log($"[UIManager] Refreshing Wardrobe: Scrap={playerScrap}, EquippedID={equippedID}, Outfits loaded={WardrobeManager.Instance.AllOutfits.Count}");

        // Update scrap counter
        if (scrapCounterText != null)
            scrapCounterText.text = $"{playerScrap} خردة";

        // Update outfit slots
        if (outfitSlots != null)
        {
            for (int i = 0; i < outfitSlots.Length; i++)
            {
                if (i < WardrobeManager.Instance.AllOutfits.Count)
                {
                    OutfitData outfit = WardrobeManager.Instance.AllOutfits[i];
                    outfitSlots[i].gameObject.SetActive(true);
                    outfitSlots[i].Initialize(outfit, WardrobeManager.Instance.OwnsOutfit(outfit.ID), equippedID == outfit.ID, playerScrap);
                    Debug.Log($"[UIManager] Slot {i}: {outfit.displayNameAR}, Cost={outfit.scrapCost}, Owned={WardrobeManager.Instance.OwnsOutfit(outfit.ID)}");
                }
                else
                {
                    outfitSlots[i].gameObject.SetActive(false);
                }
            }
        }

        Debug.Log("[UIManager] Wardrobe UI refreshed.");
    }

    private void HandleWardrobeUpdated()
    {
        // Only refresh if wardrobe panel is active (prevents unnecessary updates during runs)
        // Also check if WardrobeManager is initialized (has outfits loaded)
        if (wardrobePanel != null && 
            wardrobePanel.activeSelf && 
            WardrobeManager.Instance != null && 
            WardrobeManager.Instance.AllOutfits.Count > 0)
        {
            RefreshWardrobeUI();
        }
    }

    #endregion

    #region Event Handlers

    private void HandleStateChanged(GameState newState)
    {
        HideAllPanels();

        switch (newState)
        {
            case GameState.Wardrobe:
                // Show Wardrobe panel
                wardrobePanel.SetActive(true);
                RefreshWardrobeUI();
                break;
            case GameState.Encounter:
                encounterPanel.SetActive(true);
                break;
            case GameState.QTE:
                // QTE warning shown separately
                break;
            case GameState.InterHouseMiniGame:
                // All panels hidden - mini-game prefab handles its own UI
                break;
            case GameState.Crossroads:
                // CrossroadsPanel shown via ShowCrossroadsPanel()
                break;
            case GameState.House4Boss:
                encounterPanel.SetActive(true);
                break;
            case GameState.GameOver:
                gameOverPanel.SetActive(true);
                break;
            case GameState.Win:
                winPanel.SetActive(true);
                break;
        }
    }

    private void HandleMetersChanged(float battery, float stomach)
    {
        if (batterySlider != null)
            batterySlider.value = battery;

        if (stomachSlider != null)
            stomachSlider.value = stomach;
    }

    #endregion

    #region Inspector Test Buttons

    [Button("Test QTE Warning")]
    private void TestQTE() => ShowQTEWarning("CoffeeRefuse");

    #endregion
}
