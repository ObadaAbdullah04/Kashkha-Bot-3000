using System;
using System.Collections.Generic;
using UnityEngine;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 9.6 — Manages the swipe encounter system.
///
/// USAGE:
/// - HouseFlowController calls ShowSingleCard() for each question in the sequence
/// - Streak combos are tracked for bonus Eidia
/// - Pure coroutine-driven flow
///
/// KEY METHOD:
/// - ShowSingleCard(cardData, cardIndex, totalCards, onComplete): Shows a single card, waits for swipe/timeout
/// </summary>
public class SwipeEncounterManager : MonoBehaviour
{
    public static SwipeEncounterManager Instance { get; private set; }

    #region Inspector Fields

    [Header("Card Display")]
    [Tooltip("SwipeCard prefab to instantiate")]
    [SerializeField] private SwipeCard swipeCardPrefab;

    [Tooltip("Parent transform for card instances (center of stack)")]
    [SerializeField] private Transform cardParent;

    [Header("Timer UI")]
    [Tooltip("Timer slider for swipe decision")]
    [SerializeField] private UnityEngine.UI.Slider timerSlider;

    [Tooltip("Timer text display (RTLTextMeshPro)")]
    [SerializeField] private RTLTextMeshPro timerText;

    [Header("Card Counter UI")]
    [Tooltip("Text display showing current card progress (e.g., 'Card 1/5')")]
    [SerializeField] private RTLTextMeshPro cardCounterText;

    [Header("Timing Settings")]
    [Tooltip("Time limit per card swipe (seconds)")]
    [SerializeField] private float timePerCard = 8f;

    [Tooltip("Timer panic threshold (seconds) - text turns red below this")]
    [SerializeField] private float panicThreshold = 3f;

    [Header("Feedback Settings")]
    [Tooltip("Duration to wait after showing feedback before next card (seconds)")]
    [SerializeField] private float feedbackDelay = 1.5f;

    [Tooltip("Feedback text shown on timeout")]
    [SerializeField] private string timeoutFeedbackText = "تأخرت كثير!";

    [Header("Animation Settings")]
    [Tooltip("Card entrance animation duration")]
    [SerializeField] private float cardEntranceDuration = 0.5f;
    
    [Header("Audio Settings")]
    [Tooltip("Interval between panic tick SFX (seconds)")]
    [SerializeField] private float panicTickInterval = 0.5f;
    private float lastPainTickTime = 0f;

    #endregion

    #region Events

    /// <summary>
    /// Fires when a single card is swiped with stats.
    /// (batteryDelta, eidia, wasCorrect)
    /// </summary>
    public static Action<float, int, bool> OnCardProcessed;

    /// <summary>
    /// Fires when time runs out on a card.
    /// </summary>
    public static Action OnTimeRanOut;

    #endregion

    #region Private Fields

    private SwipeCard activeCard;
    private float timeRemaining;
    private bool isTimerRunning = false;
    private bool isProcessingSwipe = false;
    private int currentStreak = 0;
    private int streakBonusTotal = 0;
    private int currentCardIndex = 0;
    
    // Object pool for swipe cards
    private List<SwipeCard> cardPool = new List<SwipeCard>();
    private Transform poolParent;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Create pool parent
            poolParent = new GameObject("CardPool").transform;
            poolParent.SetParent(transform);
            poolParent.gameObject.SetActive(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timerSlider != null)
            timerSlider.value = Mathf.Clamp01(timeRemaining / timePerCard);

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

        // Panic color threshold
        if (timerText != null && timeRemaining < panicThreshold)
            timerText.color = Color.red;
        else if (timerText != null)
            timerText.color = Color.white;

        // Panic tick SFX (dynamic pitch based on time remaining)
        if (timeRemaining < panicThreshold && Time.time - lastPainTickTime >= panicTickInterval)
        {
            lastPainTickTime = Time.time;
            AudioManager.Instance?.PlayPanicTick(timeRemaining, panicThreshold);
        }

