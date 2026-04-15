using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// UNIFIED HUB MANAGER - Single panel for all hub interactions.
/// Merges HouseHub + Wardrobe + Tech Pit Stop into one clean interface.
///
/// ARCHITECTURE:
/// - 3 Tabs: Houses (navigation), Wardrobe (outfits), Upgrades (run bonuses)
/// - UIManager controls showing/hiding this panel
/// - GameManager subscribes to events for house/mini-game start
/// - No mid-run hacks, no confusing Enter/Exit buttons
///
/// FLOW:
/// Game Start → Hub (Houses tab) → Click "Start Run"
/// After each house → Hub appears → Choose next action
/// Player can switch tabs anytime to buy outfits or upgrades
/// Click "Start Next House/Mini-Game" → Continue run
/// </summary>
public class UnifiedHubManager : MonoBehaviour
{
    public static UnifiedHubManager Instance { get; private set; }

    #region Enums

    public enum HubTab
    {
        Houses,
        Wardrobe,
        Upgrades
    }

    public enum UpgradeType
    {
        RechargeBattery,
        ExpandBattery,
        TitaniumStomach
    }

    #endregion

    #region Inspector Fields - Tab Buttons

    [Header("Tab Buttons")]
    [Tooltip("Button to switch to Houses tab")]
    [SerializeField] private Button housesTabButton;

    [Tooltip("Button to switch to Wardrobe tab")]
    [SerializeField] private Button wardrobeTabButton;

    [Tooltip("Button to switch to Upgrades tab")]
    [SerializeField] private Button upgradesTabButton;

    [Header("Tab Panels")]
    [Tooltip("Panel containing house navigation buttons")]
    [SerializeField] private GameObject housesTabPanel;

    [Tooltip("Panel containing wardrobe UI")]
    [SerializeField] private GameObject wardrobeTabPanel;

    [Tooltip("Panel containing upgrade buttons")]
    [SerializeField] private GameObject upgradesTabPanel;

    [Header("Houses Tab UI")]
    [Tooltip("Button for House 1")]
    [SerializeField] private Button house1Button;

    [Tooltip("Button for House 2")]
    [SerializeField] private Button house2Button;

    [Tooltip("Button for House 3")]
    [SerializeField] private Button house3Button;

    [Tooltip("Button for House 4 (Insane/Boss)")]
    [SerializeField] private Button house4Button;

    [Tooltip("Mini-game button between House 1 and 2")]
    [SerializeField] private Button miniGame1Button;

    [Tooltip("Mini-game button between House 2 and 3")]
    [SerializeField] private Button miniGame2Button;

    [Tooltip("Mini-game button between House 3 and 4")]
    [SerializeField] private Button miniGame3Button;

    [Tooltip("Checkmark/icon for House 1 completion")]
    [SerializeField] private GameObject house1Checkmark;

    [Tooltip("Checkmark/icon for House 2 completion")]
    [SerializeField] private GameObject house2Checkmark;

    [Tooltip("Checkmark/icon for House 3 completion")]
    [SerializeField] private GameObject house3Checkmark;

    [Tooltip("Checkmark/icon for House 4 completion")]
    [SerializeField] private GameObject house4Checkmark;

    [Tooltip("Lock icon for House 2")]
    [SerializeField] private GameObject house2Lock;

    [Tooltip("Lock icon for House 3")]
    [SerializeField] private GameObject house3Lock;

    [Tooltip("Lock/skull icon for House 4")]
    [SerializeField] private GameObject house4Lock;

    [Header("Wardrobe Tab UI")]
    [Tooltip("Reference to WardrobeUI component (new simplified system)")]
    [SerializeField] private WardrobeUI wardrobeUI;

    [Header("Upgrades Tab UI")]
    [Tooltip("Button for Recharge Battery upgrade")]
    [SerializeField] private Button rechargeBatteryButton;

