using System;
using UnityEngine;

[Serializable]
public class FloatReference
{
    [Tooltip("If true, uses the Constant Value below. " +
             "If false, reads from the Variable asset.")]
    public bool UseConstant = true;

    public float ConstantValue;

    [SerializeField] private FloatVariable _variable;

    public float Value => UseConstant ? ConstantValue : _variable.Value;
}