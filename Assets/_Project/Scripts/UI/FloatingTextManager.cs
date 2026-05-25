using UnityEngine;
using RTLTMPro;
using DG.Tweening;

/// <summary>
/// Manages spawning of floating text (e.g., +10, -5) over UI elements.
/// Part of Phase 2: HUD & UI Clarity.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private float floatDistance = 60f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private Ease moveEase = Ease.OutQuint;
    [SerializeField] private Ease fadeEase = Ease.InQuint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // No DontDestroyOnLoad needed if it's placed on the UIManager canvas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawns a floating text at the specified screen position.
    /// </summary>
    public void SpawnText(string text, Vector2 screenPosition, Color color)
    {
        if (textPrefab == null) return;

        GameObject go = Instantiate(textPrefab, transform);
        go.transform.position = screenPosition;

        RTLTextMeshPro tm = go.GetComponentInChildren<RTLTextMeshPro>();
        if (tm != null)
        {
            tm.text = text;
            tm.color = color;
        }

        // Animation
        go.transform.DOMoveY(screenPosition.y + floatDistance, duration).SetEase(moveEase);
        
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        
        cg.DOFade(0, duration).SetEase(fadeEase).OnComplete(() => Destroy(go));
        
        // Punch scale for extra juice
        go.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
    }

    /// <summary>
    /// Spawns floating text over a specific RectTransform.
    /// </summary>
    public void SpawnTextOverUI(string text, RectTransform target, Color color)
    {
        if (target == null) return;
        SpawnText(text, target.position, color);
    }
}