    [Tooltip("Button for Expand Battery upgrade")]
    [SerializeField] private Button expandBatteryButton;

    [Tooltip("Button for Titanium Stomach upgrade")]
    [SerializeField] private Button titaniumStomachButton;

    [Tooltip("Cost display text for Recharge Battery")]
    [SerializeField] private RTLTextMeshPro rechargeCostText;

    [Tooltip("Cost display text for Expand Battery")]
    [SerializeField] private RTLTextMeshPro expandCostText;

    [Tooltip("Cost display text for Titanium Stomach")]
    [SerializeField] private RTLTextMeshPro titaniumCostText;

    [Tooltip("Level display text for Recharge Battery")]
    [SerializeField] private RTLTextMeshPro rechargeLevelText;

    [Tooltip("Level display text for Expand Battery")]
    [SerializeField] private RTLTextMeshPro expandLevelText;

    [Tooltip("Level display text for Titanium Stomach")]
    [SerializeField] private RTLTextMeshPro titaniumLevelText;

    [Header("Celebration Panel")]
    [Tooltip("Panel shown after completing all 4 houses")]
    [SerializeField] private GameObject celebrationPanel;

    [Tooltip("'Play Again' button shown after full run completion")]
    [SerializeField] private Button playAgainButton;

    [Header("Mini-Game Replay Settings")]
    [Tooltip("Allow replaying mini-games after they've been unlocked")]
    [SerializeField] private bool allowMiniGameReplay = true;

    [Header("Action Button")]
    [Tooltip("Primary action button (Start Run / Start Next House / Continue)")]
    [SerializeField] private Button actionButton;

    [Tooltip("Action button text (RTL)")]
    [SerializeField] private RTLTextMeshPro actionButtonText;

    [Header("Upgrade Settings")]
    [Tooltip("Max times player can purchase Recharge per run")]
    [SerializeField] private int maxRechargePurchases = 3;

    [Tooltip("Max times player can purchase Expand Battery per run")]
    [SerializeField] private int maxExpandPurchases = 2;

    [Tooltip("Max times player can purchase Titanium Stomach per run")]
    [SerializeField] private int maxTitaniumPurchases = 2;

    [Header("Upgrade Base Costs")]
    [SerializeField] private int rechargeBaseCost = 5;

    [SerializeField] private int expandBaseCost = 10;

    [SerializeField] private int titaniumBaseCost = 8;

    [Header("Upgrade Cost Multipliers")]
    [Tooltip("1.0 = flat cost, >1.0 = scales with each purchase")]
    [SerializeField] private float rechargeCostMultiplier = 1.0f;

    [SerializeField] private float expandCostMultiplier = 1.5f;

    [SerializeField] private float titaniumCostMultiplier = 1.4f;

    [Header("Upgrade Values")]
    [Tooltip("Battery heal amount for Recharge")]
    [SerializeField] private float rechargeHealAmount = 25f;

    [Tooltip("Max battery increase for Expand")]
    [SerializeField] private float expandMaxBatteryIncrease = 20f;

    [Tooltip("Stomach fill rate reduction for Titanium (0.10 = 10%)")]
    [SerializeField] private float titaniumStomachReduction = 0.10f;

    [Header("Visual Feedback")]
    [Tooltip("Color when upgrade is affordable")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.9f, 0.2f);

    [Tooltip("Color when upgrade is too expensive")]
    [SerializeField] private Color expensiveColor = new Color(0.9f, 0.2f, 0.2f);

    [Tooltip("Punch scale amount for purchase feedback")]
    [SerializeField] private float punchScaleAmount = 0.2f;

    [SerializeField] private float punchDuration = 0.3f;

    #endregion

    #region Cached Arrays

    private Button[] houseButtons;
    private GameObject[] houseLocks;
    private GameObject[] houseCheckmarks;
    private Button[] miniGameButtons;

    #endregion

    #region Events

