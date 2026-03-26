using UnityEngine;

[CreateAssetMenu(
    fileName = "New FloatVariable",
    menuName = "KashkhaBot/Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    // #if UNITY_EDITOR
    // [Multiline]
    // [SerializeField] private string _developerDescription = "";
    // #endif

    [Tooltip("The value this variable resets to at the start of each run.")]
    [SerializeField] private float _initialValue;
    [SerializeField] private float _runtimeValue;

    public float Value
    {
        get => _runtimeValue;
        set => _runtimeValue = Mathf.Clamp01(value);
    }

    public void ResetToInitial() => _runtimeValue = _initialValue;

    public void ApplyDelta(float delta) => Value += delta;
}
