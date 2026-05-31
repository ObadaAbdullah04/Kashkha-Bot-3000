using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

/// <summary>
/// Singleton manager that handles the tutorial overlay UI.
/// </summary>
public class TutorialOverlayManager : MonoBehaviour
{
    public static TutorialOverlayManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private GameObject overlayBackground; // Raycast blocker (Black Dim)
    [SerializeField] private RectTransform pointerContainer; // Simple container for prefabs
    [SerializeField] private RectTransform instructionContainer;
    [SerializeField] private RTLTextMeshPro instructionText;
    [SerializeField] private Button dismissButton; // Full-screen skip button
    [SerializeField] private RTLTextMeshPro tapToContinueText;

    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.5f;
    [SerializeField] private float floatAmplitude = 10f;
    [SerializeField] private float floatPeriod = 2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Test Settings")]
    [SerializeField] private TutorialAnimationType testAnimType;
    [SerializeField] private RectTransform testTarget;

    [Button("Test Selected Animation")]
    private void TestAnimation()
    {
        if (Application.isPlaying)
            ShowTutorial(testTarget, "Testing Animation: " + testAnimType, true, null, testAnimType);
        else
            Debug.LogWarning("Testing animations only works in Play Mode!");
    }

    [Header("Tutorial Targets")]
    [SerializeField] private List<TutorialTargetMapping> manualTargets = new List<TutorialTargetMapping>();

    [Serializable]
    public struct AnimationMapping
    {
        public TutorialAnimationType type;
        public TutorialPointer prefab;
    }

    [Header("Animation Prefabs")]
    [SerializeField] private List<AnimationMapping> animationPrefabs = new List<AnimationMapping>();

    private TutorialPointer activePointer;
    private Action onTutorialComplete;
    private Dictionary<string, RectTransform> registeredTargets = new Dictionary<string, RectTransform>();
    private Dictionary<TutorialAnimationType, TutorialPointer> prefabMap = new Dictionary<TutorialAnimationType, TutorialPointer>();
    private RectTransform characterPortraitTarget;
    private Tween instructionFloatTween;
    private bool forceAdvance = false;
    private bool isRoutineRunning = false;
    private Coroutine tutorialRoutine;
    private Image overlayImage;

    public bool IsTutorialActive => isRoutineRunning || (tutorialCanvas != null && tutorialCanvas.gameObject.activeSelf);

    /// <summary>
    /// Registers a generic target for tutorial highlighting.
    /// </summary>
    public void RegisterTarget(string id, RectTransform target)
    {
        registeredTargets[id] = target;
    }

    /// <summary>
    /// Registers the NPC portrait for tutorial highlighting.
    /// Called by CinematicController.
    /// </summary>
    public void RegisterCharacterPortrait(RectTransform portrait)
    {
        characterPortraitTarget = portrait;
        RegisterTarget("CharacterPortrait", portrait);
    }

    [Serializable]
    public class TutorialTargetMapping
    {
        public string targetID;
        public RectTransform targetTransform;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;

            if (graphicRaycaster == null)
                graphicRaycaster = tutorialCanvas.GetComponent<GraphicRaycaster>();

            overlayImage = overlayBackground.GetComponent<Image>();

            if (dismissButton != null)
                dismissButton.onClick.AddListener(OnDismissClicked);