    /// <summary>
    /// Fires when player clicks action button to start a house.
    /// </summary>
    public static Action<int> OnStartNextHouse; // (houseLevel)

    /// <summary>
    /// Fires when player clicks a mini-game button.
    /// </summary>
    public static Action<int> OnStartMiniGame; // (miniGameIndex 0-2)

    /// <summary>
    /// Fires when player clicks Play After completing all houses.
    /// </summary>
    public static Action OnPlayAgain;

    /// <summary>
    /// Fires when an outfit is equipped (for stat application).
    /// </summary>
    public static Action<int> OnOutfitEquipped; // (outfitID)

    #endregion

    #region Private Fields

    private HubTab activeTab = HubTab.Houses;
    private int highestUnlockedHouse = 1;
    private bool[] completedHouses = new bool[5]; // Index 1-4
    private bool isFullRunComplete = false;
    private int nextHouseLevelToPlay = 1;

    // Upgrade tracking (resets each run)
    private Dictionary<UpgradeType, int> upgradePurchaseCounts = new Dictionary<UpgradeType, int>
    {
        { UpgradeType.RechargeBattery, 0 },
        { UpgradeType.ExpandBattery, 0 },
        { UpgradeType.TitaniumStomach, 0 }
    };

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Force all tab panels OFF regardless of scene checkbox settings
            if (housesTabPanel != null) housesTabPanel.SetActive(false);
            if (wardrobeTabPanel != null) wardrobeTabPanel.SetActive(false);
            if (upgradesTabPanel != null) upgradesTabPanel.SetActive(false);

            // Start with Houses tab active
            activeTab = HubTab.Houses;
            if (housesTabPanel != null) housesTabPanel.SetActive(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        InitializeArrays();
        RegisterButtonListeners();
        WardrobeManager.OnOutfitPurchased += RefreshWardrobeUI;
        WardrobeManager.OnOutfitEquipped += HandleOutfitEquipped;
        WardrobeManager.OnScrapChanged += RefreshWardrobeUI;
        SaveManager.OnScrapChanged += OnScrapChangedFromSaveManager; // Subscribe to scrap changes from mini-games
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
        WardrobeManager.OnOutfitPurchased -= RefreshWardrobeUI;
        WardrobeManager.OnOutfitEquipped -= HandleOutfitEquipped;
        WardrobeManager.OnScrapChanged -= RefreshWardrobeUI;
        SaveManager.OnScrapChanged -= OnScrapChangedFromSaveManager; // Unsubscribe from scrap changes
    }

    private void InitializeArrays()
    {
        houseButtons = new[] { house1Button, house2Button, house3Button, house4Button };
        houseLocks = new[] { null, null, house2Lock, house3Lock, house4Lock }; // Index 2 = House 2
        houseCheckmarks = new[] { null, house1Checkmark, house2Checkmark, house3Checkmark, house4Checkmark };
        miniGameButtons = new[] { miniGame1Button, miniGame2Button, miniGame3Button };
    }

    #endregion

    #region Button Registration

    private void RegisterButtonListeners()
    {
        // Tab buttons
        if (housesTabButton != null) housesTabButton.onClick.AddListener(() => SwitchTab(HubTab.Houses));
        if (wardrobeTabButton != null) wardrobeTabButton.onClick.AddListener(() => SwitchTab(HubTab.Wardrobe));
        if (upgradesTabButton != null) upgradesTabButton.onClick.AddListener(() => SwitchTab(HubTab.Upgrades));

        // House buttons
        for (int i = 0; i < houseButtons.Length; i++)
        {
            int houseLevel = i + 1;
            if (houseButtons[i] != null)
                houseButtons[i].onClick.AddListener(() => SelectHouse(houseLevel));
        }

        // Mini-game buttons
        for (int i = 0; i < miniGameButtons.Length; i++)
        {
            int miniGameIndex = i;
            if (miniGameButtons[i] != null)
                miniGameButtons[i].onClick.AddListener(() => SelectMiniGame(miniGameIndex));
        }

        // Upgrade buttons
        if (rechargeBatteryButton != null) rechargeBatteryButton.onClick.AddListener(() => PurchaseUpgrade(UpgradeType.RechargeBattery));
        if (expandBatteryButton != null) expandBatteryButton.onClick.AddListener(() => PurchaseUpgrade(UpgradeType.ExpandBattery));
        if (titaniumStomachButton != null) titaniumStomachButton.onClick.AddListener(() => PurchaseUpgrade(UpgradeType.TitaniumStomach));

        // Action button
        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);

