using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PHASE 18: Automatically switches backgrounds based on the current House Level.
/// Attach this to an Image component in the SwipeEncounterPanel.
/// </summary>
[RequireComponent(typeof(Image))]
public class HouseBackgroundController : MonoBehaviour
{
    private Image _backgroundImage;
    [SerializeField] private string backgroundsPath = "Backgrounds";
    [SerializeField] private Sprite defaultBackground;

    private void Awake()
    {
        _backgroundImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        HouseFlowController.OnHouseStarted += UpdateBackground;
        
        // Fallback: If enabled but house level is already set, update now
        if (GameManager.Instance != null && GameManager.Instance.CurrentHouseLevel > 0)
        {
            UpdateBackground(GameManager.Instance.CurrentHouseLevel);
        }
    }

    private void OnDisable()
    {
        HouseFlowController.OnHouseStarted -= UpdateBackground;
    }

    private void UpdateBackground(int houseLevel)
    {
        if (houseLevel <= 0)
        {
            if (defaultBackground != null) _backgroundImage.sprite = defaultBackground;
            return;
        }

        // Path: Resources/Backgrounds/House1_BG, House2_BG, etc.
        string resourceName = $"{backgroundsPath}/House{houseLevel}_BG";
        Sprite newBg = Resources.Load<Sprite>(resourceName);

        if (newBg != null)
        {
            _backgroundImage.sprite = newBg;
#if UNITY_EDITOR
            Debug.Log($"[HouseBackground] Loaded background for House {houseLevel}: {resourceName}");
#endif
        }
        else if (defaultBackground != null)
        {
            _backgroundImage.sprite = defaultBackground;
#if UNITY_EDITOR
            Debug.LogWarning($"[HouseBackground] Resource {resourceName} not found. Using default.");
#endif
        }
    }
}
