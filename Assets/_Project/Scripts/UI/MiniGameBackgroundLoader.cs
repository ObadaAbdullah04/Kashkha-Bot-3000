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
        }
        
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = newBg;
            _spriteRenderer.enabled = true;
            FitToScreen(_spriteRenderer);
        }
    }

    private void FitToScreen(SpriteRenderer sr)
    {
        if (Camera.main == null) return;

        sr.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 10f);
        
        float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        sr.transform.localScale = new Vector3(
            worldScreenWidth / sr.sprite.bounds.size.x,
            worldScreenHeight / sr.sprite.bounds.size.y,
            1
        );
    }
}