        // Play again button
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    private void UnregisterButtonListeners()
    {
        if (housesTabButton != null) housesTabButton.onClick.RemoveAllListeners();
        if (wardrobeTabButton != null) wardrobeTabButton.onClick.RemoveAllListeners();
        if (upgradesTabButton != null) upgradesTabButton.onClick.RemoveAllListeners();

        foreach (var button in houseButtons)
            if (button != null) button.onClick.RemoveAllListeners();

        foreach (var button in miniGameButtons)
            if (button != null) button.onClick.RemoveAllListeners();

        if (rechargeBatteryButton != null) rechargeBatteryButton.onClick.RemoveAllListeners();
        if (expandBatteryButton != null) expandBatteryButton.onClick.RemoveAllListeners();
        if (titaniumStomachButton != null) titaniumStomachButton.onClick.RemoveAllListeners();
        if (actionButton != null) actionButton.onClick.RemoveAllListeners();
        if (playAgainButton != null) playAgainButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Public API - Hub Initialization

    /// <summary>
    /// Initializes the hub for the current game state.
    /// Called by GameManager when hub should appear.
    /// </summary>
    public void InitializeHub(int nextHouseLevel, bool[] completedHousesArray)
    {
        // Reset run state
        isFullRunComplete = false;
        nextHouseLevelToPlay = nextHouseLevel;
        this.completedHouses = new bool[5];

        if (completedHousesArray != null && completedHousesArray.Length >= 5)
            completedHousesArray.CopyTo(this.completedHouses, 0);

        // Determine highest unlocked house (House 4 unlocks after House 3 completion)
        if (completedHouses[3])
            highestUnlockedHouse = 4;
        else if (completedHouses[2])
            highestUnlockedHouse = 3;
        else if (completedHouses[1])
            highestUnlockedHouse = 2;
        else
            highestUnlockedHouse = 1;

        // Check if full run is complete
        if (completedHouses[1] && completedHouses[2] && completedHouses[3] && completedHouses[4])
            isFullRunComplete = true;

        // Reset upgrades for new run
        if (!isFullRunComplete)
        {
            upgradePurchaseCounts[UpgradeType.RechargeBattery] = 0;
            upgradePurchaseCounts[UpgradeType.ExpandBattery] = 0;
            upgradePurchaseCounts[UpgradeType.TitaniumStomach] = 0;
        }

        // Switch to Houses tab by default
        SwitchTab(HubTab.Houses);
        UpdateAllUI();

#if UNITY_EDITOR
        Debug.Log($"[UnifiedHub] Hub initialized. Next house: {nextHouseLevelToPlay}, Unlocked: {highestUnlockedHouse}");
#endif
    }

    /// <summary>
    /// Marks a house as complete and refreshes UI.
    /// </summary>
    public void MarkHouseComplete(int houseLevel)
    {
        // Validate house level to prevent indexing errors
        if (houseLevel < 1 || houseLevel > 4)
        {
            Debug.LogWarning($"[UnifiedHub] Invalid house level: {houseLevel}");
            return;
        }

        completedHouses[houseLevel] = true;

        // Update next house to play (cap at 5 since there's no house 5)
        nextHouseLevelToPlay = Mathf.Min(houseLevel + 1, 5);

        // Unlock House 4 when House 3 is completed
        if (houseLevel == 3 && highestUnlockedHouse < 4)
        {
            highestUnlockedHouse = 4;
#if UNITY_EDITOR
            Debug.Log("[UnifiedHub] House 4 unlocked!");
#endif
        }

        UpdateAllUI();

#if UNITY_EDITOR
        Debug.Log($"[UnifiedHub] House {houseLevel} marked complete. Next: {nextHouseLevelToPlay}, Unlocked: {highestUnlockedHouse}");
#endif
    }

    #endregion

    #region Tab Management

    /// <summary>
    /// Switches to a different tab in the hub.
    /// </summary>
    public void SwitchTab(HubTab tab)
    {
        activeTab = tab;

        // ONLY the active tab panel is ON, all others are OFF
        if (housesTabPanel != null) housesTabPanel.SetActive(tab == HubTab.Houses);
        if (wardrobeTabPanel != null) wardrobeTabPanel.SetActive(tab == HubTab.Wardrobe);
        if (upgradesTabPanel != null) upgradesTabPanel.SetActive(tab == HubTab.Upgrades);

        // Tab buttons: disable the currently active tab button (can't click it again)
        if (housesTabButton != null) housesTabButton.interactable = tab != HubTab.Houses;
        if (wardrobeTabButton != null) wardrobeTabButton.interactable = tab != HubTab.Wardrobe;
        if (upgradesTabButton != null) upgradesTabButton.interactable = tab != HubTab.Upgrades;

        // Refresh tab-specific UI
        if (tab == HubTab.Wardrobe) RefreshWardrobeUI();
        if (tab == HubTab.Upgrades) RefreshUpgradeUI();

#if UNITY_EDITOR
        Debug.Log($"[UnifiedHub] Switched to {tab} tab");
#endif
    }

    #endregion

    #region House & Mini-Game Selection

    private void SelectHouse(int houseLevel)
    {
        if (isFullRunComplete) return;
        if (houseLevel > highestUnlockedHouse) return;
        if (completedHouses[houseLevel]) return;

        // Validate sequential order
        if (houseLevel != nextHouseLevelToPlay)
        {
            Debug.LogWarning($"[UnifiedHub] Must complete House {nextHouseLevelToPlay} first!");
            return;
        }

        OnStartNextHouse?.Invoke(houseLevel);
    }

    private void SelectMiniGame(int miniGameIndex)
    {
        if (isFullRunComplete) return;

        int previousHouse = miniGameIndex + 1;
        int nextHouse = miniGameIndex + 2;

        // Validate previous house is complete (unlock condition)
        if (!completedHouses[previousHouse]) return;

        // For non-replay: validate next house isn't already complete
        // For replay mode: allow even if next house is complete
        if (!allowMiniGameReplay && completedHouses[nextHouse]) return;

        OnStartMiniGame?.Invoke(miniGameIndex);
    }

    #endregion

    #region Action Button

    private void OnActionButtonClicked()
    {
        if (isFullRunComplete) return;

        // Priority: Mini-game first (if available), then next house
        int miniGameIndex = nextHouseLevelToPlay - 1;

        if (miniGameIndex >= 0 && miniGameIndex < 3 && completedHouses[nextHouseLevelToPlay] && !completedHouses[nextHouseLevelToPlay + 1])
        {
            // Mini-game available
            SelectMiniGame(miniGameIndex);
        }
        else if (!completedHouses[nextHouseLevelToPlay] && nextHouseLevelToPlay <= highestUnlockedHouse)
        {
            // Next house available
            SelectHouse(nextHouseLevelToPlay);
        }
    }

    private void OnPlayAgainClicked()
    {
        OnPlayAgain?.Invoke();
    }

    #endregion

    #region Wardrobe Tab

    /// <summary>
    /// Wrapper for SaveManager.OnScrapChanged (Action<int>) to match RefreshWardrobeUI (Action).
    /// </summary>
    private void OnScrapChangedFromSaveManager(int newScrapTotal) => RefreshWardrobeUI();

    private void RefreshWardrobeUI()
    {
        if (WardrobeManager.Instance == null) return;

        // Use new WardrobeUI component to refresh
        if (wardrobeUI != null)
        {
            wardrobeUI.RefreshUI();
        }
        else
        {
            Debug.LogWarning("[UnifiedHub] WardrobeUI not assigned in inspector!");
        }
    }

    private void HandleOutfitEquipped()
    {
        int outfitID = WardrobeManager.Instance.EquippedOutfitID;
        string outfitName = "none";

        if (outfitID != 0 && WardrobeManager.Instance.AllOutfits.Count > 0)
        {
            var outfit = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == outfitID);
            if (outfit != null) outfitName = outfit.displayNameAR;
        }

        Debug.Log($"[UnifiedHub] Outfit equipped: ID={outfitID}, Name={outfitName}");

        RefreshWardrobeUI();

        // Notify GameManager for stat application
        OnOutfitEquipped?.Invoke(outfitID);
    }

