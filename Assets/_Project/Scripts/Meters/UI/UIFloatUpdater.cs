using UnityEngine;
using UnityEngine.UI;

public class UIFloatUpdater : MonoBehaviour
{
    [Tooltip("The float variable to read from (e.g., SocialBattery).")]
    [SerializeField] private FloatVariable _targetVariable;
    
    [Tooltip("The UI Image component. Make sure its Image Type is set to 'Filled'.")]
    [SerializeField] private Image _fillImage;

    public void UpdateUI()
    {
        if (_fillImage != null && _targetVariable != null)
        {
            // Our FloatVariables are already clamped 0-1, 
            // which perfectly matches Image.fillAmount!
            _fillImage.fillAmount = _targetVariable.Value;
        }
    }
}