        // Timeout detection - invoke event!
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            StopTimer();
            OnTimeRanOut?.Invoke();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Shows a SINGLE swipe card. Called by HouseFlowController for each question.
    /// Card is shown, player swipes or times out, onComplete callback fires.
    /// </summary>
    public void ShowSingleCard(SwipeCardData cardData, int cardIndex, int totalCards, Action<float, int, bool> onComplete)
    {
        if (cardData == null)
        {
            Debug.LogError("[SwipeEncounterManager] CardData is null!");
            onComplete?.Invoke(0, 0, false);
            return;
        }

        if (isProcessingSwipe)
        {
            Debug.LogWarning("[SwipeEncounterManager] Already processing a card.");
            onComplete?.Invoke(0, 0, false);
            return;
        }

        isProcessingSwipe = true;
        currentCardIndex = cardIndex;

        // Update card counter UI
        UpdateCardCounter(cardIndex + 1, totalCards);

        activeCard = GetCardFromPool();
        activeCard.Setup(cardData, cardIndex, totalCards);

        if (activeCard.transform != null)
        {
            activeCard.transform.localScale = Vector3.zero;
            activeCard.transform.DOScale(Vector3.one, cardEntranceDuration)
                .SetEase(Ease.OutBack);
        }

        StartTimer();

        Action<SwipeCard, int> swipeHandler = null;
        Action timeoutHandler = null;

        timeoutHandler = () =>
        {
            if (!isProcessingSwipe) return; // Prevent double-invocation

            SwipeCard.OnCardSwiped -= swipeHandler;
            OnTimeRanOut -= timeoutHandler;
            StopTimer();

            float batteryDelta = cardData.IncorrectBatteryDelta;
            int eidiaReward = cardData.GetEidiaReward(false);

            MeterManager.Instance?.ModifyBattery(batteryDelta);
            CameraShakeManager.Instance?.ShakeWrongAnswer();

            activeCard?.ShowResultFeedback(false, timeoutFeedbackText);
            OnCardProcessed?.Invoke(batteryDelta, eidiaReward, false);

            DOTween.Sequence()
                .AppendInterval(feedbackDelay)
                .OnComplete(() =>
                {
                    isProcessingSwipe = false;
                    if (activeCard != null) { ReturnCardToPool(activeCard); activeCard = null; }
                    onComplete?.Invoke(batteryDelta, eidiaReward, false);
                });
        };

        swipeHandler = (card, direction) =>
        {
            if (!isProcessingSwipe) return; // Prevent double-invocation

            SwipeCard.OnCardSwiped -= swipeHandler;
            OnTimeRanOut -= timeoutHandler;
            StopTimer();

            bool swipedRight = direction > 0;
            SwipeCardData data = card.Data;
            bool wasCorrect = data.WasSwipeCorrect(swipedRight);
            float batteryDelta = data.GetBatteryDelta(swipedRight);
            int eidiaReward = data.GetEidiaReward(swipedRight);
            string feedback = data.GetFeedback(swipedRight);

            if (wasCorrect)
            {
                currentStreak++;
                int bonus = CalculateStreakBonus(currentStreak);
                if (bonus > 0) streakBonusTotal += bonus;
            }
            else
            {
                currentStreak = 0;
            }

            MeterManager.Instance?.ModifyBattery(batteryDelta);
            OnCardProcessed?.Invoke(batteryDelta, eidiaReward, wasCorrect);
            card.ShowResultFeedback(wasCorrect, feedback);

            if (eidiaReward > 0)
                FloatingTextManager.Instance?.SpawnEidiaReward(eidiaReward);

            DOTween.Sequence()
                .AppendInterval(feedbackDelay)
                .OnComplete(() =>
                {
                    isProcessingSwipe = false;
                    if (activeCard != null) { ReturnCardToPool(activeCard); activeCard = null; }
                    onComplete?.Invoke(batteryDelta, eidiaReward, wasCorrect);
                });
        };

        SwipeCard.OnCardSwiped += swipeHandler;
        OnTimeRanOut += timeoutHandler;
    }