    #endregion

    #region Upgrades Tab

    private void RefreshUpgradeUI()
    {
        int playerScrap = SaveManager.Instance != null ? SaveManager.Instance.CurrentData.TotalScrap : 0;

        UpdateUpgradeUI(
            UpgradeType.RechargeBattery,
            rechargeBatteryButton,
            rechargeCostText,
            rechargeLevelText,
            playerScrap,
            maxRechargePurchases
        );

        UpdateUpgradeUI(
            UpgradeType.ExpandBattery,
            expandBatteryButton,
            expandCostText,
            expandLevelText,
            playerScrap,
            maxExpandPurchases
        );

        UpdateUpgradeUI(
            UpgradeType.TitaniumStomach,
            titaniumStomachButton,
            titaniumCostText,
            titaniumLevelText,
            playerScrap,
            maxTitaniumPurchases
        );
    }

    private void UpdateUpgradeUI(
        UpgradeType type,
        Button button,
        RTLTextMeshPro costText,
        RTLTextMeshPro levelText,
        int playerScrap,
        int maxPurchases)
    {
        if (button == null || costText == null || levelText == null) return;

        int currentCost = GetCurrentCost(type);
        int level = upgradePurchaseCounts[type];
        bool isMaxed = level >= maxPurchases;

        // Update cost text
        costText.text = isMaxed ? "الحد الأقصى" : $"{currentCost} خردة";

        // Update level text
        levelText.text = isMaxed ? $"MAX ({maxPurchases}/{maxPurchases})" : $"{level}/{maxPurchases}";

        // Button interactability
        bool canAfford = playerScrap >= currentCost && !isMaxed;
        button.interactable = canAfford;

        // Color feedback
        if (isMaxed)
        {
            costText.DOColor(Color.gray, 0.2f);
        }
        else
        {
            Color targetColor = canAfford ? affordableColor : expensiveColor;
            costText.DOColor(targetColor, 0.2f);
        }
    }

