using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// World-space mini-game where the player moves a basket to catch falling items.
/// </summary>
public class CatchMiniGame : MonoBehaviour
{
    #region Singleton

    public static CatchMiniGame Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    #endregion

    [Header("Game Duration")]
    [Tooltip("Total time for the mini-game (seconds). Hardcoded here to ignore external values.")]
    [SerializeField] private float gameDuration = 30f;

    [Header("Movement Settings")]
    [Tooltip("Player basket horizontal movement speed (world units per second).")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Input")]
    [Tooltip("Input Action for horizontal movement.")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Spawner Progressive Scaling")]
    [Tooltip("Item spawn interval at the START of the game.")]
    [SerializeField] private float startSpawnInterval = 1.0f;
    [Tooltip("Item spawn interval at the END of the game.")]
    [SerializeField] private float endSpawnInterval = 0.4f;

    [Space]
    [Tooltip("Item fall speed at the START of the game.")]
    [SerializeField] private float startFallSpeed = 6f;
    [Tooltip("Item fall speed at the END of the game.")]
    [SerializeField] private float endFallSpeed = 12f;

    [Space]
    [Tooltip("Chance to spawn two items at once at the END of the game.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxMultipleSpawnChance = 0.4f;

    [Header("World Space References")]
    [Tooltip("Visual prefab for the player's basket.")]
    [SerializeField] private GameObject playerBasketPrefab;

    [Tooltip("The fixed vertical position of the basket.")]
    [SerializeField] private float _playerY = -3f;

    [Tooltip("Empty parent transform to keep the scene hierarchy clean.")]
    [SerializeField] private Transform itemsParent;

    [Tooltip("Positive reward prefab (Eidia).")]
    [SerializeField] private GameObject fallingItemPrefab;

    [Header("Feedback Settings")]
    [SerializeField] private Vector3 catchPunchScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private float catchPunchDuration = 0.3f;

    [Header("Reward Balancing")]
    [Tooltip("Calculated scrap conversion rate from game score.")]
    [SerializeField] private float scrapPerPoint = 0.5f;

    private Transform playerBasket;

    [Header("UI Overlays (Text Only)")]
    [SerializeField] private RTLTMPro.RTLTextMeshPro timerText;
    [SerializeField] private RTLTMPro.RTLTextMeshPro scoreText;

    private float _minX;
    private float _maxX;
    private float _spawnY;
    private float _destroyY;

    private float _timeRemaining;
    private int _lastCachedSecond = -1;
    private int _score = 0;
    private bool _isPlaying = false;
    private float _spawnTimer = 0f;

    private List<Transform> _activeItems = new List<Transform>();

    private GameObject _spawnedPlayerBasket;
    private SpriteRenderer _playerSpriteRenderer;

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }

        if (playerBasket != null)
        {
            playerBasket.DOKill();
        }

        foreach (var item in _activeItems)
        {
            if (item != null)
                item.DOKill();
        }

        if (_spawnedPlayerBasket != null)
        {
            Destroy(_spawnedPlayerBasket);
        }

        _activeItems.Clear();
    }

    private void Start()
    {
        CalculateWorldBoundaries();
    }

    /// <summary>
    /// Calculates the horizontal movement constraints based on the camera's viewport and player sprite size.
    /// </summary>
    private void CalculateWorldBoundaries()
    {
        if (Camera.main == null)
        {
            _minX = -4f;
            _maxX = 4f;
            _spawnY = 6f;
            _destroyY = -7f;
            return;
        }

        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane));

        float playerHalfWidth = 0.5f; 
        SpriteRenderer sr = null;

        if (playerBasket != null)
        {
            sr = playerBasket.GetComponent<SpriteRenderer>();
        }
        else if (playerBasketPrefab != null)
        {
            sr = playerBasketPrefab.GetComponent<SpriteRenderer>();
        }

        if (sr != null && sr.sprite != null)
        {
            playerHalfWidth = sr.sprite.bounds.extents.x;
        }

        _minX = leftEdge.x + playerHalfWidth;
        _maxX = rightEdge.x - playerHalfWidth;

        _spawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1.1f, 0)).y;
        _destroyY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.1f, 0)).y;
    }

    /// <summary>
    /// Starts the mini-game, setting the timer and spawning the player basket.
    /// Note: The passed duration is ignored in favor of the local hardcoded gameDuration.
    /// </summary>
    public void Initialize(float ignoredDuration)
    {
        _timeRemaining = gameDuration;
        _score = 0;
        _isPlaying = true;
        _spawnTimer = 0f;
        _activeItems.Clear();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableAction("MoveHorizontal");
            InputManager.Instance.EnableAction("TouchPosition");
            InputManager.Instance.EnableAction("TouchStart");
        }

        if (playerBasketPrefab != null)
        {
            GameObject basketGo = Instantiate(playerBasketPrefab, new Vector3(0, _playerY, 0), Quaternion.identity);
            playerBasket = basketGo.transform;
            _spawnedPlayerBasket = basketGo;
            _playerSpriteRenderer = basketGo.GetComponentInChildren<SpriteRenderer>();
        }

        if (scoreText != null)
            scoreText.text = "0";
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _timeRemaining -= Time.deltaTime;

        int currentSecond = Mathf.CeilToInt(_timeRemaining);
        if (currentSecond != _lastCachedSecond)
        {
            _lastCachedSecond = currentSecond;
            if (timerText != null)
                timerText.text = _lastCachedSecond.ToString();
        }

        if (_timeRemaining > 0f)
        {
            float difficultyProgress = 1f - (_timeRemaining / gameDuration);
            float currentSpawnInterval = Mathf.Lerp(startSpawnInterval, endSpawnInterval, difficultyProgress);

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= currentSpawnInterval)
            {
                _spawnTimer = 0f;
                
                // Spawn one or two items based on progress
                SpawnItem(difficultyProgress);
                
                float multiSpawnChance = Mathf.Lerp(0, maxMultipleSpawnChance, difficultyProgress);
                if (Random.value < multiSpawnChance)
                {
                    SpawnItem(difficultyProgress);
                }
            }
        }
        else
        {
            _timeRemaining = 0f;
            EndGame();
            return;
        }

        HandlePlayerMovement();
        UpdateFallingItems();
    }

    /// <summary>
    /// Processes movement inputs, prioritizing direct touch follow on mobile with snappy responsiveness.
    /// </summary>
    private void HandlePlayerMovement()
    {
        if (playerBasket == null) return;
        if (InputManager.Instance == null) return;

        float moveInput = 0f;
        bool isDirectTouch = false;
        float targetWorldX = playerBasket.position.x;

        if (InputManager.Instance.IsTouching())
        {
            isDirectTouch = true;
            Vector2 screenPos = InputManager.Instance.GetTouchPosition();
            
            if (Camera.main != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
                targetWorldX = worldPos.x;
            }
        }
        else
        {
            moveInput = InputManager.Instance.GetMoveHorizontalValue().x;
        }

        float currentX = playerBasket.position.x;
        float nextX = currentX;

        if (isDirectTouch)
        {
            const float TOUCH_MOVE_MULTIPLIER = 1.5f; 
            nextX = Mathf.MoveTowards(currentX, targetWorldX, moveSpeed * TOUCH_MOVE_MULTIPLIER * Time.deltaTime);
            moveInput = (nextX - currentX) / Time.deltaTime;
        }
        else if (moveInput != 0f)
        {
            nextX = currentX + (moveInput * moveSpeed * Time.deltaTime);
        }

        nextX = Mathf.Clamp(nextX, _minX, _maxX);

        if (_playerSpriteRenderer != null)
        {
            if (moveInput > 0.1f) _playerSpriteRenderer.flipX = false; 
            else if (moveInput < -0.1f) _playerSpriteRenderer.flipX = true; 
        }

        playerBasket.position = new Vector3(nextX, playerBasket.position.y, playerBasket.position.z);
    }

    /// <summary>
    /// Spawns a new falling item at a random horizontal position.
    /// Now supports progressive fall speed.
    /// </summary>
    private void SpawnItem(float difficultyProgress)
    {
        if (fallingItemPrefab == null)
        {
            return;
        }

        float randomX = Random.Range(_minX, _maxX);
        Vector3 spawnPos = new Vector3(randomX, _spawnY, 0f);

        GameObject newItem = Instantiate(fallingItemPrefab, spawnPos, Quaternion.identity);
        
        // Apply progressive fall speed
        FallingItem itemScript = newItem.GetComponent<FallingItem>();
        if (itemScript != null)
        {
            float currentFallSpeed = Mathf.Lerp(startFallSpeed, endFallSpeed, difficultyProgress);
            itemScript.SetSpeed(currentFallSpeed);
        }

        SpriteRenderer sr = newItem.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
        }

        Vector3 originalScale = newItem.transform.localScale;
        newItem.transform.localScale = Vector3.zero;
        newItem.transform.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack);

        _activeItems.Add(newItem.transform);
    }

    private void UpdateFallingItems()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            if (_activeItems[i] == null)
            {
                _activeItems.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Callback from falling items when they collide with the basket.
    /// Triggers scoring, SFX, and visual feedback.
    /// </summary>
    public void OnItemCaught()
    {
        if (!_isPlaying) return;

        _score++;
        if (scoreText != null)
        {
            scoreText.text = _score.ToString();
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        if (playerBasket != null)
        {
            playerBasket.DOKill();
            playerBasket.localScale = Vector3.one;
            playerBasket.DOPunchScale(catchPunchScale, catchPunchDuration).SetUpdate(true);
        }

        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.CatchGood);
    }

    /// <summary>
    /// Ends the mini-game session, calculates rewards, and notifies the manager.
    /// </summary>
    private void EndGame()
    {
        _isPlaying = false;

        if (timerText != null)
        {
            timerText.text = "00";
            timerText.color = Color.red;
            timerText.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f).SetUpdate(true);
        }

        DOVirtual.DelayedCall(1.5f, () => {
            int scrapReward = _score > 0 ? Mathf.Max(1, Mathf.FloorToInt(_score * scrapPerPoint)) : 0;

            if (MiniGameManager.Instance != null)
                MiniGameManager.Instance.EndMiniGame(_score, scrapReward);

            Destroy(gameObject);
        }).SetUpdate(true);
    }
}
