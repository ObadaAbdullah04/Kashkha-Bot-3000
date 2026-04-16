using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 9.6 — Self-Driving House Flow Controller (Coroutine-Based).
///
/// HOW IT WORKS:
/// 1. GameManager calls StartHouse(houseLevel, sequence)
/// 2. HouseFlowController starts PlayHouseSequence() coroutine
/// 3. Coroutine iterates through elements ONE AT A TIME:
///    - Triggers current element (Question or Cutscene)
///    - YIELDS (pauses) until element completes via callback
///    - Advances to next element → repeat
/// 4. All elements done → OnHouseCompleted → GameManager.EndHouse()
///
/// KEY DESIGN:
/// - Pure coroutine-driven architecture
/// - Each element controls its own duration (player-paced)
/// - Simple to extend: add new ElementType → add case in switch
/// - Easy to add pauses: yield return new WaitForSeconds(X)
///
/// USAGE:
/// 1. Add HouseFlowController component to a GameObject
/// 2. Assign system references in inspector (SwipeEncounterManager, etc.)
/// 3. GameManager calls: StartCoroutine(hfc.PlayHouseSequence(houseLevel, sequence))
/// </summary>
public class HouseFlowController : MonoBehaviour
{
    public static HouseFlowController Instance { get; private set; }

    #region Inspector Fields

    [Header("System References")]
    [Tooltip("Reference to SwipeEncounterManager for questions")]
    [SerializeField] private SwipeEncounterManager swipeEncounterManager;

    [Tooltip("Reference to CinematicController for unified cinematics (Timeline or DOTween)")]
    [SerializeField] private CinematicController cinematicController;

    [Tooltip("Reference to InteractionHUDController for interactions")]
    [SerializeField] private InteractionHUDController interactionHUDController;

    [Header("Timing")]
    [Tooltip("Pause between elements (seconds)")]
    [SerializeField] private float pauseBetweenElements = 0.5f;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging")]
    // [SerializeField] private bool debugLogging = false;

    #endregion

    #region State

    private int currentHouseLevel = 0;
    private List<SequenceElement> currentSequence = new List<SequenceElement>();
    private bool isSequencePlaying = false;

    #endregion

    #region Events

    /// <summary>
    /// Fires when a house sequence starts.
    /// </summary>
    public static Action<int> OnHouseStarted;

    /// <summary>
    /// Fires when a house sequence completes.
    /// </summary>
    public static Action<int> OnHouseCompleted;

    /// <summary>
    /// Fires when an element completes. (ElementType, ElementID)
    /// </summary>
    public static Action<ElementType, string> OnElementCompleted;

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
            // Debug.LogError("[HouseFlowController] Duplicate instance! Destroying.");
            Destroy(gameObject);
            return;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts a house sequence. Called by GameManager.
    /// Returns the coroutine — caller must StartCoroutine() it.
    /// </summary>
    public IEnumerator PlayHouseSequence(int houseLevel, HouseSequenceData sequence)
    {
        if (isSequencePlaying)
        {
            // Debug.LogWarning("[HouseFlowController] Sequence already playing!");
            yield break;
        }

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            // Debug.LogError($"[HouseFlowController] No sequence data for House {houseLevel}!");
            OnHouseCompleted?.Invoke(houseLevel);
            yield break;
        }

        currentHouseLevel = houseLevel;
        currentSequence = new List<SequenceElement>(sequence.Sequence);
        isSequencePlaying = true;

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Starting House {houseLevel} ===");
        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Sequence: {sequence.GetSequenceSummary()}");

        OnHouseStarted?.Invoke(houseLevel);

        // Count total questions for card counter
        int totalQuestions = 0;
        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (currentSequence[i].Type == ElementType.Question)
                totalQuestions++;
        }
        int questionIndex = 0;

        // Iterate through each element in sequence
        for (int i = 0; i < currentSequence.Count; i++)
        {
            SequenceElement element = currentSequence[i];

            if (element == null || string.IsNullOrWhiteSpace(element.ElementID))
            {
                // Debug.LogWarning($"[HouseFlowController] Element {i} is null or empty — skipping.");
                continue;
            }

            // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Element {i + 1}/{currentSequence.Count}: [{element.Type}] {element.ElementID}");

            // Trigger element and WAIT for completion
            switch (element.Type)
            {
                case ElementType.Question:
                    // Ensure portraits are visible, hide dialogue box for questions
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                        cinematicController.ToggleDialogueBox(false);
                    }
                    yield return PlayQuestion(element.ElementID, questionIndex, totalQuestions);
                    questionIndex++;
                    break;

                case ElementType.Cinematic:
                    // Ensure portraits are visible and show dialogue box for cinematics
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                        cinematicController.ToggleDialogueBox(true);
                    }
                    yield return PlayCinematic(element.ElementID);
                    break;

                case ElementType.Interaction:
                    // Panel and dialogue box stay visible from previous cinematic
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                    }
                    yield return PlayInteraction(element.ElementID);
                    
                    // Panel stays visible until the entire house sequence ends (handled below)
                    break;

                default:
                    // Debug.LogError($"[HouseFlowController] Unknown element type: {element.Type}");
                    break;
            }

            // Pause between elements (skip pause after last element)
            if (i < currentSequence.Count - 1 && pauseBetweenElements > 0)
            {
                // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Pausing {pauseBetweenElements}s before next element...");
                yield return new WaitForSeconds(pauseBetweenElements);
            }
        }
