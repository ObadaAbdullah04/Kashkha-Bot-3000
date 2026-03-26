using UnityEngine;
using TMPro;

public class UIIntUpdater : MonoBehaviour
{
    [Tooltip("The int variable to read from (e.g., Eidia).")]
    [SerializeField] private IntVariable _targetVariable;
    
    [SerializeField] private TextMeshProUGUI _textDisplay;
    
    [Tooltip("Optional: Text to display before the number (e.g., 'Scrap: ').")]
    [SerializeField] private string _prefix = "";
    
    [Tooltip("Optional: Text to display after the number (e.g., ' JOD').")]
    [SerializeField] private string _suffix = "";

    public void UpdateUI()
    {
        if (_textDisplay != null && _targetVariable != null)
        {
            _textDisplay.text = $"{_prefix}{_targetVariable.Value}{_suffix}";
        }
    }
}