    private void PurchaseUpgrade(UpgradeType upgradeType)
    {
        int maxPurchases = upgradeType switch
        {
            UpgradeType.RechargeBattery => maxRechargePurchases,
            UpgradeType.ExpandBattery => maxExpandPurchases,
            UpgradeType.TitaniumStomach => maxTitaniumPurchases,
            _ => 0
        };

        if (upgradePurchaseCounts[upgradeType] >= maxPurchases)
        {
            Debug.LogWarning($"[UnifiedHub] Upgrade {upgradeType} is MAXED!");
            return;
        }

        int currentCost = GetCurrentCost(upgradeType);
        int playerScrap = SaveManager.Instance != null ? SaveManager.Instance.CurrentData.TotalScrap : 0;

        if (playerScrap < currentCost)
        {
            Debug.LogWarning($"[UnifiedHub] Can't afford {upgradeType}. Need {currentCost}, have {playerScrap}");
            return;
        }

        // Deduct scrap
        SaveManager.Instance.CurrentData.TotalScrap -= currentCost;
        SaveManager.Instance.SaveGame();

        // Apply upgrade effect
        ApplyUpgradeEffect(upgradeType);

        // Increment purchase count
        upgradePurchaseCounts[upgradeType]++;

        // Play feedback animation
        PlayPurchaseAnimation(upgradeType);

        // Refresh UI
        RefreshUpgradeUI();

#if UNITY_EDITOR
        Debug.Log($"[UnifiedHub] Purchased {upgradeType} for {currentCost}. Level: {upgradePurchaseCounts[upgradeType]}/{maxPurchases}");
#endif
    }

