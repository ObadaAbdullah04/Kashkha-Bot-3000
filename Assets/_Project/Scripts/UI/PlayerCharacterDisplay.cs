using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PHASE 18: Displays the player's currently selected outfit.
/// Attach this to an Image component in the HUD/Encounters to show the player character.
/// </summary>
[RequireComponent(typeof(Image))]
public class PlayerCharacterDisplay : MonoBehaviour
{
    private Image _image;
    [SerializeField] private Sprite defaultSprite;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        UpdateDisplay();
        WardrobeManager.OnOutfitEquipped += UpdateDisplay;
    }

    private void OnDisable()
    {
        WardrobeManager.OnOutfitEquipped -= UpdateDisplay;
    }

    public void UpdateDisplay()
    {
        if (WardrobeManager.Instance == null) return;

        int id = WardrobeManager.Instance.EquippedOutfitID;
        OutfitData data = WardrobeManager.Instance.AllOutfits.Find(o => o.ID == id);

        if (data != null && !string.IsNullOrEmpty(data.spriteName))
        {
            Sprite s = Resources.Load<Sprite>("CharacterSprites/" + data.spriteName);
            if (s != null)
            {
                _image.sprite = s;
                _image.enabled = true;
            }
            else
            {
                // Debug.LogWarning($"[PlayerCharacterDisplay] Sprite not found in Resources: CharacterSprites/{data.spriteName}");
                _image.sprite = defaultSprite;
            }
        }
        else
        {
            _image.sprite = defaultSprite;
        }
    }
}
