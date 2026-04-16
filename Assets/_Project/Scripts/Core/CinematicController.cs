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

    [Tooltip("Root object for dialogue text and box (hidden when just portraits show)")]
    [SerializeField] private GameObject dialogueRoot;

    [Tooltip("Text display for DOTween cinematics (only used in DOTween mode)")]
    [SerializeField] private RTLTextMeshPro cutsceneText;

    [Tooltip("UI Image for NPC portraits (only used in DOTween mode)")]
    [SerializeField] private UnityEngine.UI.Image visualImage;

    [Tooltip("UI Image for Player (Robot) portrait")]
    [SerializeField] private UnityEngine.UI.Image playerImage;

    [Tooltip("Speaker name text display (optional)")]
    [SerializeField] private RTLTextMeshPro speakerNameText;

    [Tooltip("Panel holding the speaker name (optional - will be hidden if no speaker)")]
    [SerializeField] private GameObject speakerNamePanel;

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
    [Tooltip("Type a cinematic ID from DataManager to test (e.g. C_GREET_1)")]
    [SerializeField] private string testCinematicID = "C_GREET_1";
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

        // Listen for interaction results to update character expressions
        InteractionHUDController.OnInteractionFinished += HandleInteractionFinished;

        // Listen for house starts to show portraits immediately
        HouseFlowController.OnHouseStarted += InitializeHousePortraits;
    }

    private void OnDestroy()
    {
        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        InteractionHUDController.OnInteractionFinished -= HandleInteractionFinished;
        WardrobeManager.OnOutfitEquipped -= UpdatePlayerPortrait;
        HouseFlowController.OnHouseStarted -= InitializeHousePortraits;
    }

    private void InitializeHousePortraits(int houseLevel)
    {
        if (debugLogging) Debug.Log($"[CinematicController] Initializing portraits for House {houseLevel}");

        // 1. Ensure panel is active but not blocking
        if (cutscenePanel != null)
        {
            cutscenePanel.SetActive(true);
            CanvasGroup cg = cutscenePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = false;
            }
        }

        // Hide dialogue box initially
        if (dialogueRoot != null) dialogueRoot.SetActive(false);

        // 2. Show Player (Robot)
        UpdatePlayerPortrait();
        if (playerImage != null) playerImage.gameObject.SetActive(true);

        // 3. Show House NPC
        string npcName = houseLevel switch
        {
            1 => "خالة",
            2 => "عمو",
            3 => "جدو",
            4 => "ابن العم",
            _ => ""
        };

        if (!string.IsNullOrEmpty(npcName))
        {
            var speaker = DataManager.Instance?.GetSpeakerByName(npcName);
            if (speaker != null && visualImage != null)
            {
                visualImage.sprite = speaker.GetExpressionSprite("Default");
                visualImage.gameObject.SetActive(true);
                
                if (speakerNameText != null) speakerNameText.text = speaker.characterName;
                // Keep name panel hidden until someone actually speaks
                if (speakerNamePanel != null) speakerNamePanel.SetActive(false);
            }
        }
    }

    private void Start()
    {
        // Listen for outfit changes to update player portrait
        WardrobeManager.OnOutfitEquipped += UpdatePlayerPortrait;
        UpdatePlayerPortrait(); // Initial update
    }

    /// <summary>
    /// Updates the player (Robot) portrait sprite based on the currently equipped outfit.
    /// </summary>
    public void UpdatePlayerPortrait()
    {
        if (playerImage == null || WardrobeManager.Instance == null) return;

        int id = WardrobeManager.Instance.EquippedOutfitID;
        OutfitData data = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == id);

        if (data != null && !string.IsNullOrEmpty(data.spriteName))
        {
            Sprite s = Resources.Load<Sprite>("CharacterSprites/" + data.spriteName);
            if (s != null)
            {
                playerImage.sprite = s;
                playerImage.gameObject.SetActive(true);
            }
        }
    }

    private void HandleInteractionFinished(InteractionData data, bool succeeded)
    {
        if (data == null) return;

        // Only update if a speaker and expression are defined for this result
        string targetExpression = succeeded ? data.SuccessExpression : data.FailureExpression;
        
        if (!string.IsNullOrEmpty(data.SpeakerName) && !string.IsNullOrEmpty(targetExpression))
        {
            if (debugLogging)
                Debug.Log($"[CinematicController] Interaction result: Updating {data.SpeakerName} to {targetExpression}");

            // Find speaker data from DataManager
            var speaker = DataManager.Instance?.GetSpeakerByName(data.SpeakerName);
            if (speaker != null)
            {
                UpdateExpressionExternally(speaker, targetExpression);
            }
        }
    }

    /// <summary>
    /// Updates the speaker portrait expression even if no cinematic is playing.
    /// Useful for showing reactions to gameplay (e.g., interaction success/failure).
    /// </summary>
    public void UpdateExpressionExternally(CharacterExpressionSO speaker, string expressionName)
    {
        if (visualImage == null) return;

        bool wasAlreadyVisible = visualImage.gameObject.activeSelf;
        Sprite newSprite = speaker.GetExpressionSprite(expressionName);

        // Ensure cutscene panel is active to show the reaction
        if (cutscenePanel != null)
        {
            bool wasPanelActive = cutscenePanel.activeSelf;
            cutscenePanel.SetActive(true);
            
            CanvasGroup cg = cutscenePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                // Only fade if panel was actually hidden or semi-transparent
                if (!wasPanelActive || cg.alpha < 0.9f)
                {
                    cg.alpha = 0f;
                    cg.DOFade(1f, 0.3f);
                }
                cg.blocksRaycasts = false; // Reaction shouldn't block gameplay input
            }
        }

        // Also ensure Robot is updated and shown
        UpdatePlayerPortrait();
        if (playerImage != null) playerImage.gameObject.SetActive(true);

        bool isSameSpeaker = visualImage.sprite == newSprite && wasAlreadyVisible;

        visualImage.gameObject.SetActive(true);
        visualImage.sprite = newSprite;
        
        if (speakerNameText != null)
            speakerNameText.text = speaker.characterName;

        if (speakerNamePanel != null)
            speakerNamePanel.SetActive(true);

        // ONLY play pop-in juice if we are switching characters or it was hidden
        if (!isSameSpeaker)
        {
            ApplyAnimation(visualImage.transform, AnimationType.Bounce);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Plays a cinematic. Supports both Unity Timeline and DOTween modes.
    /// Called by HouseFlowController when a Cinematic element is encountered.
    /// </summary>
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

        // PHASE 18 REFINEMENT: 
        // Only hide gameplay UI for Unity Timelines (full screen movies).
        // DOTween dialogue stays OVER the gameplay UI so you see cards and characters.
        if (cinematicData.Type == CinematicType.UnityTimeline)
        {
            HideGameplayUI();
        }

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

    /// <summary>
    /// Checks if the persistent cinematic UI is currently displaying a specific character.
    /// </summary>
    public bool IsShowingCharacter(string charName)
    {
        if (visualImage == null || !visualImage.gameObject.activeSelf) return false;
        if (speakerNameText == null) return false;
        
        return speakerNameText.text.Equals(charName, StringComparison.OrdinalIgnoreCase);
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

            // PHASE 17 Refinement: Restore gameplay UI and clear dialogue chrome
            ShowGameplayUI();
            
            if (cutsceneText != null) cutsceneText.text = "";
            if (speakerNamePanel != null) speakerNamePanel.SetActive(false);
            
            // Disable raycast blocking so interactions can be touched
            var cg = cutscenePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
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

        // Show panel and setup visual visuals
        if (cutscenePanel != null)
            ShowCutsceneUI();

        // 1. Setup player image (Robot)
        UpdatePlayerPortrait();
        if (playerImage != null) playerImage.gameObject.SetActive(true);

        // 2. Setup NPC/Visual image (Portrait or Prop)
        if (visualImage != null)
        {
            Sprite spriteToShow = null;
            bool wasAlreadyVisible = visualImage.gameObject.activeSelf;
            bool isResourceValid = false;
            
            // PRIORITY 1: Dynamic Prop (Item) from Resources
            if (!string.IsNullOrEmpty(cinematicData.ResourceImageName))
            {
                spriteToShow = Resources.Load<Sprite>($"InteractionIcons/{cinematicData.ResourceImageName}");
                if (spriteToShow == null) spriteToShow = Resources.Load<Sprite>(cinematicData.ResourceImageName);
                
                if (spriteToShow != null)
                {
                    isResourceValid = true;
                    if (speakerNamePanel != null) speakerNamePanel.SetActive(false);
                }
            }
            
            // PRIORITY 2: Character Portrait (only if resource didn't load)
            if (!isResourceValid && cinematicData.Speaker != null)
            {
                spriteToShow = cinematicData.Speaker.GetExpressionSprite(cinematicData.Expression);
                if (speakerNameText != null) speakerNameText.text = cinematicData.Speaker.characterName;
                if (speakerNamePanel != null) speakerNamePanel.SetActive(true);
            }
            else if (!isResourceValid)
            {
                if (speakerNamePanel != null) speakerNamePanel.SetActive(false);
            }

            // Apply sprite and play juice animation
            if (spriteToShow != null)
            {
                bool isSameSprite = visualImage.sprite == spriteToShow && wasAlreadyVisible;
                
                visualImage.gameObject.SetActive(true);
                visualImage.sprite = spriteToShow;
                
                // Implement AnimationType logic for portrait
                if (!isSameSprite)
                {
                    ApplyAnimation(visualImage.transform, cinematicData.Animation);
                }
            }
            else
            {
                visualImage.gameObject.SetActive(false);
            }
        }

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

            // PHASE 17: UI is NOT hidden here to allow seamless transitions in HouseFlowController
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

    /// <summary>
    /// Applies a specific DOTween animation style to a target transform.
    /// </summary>
    private void ApplyAnimation(Transform target, AnimationType type)
    {
        if (target == null) return;

        // Kill existing tweens on target to prevent stacking
        target.DOKill();

        switch (type)
        {
            case AnimationType.FadeIn:
                // For portraits, FadeIn is usually handled by CanvasGroup, 
                // but we can add a subtle scale up too
                target.localScale = Vector3.one * 0.95f;
                target.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutQuad);
                break;

            case AnimationType.Bounce:
                target.localScale = Vector3.zero;
                target.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
                break;

            case AnimationType.Slide:
                Vector3 originalPos = target.localPosition;
                target.localPosition = originalPos + new Vector3(200f, 0f, 0f);
                target.DOLocalMove(originalPos, 0.4f).SetEase(Ease.OutCubic);
                target.localScale = Vector3.one;
                break;

            case AnimationType.Pulse:
                target.localScale = Vector3.one;
                target.DOScale(Vector3.one * 1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                break;

            default:
                target.localScale = Vector3.one;
                break;
        }
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Hides all gameplay UI during cinematic playback to prevent parallel execution.
    /// ONLY used for full-screen Unity Timelines.
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

        // Save state and hide timer slider
        if (timerSlider != null)
        {
            wasTimerActive = timerSlider.gameObject.activeSelf;
            timerSlider.gameObject.SetActive(false);
        }

        if (debugLogging)
            Debug.Log("[CinematicController] Gameplay UI hidden - EXCLUSIVE mode");
    }

    /// <summary>
    /// Restores all gameplay UI after cinematic completes.
    /// </summary>
    public void ShowGameplayUI()
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

        // Restore timer slider if it was active
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

        bool wasAlreadyActive = cutscenePanel.activeSelf;
        cutscenePanel.SetActive(true);
        
        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true; // Block input during active cinematic dialogue
            
            // ONLY fade in if it wasn't already showing (prevents flickering)
            if (!wasAlreadyActive || canvasGroup.alpha < 0.9f)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
            }
        }
    }

    /// <summary>
    /// Hides the cutscene UI with an optional fade out.
    /// </summary>
    public void HideCutsceneUI(bool immediate = false)
    {
        if (cutscenePanel == null) return;

        // Kill any ongoing fade animation to prevent conflicts
        CanvasGroup canvasGroup = cutscenePanel.GetComponent<CanvasGroup>();
        
        if (immediate)
        {
            if (canvasGroup != null) DOTween.Kill(canvasGroup);
            cutscenePanel.SetActive(false);
        }
        else
        {
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

    [Button("Play Cinematic by ID")]
    private void TestPlayByID()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CinematicController] Enter Play mode to test.");
            return;
        }

        var data = DataManager.Instance?.GetCinematicByID(testCinematicID);
        if (data != null)
        {
            PlayCinematic(data, (id) => Debug.Log($"[CinematicController Test] Completed: {id}"));
        }
        else
        {
            Debug.LogError($"[CinematicController] Cinematic ID '{testCinematicID}' not found in DataManager.");
        }
    }

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

