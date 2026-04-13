using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;

/// <summary>
/// UI component for a single outfit slot in the Wardrobe.
/// Handles display, purchase button, and equip/unequip interactions.
/// </summary>
public class OutfitSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private RTLTextMeshPro nameText;
    [SerializeField] private RTLTextMeshPro descriptionText;
    [SerializeField] private RTLTextMeshPro costText;
    [SerializeField] private Button actionButton;
    [SerializeField] private GameObject ownedCheckmark;
    [SerializeField] private GameObject equippedCheckmark;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 1f);

    [Header("Settings")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.2f, 0.2f);

    [Header("Animation Settings")]
    [SerializeField] private float popInDuration = 0.4f;
    [SerializeField] private Ease popInEase = Ease.OutBack;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.2f;

    private OutfitData _currentOutfit;
    private bool _isOwned;
    private bool _isEquipped;
    private Tween _hoverTween;
    private RectTransform _rectTransform;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);

        // Cache RectTransform for animations
        _rectTransform = GetComponent<RectTransform>();

        // Add hover effect to button
        if (actionButton != null)
        {
            var enterEvent = new UnityEngine.Events.UnityEvent();
            enterEvent.AddListener(OnHoverEnter);
            var exitEvent = new UnityEngine.Events.UnityEvent();
            exitEvent.AddListener(OnHoverExit);

            // Note: Unity UI Button doesn't have built-in hover events
            // We'll use EventTrigger instead or check in Update
        }
    }

    private void OnEnable()
    {
        // Play pop-in animation
        if (_rectTransform != null)
        {
            _rectTransform.DOKill();
            _rectTransform.localScale = Vector3.zero;
            _rectTransform.DOScale(Vector3.one, popInDuration).SetEase(popInEase).SetUpdate(true);
        }
    }

    private void OnHoverEnter()
    {
        if (_rectTransform != null)
        {
            _hoverTween?.Kill();
            _hoverTween = _rectTransform.DOScale(Vector3.one * hoverScale, hoverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    private void OnHoverExit()
    {
        if (_rectTransform != null)
        {
            _hoverTween?.Kill();
            _hoverTween = _rectTransform.DOScale(Vector3.one, hoverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        // Reset state when disabled
        if (actionButton != null)
            actionButton.interactable = true;
    }

    /// <summary>
    /// Initializes the slot with outfit data and player state.
    /// </summary>
    public void Initialize(OutfitData outfit, bool isOwned, bool isEquipped, int playerScrap)
    {
        _currentOutfit = outfit;
        _isOwned = isOwned;
        _isEquipped = isEquipped;

        if (nameText != null)
            nameText.text = outfit.displayNameAR;

        if (descriptionText != null)
            descriptionText.text = outfit.descriptionAR;

        if (ownedCheckmark != null)
            ownedCheckmark.SetActive(isOwned);

        if (equippedCheckmark != null)
            equippedCheckmark.SetActive(isEquipped);

        // Update button text and color based on state
        UpdateActionButton(playerScrap);

        // Apply rarity color to border/name
        ApplyRarityColor(outfit.rarity);

        // Load icon sprite (from Resources folder)
        if (iconImage != null && !string.IsNullOrEmpty(outfit.iconSpritePath))
        {
            Sprite icon = Resources.Load<Sprite>(outfit.iconSpritePath);
            if (icon != null)
                iconImage.sprite = icon;
            else
                Debug.LogWarning($"[OutfitSlot] Icon not found: {outfit.iconSpritePath}");
        }
    }

    /// <summary>
    /// Updates the action button based on ownership and affordance.
    /// </summary>
    public void UpdateActionButton(int playerScrap)
    {
        if (actionButton == null || costText == null) return;

        if (_isOwned)
        {
            // Owned: Show Equip/Unequip button
            costText.text = _isEquipped ? "مجهز" : "جهّز";
            actionButton.interactable = !_isEquipped; // Can't equip if already equipped
            costText.color = _isEquipped ? Color.gray : affordableColor;
        }
        else
        {
            // Not owned: Show Purchase button
            costText.text = $"{_currentOutfit.scrapCost} خردة";
            actionButton.interactable = playerScrap >= _currentOutfit.scrapCost;
            costText.color = actionButton.interactable ? affordableColor : unaffordableColor;
        }
    }

    /// <summary>
    /// Applies rarity color to the slot border/name.
    /// </summary>
    private void ApplyRarityColor(OutfitRarity rarity)
    {
        Color rarityColor = rarity switch
        {
            OutfitRarity.Common => commonColor,
            OutfitRarity.Rare => rareColor,
            OutfitRarity.Epic => epicColor,
            _ => commonColor
        };

        if (nameText != null)
            nameText.color = rarityColor;
    }

    private void OnActionButtonClicked()
    {
        if (_currentOutfit == null) return;

        if (_isOwned)
        {
            // Equip this outfit
            Debug.Log($"[OutfitSlot] Equipping outfit: {_currentOutfit.displayNameAR} (ID: {_currentOutfit.ID})");
            if (WardrobeManager.Instance != null)
                WardrobeManager.Instance.EquipOutfit(_currentOutfit.ID);
        }
        else
        {
            // Purchase this outfit
            Debug.Log($"[OutfitSlot] Purchasing outfit: {_currentOutfit.displayNameAR} (ID: {_currentOutfit.ID}, Cost: {_currentOutfit.scrapCost})");
            if (WardrobeManager.Instance != null)
                WardrobeManager.Instance.PurchaseOutfit(_currentOutfit.ID);
        }
    }

    /// <summary>
    /// Refreshes the slot state (called after purchase/equip events).
    /// </summary>
    public void Refresh(int playerScrap, int equippedOutfitID)
    {
        if (_currentOutfit == null) return;

        _isOwned = WardrobeManager.Instance.OwnsOutfit(_currentOutfit.ID);
        _isEquipped = equippedOutfitID == _currentOutfit.ID;

        Initialize(_currentOutfit, _isOwned, _isEquipped, playerScrap);
    }
}