// All elements done
// // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === House {houseLevel} Complete! All {currentSequence.Count} elements processed. ===");

// PHASE 17: Clean up cinematic UI and restore gameplay HUD
if (cinematicController != null)
{
    cinematicController.HideCutsceneUI();
    // Restoring gameplay UI is now handled by HouseFlowController finishing or transition to next house
}

isSequencePlaying = false;
OnHouseCompleted?.Invoke(houseLevel);
}
    /// <summary>
    /// Cancels the currently playing sequence.
    /// </summary>
    public void CancelActiveSequence()
    {
        if (!isSequencePlaying) return;

        // // if (debugLogging) {} // Debug.Log("[HouseFlowController] Sequence cancelled externally. Stopping all coroutines.");

        // Actually stop the coroutines
        StopAllCoroutines();

        isSequencePlaying = false;
        
        // Ensure cinematic UI is hidden if we cancel mid-cinematic
        if (cinematicController != null) cinematicController.HideCutsceneUI();
    }

    #endregion

    #region Element Players

    /// <summary>
    /// Plays a Question element. Shows swipe card, waits for player action.
    /// </summary>
    private IEnumerator PlayQuestion(string questionID, int questionIndex, int totalQuestions)
    {
        SwipeCardData questionData = DataManager.Instance?.GetQuestionByID(questionID);
        if (questionData == null)
        {
            // Debug.LogError($"[HouseFlowController] Question not found: {questionID}");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        if (swipeEncounterManager == null)
        {
            // Debug.LogError("[HouseFlowController] SwipeEncounterManager not assigned!");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Playing Question: {questionID} ({questionIndex + 1}/{totalQuestions}) ===");

        // Show card and wait for completion
        bool cardDone = false;
        swipeEncounterManager.ShowSingleCard(questionData, questionIndex, totalQuestions, (batteryDelta, eidia, wasCorrect) =>
        {
            cardDone = true;
            // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Question complete: {questionID} | Correct={wasCorrect}");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
        });

        // Wait for card to be answered or timeout
        yield return new WaitUntil(() => cardDone);

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Question {questionID} Finished ===");
    }

    /// <summary>
    /// Plays a Cinematic element. Supports both Unity Timeline and DOTween modes.
    /// PHASE 15: Unified cinematic system.
    /// 
    /// IMPORTANT: Cinematics play EXCLUSIVELY - all other systems (swipe cards, interactions)
    /// are disabled during cinematic playback to prevent parallel execution.
    /// </summary>
    private IEnumerator PlayCinematic(string cinematicID)
    {
        if (cinematicController == null)
        {
            // Debug.LogError("[HouseFlowController] CinematicController not assigned!");
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
            yield break;
        }

        // Get cinematic data from DataManager
        var cinematicData = DataManager.Instance?.GetCinematicByID(cinematicID);
        if (cinematicData == null)
        {
            // Debug.LogError($"[HouseFlowController] Cinematic not found: {cinematicID}");
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
            yield break;
        }

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Playing Cinematic: {cinematicID} [EXCLUSIVE MODE] ===");

        // Play cinematic and wait for completion
        bool cinematicDone = false;
        cinematicController.PlayCinematic(cinematicData, (id) =>
        {
            cinematicDone = true;
            // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Cinematic complete: {cinematicID} | Type: {cinematicData.Type}");
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
        });

        // Wait for cinematic to finish - blocks all other gameplay
        yield return new WaitUntil(() => cinematicDone);

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Cinematic {cinematicID} Finished - Resuming House Flow ===");
    }

    /// <summary>
    /// Plays an Interaction element. Shows interaction HUD, waits for player input.
    /// </summary>
    private IEnumerator PlayInteraction(string interactionID)
    {
        InteractionData interactionData = DataManager.Instance?.GetInteractionByID(interactionID);
        if (interactionData == null)
        {
            // Debug.LogError($"[HouseFlowController] Interaction not found: {interactionID}");
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
            yield break;
        }

        if (interactionHUDController == null)
        {
            // Debug.LogError("[HouseFlowController] InteractionHUDController not assigned!");
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
            yield break;
        }

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Playing Interaction: {interactionID} ===");

        bool interactionDone = false;
        interactionHUDController.RunInteraction(interactionData, (succeeded, batteryDelta, eidiaReward) =>
        {
            interactionDone = true;
            // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Interaction complete: {interactionID} | Success={succeeded} | Battery:{batteryDelta} | Eid:{eidiaReward}");
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
        });

        yield return new WaitUntil(() => interactionDone);

        // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] === Interaction {interactionID} Finished ===");
    }

    #endregion

    #region Inspector Buttons

    [Button("Test Current Sequence")]
    private void TestSequence()
    {
        // This button requires a HouseSequenceData to be assigned
        // Use GameManager.StartHouse() in Play mode instead
        // Debug.LogWarning("[HouseFlowController] Use GameManager.StartHouse() to test. This button is for reference only.");
    }

    #endregion
}
