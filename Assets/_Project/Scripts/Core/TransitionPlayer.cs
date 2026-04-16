using System;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using RTLTMPro;

/// <summary>
/// PHASE 6: Transition scene player - handles transitions between houses.
/// 
/// CURRENT: Simple fade + Arabic text overlay
/// FUTURE: Rich cinematic animations (walking, door opening, etc.)
/// 
/// FLOW:
/// House Hub → Click House 2 → PlayTransition("بيت خالة أم محمد") → Fade in → Text → Fade out → House 2 encounters
/// </summary>
public class TransitionPlayer : MonoBehaviour
{
    public static TransitionPlayer Instance { get; private set; }

    #region Inspector Fields

    [Header("Transition Panel")]
    [Tooltip("Full-screen black panel for fade transitions")]
    [SerializeField] private GameObject transitionPanel;

    [Tooltip("Arabic text shown during transition (e.g., 'السفر إلى بيت خالة أم محمد...')")]
    [SerializeField] private RTLTextMeshPro transitionText;

    [Header("Timing")]
    [Tooltip("Fade in duration (seconds)")]
    [SerializeField] private float fadeInDuration = 0.6f;

    [Tooltip("Text display duration (seconds)")]
    [SerializeField] private float textDuration = 2.5f; // User preferred duration

    [Tooltip("Fade out duration (seconds)")]
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("Visual")]
    [Tooltip("Fade color (usually black)")]
    [SerializeField] private Color fadeColor = Color.black;

    #endregion

    #region Events

    /// <summary>
    /// Fires when the transition animation completes.
    /// </summary>
    public static Action OnTransitionComplete;

    #endregion

    #region Private Fields

    private UnityEngine.UI.Image fadeImage;
    private Sequence _activeSequence;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // PHASE 18: Ensure transition is on top of everything
            // Force add or get Canvas component to manage absolute sorting
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;

            // Add GraphicRaycaster to block input during transitions
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else
        {
            Destroy(gameObject);
        }

        // Setup transition panel
        if (transitionPanel != null)
        {
            // PHASE 18: Move panel to this root to ensure it uses our high-sorting Canvas
            transitionPanel.transform.SetParent(this.transform, false);
            
            // Ensure it fills the screen
            RectTransform rt = transitionPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }

            transitionPanel.SetActive(false);
            fadeImage = transitionPanel.GetComponent<UnityEngine.UI.Image>();
            if (fadeImage == null)
            {
                fadeImage = transitionPanel.AddComponent<UnityEngine.UI.Image>();
            }
            fadeImage.color = fadeColor;
            
            // Ensure alpha is zero at start
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
            }
            if (transitionText != null) transitionText.alpha = 0f;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a transition scene with the given destination text.
    /// PHASE 18 Refined: Two-stage transition (Fade In -> onMidpoint -> Wait -> onReady -> Fade Out)
    /// </summary>
    /// <param name="destinationTextAR">Arabic text shown during transition</param>
    /// <param name="onMidpoint">Callback when screen is fully black (perfect time to update backgrounds!)</param>
    /// <param name="overrideTextDuration">Optional: Override the default text duration (0 = use default)</param>
    /// <param name="onReady">Optional: Callback when text wait is done (perfect time to start gameplay action!)</param>
    public void PlayTransition(string destinationTextAR, Action onMidpoint = null, float overrideTextDuration = 0f, Action onReady = null)
    {
        // Force kill any existing transition WITHOUT completing it (to avoid callback flashes)
        if (_activeSequence != null && _activeSequence.IsActive())
        {
            Debug.Log("[TransitionPlayer] Killing active transition to start new one.");
            _activeSequence.Kill(false); 
        }

        // Set text
        if (transitionText != null)
        {
            transitionText.text = destinationTextAR;
            transitionText.alpha = 0f;
        }

        float duration = overrideTextDuration > 0f ? overrideTextDuration : textDuration;

        if (debugLogging)
            Debug.Log($"[TransitionPlayer] Starting transition: '{destinationTextAR}' | Duration: {duration}s");

        // Start fade in sequence
        PlayFadeSequence(onMidpoint, duration, onReady);
    }

    /// <summary>
    /// Skips the current transition and fires the complete event immediately.
    /// </summary>
    public void SkipTransition()
    {
        if (_activeSequence != null && _activeSequence.IsActive())
        {
            _activeSequence.Kill();
        }

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }
    }

    #endregion

    #region Animation Sequence

    private void PlayFadeSequence(Action midPointAction, float duration, Action readyAction)
    {
        if (transitionPanel == null)
        {
            // No panel - just fire callbacks immediately
            midPointAction?.Invoke();
            readyAction?.Invoke();
            OnTransitionComplete?.Invoke();
            return;
        }

        // Play transition sound
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.Transition);

        // Ensure panel is active
        transitionPanel.SetActive(true);

        // Create fade sequence
        _activeSequence = DOTween.Sequence();

        // Step 1: Fade in (black screen appears)
        if (fadeImage != null)
        {
            _activeSequence.Append(fadeImage.DOFade(1f, fadeInDuration).SetUpdate(true));
        }
        else
        {
            _activeSequence.AppendInterval(fadeInDuration);
        }

        // Step 2: Show text
        if (transitionText != null)
        {
            _activeSequence.Join(transitionText.DOFade(1f, 0.4f).SetUpdate(true));
        }

        // Step 3: MID-POINT CALLBACK (Screen is black here!)
        _activeSequence.AppendCallback(() => 
        {
            if (debugLogging) Debug.Log("[TransitionPlayer] Mid-point reached. Firing update callback.");
            midPointAction?.Invoke();
        });

        // Step 4: Wait for text duration (Increase for readability)
        _activeSequence.AppendInterval(duration);

        // Step 5: READY CALLBACK (Wait is over, about to fade out)
        _activeSequence.AppendCallback(() =>
        {
            if (debugLogging) Debug.Log("[TransitionPlayer] Wait complete. Firing ready callback.");
            readyAction?.Invoke();
        });

        // Step 6: Fade text out
        if (transitionText != null)
        {
            _activeSequence.Append(transitionText.DOFade(0f, 0.4f).SetUpdate(true));
        }
        else
        {
            _activeSequence.AppendInterval(0.4f);
        }

        // Step 7: Fade out (black screen disappears)
        if (fadeImage != null)
        {
            _activeSequence.Join(fadeImage.DOFade(0f, fadeOutDuration).SetUpdate(true));
        }
        else
        {
            _activeSequence.AppendInterval(fadeOutDuration);
        }

        // Step 8: Complete
        _activeSequence.OnComplete(() =>
        {
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(false);
            }

            OnTransitionComplete?.Invoke();

            if (debugLogging) Debug.Log("[TransitionPlayer] Transition sequence complete!");
        });
        
        _activeSequence.SetUpdate(true); // Ensure it works even if time is paused
    }

    #endregion

    #region Debug

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    #endregion

    #region Inspector Test Buttons

    [Button("Test Transition: House 1 to 2")]
    private void TestTransition1to2()
    {
        PlayTransition("السفر إلى بيت خالة أم محمد...", () =>
        {
            Debug.Log("[TransitionPlayer] Callback: Ready for House 2!");
        });
    }

    [Button("Test Transition: House 2 to 3")]
    private void TestTransition2to3()
    {
        PlayTransition("الذهاب إلى بيت جدو الحاج...", () =>
        {
            Debug.Log("[TransitionPlayer] Callback: Ready for House 3!");
        });
    }

    [Button("Skip Transition")]
    private void TestSkip() => SkipTransition();

    #endregion
}
