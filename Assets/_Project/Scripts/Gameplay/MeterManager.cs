using System;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Manages Social Battery and Stomach meters, plus the "Three-Strike" Hospitality system.
/// Tracks accepted offers per house and fires events for strike-based multiplier application.
/// </summary>
public class MeterManager : MonoBehaviour
{
    public static MeterManager Instance { get; private set; }

    #region Inspector Fields - Tunable Values

    [Header("Meter Values")]
    [Range(0f, 100f), SerializeField] private float currentBattery = 100f;
    [Range(0f, 100f), SerializeField] private float currentStomach = 0f;

    [Header("Outfit Stat Bonuses (Applied at Runtime)")]
    [Tooltip("Starting battery bonus from outfit (e.g. +10 = start with 110%)")]
    [ReadOnly] [SerializeField] private float outfitBatteryBonus = 0f;
    [Tooltip("Stomach fill rate reduction from outfit (e.g. -10 = 10% less stomach fill)")]
    [ReadOnly] [SerializeField] private float outfitStomachResist = 0f;

    [Header("Hospitality Strike Multipliers - Eidia")]
    [Tooltip("Eidia multiplier for 1st accepted offer (polite)")]
    [SerializeField] private float firstStrikeEidiaMultiplier = 1.0f;
    [Tooltip("Eidia multiplier for 2nd accepted offer (pushing it)")]
    [SerializeField] private float secondStrikeEidiaMultiplier = 1.0f;
    [Tooltip("Eidia multiplier for 3rd accepted offer (exhausted - NO REWARD)")]
    [SerializeField] private float thirdStrikeEidiaMultiplier = 0f;

    [Header("Hospitality Strike Multipliers - Stomach")]
    [Tooltip("Stomach fill multiplier for 1st accepted offer")]
    [SerializeField] private float firstStrikeStomachMultiplier = 1.0f;
    [Tooltip("Stomach fill multiplier for 2nd accepted offer")]
    [SerializeField] private float secondStrikeStomachMultiplier = 1.5f;
    [Tooltip("Stomach fill multiplier for 3rd accepted offer")]
    [SerializeField] private float thirdStrikeStomachMultiplier = 3.0f;

    [Header("Hospitality Strike Multipliers - Battery Drain")]
    [Tooltip("Battery drain for 1st accepted offer (minimum drain for realism)")]
    [SerializeField] private float firstStrikeBatteryDrain = 5f;
    [Tooltip("Battery drain for 2nd accepted offer")]
    [SerializeField] private float secondStrikeBatteryDrain = 10f;
    [Tooltip("Battery drain for 3rd accepted offer")]
    [SerializeField] private float thirdStrikeBatteryDrain = 25f;

    [Header("House 4 Boss Mode Multipliers")]
    [Tooltip("Stomach fill multiplier in House 4 (insane mode)")]
    [SerializeField] private float house4StomachMultiplier = 2.0f;
    [Tooltip("Battery drain multiplier in House 4 (insane mode)")]
    [SerializeField] private float house4BatteryDrainMultiplier = 1.5f;

    #endregion

    #region Public Properties

    public float CurrentBattery => currentBattery;
    public float CurrentStomach => currentStomach;
    public int AcceptedOffersThisHouse => acceptedOffersThisHouse;
    public bool IsHouse4Active => isHouse4Active;

    #endregion

    #region Events

    public static Action OnBatteryDrained;
    public static Action OnStomachFull;
    public static Action<float, float> OnMetersChanged;
    public static Action<float, float> OnBatteryModified;
    public static Action<float, float> OnStomachModified;
    
    /// <summary>
    /// Fires when player accepts a hospitality offer.
    /// Passes the strike level (0=First, 1=Second, 2=Third) for multiplier application.
    /// </summary>
    public static Action<HospitalityStrike> OnOfferAccepted;

    #endregion

    #region Private Fields

    private int acceptedOffersThisHouse = 0;
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
    /// Modifies battery by amount. Clamps to [0, 100].
    /// Fires OnBatteryModified with (newValue, delta) for UI feedback.
    /// Fires OnBatteryDrained if battery reaches 0.
    /// </summary>
    public void ModifyBattery(float amount)
    {
        if (Mathf.Abs(amount) < 0.01f) return;

        float previous = currentBattery;
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, 100f);
        float delta = currentBattery - previous;

        Debug.Log($"[Meter] Battery: {previous:F0} → {currentBattery:F0} ({(amount > 0 ? "+" : "")}{amount:F0})");

        if (currentBattery <= 0f && previous > 0f)
            OnBatteryDrained?.Invoke();

        OnMetersChanged?.Invoke(currentBattery, currentStomach);

