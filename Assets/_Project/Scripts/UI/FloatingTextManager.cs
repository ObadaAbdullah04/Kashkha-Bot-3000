using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// PHASE 4: Highly performant floating text manager with object pooling.
/// Spawns floating feedback text when meters change or Eidia is earned.
/// 
/// PERFORMANCE FEATURES:
/// - Object pooling (no runtime Instantiate/Destroy)
/// - CanvasGroup alpha fading (more efficient than textMesh.alpha)
/// - DOTween recycling enabled
/// - Minimal GC allocations
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    #region Singleton

    public static FloatingTextManager Instance { get; private set; }

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

    [Header("Pool Settings")]
    [Tooltip("Prefab to pool (must have FloatingText component and CanvasGroup)")]
    [SerializeField] private FloatingText prefab;

    [Tooltip("Number of text objects to pre-spawn in pool")]
    [SerializeField] private int poolSize = 20; // Increased for hackathon

    [Tooltip("Parent transform for pooled objects (Canvas)")]
    [SerializeField] private Transform canvasParent;

    [Header("Spawn Positions")]
    [Tooltip("Spawn position for battery change text (relative to parent)")]
    [SerializeField] private Vector3 batteryOffset = new Vector3(-250, -100, 0);

    [Tooltip("Spawn position for stomach change text (relative to parent)")]
    [SerializeField] private Vector3 stomachOffset = new Vector3(250, -100, 0);

    [Tooltip("Spawn position for Eidia/scrap reward text")]
    [SerializeField] private Vector3 rewardOffset = new Vector3(0, 200, 0);

    [Header("Colors")]
    [SerializeField] private Color batteryGainColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan
    [SerializeField] private Color batteryLossColor = new Color(1f, 0.3f, 0.3f, 1f); // Red
    [SerializeField] private Color stomachGainColor = new Color(1f, 0.3f, 0.3f, 1f); // Red (bad)
    [SerializeField] private Color stomachLossColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan (good)
    [SerializeField] private Color eidiaColor = new Color(1f, 0.84f, 0f, 1f); // Gold
    [SerializeField] private Color scrapColor = new Color(0.8f, 0.6f, 0.4f, 1f); // Bronze

    [Header("Animation")]
    [SerializeField] private float floatDistance = 80f;
    [SerializeField] private float animationDuration = 1.5f;

    #endregion

    #region Private Fields

    private List<FloatingText> _pool;
    private Sequence _poolInitSequence;
    private bool _isPoolInitialized;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Initialize pool
        _pool = new List<FloatingText>(poolSize);

        if (prefab == null)
        {
            // Debug.LogError("[FloatingTextManager] Prefab not assigned!");
            return;
        }

        if (canvasParent == null)
        {
            // Try to find the main HUD canvas by tag first, then by name fallback
            GameObject mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
            if (mainCanvas != null)
            {
                canvasParent = mainCanvas.GetComponent<Canvas>()?.transform;
            }

            if (canvasParent == null)
            {
                canvasParent = FindObjectOfType<Canvas>()?.transform;
                // Debug.LogWarning("[FloatingTextManager] canvasParent not assigned and no tagged canvas found. Using arbitrary canvas. Please assign in inspector or tag main canvas as 'MainCanvas'.");
            }
        }

        // Pre-instantiate all pool objects
        for (int i = 0; i < poolSize; i++)
        {
            FloatingText newText = Instantiate(prefab, canvasParent);
            newText.gameObject.SetActive(false);
            _pool.Add(newText);
        }

        _isPoolInitialized = true;
        // Debug.Log($"[FloatingTextManager] Pool initialized with {poolSize} objects.");
    }

    private void OnEnable()
    {
        MeterManager.OnBatteryModified += HandleBatteryModified;
        MeterManager.OnStomachModified += HandleStomachModified;
        GameManager.OnRunStarted += HandleRunStarted;
        MiniGameManager.OnMiniGameEnded += HandleMiniGameRewards;
    }

    private void OnDisable()
    {
        MeterManager.OnBatteryModified -= HandleBatteryModified;
        MeterManager.OnStomachModified -= HandleStomachModified;
        GameManager.OnRunStarted -= HandleRunStarted;
        MiniGameManager.OnMiniGameEnded -= HandleMiniGameRewards;

        // Kill all active tweens and return objects to pool state
        if (_pool != null)
        {
            foreach (var item in _pool)
            {
                if (item != null)
                {
                    item.KillTween();
                    item.gameObject.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region Event Handlers

    private void HandleBatteryModified(float value, float delta)
    {
        if (!_isPoolInitialized)
        {
            // Debug.LogWarning("[FloatingTextManager] Pool not initialized! Check prefab assignment.");
            return;
        }
        if (Mathf.Abs(delta) < 0.1f) return;

        Color color = delta > 0 ? batteryGainColor : batteryLossColor;
        string label = delta > 0 ? "بطارية" : "بطارية";
        SpawnFeedback(delta, batteryOffset, label, color);
    }

    private void HandleStomachModified(float value, float delta)
    {
        if (!_isPoolInitialized)
        {
            // Debug.LogWarning("[FloatingTextManager] Pool not initialized! Check prefab assignment.");
            return;
        }
        if (Mathf.Abs(delta) < 0.1f) return;

        // Invert color logic: +stomach is BAD (red), -stomach is GOOD (cyan)
        Color color = delta > 0 ? stomachGainColor : stomachLossColor;
        string label = delta > 0 ? "معدة" : "معدة";
        SpawnFeedback(delta, stomachOffset, label, color);
    }

    private void HandleRunStarted()
    {
        if (!_isPoolInitialized) return;

        // Reset all pool objects on new run
        foreach (var item in _pool)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                item.KillTween();
                item.gameObject.SetActive(false);
            }
        }
    }

    private void HandleMiniGameRewards(int eidiaEarned, int scrapEarned)
    {
        if (!_isPoolInitialized) return;

        if (eidiaEarned > 0)
            SpawnEidiaReward(eidiaEarned);

        if (scrapEarned > 0)
            SpawnScrapReward(scrapEarned);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Spawns floating text for battery/stomach changes.
    /// </summary>
    public void SpawnFeedback(float delta, Vector3 offset, string label, Color color)
    {
        FloatingText ft = GetFromPool();
        if (ft == null)
        {
            // Debug.LogWarning("[FloatingTextManager] Pool exhausted! Consider increasing poolSize.");
            return;
        }

        // Format text (optimized: no string.Format to reduce GC)
        string sign = delta > 0 ? "+" : "";
        string content = sign + delta.ToString("F0") + " " + label;

        ft.transform.localPosition = offset;
        ft.gameObject.SetActive(true);
        ft.Spawn(content, color, floatDistance, animationDuration);
    }

    /// <summary>
    /// Spawns floating text for Eidia rewards (centered, gold).
    /// </summary>
    public void SpawnEidiaReward(int amount)
    {
        FloatingText ft = GetFromPool();
        if (ft == null) return;

        string content = $"+{amount} دينار";

        ft.transform.localPosition = rewardOffset;
        ft.gameObject.SetActive(true);
        ft.Spawn(content, eidiaColor, floatDistance * 1.2f, animationDuration);
    }

    /// <summary>
    /// Spawns floating text for Scrap (now Eidia) rewards (centered, gold).
    /// </summary>
    public void SpawnScrapReward(int amount)
    {
        FloatingText ft = GetFromPool();
        if (ft == null) return;

        string content = $"+{amount} عيدية";

        ft.transform.localPosition = rewardOffset;
        ft.gameObject.SetActive(true);
        ft.Spawn(content, eidiaColor, floatDistance, animationDuration);
    }

    /// <summary>
    /// Spawns custom floating text at specified position.
    /// </summary>
    public void SpawnCustom(string text, Color color, Vector3 position, float duration = 1.5f)
    {
        FloatingText ft = GetFromPool();
        if (ft == null) return;

        ft.transform.localPosition = position;
        ft.gameObject.SetActive(true);
        ft.Spawn(text, color, floatDistance, duration);
    }

    #endregion

    #region Pool Management

    /// <summary>
    /// Gets the first inactive object from the pool.
    /// Returns null if all objects are in use.
    /// </summary>
    private FloatingText GetFromPool()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeSelf)
            {
                return _pool[i];
            }
        }
        return null;
    }

    #endregion

    #region Inspector Test Buttons

    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/FloatingText/Test Battery Gain")]
    private static void TestBatteryGain()
    {
        if (Instance != null)
            Instance.SpawnFeedback(25, Instance.batteryOffset, "بطارية", Instance.batteryGainColor);
    }

    [UnityEditor.MenuItem("Tools/FloatingText/Test Battery Loss")]
    private static void TestBatteryLoss()
    {
        if (Instance != null)
            Instance.SpawnFeedback(-25, Instance.batteryOffset, "بطارية", Instance.batteryLossColor);
    }

    [UnityEditor.MenuItem("Tools/FloatingText/Test Eidia")]
    private static void TestEidia()
    {
        if (Instance != null)
            Instance.SpawnEidiaReward(50);
    }
    #endif

    #endregion
}
