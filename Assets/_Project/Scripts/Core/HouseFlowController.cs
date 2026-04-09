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
///    - Triggers current element (Question / QTE / Cutscene)
///    - YIELDS (pauses) until element completes via callback
///    - Advances to next element → repeat
/// 4. All elements done → OnHouseCompleted → GameManager.EndHouse()
///
/// KEY DESIGN:
/// - No Timeline, no Signals, no INotification needed
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

    [Tooltip("Reference to QTEController for QTEs")]
    [SerializeField] private QTEController qteController;

    [Tooltip("Reference to CutsceneTrigger for cutscenes")]
    [SerializeField] private CutsceneTrigger cutsceneTrigger;

    [Header("Timing")]
    [Tooltip("Pause between elements (seconds)")]
    [SerializeField] private float pauseBetweenElements = 0.5f;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logging")]
    [SerializeField] private bool debugLogging = false;

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
            Debug.LogError("[HouseFlowController] Duplicate instance! Destroying.");
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
            Debug.LogWarning("[HouseFlowController] Sequence already playing!");
            yield break;
        }

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            Debug.LogError($"[HouseFlowController] No sequence data for House {houseLevel}!");
            OnHouseCompleted?.Invoke(houseLevel);
            yield break;
        }

        currentHouseLevel = houseLevel;
        currentSequence = new List<SequenceElement>(sequence.Sequence);
        isSequencePlaying = true;

        if (debugLogging)
            Debug.Log($"[HouseFlowController] === Starting House {houseLevel} ===");
        if (debugLogging)
            Debug.Log($"[HouseFlowController] Sequence: {sequence.GetSequenceSummary()}");

        OnHouseStarted?.Invoke(houseLevel);

        // Iterate through each element in sequence
        for (int i = 0; i < currentSequence.Count; i++)
        {
            SequenceElement element = currentSequence[i];

            if (element == null || string.IsNullOrWhiteSpace(element.ElementID))
            {
                Debug.LogWarning($"[HouseFlowController] Element {i} is null or empty — skipping.");
                continue;
            }

            if (debugLogging)
                Debug.Log($"[HouseFlowController] Element {i + 1}/{currentSequence.Count}: [{element.Type}] {element.ElementID}");

            // Trigger element and WAIT for completion
            switch (element.Type)
            {
                case ElementType.Question:
                    yield return PlayQuestion(element.ElementID);
                    break;

                case ElementType.QTE:
                    yield return PlayQTE(element.ElementID);
                    break;

                case ElementType.Cutscene:
                    yield return PlayCutscene(element.ElementID);
                    break;

                default:
                    Debug.LogError($"[HouseFlowController] Unknown element type: {element.Type}");
                    break;
            }

            // Pause between elements (skip pause after last element)
            if (i < currentSequence.Count - 1 && pauseBetweenElements > 0)
            {
                if (debugLogging)
                    Debug.Log($"[HouseFlowController] Pausing {pauseBetweenElements}s before next element...");
                yield return new WaitForSeconds(pauseBetweenElements);
            }
        }

        // All elements done
        if (debugLogging)
            Debug.Log($"[HouseFlowController] === House {houseLevel} Complete! All {currentSequence.Count} elements processed. ===");

        isSequencePlaying = false;
        OnHouseCompleted?.Invoke(houseLevel);
    }

    /// <summary>
    /// Cancels the currently playing sequence.
    /// </summary>
    public void CancelActiveSequence()
    {
        if (!isSequencePlaying) return;

        if (debugLogging)
            Debug.Log("[HouseFlowController] Sequence cancelled externally.");

        isSequencePlaying = false;
        OnHouseCompleted?.Invoke(currentHouseLevel);
    }

    #endregion

    #region Element Players

    /// <summary>
    /// Plays a Question element. Shows swipe card, waits for player action.
    /// </summary>
    private IEnumerator PlayQuestion(string questionID)
    {
        SwipeCardData questionData = DataManager.Instance?.GetQuestionByID(questionID);
        if (questionData == null)
        {
            Debug.LogError($"[HouseFlowController] Question not found: {questionID}");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        if (swipeEncounterManager == null)
        {
            Debug.LogError("[HouseFlowController] SwipeEncounterManager not assigned!");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        // Show card and wait for completion
        bool cardDone = false;
        swipeEncounterManager.ShowSingleCard(questionData, (batteryDelta, eidia, wasCorrect) =>
        {
            cardDone = true;
            if (debugLogging)
                Debug.Log($"[HouseFlowController] Question complete: {questionID} | Correct={wasCorrect}");
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
        });

        // Wait for card to be answered or timeout
        yield return new WaitUntil(() => cardDone);
    }

    /// <summary>
    /// Plays a QTE element. Shows QTE prompt, waits for player input.
    /// </summary>
    private IEnumerator PlayQTE(string qteID)
    {
        QTEData qteData = DataManager.Instance?.GetQTEByID(qteID);
        if (qteData == null)
        {
            Debug.LogError($"[HouseFlowController] QTE not found: {qteID}");
            OnElementCompleted?.Invoke(ElementType.QTE, qteID);
            yield break;
        }

        if (qteController == null)
        {
            Debug.LogWarning("[HouseFlowController] QTEController not assigned — skipping QTE.");
            OnElementCompleted?.Invoke(ElementType.QTE, qteID);
            yield break;
        }

        // Start QTE and wait for completion
        bool qteDone = false;
        qteController.StartQTE(qteData, (wasSuccess, batteryDelta, id) =>
        {
            qteDone = true;
            if (debugLogging)
                Debug.Log($"[HouseFlowController] QTE complete: {qteID} | Success={wasSuccess}");
            OnElementCompleted?.Invoke(ElementType.QTE, qteID);
        });

        // Wait for QTE to complete (success, fail, or timeout)
        yield return new WaitUntil(() => qteDone);
    }

    /// <summary>
    /// Plays a Cutscene element. Shows cutscene animation, waits for duration.
    /// </summary>
    private IEnumerator PlayCutscene(string cutsceneID)
    {
        CutsceneData cutsceneData = DataManager.Instance?.GetCutsceneByID(cutsceneID);
        if (cutsceneData == null)
        {
            Debug.LogError($"[HouseFlowController] Cutscene not found: {cutsceneID}");
            OnElementCompleted?.Invoke(ElementType.Cutscene, cutsceneID);
            yield break;
        }

        if (cutsceneTrigger == null)
        {
            Debug.LogWarning("[HouseFlowController] CutsceneTrigger not assigned — skipping cutscene.");
            OnElementCompleted?.Invoke(ElementType.Cutscene, cutsceneID);
            yield break;
        }

        // Play cutscene and wait for completion
        bool cutsceneDone = false;
        cutsceneTrigger.PlayCutscene(cutsceneData, (id) =>
        {
            cutsceneDone = true;
            if (debugLogging)
                Debug.Log($"[HouseFlowController] Cutscene complete: {cutsceneID}");
            OnElementCompleted?.Invoke(ElementType.Cutscene, cutsceneID);
        });

        // Wait for cutscene animation to finish
        yield return new WaitUntil(() => cutsceneDone);
    }

    #endregion

    #region Inspector Buttons

    [Button("▶ Test Current Sequence")]
    private void TestSequence()
    {
        // This button requires a HouseSequenceData to be assigned
        // Use GameManager.StartHouse() in Play mode instead
        Debug.LogWarning("[HouseFlowController] Use GameManager.StartHouse() to test. This button is for reference only.");
    }

    #endregion
}