        if (!_suppressEvents)
            OnBatteryModified?.Invoke(currentBattery, delta);
    }

    /// <summary>
    /// Modifies stomach by amount. Clamps to [0, 100].
    /// Applies outfit stomach resistance bonus.
    /// Fires OnStomachModified with (newValue, delta) for UI feedback.
    /// Fires OnStomachFull if stomach reaches 100.
    /// </summary>
    public void ModifyStomach(float amount)
    {
        if (Mathf.Abs(amount) < 0.01f) return;

        // Apply outfit stomach resistance (negative value = reduction)
        float modifiedAmount = amount;
        if (outfitStomachResist < 0 && amount > 0)
        {
            // Reduce stomach fill by outfit bonus percentage
            float resistance = Mathf.Abs(outfitStomachResist) / 100f;
            modifiedAmount = amount * (1f - resistance);
            Debug.Log($"[Meter] Stomach resistance applied: {amount:F0} → {modifiedAmount:F0} ({outfitStomachResist}% resist)");
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

    /// <summary>
    /// Applies battery drain with House 4 multiplier if active.
    /// </summary>
    public void ApplyBatteryDrain(float baseDrain)
    {
        float finalDrain = isHouse4Active ? baseDrain * house4BatteryDrainMultiplier : baseDrain;
        ModifyBattery(-finalDrain);
    }

    /// <summary>
    /// Applies stomach fill with House 4 multiplier if active.
    /// </summary>
    public void ApplyStomachFill(float baseFill)
    {
        float finalFill = isHouse4Active ? baseFill * house4StomachMultiplier : baseFill;
        ModifyStomach(finalFill);
    }

    #endregion

    #region Hospitality Strike System

    /// <summary>
    /// Call this when player accepts a hospitality offer.
    /// Increments strike counter and fires OnOfferAccepted event.
    /// GameManager listens to this event and applies multipliers.
    /// </summary>
    public void RegisterAcceptedOffer()
    {
        if (acceptedOffersThisHouse >= 3)
        {
            Debug.LogWarning("[MeterManager] Already at 3 strikes! Ignoring additional offer.");
            return;
        }

        acceptedOffersThisHouse++;
        HospitalityStrike strike = (HospitalityStrike)(acceptedOffersThisHouse - 1);
        
        Debug.Log($"[MeterManager] Hospitality Offer Accepted! Strike: {strike} ({acceptedOffersThisHouse}/3)");
        OnOfferAccepted?.Invoke(strike);
    }

    /// <summary>
    /// Gets the Eidia multiplier for the current strike level.
    /// </summary>
    public float GetEidiaMultiplier(HospitalityStrike strike)
    {
        return strike switch
        {
            HospitalityStrike.First => firstStrikeEidiaMultiplier,
            HospitalityStrike.Second => secondStrikeEidiaMultiplier,
            HospitalityStrike.Third => thirdStrikeEidiaMultiplier,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Gets the Stomach multiplier for the current strike level.
    /// </summary>
    public float GetStomachMultiplier(HospitalityStrike strike)
    {
        return strike switch
        {
            HospitalityStrike.First => firstStrikeStomachMultiplier,
            HospitalityStrike.Second => secondStrikeStomachMultiplier,
            HospitalityStrike.Third => thirdStrikeStomachMultiplier,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Gets the Battery drain for the current strike level.
    /// </summary>
    public float GetBatteryDrain(HospitalityStrike strike)
    {
        return strike switch
        {
            HospitalityStrike.First => firstStrikeBatteryDrain,
            HospitalityStrike.Second => secondStrikeBatteryDrain,
            HospitalityStrike.Third => thirdStrikeBatteryDrain,
            _ => 0f
        };
    }

    #endregion

    #region Run Lifecycle

    /// <summary>
    /// Resets all meters and strike counter at start of new run.
    /// Applies outfit bonuses before resetting.
    /// </summary>
    public void ResetMeters()
    {
        // Apply outfit bonuses first
        ApplyOutfitBonuses();

        _suppressEvents = true;
        currentBattery = Mathf.Clamp(100f + outfitBatteryBonus, 0f, 100f); // Cap at 100
        currentStomach = 0f;
        acceptedOffersThisHouse = 0;
        isHouse4Active = false;
        OnMetersChanged?.Invoke(currentBattery, currentStomach);
        _suppressEvents = false;

        Debug.Log($"[MeterManager] Run started! Meters reset. Battery: {currentBattery:F0} (base: 100 + outfit: {outfitBatteryBonus}), Stomach: 0, Strikes: 0");
    }

    /// <summary>
    /// Applies outfit stat bonuses from WardrobeManager. Call at start of run.
    /// </summary>
    public void ApplyOutfitBonuses()
    {
        if (WardrobeManager.Instance == null) return;

        var (statType, value) = WardrobeManager.Instance.GetEquippedStatBonus();

        switch (statType)
        {
            case OutfitStatType.BatteryStart:
                outfitBatteryBonus = value;
                break;
            case OutfitStatType.StomachResist:
                outfitStomachResist = value;
                break;
        }
    }

    /// <summary>
    /// Resets strike counter at start of each new house.
    /// Called by GameManager.StartHouse().
    /// </summary>
    public void ResetHouseCounters()
    {
        acceptedOffersThisHouse = 0;
        Debug.Log("[MeterManager] House counters reset. Accepted offers: 0");
    }

    /// <summary>
    /// Enables House 4 boss mode multipliers.
    /// Called by GameManager when player chooses "Risk House 4".
    /// </summary>
    public void EnableHouse4Mode()
    {
        isHouse4Active = true;
        Debug.Log("[MeterManager] HOUSE 4 BOSS MODE ACTIVATED! All penalties doubled.");
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

    [Button("Test Strike: First")]
    private void TestStrike1() => RegisterAcceptedOffer();

    [Button("Test Strike: Second")]
    private void TestStrike2() { acceptedOffersThisHouse = 1; RegisterAcceptedOffer(); }

    [Button("Test Strike: Third")]
    private void TestStrike3() { acceptedOffersThisHouse = 2; RegisterAcceptedOffer(); }

    [Button("☠️ Enable House 4 Mode")]
    private void TestHouse4() => EnableHouse4Mode();

    #endregion
}
