using System;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using RTLTMPro;

/// <summary>
/// PHASE 6: Transition scene player - handles transitions between houses.
/// 
/// CURRENT: Simple fade + Arabic text overlay
/// FUTURE: Timeline-based travel animations (walking, door opening, etc.)
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
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Text display duration (seconds)")]
    [SerializeField] private float textDuration = 1.5f;

    [Tooltip("Fade out duration (seconds)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

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
    private bool isTransitionPlaying = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Setup transition panel
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
            fadeImage = transitionPanel.GetComponent<UnityEngine.UI.Image>();
            if (fadeImage == null)
            {
                fadeImage = transitionPanel.AddComponent<UnityEngine.UI.Image>();
            }
            fadeImage.color = fadeColor;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a transition scene with the given destination text.
    /// </summary>
    /// <param name="destinationTextAR">Arabic text shown during transition</param>
    /// <param name="onComplete">Callback when transition finishes</param>
    public void PlayTransition(string destinationTextAR, Action onComplete = null)
    {
        if (isTransitionPlaying)
        {
            Debug.LogWarning("[TransitionPlayer] Transition already playing!");
            return;
        }

        isTransitionPlaying = true;

        // Wire completion callback
        OnTransitionComplete += onComplete;

#if UNITY_EDITOR
        Debug.Log($"[TransitionPlayer] Playing transition: {destinationTextAR}");
#endif

        // Set text
        if (transitionText != null)
        {
            transitionText.text = destinationTextAR;
            transitionText.alpha = 0f;
        }

        // Start fade in sequence
        PlayFadeSequence();
    }

    /// <summary>
    /// Skips the current transition and fires the complete event immediately.
    /// </summary>
    public void SkipTransition()
    {
        if (!isTransitionPlaying) return;

        DOTween.Kill(this);

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }

        isTransitionPlaying = false;
        OnTransitionComplete?.Invoke();
        OnTransitionComplete = null; // Clear single-use callback
    }

    #endregion

    #region Animation Sequence

    private void PlayFadeSequence()
    {
        if (transitionPanel == null)
        {
            // No panel - just fire callback immediately
            isTransitionPlaying = false;
            OnTransitionComplete?.Invoke();
            OnTransitionComplete = null;
            return;
        }

        // Ensure panel is active
        transitionPanel.SetActive(true);

        // Create fade sequence
        Sequence seq = DOTween.Sequence();

        // Step 1: Fade in (black screen appears)
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            seq.Append(fadeImage.DOColor(fadeColor, fadeInDuration));
        }
        else
        {
            seq.AppendInterval(fadeInDuration);
        }

        // Step 2: Show text
        if (transitionText != null)
        {
            seq.Join(transitionText.DOFade(1f, 0.3f));
        }

        // Step 3: Wait for text duration
        seq.AppendInterval(textDuration);

        // Step 4: Fade text out
        if (transitionText != null)
        {
            seq.Append(transitionText.DOFade(0f, 0.3f));
        }
        else
        {
            seq.AppendInterval(0.3f);
        }

        // Step 5: Fade out (black screen disappears)
        if (fadeImage != null)
        {
            seq.Join(fadeImage.DOColor(new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f), fadeOutDuration));
        }
        else
        {
            seq.AppendInterval(fadeOutDuration);
        }

        // Step 6: Complete
        seq.OnComplete(() =>
        {
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(false);
            }

            isTransitionPlaying = false;
            OnTransitionComplete?.Invoke();
            OnTransitionComplete = null; // Clear single-use callback

#if UNITY_EDITOR
            Debug.Log("[TransitionPlayer] Transition complete!");
#endif
        });
    }

    #endregion

    #region Inspector Test Buttons

    [Button("▶ Test Transition: House 1 → 2")]
    private void TestTransition1to2()
    {
        PlayTransition("السفر إلى بيت خالة أم محمد...", () =>
        {
            Debug.Log("[TransitionPlayer] Callback: Ready for House 2!");
        });
    }

    [Button("▶ Test Transition: House 2 → 3")]
    private void TestTransition2to3()
    {
        PlayTransition("الذهاب إلى بيت جدو الحاج...", () =>
        {
            Debug.Log("[TransitionPlayer] Callback: Ready for House 3!");
        });
    }

    [Button("⏹ Skip Transition")]
    private void TestSkip() => SkipTransition();

    #endregion
}
