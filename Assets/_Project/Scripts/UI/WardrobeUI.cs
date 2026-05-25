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
    [SerializeField] private RTLTextMeshPro scrapText; // Restored field name to keep Inspector reference
    [SerializeField] private RTLTextMeshPro selectedOutfitNameText;

    private int _selectedID = 0;

    private void OnEnable()
    {
        RefreshUI();
        WardrobeManager.OnOutfitEquipped += RefreshUI;
        WardrobeManager.OnOutfitPurchased += RefreshUI;
        WardrobeManager.OnScrapChanged += RefreshUI;
        SaveManager.OnEidiaChanged += HandleGlobalCurrencyChanged;
    }

    private void OnDisable()
    {
        WardrobeManager.OnOutfitEquipped -= RefreshUI;
        WardrobeManager.OnOutfitPurchased -= RefreshUI;
        WardrobeManager.OnScrapChanged -= RefreshUI;
        SaveManager.OnEidiaChanged -= HandleGlobalCurrencyChanged;
    }

    private void HandleGlobalCurrencyChanged(int newTotal)
    {
        if (scrapText != null) scrapText.text = newTotal.ToString();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (WardrobeManager.Instance == null) return;

        _selectedID = WardrobeManager.Instance.EquippedOutfitID;

        // Display Player Eidia balance (using restored field name scrapText)
        if (scrapText != null && SaveManager.Instance != null)
        {
            scrapText.text = SaveManager.Instance.CurrentData.TotalEidia.ToString();
        }

        // Update preview image
        UpdatePreview();

        // Update buttons
        for (int i = 0; i < outfitButtons.Length; i++)
        {
            if (i >= WardrobeManager.Instance.AllOutfits.Count) break;

            // Register the first slot as a target for the tutorial
            if (i == 0 && TutorialOverlayManager.Instance != null && outfitButtons[0] != null)
            {
                TutorialOverlayManager.Instance.RegisterTarget("FirstOutfitSlot", outfitButtons[0].GetComponent<RectTransform>()); 
            }

            OutfitData data = WardrobeManager.Instance.AllOutfits[i];
            int outfitID = data.ID;

            bool isOwned = WardrobeManager.Instance.OwnsOutfit(outfitID);
            bool isEquipped = (outfitID == _selectedID);

            // Click listener
            int idCopy = outfitID;
            outfitButtons[i].onClick.RemoveAllListeners();
            outfitButtons[i].onClick.AddListener(() => OnOutfitClicked(idCopy));

            // Show lock only if it is actually locked AND not owned
            bool shouldShowLock = data.isLocked && !isOwned;
            if (i < lockOverlays.Length && lockOverlays[i] != null)
            {
                lockOverlays[i].SetActive(shouldShowLock);
            }

            // Update button icon
            if (outfitButtons[i].image != null)
            {
                if (!string.IsNullOrEmpty(data.spriteName))
                {
                    Sprite btnSprite = Resources.Load<Sprite>($"CharacterSprites/{data.spriteName}");
                    if (btnSprite != null)
                    {
                        outfitButtons[i].image.sprite = btnSprite;
                        outfitButtons[i].image.enabled = true;
                    }
                    else
                    {
                        outfitButtons[i].image.enabled = false;
                    }
                }
                else
                {
                    outfitButtons[i].image.enabled = false;
                }

                // Highlight equipped button
                outfitButtons[i].image.color = isEquipped ? new Color(0.8f, 1f, 0.8f) : Color.white;
            }

            // Update cost / state text
            if (i < costTexts.Length && costTexts[i] != null)
            {
                if (isEquipped)
                {
                    costTexts[i].text = "مرتدي";
                    costTexts[i].color = new Color(0.2f, 0.8f, 0.2f); // Green
                }
                else if (isOwned)
                {
                    costTexts[i].text = "مملوك";
                    costTexts[i].color = Color.white;
                }
                else
                {
                    costTexts[i].text = $"{data.scrapCost} عيدية";
                    bool canAfford = SaveManager.Instance != null && SaveManager.Instance.CurrentData.TotalEidia >= data.scrapCost;
                    costTexts[i].color = canAfford ? Color.green : Color.red;
                }
            }
        }
    }

    private void OnOutfitClicked(int id)
    {
        // Play click sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.SFXType.ButtonClick);
        }

        // Find the button for juice
        Button clickedButton = null;
        for (int i = 0; i < outfitButtons.Length; i++)
        {
            if (i < WardrobeManager.Instance.AllOutfits.Count && WardrobeManager.Instance.AllOutfits[i].ID == id)
            {
                clickedButton = outfitButtons[i];
                break;
            }
        }

        if (WardrobeManager.Instance.OwnsOutfit(id))
        {
            if (WardrobeManager.Instance.EquipOutfit(id))
            {
                // Juice: Punch scale on successful equip
                clickedButton?.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f).SetUpdate(true);
            }
        }
        else
        {
            // Attempt to buy
            if (WardrobeManager.Instance.UnlockOutfit(id))
            {
                WardrobeManager.Instance.EquipOutfit(id);
                // Juice: Punch scale on successful purchase
                clickedButton?.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f).SetUpdate(true);
            }
            else
            {
                // Visual feedback: Shake the button if can't afford
                clickedButton?.transform.DOShakePosition(0.3f, 10f).SetUpdate(true);
            }
        }

        // Juice for the preview character
        if (characterPreviewImage != null)
        {
            characterPreviewImage.transform.DOKill();
            characterPreviewImage.transform.localScale = Vector3.one;
            characterPreviewImage.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f).SetUpdate(true);
        }

        RefreshUI();
    }

    private void UpdatePreview()
    {
        OutfitData data = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == _selectedID);

        // Update preview image
        if (characterPreviewImage != null)
        {
            if (data != null && !string.IsNullOrEmpty(data.spriteName))
            {
                // Try load sprite from Resources/CharacterSprites/
                Sprite s = Resources.Load<Sprite>($"CharacterSprites/{data.spriteName}");
                if (s != null)
                {
                    characterPreviewImage.sprite = s;
                    characterPreviewImage.gameObject.SetActive(true);
                }
                else
                {
                    characterPreviewImage.sprite = defaultCharacterSprite;
                }
            }
            else
            {
                characterPreviewImage.sprite = defaultCharacterSprite;
            }
        }

        // Update name text
        if (selectedOutfitNameText != null)
        {
            selectedOutfitNameText.text = data != null ? data.displayNameAR : "الشكل الافتراضي";
        }
    }
}