    private int GetCurrentCost(UpgradeType upgradeType)
    {
        int baseCost = upgradeType switch
        {
            UpgradeType.RechargeBattery => rechargeBaseCost,
            UpgradeType.ExpandBattery => expandBaseCost,
            UpgradeType.TitaniumStomach => titaniumBaseCost,
            _ => 0
        };

        float multiplier = upgradeType switch
        {
            UpgradeType.RechargeBattery => rechargeCostMultiplier,
            UpgradeType.ExpandBattery => expandCostMultiplier,
            UpgradeType.TitaniumStomach => titaniumCostMultiplier,
            _ => 1.0f
        };

        int purchaseCount = upgradePurchaseCounts[upgradeType];
        float scaledCost = baseCost * Mathf.Pow(multiplier, purchaseCount);
        return Mathf.RoundToInt(scaledCost);
    }

    private void ApplyUpgradeEffect(UpgradeType upgradeType)
    {
        if (MeterManager.Instance == null) return;

        switch (upgradeType)
        {
            case UpgradeType.RechargeBattery:
                MeterManager.Instance.ModifyBattery(rechargeHealAmount);
                break;

            case UpgradeType.ExpandBattery:
                MeterManager.Instance.IncreaseMaxBattery(expandMaxBatteryIncrease);
                break;

            case UpgradeType.TitaniumStomach:
                MeterManager.Instance.ReduceStomachFillRate(titaniumStomachReduction);
                break;
        }
    }

    private void PlayPurchaseAnimation(UpgradeType upgradeType)
    {
        Button targetButton = upgradeType switch
        {
            UpgradeType.RechargeBattery => rechargeBatteryButton,
            UpgradeType.ExpandBattery => expandBatteryButton,
            UpgradeType.TitaniumStomach => titaniumStomachButton,
            _ => null
        };

        if (targetButton != null)
        {
            targetButton.transform.DOPunchScale(
                Vector3.one * punchScaleAmount,
                punchDuration,
                vibrato: 3,
                elasticity: 1f
            ).SetUpdate(true);
        }
    }

    #endregion

    #region UI Updates

    private void UpdateAllUI()
    {
        UpdateHousesUI();
        UpdateMiniGameButtons();
        UpdateActionButton();
        UpdateCelebrationUI();
    }

    private void UpdateHousesUI()
    {
        for (int i = 1; i <= 4; i++)
        {
            // Update checkmarks (1-based index)
            if (i < houseCheckmarks.Length && houseCheckmarks[i] != null)
                houseCheckmarks[i].SetActive(completedHouses[i]);

            // Update locks (1-based index)
            if (i < houseLocks.Length && houseLocks[i] != null)
            {
                // House i is locked if the highest unlocked house is lower than i
                bool isLocked = highestUnlockedHouse < i;
                houseLocks[i].SetActive(isLocked);
            }

            // Update buttons (array is 0-indexed)
            if (i <= houseButtons.Length && houseButtons[i - 1] != null)
            {
                bool isUnlocked = highestUnlockedHouse >= i;
                bool isCurrentTarget = i == nextHouseLevelToPlay;
                bool alreadyDone = completedHouses[i];
                
                houseButtons[i - 1].interactable = isUnlocked && isCurrentTarget && !alreadyDone;
            }
        }
    }

