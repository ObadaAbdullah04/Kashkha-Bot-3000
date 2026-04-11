using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 15: Unified cinematic controller — supports both Unity Timeline and DOTween modes.
///
/// UNITY TIMELINE MODE:
/// - Plays Timeline assets from Resources/Timelines/
/// - Use for complex animations: character movement, camera pans, multi-object choreography
/// - Create in Unity: Window → Sequencing → Timeline
///
/// DOTWEEN MODE:
/// - Plays DOTween-based text animations
/// - Use for simple text reveals, dialogue, narration
/// - No asset creation needed — just configure TextAR and Duration
///
/// HOW IT WORKS:
/// 1. HouseFlowController calls PlayCinematic(cinematicData, onComplete)
/// 2. CinematicController checks Type (UnityTimeline or DOTween)
/// 3. Plays appropriate animation system
/// 4. Calls onComplete when finished
///
/// SETUP IN UNITY:
/// - Assign PlayableDirector component in inspector
/// - Assign UI references (cutscenePanel, cutsceneText) for DOTween mode
/// - Place Timeline assets in Resources/Timelines/ folder
/// </summary>
public class CinematicController : MonoBehaviour
{
    public static CinematicController Instance { get; private set; }

    #region Inspector Fields

    [Header("Timeline Player")]
    [Tooltip("PlayableDirector component for Unity Timeline playback")]
    [SerializeField] private PlayableDirector director;

    [Header("DOTween UI")]
    [Tooltip("Main cutscene panel/background (used for BOTH Timeline and DOTween modes)")]
    [SerializeField] private GameObject cutscenePanel;

    [Tooltip("Text display for DOTween cinematics (only used in DOTween mode)")]
    [SerializeField] private RTLTextMeshPro cutsceneText;

    [Header("Gameplay UI References (hidden during cinematics)")]
    [Tooltip("Swipe encounter panel - hidden during cinematics to prevent parallel UI")]
    [SerializeField] private GameObject swipeEncounterPanel;

    [Tooltip("Interaction HUD panel - hidden during cinematics")]
    [SerializeField] private GameObject interactionHUDPanel;

    [Tooltip("Timer slider - hidden during cinematics")]
    [SerializeField] private UnityEngine.UI.Slider timerSlider;

    [Header("Timeline Loading")]
    [Tooltip("Resources path for timeline assets (relative to any Resources/ folder)")]
    [SerializeField] private string timelinesResourcesPath = "Timelines";

    [Header("Fallback Behavior")]
    [Tooltip("If Timeline not found, automatically fallback to DOTween mode")]
    [SerializeField] private bool fallbackToDOTween = true;

    [Header("DOTween Settings")]
    [Tooltip("Text reveal speed (characters per second) for typewriter animation")]
    [SerializeField] private float typewriterSpeed = 20f;

    [Header("Visual Feedback")]
    [Tooltip("Color for normal text")]
    [SerializeField] private Color normalTextColor = Color.white;

    [Tooltip("Color for emphasized text")]
    [SerializeField] private Color emphasizedTextColor = new Color(1f, 0.85f, 0f);

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    #endregion

    #region State

    private Dictionary<string, TimelineAsset> timelineCache = new Dictionary<string, TimelineAsset>();
    private Action<string> onCompleteCallback;
    private bool isPlaying = false;
    private string currentCinematicID;
    private Coroutine playbackCoroutine;
    
    // Track gameplay UI state to restore after cinematic
    private bool wasSwipePanelActive = false;
    private bool wasInteractionPanelActive = false;
    private bool wasTimerActive = false;

    #endregion

    #region Events

    /// <summary>
    /// Fires when a cinematic starts playing. (cinematicID)
    /// </summary>
    public static Action<string> OnCinematicStarted;

