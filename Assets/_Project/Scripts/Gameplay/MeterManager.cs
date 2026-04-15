using System;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// PHASE 6 REFACTORED: Simplified meter manager.
/// Manages Social Battery and Stomach meters without complex strike system.
/// </summary>
public class MeterManager : MonoBehaviour
{
    public static MeterManager Instance { get; private set; }

    #region Inspector Fields

    [Header("Meter Values")]
    [Range(0f, 100f), SerializeField] private float currentBattery = 100f;
    [Range(0f, 100f), SerializeField] private float currentStomach = 0f;

    [Header("Outfit Stat Bonuses (Applied at Runtime)")]
    [Tooltip("Starting battery bonus from outfit (e.g. +10 = start with 110%)")]
    [ReadOnly] [SerializeField] private float outfitBatteryBonus = 0f;
    [Tooltip("Stomach fill rate reduction from outfit (e.g. -10 = 10% less stomach fill)")]
    [ReadOnly] [SerializeField] private float outfitStomachResist = 0f;

    [Header("Upgrade Stat Modifiers (Per-Run, from UnifiedHubManager)")]
    [Tooltip("Max battery capacity (can be increased by Expand Battery upgrade)")]
    [SerializeField] private float maxBattery = 100f;
    [Tooltip("Stomach fill rate multiplier (can be reduced by Titanium Stomach upgrade)")]
    [SerializeField] private float stomachFillMultiplier = 1.0f;
    [ReadOnly] [Tooltip("Total battery capacity increase from upgrades")]
    [SerializeField] private float upgradeBatteryBonus = 0f;
    [ReadOnly] [Tooltip("Total stomach fill rate reduction from upgrades")]
    [SerializeField] private float upgradeStomachReduction = 0f;

    [Header("House 4 Insane Mode Multipliers")]
    [Tooltip("Stomach fill multiplier in House 4")]
    [SerializeField] private float house4StomachMultiplier = 2.0f;
    [Tooltip("Battery drain multiplier in House 4")]
    [SerializeField] private float house4BatteryMultiplier = 1.5f;

    #endregion

    #region Public Properties

    public float CurrentBattery => currentBattery;
    public float CurrentStomach => currentStomach;
    public float MaxBattery => maxBattery;
    public float StomachFillMultiplier => stomachFillMultiplier;
    public bool IsHouse4Active => isHouse4Active;

    #endregion

    #region Events

    public static Action OnBatteryDrained;
    public static Action OnStomachFull;
    public static Action<float, float> OnMetersChanged;
    public static Action<float, float> OnBatteryModified;
    public static Action<float, float> OnStomachModified;

    #endregion

    #region Private Fields

    private bool isHouse4Active = false;
    private bool _suppressEvents = false;

    #endregion

    #region Unity Lifecycle

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

    #region Public API - Meter Modification

    /// <summary>
    /// Modifies battery by amount. Clamps to [0, maxBattery].
    /// Fires OnBatteryModified with (newValue, delta) for UI feedback.
    /// Fires OnBatteryDrained if battery reaches 0.
    /// </summary>
    public void ModifyBattery(float amount)
    {
        if (Mathf.Abs(amount) < 0.01f) return;

        // Apply House 4 multiplier if active
        float modifiedAmount = amount;
        if (isHouse4Active && amount < 0)
        {
            modifiedAmount *= house4BatteryMultiplier;
        }

        float previous = currentBattery;
        currentBattery = Mathf.Clamp(currentBattery + modifiedAmount, 0f, maxBattery);
        float delta = currentBattery - previous;

#if UNITY_EDITOR
        Debug.Log($"[Meter] Battery: {previous:F0} → {currentBattery:F0} ({(modifiedAmount > 0 ? "+" : "")}{modifiedAmount:F0})");
#endif

        if (currentBattery <= 0f && previous > 0f)
            OnBatteryDrained?.Invoke();

        OnMetersChanged?.Invoke(currentBattery, currentStomach);

        if (!_suppressEvents)
            OnBatteryModified?.Invoke(currentBattery, delta);
    }