            InitializeTargets();
            HideTutorialImmediate();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (dismissButton != null)
                dismissButton.onClick.RemoveListener(OnDismissClicked);
            Instance = null;
        }
    }

    [Button("Initialize & Validate Targets")]
    private void InitializeTargets()
    {
        // Don't clear registeredTargets as some might be registered via code (sliders, etc.)
        foreach (var mapping in manualTargets)
        {
            if (mapping.targetTransform != null && !string.IsNullOrEmpty(mapping.targetID))
            {
                registeredTargets[mapping.targetID] = mapping.targetTransform;
            }
        }

        prefabMap.Clear();
        foreach (var mapping in animationPrefabs)
        {
            if (mapping.prefab != null)
                prefabMap[mapping.type] = mapping.prefab;
        }
    }

    [Button("Print Required Targets from CSV")]
    public void PrintRequiredTargets()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[TutorialManager] DataManager Instance not found.");
            return;
        }

        HashSet<string> required = new HashSet<string>();
        foreach (var list in DataManager.Instance.tutorialStepsByID.Values)
        {
            foreach (var step in list)
            {
                if (!string.IsNullOrEmpty(step.TargetID) && step.TargetID != "None")
                    required.Add(step.TargetID);
            }
        }

        string report = "<b>[TutorialManager] Required Targets:</b>\n";
        foreach (var id in required)
        {
            bool isAssigned = registeredTargets.ContainsKey(id) && registeredTargets[id] != null;
            report += isAssigned ? $"<color=green>✅ {id}</color>\n" : $"<color=red>❌ {id} (MISSING!)</color>\n";
        }
        Debug.Log(report);
    }

    public void PlayTutorial(string tutorialID, Action onComplete = null)
    {
        InitializeTargets();
        if (DataManager.Instance == null || !DataManager.Instance.tutorialStepsByID.ContainsKey(tutorialID))
        {
            onComplete?.Invoke();
            return;
        }

        if (tutorialRoutine != null) StopCoroutine(tutorialRoutine);
        tutorialRoutine = StartCoroutine(TutorialSequenceRoutine(tutorialID, DataManager.Instance.tutorialStepsByID[tutorialID], onComplete));
    }

    /// <summary>
    /// Forcefully stops any active tutorial sequence and hides the UI immediately.
    /// </summary>
    public void StopTutorial()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        isRoutineRunning = false;
        forceAdvance = false;
        onTutorialComplete = null;
        
        HideTutorialImmediate();
    }

    private IEnumerator TutorialSequenceRoutine(string tutorialID, List<TutorialStepData> steps, Action onComplete)
    {
        isRoutineRunning = true;
        forceAdvance = false; // Reset at the start of the sequence

        // REACTION FIX: Allow skipping the initial delay if player advances early
        float elapsedDelay = 0;
        float adjustedDelay = 0.4f; // Shortened from 0.8f for better responsiveness
        while (elapsedDelay < adjustedDelay)
        {
            if (forceAdvance) break;
            elapsedDelay += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            bool stepFinished = false;
            
            // If we didn't advance during the delay or previous step, reset it for this step
            // Otherwise, we "consume" the previous advance to skip this step's waiting period
            if (forceAdvance)
            {
                forceAdvance = false;
                if (i < steps.Count - 1) continue; // Skip to next step
                else break; // End sequence
            }

            RectTransform targetTransform = null;
            if (registeredTargets.ContainsKey(step.TargetID))
                targetTransform = registeredTargets[step.TargetID];
            
            if (targetTransform == null && step.TargetID != "None")
            {
                Debug.LogError($"[TutorialManager] Step {step.StepIndex} Target '{step.TargetID}' missing!");
                continue; 
            }

            float oldTimeScale = Time.timeScale;
            Time.timeScale = step.TimeScale;

            bool canActuallyClickTarget = step.RequireTargetClick && targetTransform != null && targetTransform.GetComponent<Button>() != null;
            bool requireClickToDismiss = !step.RequireTargetClick || !canActuallyClickTarget;

            ShowTutorial(targetTransform, step.InstructionAR, requireClickToDismiss, () => {
                stepFinished = true;
            }, step.AnimationType);

            Button targetBtn = null;
            UnityEngine.Events.UnityAction btnAction = null;
            if (canActuallyClickTarget)
            {
                targetBtn = targetTransform.GetComponent<Button>();
                btnAction = () => { stepFinished = true; };
                targetBtn.onClick.AddListener(btnAction);
            }

            yield return new WaitUntil(() => stepFinished || forceAdvance);
            
            Time.timeScale = oldTimeScale;
            if (targetBtn != null && btnAction != null) targetBtn.onClick.RemoveListener(btnAction);
            forceAdvance = false; // Reset after the step is successfully completed/skipped

            if (i < steps.Count - 1 && !forceAdvance)
                yield return new WaitForSecondsRealtime(0.15f);
        }

        HideTutorial();
        isRoutineRunning = false;
        onComplete?.Invoke();
    }

    public void AdvanceTutorial()
    {
        if (IsTutorialActive)
        {
            forceAdvance = true;

            // If it's a manual ShowTutorial call (not a routine), we must finish it here
            if (!isRoutineRunning)
            {
                Action complete = onTutorialComplete;
                onTutorialComplete = null;
                complete?.Invoke();
                HideTutorial();
            }
        }
    }

    public void ShowTutorial(Transform targetToHighlight, string text, bool requireClickToDismiss = true, Action onComplete = null, TutorialAnimationType animationType = TutorialAnimationType.None)
    {
        onTutorialComplete = onComplete;
        instructionText.text = text;

        CanvasGroup canvasGroup = tutorialCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup != null) { canvasGroup.DOKill(); canvasGroup.alpha = 1f; }

        tutorialCanvas.gameObject.SetActive(true);
        overlayBackground.SetActive(true);
        dismissButton.gameObject.SetActive(requireClickToDismiss);
        
        // CRITICAL: If we require interaction with the game world (swipe/button), 
        // the background must not block raycasts!
        if (overlayImage != null)
            overlayImage.raycastTarget = requireClickToDismiss;

        if (tapToContinueText != null)
        {
            tapToContinueText.gameObject.SetActive(requireClickToDismiss);
            if (requireClickToDismiss)
            {
                tapToContinueText.DOKill();
                tapToContinueText.alpha = 0f;
                tapToContinueText.DOFade(1f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        // DYNAMIC POSITIONING: Avoid center if an encounter is active and no target is specified
        bool isEncounterActive = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Encounter;
        
        if (targetToHighlight == null && isEncounterActive)
        {
            // Move panel to bottom area to avoid covering cards
            instructionContainer.localPosition = new Vector3(0, -380f, 0); 
        }
        else if (targetToHighlight != null)
        {
            // Use normalized viewport position for safer screen-half detection
            Vector2 viewportPos = Camera.main != null 
                ? Camera.main.WorldToViewportPoint(targetToHighlight.position)
                : new Vector2(0.5f, targetToHighlight.position.y / Screen.height);

            if (viewportPos.y < 0.5f) instructionContainer.localPosition = new Vector3(0, 320f, 0);
            else instructionContainer.localPosition = new Vector3(0, -320f, 0);
        }

        // --- ANIMATION SYSTEM OVERHAUL ---
        if (activePointer != null)
        {
            Destroy(activePointer.gameObject);
            activePointer = null;
        }

        if (targetToHighlight != null && animationType != TutorialAnimationType.None)
        {
            pointerContainer.gameObject.SetActive(true);
            pointerContainer.position = targetToHighlight.position;
            pointerContainer.DOKill();
            pointerContainer.localScale = Vector3.one;
            pointerContainer.localRotation = Quaternion.identity;

            // Instantiate prefab based on AnimationType
            if (prefabMap.ContainsKey(animationType))
            {
                TutorialPointer prefab = prefabMap[animationType];
                if (prefab != null)
                {
                    activePointer = Instantiate(prefab, pointerContainer);
                    // PHASE 18: Reset position - offset is now handled INTERNALLY by the pointer script
                    activePointer.transform.localPosition = Vector3.zero;
                    activePointer.transform.localScale = Vector3.one;
                    activePointer.transform.localRotation = Quaternion.identity;
                    activePointer.PlayAnimation(animationType);
                }
            }
            
            // Bring target to front visually if possible (requires Canvas on target)
            var targetCanvas = targetToHighlight.GetComponent<Canvas>();
            if (targetCanvas != null)
            {
                targetCanvas.overrideSorting = true;
                targetCanvas.sortingOrder = tutorialCanvas.sortingOrder + 1;
            }
        }
        else pointerContainer.gameObject.SetActive(false);

        instructionContainer.DOKill();
        instructionFloatTween?.Kill();
        instructionContainer.localScale = Vector3.zero;
        Vector3 targetPos = instructionContainer.localPosition;
        instructionContainer.localPosition = targetPos + new Vector3(0, -100f, 0);
        
        Sequence popSeq = DOTween.Sequence().SetUpdate(true);
        popSeq.Append(instructionContainer.DOScale(Vector3.one, popDuration).SetEase(Ease.OutBack));
        popSeq.Join(instructionContainer.DOLocalMove(targetPos, popDuration).SetEase(Ease.OutCubic));
        popSeq.OnComplete(() => {
            instructionFloatTween = instructionContainer.DOLocalMoveY(targetPos.y + floatAmplitude, floatPeriod)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        });
    }

    // Legacy support for string-based tutorialID (one-offs)
    public void ShowTutorial(Transform targetToHighlight, string text, bool requireClickToDismiss, Action onComplete, string tutorialID)
    {
        TutorialAnimationType anim = TutorialAnimationType.None;
        if (!string.IsNullOrEmpty(tutorialID))
        {
            if (tutorialID.Contains("Shake")) anim = TutorialAnimationType.Shake;
            else if (tutorialID.Contains("Tap")) anim = TutorialAnimationType.Tap;
            else if (tutorialID.Contains("Hold")) anim = TutorialAnimationType.Hold;
            else if (tutorialID.Contains("Draw")) anim = TutorialAnimationType.Draw;
            else if (tutorialID.Contains("Intro") || tutorialID.Contains("Swipe")) anim = TutorialAnimationType.Swipe;
        }
        ShowTutorial(targetToHighlight, text, requireClickToDismiss, onComplete, anim);
    }

    public void ShowTutorial(Transform targetToHighlight, string text, Action onComplete)
        => ShowTutorial(targetToHighlight, text, true, onComplete);

    private void OnDismissClicked() => onTutorialComplete?.Invoke();

    public void HideTutorial()
    {
        CanvasGroup canvasGroup = tutorialCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = tutorialCanvas.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true).OnComplete(() => {
            tutorialCanvas.gameObject.SetActive(false);
            overlayBackground.SetActive(false);
            dismissButton.gameObject.SetActive(false);
            pointerContainer.gameObject.SetActive(false);
            if (activePointer != null)
            {
                Destroy(activePointer.gameObject);
                activePointer = null;
            }
            canvasGroup.alpha = 1f;
        });
        instructionFloatTween?.Kill();
        if (tapToContinueText != null) tapToContinueText.DOKill();
    }

    private void HideTutorialImmediate()
    {
        tutorialCanvas.gameObject.SetActive(false);
        overlayBackground.SetActive(false);
        dismissButton.gameObject.SetActive(false);
        pointerContainer.gameObject.SetActive(false);
        instructionFloatTween?.Kill();
        if (tapToContinueText != null) tapToContinueText.DOKill();
    }
}