    /// <summary>
    /// Fires when a cinematic completes. (cinematicID)
    /// </summary>
    public static Action<string> OnCinematicCompleted;

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
            Debug.LogError("[CinematicController] Duplicate instance! Destroying.");
            Destroy(gameObject);
            return;
        }

        // Validate director (required for UnityTimeline mode)
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }

        // Ensure UI is hidden on start
        if (cutscenePanel != null)
            cutscenePanel.SetActive(false);

        // Preload timelines from Resources
        PreloadTimelinesFromResources();
    }

    private void OnDestroy()
    {
        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a cinematic. Supports both Unity Timeline and DOTween modes.
    /// Called by HouseFlowController when a Cinematic element is encountered.
    /// 
    /// SMART FALLBACK: If Timeline mode is selected but asset not found,
    /// automatically falls back to DOTween mode (if available).
    /// </summary>
    /// <param name="cinematicData">Cinematic configuration from CinematicData</param>
    /// <param name="onComplete">Callback when cinematic finishes</param>
    public void PlayCinematic(CinematicData cinematicData, Action<string> onComplete)
    {
        if (isPlaying)
        {
            Debug.LogWarning($"[CinematicController] Cinematic already playing: {currentCinematicID}. Ignoring PlayCinematic({cinematicData.ID})");
            return;
        }

        currentCinematicID = cinematicData.ID;
        onCompleteCallback = onComplete;
        isPlaying = true;

        // HIDE all gameplay UI to prevent parallel execution
        HideGameplayUI();

        if (debugLogging)
            Debug.Log($"[CinematicController] Playing cinematic: {cinematicData.ID} | Type: {cinematicData.Type}");

        OnCinematicStarted?.Invoke(cinematicData.ID);

        // Start appropriate playback mode
        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        // SMART FALLBACK: Try requested mode, fallback to DOTween if Timeline fails
        CinematicType actualType = cinematicData.Type;
        
        if (cinematicData.Type == CinematicType.UnityTimeline && !TimelineExists(cinematicData.TimelineAssetName))
        {
            if (fallbackToDOTween && !string.IsNullOrEmpty(cinematicData.TextAR))
            {
                Debug.LogWarning($"[CinematicController] Timeline '{cinematicData.TimelineAssetName}' not found. Falling back to DOTween mode.");
                actualType = CinematicType.DOTween;
            }
            else if (!fallbackToDOTween)
            {
                Debug.LogError($"[CinematicController] Timeline '{cinematicData.TimelineAssetName}' not found and fallback disabled!");
                isPlaying = false;
                ShowGameplayUI(); // Restore UI
                onComplete?.Invoke(cinematicData.ID);
                return;
            }
        }

        switch (actualType)
        {
            case CinematicType.UnityTimeline:
                playbackCoroutine = StartCoroutine(PlayTimelineMode(cinematicData));
                break;

            case CinematicType.DOTween:
                playbackCoroutine = StartCoroutine(PlayDOTweenMode(cinematicData));
                break;

            default:
                Debug.LogError($"[CinematicController] Unknown cinematic type: {cinematicData.Type}");
                isPlaying = false;
                ShowGameplayUI(); // Restore UI
                onComplete?.Invoke(cinematicData.ID);
                break;
        }
    }

    /// <summary>
    /// Checks if a Timeline asset exists in the cache.
    /// </summary>
    private bool TimelineExists(string timelineName)
    {
        if (string.IsNullOrEmpty(timelineName)) return false;
        return timelineCache.ContainsKey(timelineName);
    }

    /// <summary>
    /// Cancels the currently playing cinematic.
    /// </summary>
    public void CancelActiveCinematic()
    {
        if (!isPlaying) return;

        if (debugLogging)
            Debug.Log("[CinematicController] Cinematic cancelled externally.");

        isPlaying = false;
        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        if (director != null && director.state == PlayState.Playing)
            director.Stop();

        onCompleteCallback?.Invoke(currentCinematicID);
        OnCinematicCompleted?.Invoke(currentCinematicID);

        HideCutsceneUI();
        ShowGameplayUI(); // Restore gameplay UI
        
        currentCinematicID = null;
        onCompleteCallback = null;
    }

    #endregion

    #region Unity Timeline Mode

    /// <summary>
    /// Plays a Unity Timeline asset.
    /// </summary>
    private IEnumerator PlayTimelineMode(CinematicData cinematicData)
    {
        if (director == null)
        {
            Debug.LogError("[CinematicController] PlayableDirector not assigned! Cannot play Unity Timeline.");
            isPlaying = false;
            ShowGameplayUI(); // Restore gameplay UI
            onCompleteCallback?.Invoke(cinematicData.ID);
            yield break;
        }

        // Find timeline in cache
        if (!timelineCache.ContainsKey(cinematicData.TimelineAssetName))
        {
            Debug.LogError($"[CinematicController] Timeline not found: {cinematicData.TimelineAssetName}");
            isPlaying = false;
            ShowGameplayUI(); // Restore gameplay UI
            onCompleteCallback?.Invoke(cinematicData.ID);
            yield break;
        }

        TimelineAsset timeline = timelineCache[cinematicData.TimelineAssetName];
        if (timeline == null)
        {
            Debug.LogError($"[CinematicController] Timeline asset is null: {cinematicData.TimelineAssetName}");
            isPlaying = false;
            ShowGameplayUI(); // Restore gameplay UI
            onCompleteCallback?.Invoke(cinematicData.ID);
            yield break;
        }

        // For Timeline mode: ONLY show cutscene panel if there's text to display
        // (Timelines usually have their own UI, so we only show panel for fallback text)
        bool shouldShowPanelForTimeline = !string.IsNullOrEmpty(cinematicData.TextAR);
        
        if (cutscenePanel != null && shouldShowPanelForTimeline)
        {
            ShowCutsceneUI();
            if (cutsceneText != null)
            {
                cutsceneText.text = cinematicData.TextAR;
                cutsceneText.color = normalTextColor;
            }
        }
        else if (cutscenePanel != null && !shouldShowPanelForTimeline)
        {
            // TIMELINE MODE: Immediately hide cutscene panel - Timeline has its own visuals
            cutscenePanel.SetActive(false);
            if (debugLogging)
                Debug.Log("[CinematicController] Timeline mode - cutscene panel hidden (no text to display)");
        }

        // CORRECT timeline playback sequence
        director.playableAsset = timeline;
        director.RebuildGraph();
        director.Play();

        // Get timeline duration for safety timeout
        double timelineDuration = timeline.duration;
        float safetyTimeout = Mathf.Max((float)timelineDuration + 2f, 5f); // Add 2s buffer, minimum 5s
        float elapsed = 0f;

        if (debugLogging)
            Debug.Log($"[CinematicController] Timeline started: {cinematicData.TimelineAssetName} | Duration: {timelineDuration:F2}s | Timeout: {safetyTimeout:F2}s | State: {director.state}");

        // Wait for timeline to complete with safety timeout
        yield return new WaitUntil(() => 
        {
            elapsed += Time.deltaTime;
            
            // Safety timeout: force completion if timeline runs too long
            if (elapsed >= safetyTimeout)
            {
                if (debugLogging)
                    Debug.LogWarning($"[CinematicController] Timeline safety timeout triggered ({elapsed:F2}s >= {safetyTimeout:F2}s). Forcing completion.");
                
                // Stop the director to ensure clean state
                if (director != null && director.state == PlayState.Playing)
                    director.Stop();
                
                return true;
            }
            
            // Normal completion: timeline stopped playing OR cinematic was cancelled
            bool timelineEnded = director.state != PlayState.Playing;
            bool wasCancelled = !isPlaying;
            
            if (timelineEnded && debugLogging)
                Debug.Log($"[CinematicController] Timeline naturally ended at {elapsed:F2}s | Final State: {director.state}");
            
            return timelineEnded || wasCancelled;
        });

        // Timeline finished
        if (isPlaying)
        {
            if (debugLogging)
                Debug.Log($"[CinematicController] Timeline complete: {cinematicData.ID} | Elapsed: {elapsed:F2}s | Final State: {director.state}");

            isPlaying = false;
            OnCinematicCompleted?.Invoke(cinematicData.ID);
            onCompleteCallback?.Invoke(cinematicData.ID);

            yield return new WaitForSeconds(0.3f);
            
            // Hide cutscene panel (handles both Timeline and DOTween modes)
            HideCutsceneUI();
            
            // Restore gameplay UI
            ShowGameplayUI();
        }

        currentCinematicID = null;
        onCompleteCallback = null;
    }

    #endregion

    #region DOTween Mode

    /// <summary>
    /// Plays a DOTween-based text cinematic.
    /// </summary>
    private IEnumerator PlayDOTweenMode(CinematicData cinematicData)
    {
        if (cutsceneText == null)
        {
            Debug.LogError("[CinematicController] CutsceneText not assigned! Cannot play DOTween cinematic.");
            isPlaying = false;
            ShowGameplayUI(); // Restore gameplay UI
            onCompleteCallback?.Invoke(cinematicData.ID);
            yield break;
        }

        // Show panel and text
        if (cutscenePanel != null)
            ShowCutsceneUI();

        // Setup text
        cutsceneText.text = "";
        cutsceneText.color = normalTextColor;

        // Ensure CanvasGroup is visible
        CanvasGroup textGroup = cutsceneText.GetComponent<CanvasGroup>();
        if (textGroup == null) textGroup = cutsceneText.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 1f;

        // Typewriter effect using Rune API for Arabic support
#if UNITY_2023_1_OR_NEWER
        var runes = cinematicData.TextAR.EnumerateRunes().ToArray();
        int totalRunes = runes.Length;
#else
        int totalRunes = CountTextElements(cinematicData.TextAR);
#endif

        float charDelay = 1f / typewriterSpeed;
        float estimatedTime = totalRunes * charDelay;
        float duration = cinematicData.Duration > 0 ? cinematicData.Duration : Mathf.Max(estimatedTime, 2f);

        // Adjust speed to fit duration
        if (estimatedTime > duration)
            charDelay = duration / totalRunes;

        // Animate text reveal
        for (int i = 0; i < totalRunes; i++)
        {
            if (!isPlaying) yield break;

#if UNITY_2023_1_OR_NEWER
            cutsceneText.text = new string(runes.AsSpan(0, i + 1));
#else
            cutsceneText.text = SafeSubstring(cinematicData.TextAR, i + 1);
#endif
            yield return new WaitForSeconds(charDelay);
        }

        // Wait remaining duration
        float remaining = duration - (totalRunes * charDelay);
        if (remaining > 0)
            yield return new WaitForSeconds(remaining);

        // Finish
        if (isPlaying)
        {
            if (debugLogging)
                Debug.Log($"[CinematicController] DOTween cinematic complete: {cinematicData.ID}");

            isPlaying = false;
            OnCinematicCompleted?.Invoke(cinematicData.ID);
            onCompleteCallback?.Invoke(cinematicData.ID);

            yield return new WaitForSeconds(0.3f);
            HideCutsceneUI();
            ShowGameplayUI(); // Restore gameplay UI after cinematic
        }

        currentCinematicID = null;
        onCompleteCallback = null;
    }

    /// <summary>
    /// Safely extracts a substring without breaking UTF-16 surrogate pairs.
    /// </summary>
    private string SafeSubstring(string text, int elementCount)
    {
        int charIndex = 0;
        int elementsFound = 0;

        while (elementsFound < elementCount && charIndex < text.Length)
        {
            if (char.IsHighSurrogate(text[charIndex]) && charIndex + 1 < text.Length && char.IsLowSurrogate(text[charIndex + 1]))
                charIndex += 2;
            else
                charIndex += 1;
            elementsFound++;
        }

        return text.Substring(0, charIndex);
    }

    /// <summary>
    /// Counts visible text elements (respects surrogate pairs).
    /// </summary>
    private int CountTextElements(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        int count = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                i += 2;
            else
                i += 1;
            count++;
        }
        return count;
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Hides all gameplay UI during cinematic playback to prevent parallel execution.
    /// </summary>
    private void HideGameplayUI()
    {
        // Save state and hide swipe encounter panel
        if (swipeEncounterPanel != null)
        {
            wasSwipePanelActive = swipeEncounterPanel.activeSelf;
            swipeEncounterPanel.SetActive(false);
        }

        // Save state and hide interaction HUD
        if (interactionHUDPanel != null)
        {
            wasInteractionPanelActive = interactionHUDPanel.activeSelf;
            interactionHUDPanel.SetActive(false);
        }

        // Save state and hide timer
        if (timerSlider != null)
        {
            wasTimerActive = timerSlider.gameObject.activeSelf;
            timerSlider.gameObject.SetActive(false);
        }

        if (debugLogging)
            Debug.Log("[CinematicController] Gameplay UI hidden - exclusive cinematic mode");
    }

    /// <summary>
    /// Restores all gameplay UI after cinematic completes.
    /// </summary>
    private void ShowGameplayUI()
    {
        // Restore swipe encounter panel if it was active
        if (swipeEncounterPanel != null && wasSwipePanelActive)
        {
            swipeEncounterPanel.SetActive(true);
        }

        // Restore interaction HUD if it was active
        if (interactionHUDPanel != null && wasInteractionPanelActive)
        {
            interactionHUDPanel.SetActive(true);
        }

        // Restore timer if it was active
        if (timerSlider != null && wasTimerActive)
        {
            timerSlider.gameObject.SetActive(true);
        }

        // Reset state tracking
        wasSwipePanelActive = false;
        wasInteractionPanelActive = false;
        wasTimerActive = false;

        if (debugLogging)
            Debug.Log("[CinematicController] Gameplay UI restored");
    }

    private void ShowCutsceneUI()
    {
        if (cutscenePanel == null) return;

        cutscenePanel.SetActive(true);
        
        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.3f);
        }
    }

    private void HideCutsceneUI()
    {
        if (cutscenePanel == null) return;

        // Kill any ongoing fade animation to prevent conflicts
        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
            canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                cutscenePanel.SetActive(false);
            });
        }
        else
        {
            cutscenePanel.SetActive(false);
        }

        if (debugLogging)
            Debug.Log("[CinematicController] Cutscene UI hidden");
    }

    #endregion

    #region Timeline Loading

    /// <summary>
    /// Preload all timeline assets from Resources/Timelines/ folder.
    /// </summary>
    private void PreloadTimelinesFromResources()
    {
        if (string.IsNullOrEmpty(timelinesResourcesPath)) return;

        var timelines = Resources.LoadAll<TimelineAsset>(timelinesResourcesPath);
        if (timelines == null || timelines.Length == 0)
        {
            Debug.LogWarning($"[CinematicController] No timelines found at Resources/{timelinesResourcesPath}/");
            return;
        }

        foreach (var timeline in timelines)
        {
            if (timeline != null && !timelineCache.ContainsKey(timeline.name))
            {
                timelineCache[timeline.name] = timeline;
                if (debugLogging)
                    Debug.Log($"[CinematicController] Preloaded timeline: {timeline.name}");
            }
        }

        Debug.Log($"[CinematicController] Preloaded {timelines.Length} timelines from Resources");
    }

    #endregion

    #region Inspector Buttons

    [Button("Test DOTween Cinematic")]
    private void TestDOTween()
    {
        if (Application.isPlaying)
        {
            var testData = new CinematicData
            {
                ID = "Test_DOTween",
                HouseLevel = 1,
                Type = CinematicType.DOTween,
                TextAR = "شربت القهوة وخلصت! ☕",
                Duration = 3f,
                Animation = AnimationType.FadeIn
            };

            PlayCinematic(testData, (id) =>
            {
                Debug.Log($"[CinematicController Test] Completed: {id}");
            });
        }
        else
        {
            Debug.LogWarning("[CinematicController] Enter Play mode to test.");
        }
    }

    [Button("List Loaded Timelines")]
    private void ListTimelines()
    {
        Debug.Log($"[CinematicController] Loaded timelines: {timelineCache.Count}");
        foreach (var kvp in timelineCache)
        {
            Debug.Log($"  - {kvp.Key}");
        }
    }

    #endregion
}