    /// <summary>
    /// Modifies stomach by amount. Clamps to [0, 100].
    /// Applies stomach fill multiplier, outfit stomach resistance, and House 4 multiplier.
    /// Fires OnStomachModified with (newValue, delta) for UI feedback.
    /// Fires OnStomachFull if stomach reached 100.
    /// </summary>
    public void ModifyStomach(float amount)
    {
        if (Mathf.Abs(amount) < 0.01f) return;

        // Apply stomach fill rate multiplier (from upgrades)
        float modifiedAmount = amount;
        if (amount > 0)
        {
            modifiedAmount *= stomachFillMultiplier;
        }

        // Apply outfit stomach resistance (negative value = reduction)
        if (outfitStomachResist < 0 && modifiedAmount > 0)
        {
            float resistance = Mathf.Abs(outfitStomachResist) / 100f;
            resistance = Mathf.Clamp(resistance, 0f, 0.99f); // Prevent division inversion
            modifiedAmount *= (1f - resistance);
        }

        // Apply House 4 multiplier
        if (isHouse4Active && modifiedAmount > 0)
        {
            modifiedAmount *= house4StomachMultiplier;
        }

        float previous = currentStomach;
        currentStomach = Mathf.Clamp(currentStomach + modifiedAmount, 0f, 100f);
        float delta = currentStomach - previous;

        Debug.Log($"[Meter] Stomach: {previous:F0} → {currentStomach:F0} ({(modifiedAmount > 0 ? "+" : "")}{modifiedAmount:F0})");

        if (currentStomach >= 100f && previous < 100f)
            OnStomachFull?.Invoke();

        OnMetersChanged?.Invoke(currentBattery, currentStomach);

        if (!_suppressEvents)
            OnStomachModified?.Invoke(currentStomach, delta);
    }

    #endregion

    #region Run Lifecycle

    /// <summary>
    /// Resets all meters at start of new run.
    /// Applies outfit bonuses before resetting.
    /// </summary>
    public void ResetMeters()
    {
        // Reset upgrade modifiers first
        ResetUpgradeModifiers();

        _suppressEvents = true;
        currentBattery = Mathf.Clamp(100f + outfitBatteryBonus, 0f, maxBattery);
        currentStomach = 0f;
        isHouse4Active = false;
        OnMetersChanged?.Invoke(currentBattery, currentStomach);
        _suppressEvents = false;

        Debug.Log($"[MeterManager] Run started! Battery: {currentBattery:F0}/{maxBattery:F0}, Stomach: 0");
    }

    /// <summary>
    /// Resets upgrade modifiers at start of new run.
    /// Called by ResetMeters() to ensure clean state.
    /// </summary>
    public void ResetUpgradeModifiers()
    {
        maxBattery = 100f;
        stomachFillMultiplier = 1.0f;
        upgradeBatteryBonus = 0f;
        upgradeStomachReduction = 0f;
    }

    /// <summary>
    /// Increases max battery capacity (called by UnifiedHubManager).
    /// Also heals the same amount to feel rewarding.
    /// </summary>
    public void IncreaseMaxBattery(float amount)
    {
        maxBattery = Mathf.Min(maxBattery + amount, 200f); // Cap at 200
        upgradeBatteryBonus += amount;
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery); // Heal too!
        OnMetersChanged?.Invoke(currentBattery, currentStomach);
        Debug.Log($"[Meter] Max Battery increased to {maxBattery:F0} (Bonus: +{upgradeBatteryBonus:F0})");
    }

    /// <summary>
    /// Reduces stomach fill rate multiplier (called by UnifiedHubManager).
    /// </summary>
    public void ReduceStomachFillRate(float reduction)
    {
        stomachFillMultiplier = Mathf.Max(stomachFillMultiplier - reduction, 0.1f); // Min 0.1
        upgradeStomachReduction += reduction;
        Debug.Log($"[Meter] Stomach Fill Rate Multiplier: {stomachFillMultiplier:F2} (Reduction: -{upgradeStomachReduction:P1})");
    }

    /// <summary>
    /// Resets house-specific state (no strike counters anymore).
    /// </summary>
    public void ResetHouseCounters()
    {
        Debug.Log("[MeterManager] House counters reset.");
    }

    /// <summary>
    /// Enables House 4 insane mode multipliers.
    /// </summary>
    public void EnableHouse4Mode()
    {
        isHouse4Active = true;
        Debug.Log("[MeterManager] HOUSE 4 INSANE MODE ACTIVATED!");
    }

    #endregion

    #region Inspector Test Buttons

    [Button("Battery: +25")]
    private void TestAddBattery() => ModifyBattery(25f);

    [Button("Battery: -25")]
    private void TestSubtractBattery() => ModifyBattery(-25f);

    [Button("⚠ Drain Battery")]
    private void TestDrain() { currentBattery = 0f; OnBatteryDrained?.Invoke(); OnMetersChanged?.Invoke(currentBattery, currentStomach); }

    [Button("Stomach: +25")]
    private void TestAddStomach() => ModifyStomach(25f);

    [Button("Stomach: -25")]
    private void TestSubtractStomach() => ModifyStomach(-25f);

    [Button("⚠ Fill Stomach")]
    private void TestFill() { currentStomach = 100f; OnStomachFull?.Invoke(); OnMetersChanged?.Invoke(currentBattery, currentStomach); }

    [Button("Reset")]
    private void TestReset() => ResetMeters();

    [Button("Enable House 4 Mode")]
    private void TestHouse4() => EnableHouse4Mode();

    #endregion
}