    private void UpdateMiniGameButtons()
    {
        // Mini-game 1: Between House 1 and 2
        if (miniGameButtons[0] != null)
        {
            bool unlocked1 = completedHouses[1];
            bool available1 = allowMiniGameReplay ? unlocked1 : (unlocked1 && !completedHouses[2]);
            miniGameButtons[0].interactable = available1;
        }

        // Mini-game 2: Between House 2 and 3
        if (miniGameButtons[1] != null)
        {
            bool unlocked2 = completedHouses[2];
            bool available2 = allowMiniGameReplay ? unlocked2 : (unlocked2 && !completedHouses[3]);
            miniGameButtons[1].interactable = available2;
        }

        // Mini-game 3: Between House 3 and 4
        if (miniGameButtons[2] != null)
        {
            bool unlocked3 = completedHouses[3] && highestUnlockedHouse >= 4;
            bool available3 = allowMiniGameReplay ? unlocked3 : (unlocked3 && !completedHouses[4]);
            miniGameButtons[2].interactable = available3;
        }
    }

    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null) return;

        if (isFullRunComplete)
        {
            actionButton.gameObject.SetActive(false);
            return;
        }

        actionButton.gameObject.SetActive(true);

        // Determine button text based on what's available
        // Guard against nextHouseLevelToPlay > 4 (all houses complete)
        if (nextHouseLevelToPlay > 4)
        {
            actionButtonText.text = "متابعة";
            actionButton.interactable = false;
            return;
        }

        int miniGameIndex = nextHouseLevelToPlay - 1;
        bool miniGameAvailable = miniGameIndex >= 0 && miniGameIndex < 3 &&
                                 completedHouses[nextHouseLevelToPlay] &&
                                 nextHouseLevelToPlay + 1 <= 4 &&
                                 !completedHouses[nextHouseLevelToPlay + 1];

        bool houseAvailable = !completedHouses[nextHouseLevelToPlay] &&
                              nextHouseLevelToPlay <= highestUnlockedHouse;

        if (miniGameAvailable)
        {
            actionButtonText.text = $"ابدأ اللعبة المصغرة {miniGameIndex + 1}";
        }
        else if (houseAvailable)
        {
            actionButtonText.text = $"ابدأ البيت {nextHouseLevelToPlay}";
        }
        else
        {
            actionButtonText.text = "متابعة";
        }

        actionButton.interactable = miniGameAvailable || houseAvailable;
    }

    private void UpdateCelebrationUI()
    {
        if (celebrationPanel != null)
            celebrationPanel.SetActive(isFullRunComplete);

        if (playAgainButton != null)
            playAgainButton.gameObject.SetActive(isFullRunComplete);
    }

    #endregion

    #region Inspector Test Buttons

    [Button("Test: Hub Start (House 1)")]
    private void TestHubStart()
    {
        bool[] completed = new bool[5];
        InitializeHub(1, completed);
    }

    [Button("Test: After House 1")]
    private void TestHubAfterHouse1()
    {
        bool[] completed = new bool[5];
        completed[1] = true;
        InitializeHub(2, completed);
    }

    [Button("Test: After House 3 (House 4 unlocked)")]
    private void TestHubAfterHouse3()
    {
        bool[] completed = new bool[5];
        completed[1] = true;
        completed[2] = true;
        completed[3] = true;
        InitializeHub(4, completed);
    }

    [Button("Test: Full Run Complete")]
    private void TestHubFullComplete()
    {
        bool[] completed = new bool[5];
        completed[1] = true;
        completed[2] = true;
        completed[3] = true;
        completed[4] = true;
        InitializeHub(4, completed);
    }

    #endregion
}
