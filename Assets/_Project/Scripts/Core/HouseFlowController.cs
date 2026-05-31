using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// Orchestrates the ordered execution of elements within a house visit.
/// Driven by a coroutine-based state machine that ensures elements (Questions, Cinematics, Interactions)
/// are processed sequentially and respect terminal game states.
/// </summary>
public class HouseFlowController : MonoBehaviour
{
    public static HouseFlowController Instance { get; private set; }

    #region Inspector Fields

    [Header("System References")]
    [Tooltip("Reference to SwipeEncounterManager for processing question cards.")]
    [SerializeField] private SwipeEncounterManager swipeEncounterManager;

    [Tooltip("Reference to CinematicController for dialogue and timeline sequences.")]
    [SerializeField] private CinematicController cinematicController;

    [Tooltip("Reference to InteractionHUDController for QTE interactions.")]
    [SerializeField] private InteractionHUDController interactionHUDController;

    [Header("Timing")]
    [Tooltip("Small delay between sequence elements (seconds).")]
    [SerializeField] private float pauseBetweenElements = 0.5f;

    #endregion

    #region State

    private int currentHouseLevel = 0;
    private List<SequenceElement> currentSequence = new List<SequenceElement>();
    private bool isSequencePlaying = false;

    #endregion

    #region Events

    public static Action<int> OnHouseStarted;
    public static Action<int> OnHouseCompleted;
    public static Action<ElementType, string> OnElementCompleted;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnHouseStarted = null;
            OnHouseCompleted = null;
            OnElementCompleted = null;
            Instance = null;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Forcefully stops any active sequence coroutine and resets the controller state.
    /// </summary>
    public void CancelActiveSequence()
    {
        StopAllCoroutines();
        
        isSequencePlaying = false;
        currentHouseLevel = 0;
        currentSequence.Clear();

        if (cinematicController != null) 
            cinematicController.HideCutsceneUI();
    }

    /// <summary>
    /// Executes the full house visit sequence element by element.
    /// </summary>
    public IEnumerator PlayHouseSequence(int houseLevel, HouseSequenceData sequence)
    {
        if (isSequencePlaying)
        {
            yield break;
        }

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            OnHouseCompleted?.Invoke(houseLevel);
            yield break;
        }

        currentHouseLevel = houseLevel;
        currentSequence = new List<SequenceElement>(sequence.Sequence);
        isSequencePlaying = true;

        OnHouseStarted?.Invoke(houseLevel);

        int totalQuestions = 0;
        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (currentSequence[i].Type == ElementType.Question)
                totalQuestions++;
        }
        int questionIndex = 0;

        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Win))
            {
                yield break;
            }

            SequenceElement element = currentSequence[i];

            if (element == null || string.IsNullOrWhiteSpace(element.ElementID))
            {
                continue;
            }

            // // if (debugLogging) {} // Debug.Log($"[HouseFlowController] Element {i + 1}/{currentSequence.Count}: [{element.Type}] {element.ElementID}");

            // Trigger element and WAIT for completion
            switch (element.Type)
            {
                case ElementType.Question:
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                        cinematicController.ToggleDialogueBox(false);
                    }
                    
                    UIManager.Instance?.SetTimerVisibility(true);
                    yield return PlayQuestion(element.ElementID, questionIndex, totalQuestions);
                    questionIndex++;
                    break;

                case ElementType.Cinematic:
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                        cinematicController.ToggleDialogueBox(true);
                    }

                    UIManager.Instance?.SetTimerVisibility(false);
                    yield return PlayCinematic(element.ElementID);
                    break;

                case ElementType.Interaction:
                    if (cinematicController != null)
                    {
                        cinematicController.EnsurePortraitsVisible();
                    }

                    UIManager.Instance?.SetTimerVisibility(false);
                    yield return PlayInteraction(element.ElementID);
                    break;

                case ElementType.Video:
                    UIManager.Instance?.SetTimerVisibility(false);
                    yield return PlayVideo(element.ElementID);
                    break;

                default:
                    break;
            }

            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Win))
            {
                yield break;
            }

            if (TutorialOverlayManager.Instance != null && TutorialOverlayManager.Instance.IsTutorialActive)
            {
                yield return new WaitUntil(() => !TutorialOverlayManager.Instance.IsTutorialActive);
            }

            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Win))
            {
                yield break;
            }

            if (i < currentSequence.Count - 1 && pauseBetweenElements > 0)
            {
                yield return new WaitForSeconds(pauseBetweenElements);
            }
        }
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Encounter)
        {
             yield break;
        }

        if (cinematicController != null)
        {
            cinematicController.HideCutsceneUI();
        }

        isSequencePlaying = false;
        OnHouseCompleted?.Invoke(houseLevel);
    }

    /// <summary>
    /// Plays a full-screen video element.
    /// </summary>
    private IEnumerator PlayVideo(string videoName)
    {
        if (cinematicController == null)
        {
            OnElementCompleted?.Invoke(ElementType.Video, videoName);
            yield break;
        }

        bool videoDone = false;
        cinematicController.PlayVideo(videoName, (id) =>
        {
            videoDone = true;
            OnElementCompleted?.Invoke(ElementType.Video, videoName);
        });

        yield return new WaitUntil(() => videoDone);
    }

    #endregion

    #region Element Players

    /// <summary>
    /// Triggers a swipe card encounter and yields until the card is resolved.
    /// </summary>
    private IEnumerator PlayQuestion(string questionID, int questionIndex, int totalQuestions)
    {
        SwipeCardData questionData = DataManager.Instance?.GetQuestionByID(questionID);
        if (questionData == null)
        {
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        if (swipeEncounterManager == null)
        {
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
            yield break;
        }

        bool cardDone = false;
        swipeEncounterManager.ShowSingleCard(questionData, questionIndex, totalQuestions, (batteryDelta, eidia, wasCorrect) =>
        {
            cardDone = true;
            OnElementCompleted?.Invoke(ElementType.Question, questionID);
        });

        yield return new WaitUntil(() => cardDone);
    }

    /// <summary>
    /// Triggers a cinematic sequence (Timeline or DOTween) and yields until playback concludes.
    /// </summary>
    private IEnumerator PlayCinematic(string cinematicID)
    {
        if (cinematicController == null)
        {
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
            yield break;
        }

        var cinematicData = DataManager.Instance?.GetCinematicByID(cinematicID);
        if (cinematicData == null)
        {
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
            yield break;
        }

        bool cinematicDone = false;
        cinematicController.PlayCinematic(cinematicData, (id) =>
        {
            cinematicDone = true;
            OnElementCompleted?.Invoke(ElementType.Cinematic, cinematicID);
        });

        yield return new WaitUntil(() => cinematicDone);
    }

    /// <summary>
    /// Triggers a QTE interaction and yields until the interaction finishes.
    /// </summary>
    private IEnumerator PlayInteraction(string interactionID)
    {
        InteractionData interactionData = DataManager.Instance?.GetInteractionByID(interactionID);
        if (interactionData == null)
        {
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
            yield break;
        }

        if (interactionHUDController == null)
        {
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
            yield break;
        }

        bool interactionDone = false;
        interactionHUDController.RunInteraction(interactionData, (succeeded, batteryDelta, eidiaReward) =>
        {
            interactionDone = true;
            OnElementCompleted?.Invoke(ElementType.Interaction, interactionID);
        });

        yield return new WaitUntil(() => interactionDone);
    }

    #endregion

    #region Inspector Buttons

    [Button("Test Current Sequence")]
    private void TestSequence()
    {
        // Debug placeholder
    }

    #endregion
}
