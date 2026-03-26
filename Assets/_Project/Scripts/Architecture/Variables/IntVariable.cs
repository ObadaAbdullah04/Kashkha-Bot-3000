using UnityEngine;

[CreateAssetMenu(
    fileName = "New IntVariable",
    menuName = "KashkhaBot/Variables/Int Variable")]
public class IntVariable : ScriptableObject
{
    // #if UNITY_EDITOR
    // [Multiline]
    // [SerializeField] private string _developerDescription = "";
    // #endif

    [Tooltip("The value this variable resets to at the start of each run.")]
    [SerializeField] private int _initialValue;
    [SerializeField] private int _runtimeValue;

    public int Value
    {
        get => _runtimeValue;
        set => _runtimeValue = Mathf.Max(0, value);
    }

    public void ResetToInitial() => _runtimeValue = _initialValue;

    public void ApplyDelta(int delta) => Value += delta;
}
