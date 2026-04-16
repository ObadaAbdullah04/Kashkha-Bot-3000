using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;

/// <summary>
/// SUPER SIMPLIFIED Wardrobe UI - Phase 18
/// Manages exactly 4 choices: Default + 3 Outfits.
/// </summary>
public class WardrobeUI : MonoBehaviour
{
    [Header("Character Preview")]
    [SerializeField] private Image characterPreviewImage;
    [SerializeField] private Sprite defaultCharacterSprite;

    [Header("Outfit Buttons")]
    [SerializeField] private Button[] outfitButtons; // Expecting exactly 4 buttons (0=Default, 1, 2, 3)
    [SerializeField] private GameObject[] lockOverlays; // Overlays for locked outfits
    [SerializeField] private RTLTextMeshPro[] costTexts;

    [Header("UI Info")]
    [SerializeField] private RTLTextMeshPro scrapText;
    [SerializeField] private RTLTextMeshPro selectedOutfitNameText;

    private int _selectedID = 0;

    private void OnEnable()
    {
        RefreshUI();
        WardrobeManager.OnOutfitEquipped += RefreshUI;
        WardrobeManager.OnOutfitPurchased += RefreshUI;
        WardrobeManager.OnScrapChanged += RefreshUI;
        SaveManager.OnScrapChanged += HandleGlobalScrapChanged;
    }

    private void OnDisable()
    {
        WardrobeManager.OnOutfitEquipped -= RefreshUI;
        WardrobeManager.OnOutfitPurchased -= RefreshUI;
        WardrobeManager.OnScrapChanged -= RefreshUI;
        SaveManager.OnScrapChanged -= HandleGlobalScrapChanged;
    }

    private void HandleGlobalScrapChanged(int newTotal)
    {
        Debug.Log($"[WardrobeUI] HandleGlobalScrapChanged called with: {newTotal}");
        
        if (scrapText != null)
        {
            scrapText.text = newTotal.ToString();
            Debug.Log($"[WardrobeUI] Scrap text updated to: {newTotal}");
        }
        else
        {
            Debug.LogWarning("[WardrobeUI] scrapText is null!");
        }
        
        if (WardrobeManager.Instance != null)
        {
            WardrobeManager.Instance.SyncScrap();
        }
        else
        {
            Debug.LogWarning("[WardrobeUI] WardrobeManager.Instance is null!");
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (WardrobeManager.Instance == null) return;

        _selectedID = WardrobeManager.Instance.EquippedOutfitID;

        if (scrapText != null && WardrobeManager.Instance.AllOutfits.Count > 0)
        {
            scrapText.text = WardrobeManager.Instance.CurrentScrap.ToString();
        }
        else if (scrapText != null && SaveManager.Instance != null)
        {
            scrapText.text = SaveManager.Instance.CurrentData.TotalEidia.ToString();
        }

        // Update preview
        UpdatePreview();

        // Update buttons
        for (int i = 0; i < outfitButtons.Length; i++)
        {
            if (i >= WardrobeManager.Instance.AllOutfits.Count) break;

            OutfitData data = WardrobeManager.Instance.AllOutfits[i];
            int outfitID = data.ID;

            bool isOwned = WardrobeManager.Instance.OwnsOutfit(outfitID);
            bool isEquipped = (_selectedID == outfitID);

            // Setup button listener once
            int idCopy = outfitID;
            outfitButtons[i].onClick.RemoveAllListeners();
            outfitButtons[i].onClick.AddListener(() => OnOutfitClicked(idCopy));

            // Show lock only if it is actually locked AND not owned
            bool shouldShowLock = data.isLocked && !isOwned;
            if (i < lockOverlays.Length && lockOverlays[i] != null)
            {
                lockOverlays[i].SetActive(shouldShowLock);
            }

            if (i < costTexts.Length && costTexts[i] != null)
            {
                costTexts[i].text = (shouldShowLock && data.scrapCost > 0) ? $"{data.scrapCost} عيدية" : "";
            }

            // Update button sprite automatically
            if (outfitButtons[i].image != null)
            {
                if (!string.IsNullOrEmpty(data.spriteName))
                {
                    Sprite btnSprite = Resources.Load<Sprite>("CharacterSprites/" + data.spriteName);
                    if (btnSprite != null)
                    {
                        outfitButtons[i].image.sprite = btnSprite;
                        outfitButtons[i].image.enabled = true;
                    }
                    else
                    {
                        outfitButtons[i].image.enabled = false;
                        Debug.LogWarning($"[WardrobeUI] Sprite not found: CharacterSprites/{data.spriteName}");
                    }
                }
                else
                {
                    outfitButtons[i].image.enabled = false;
                }
            }

            // Visual highlight for equipped (using clear colors)
            if (outfitButtons[i].image != null)
            {
                // Ensure alpha is 1.0 (Color.white is 1,1,1,1)
                outfitButtons[i].image.color = isEquipped ? Color.green : Color.white;
            }
        }
    }

    public void OnOutfitClicked(int id)
    {
        if (WardrobeManager.Instance == null) return;

        OutfitData data = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == id);
        if (data == null) return;

        if (!WardrobeManager.Instance.OwnsOutfit(id) && id != 0)
        {
            // Try to purchase
            if (WardrobeManager.Instance.UnlockOutfit(id))
            {
                WardrobeManager.Instance.EquipOutfit(id);
            }
        }
        else
        {
            // Already owned, just equip
            WardrobeManager.Instance.EquipOutfit(id);
        }

        RefreshUI();
        
        // Bounce animation
        if (characterPreviewImage != null)
        {
            characterPreviewImage.transform.DOKill();
            characterPreviewImage.transform.localScale = Vector3.one;
            characterPreviewImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }

    private void UpdatePreview()
    {
        if (characterPreviewImage == null) return;

        OutfitData data = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == _selectedID);
        if (data != null && !string.IsNullOrEmpty(data.spriteName))
        {
            Sprite s = Resources.Load<Sprite>("CharacterSprites/" + data.spriteName);
            if (s != null)
            {
                characterPreviewImage.sprite = s;
                characterPreviewImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[WardrobeUI] Could not load sprite: CharacterSprites/{data.spriteName}");
                characterPreviewImage.sprite = defaultCharacterSprite;
            }
        }
        else
        {
            characterPreviewImage.sprite = defaultCharacterSprite;
        }

        // Update text
        if (selectedOutfitNameText != null)
        {
            selectedOutfitNameText.text = data != null ? data.displayNameAR : "الشكل الافتراضي";
        }
    }
}