    public int GetStreakBonus() => streakBonusTotal;

    #endregion

    #region Helpers

    /// <summary>
    /// Updates the card counter text display (e.g., "Card 1/5").
    /// </summary>
    private void UpdateCardCounter(int current, int total)
    {
        if (cardCounterText == null)
        {
            Debug.LogWarning("[SwipeEncounterManager] cardCounterText is not assigned! Please assign in inspector.");
            return;
        }

        if (total > 0)
        {
            cardCounterText.text = $"{current}/{total}";
#if UNITY_EDITOR
            Debug.Log($"[SwipeEncounterManager] Card counter updated: {cardCounterText.text}");
#endif
        }
        else
        {
            cardCounterText.text = "";
        }
    }

    private void StartTimer()
    {
        timeRemaining = timePerCard;
        isTimerRunning = true;
        if (timerSlider != null) { timerSlider.maxValue = timePerCard; timerSlider.value = 1f; }
        if (timerText != null) { timerText.text = Mathf.CeilToInt(timeRemaining).ToString(); timerText.color = Color.white; }
    }

    private void StopTimer()
    {
        isTimerRunning = false;
        AudioManager.Instance?.StopPanicTicks();
    }

    private int CalculateStreakBonus(int streak)
    {
        return streak switch { 2 => 3, 3 => 5, >= 4 => 8, _ => 0 };
    }

    /// <summary>
    /// Gets a card from the pool or creates a new one if pool is empty.
    /// </summary>
    private SwipeCard GetCardFromPool()
    {
        if (cardPool.Count > 0)
        {
            SwipeCard card = cardPool[0];
            cardPool.RemoveAt(0);
            card.gameObject.SetActive(true);
            card.transform.SetParent(cardParent);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one;
            card.transform.localRotation = Quaternion.identity;
            return card;
        }
        
        // Pool empty, create new
        return Instantiate(swipeCardPrefab, cardParent);
    }

    /// <summary>
    /// Returns a card to the pool for reuse.
    /// </summary>
    private void ReturnCardToPool(SwipeCard card)
    {
        if (card == null) return;
        
        card.gameObject.SetActive(false);
        card.transform.SetParent(poolParent);
        cardPool.Add(card);
    }

    /// <summary>
    /// Clears all cards from scene and returns them to pool.
    /// </summary>
    private void ClearCards()
    {
        if (cardParent == null) return;
        
        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            Transform child = cardParent.GetChild(i);
            SwipeCard card = child.GetComponent<SwipeCard>();
            if (card != null)
            {
                ReturnCardToPool(card);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        activeCard = null;
    }

    #endregion

    #region Inspector Buttons

    [Button("Test Single Card")]
    private void TestSingleCard()
    {
        var testData = new SwipeCardData
        {
            CardName = "خالة أم محمد",
            Speaker = "خالة أم محمد",
            QuestionAR = "تفضلي معمول مع الشاي!",
            OptionCorrectAR = "أكل بشكر",
            OptionWrongAR = "لا بشكر",
            RightIsCorrect = true,
            CorrectFeedbackAR = "قبلتِ الضيافة بأدب!",
            IncorrectFeedbackAR = "رفضتِ الضيافة - زعلت الخالة",
            CorrectBatteryDelta = -5f,
            IncorrectBatteryDelta = -15f,
            BaseEid = 10
        };

        ShowSingleCard(testData, 0, 1, (batteryDelta, eidia, wasCorrect) =>
        {
            Debug.Log($"[Test] Card complete: {(wasCorrect ? "CORRECT" : "INCORRECT")} | Battery: {batteryDelta}, Eidia: {eidia}");
        });
    }

    #endregion
}
