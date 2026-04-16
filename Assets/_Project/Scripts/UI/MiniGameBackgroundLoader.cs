using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PHASE 18: Smart Background Loader for Mini-Games.
/// Now supports specific game backgrounds (e.g. "Catch_BG") 
/// and can still fall back to house-based backgrounds.
/// </summary>
public class MiniGameBackgroundLoader : MonoBehaviour
{
    private Image _uiImage;
    private SpriteRenderer _spriteRenderer;
    
    [Header("Settings")]
    [Tooltip("Folder in Resources")]
    [SerializeField] private string backgroundsPath = "Backgrounds";

    [Tooltip("Optional: Specific name (e.g. 'Catch_BG'). If empty, uses 'HouseX_BG'")]
    [SerializeField] private string specificBackgroundName = "";

    [SerializeField] private Sprite defaultBackground;

    private void Awake()
    {
        _uiImage = GetComponent<Image>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Updates the background.
    /// </summary>
    public void Initialize(int houseLevel)
    {
        string resourceName;

        if (!string.IsNullOrEmpty(specificBackgroundName))
        {
            // Use the specific name provided in Inspector
            resourceName = $"{backgroundsPath}/{specificBackgroundName}";
        }
        else
        {
            // Default to house-based naming
            resourceName = $"{backgroundsPath}/House{houseLevel}_BG";
        }

        Sprite newBg = Resources.Load<Sprite>(resourceName);

        if (newBg == null) newBg = defaultBackground;
        if (newBg == null) return;

        if (_uiImage != null)
        {
            _uiImage.sprite = newBg;
            _uiImage.enabled = true;
            
            // PHASE 18: Ensure UI background fills the screen
            RectTransform rect = _uiImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                // Keep it at Z=0 but manage via Sorting Order
                rect.localPosition = new Vector3(0, 0, 0); 
            }

            // PHASE 18: Use nested Canvas for absolute sorting
            // This ensures the background is BEHIND the world obstacles (at 50)
            // but still part of the mini-game hierarchy.
            Canvas nestedCanvas = _uiImage.gameObject.GetComponent<Canvas>();
            if (nestedCanvas == null) nestedCanvas = _uiImage.gameObject.AddComponent<Canvas>();
            
            nestedCanvas.overrideSorting = true;
            nestedCanvas.sortingOrder = 40; // Below world objects (50) and UI (70)
            
            // Required for UI interaction/rendering to work properly on nested canvas
            if (_uiImage.gameObject.GetComponent<GraphicRaycaster>() == null)
                _uiImage.gameObject.AddComponent<GraphicRaycaster>();

            Debug.Log($"[BackgroundLoader] Image Background configured with SortingOrder 40");
        }
        
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = newBg;
            _spriteRenderer.enabled = true;
            
            // For world backgrounds, use a very low order but still within range
            _spriteRenderer.sortingOrder = 40;
            
            FitToScreen(_spriteRenderer);
            
            // Re-run after a frame to ensure camera is fully initialized
            Invoke(nameof(ReFit), 0.1f);
        }
    }

    private void ReFit()
    {
        if (_spriteRenderer != null) FitToScreen(_spriteRenderer);
    }

    private void FitToScreen(SpriteRenderer sr)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        // Position it at the camera center
        // Z=10 is standard world distance from camera at Z=-10
        sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
        
        float worldScreenHeight = cam.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight * cam.aspect;

        if (sr.sprite != null)
        {
            sr.transform.localScale = new Vector3(
                worldScreenWidth / sr.sprite.bounds.size.x,
                worldScreenHeight / sr.sprite.bounds.size.y,
                1
            );
        }
    }
}
