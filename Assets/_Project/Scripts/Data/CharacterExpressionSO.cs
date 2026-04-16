using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Character Expression ScriptableObject for cutscene system.
/// 
/// USAGE:
/// 1. Create: Right-click in Project window → Create → Character Expression
/// 2. Add expressions (Happy, Sad, Angry, Neutral, etc.) with corresponding sprites
/// 3. Assign to CinematicController's expression list (if using character sprites in cinematics)
/// 4. Reference by name in CSV or code: "Happy", "Angry", etc.
/// 
/// BENEFITS:
/// - Data-driven expression management
/// - Reusable across multiple characters
/// - Easy to add new expressions without code changes
/// - Inspector-friendly
/// </summary>
[CreateAssetMenu(fileName = "CharacterExpression", menuName = "Kashkha/Character Expression", order = 1)]
public class CharacterExpressionSO : ScriptableObject
{
    [Header("Character Identity")]
    [Tooltip("Character name (e.g., 'خالة أم محمد', 'العم أبو محمد')")]
    public string characterName;

    [Tooltip("Character portrait sprite (default/neutral expression)")]
    public Sprite defaultSprite;

    [Header("Expressions")]
    [Tooltip("List of character expressions with names and sprites")]
    public List<Expression> expressions = new List<Expression>();

    /// <summary>
    /// Gets sprite by expression name.
    /// </summary>
    /// <param name="expressionName">Expression name (case-insensitive)</param>
    /// <returns>Sprite if found, null otherwise</returns>
    public Sprite GetExpressionSprite(string expressionName)
    {
        if (string.IsNullOrEmpty(expressionName))
            return defaultSprite;

        foreach (var expr in expressions)
        {
            if (expr.name.Equals(expressionName, StringComparison.OrdinalIgnoreCase))
            {
                return expr.sprite != null ? expr.sprite : defaultSprite;
            }
        }

        // Debug.LogWarning($"[CharacterExpressionSO] Expression '{expressionName}' not found for '{characterName}'. Using default.");
        return defaultSprite;
    }

    /// <summary>
    /// Gets all expression names for this character.
    /// </summary>
    public List<string> GetExpressionNames()
    {
        List<string> names = new List<string>();
        foreach (var expr in expressions)
        {
            names.Add(expr.name);
        }
        return names;
    }
}

/// <summary>
/// Single expression definition: name + sprite.
/// </summary>
[Serializable]
public class Expression
{
    [Tooltip("Expression name (e.g., 'Happy', 'Angry', 'Surprised', 'Neutral')")]
    public string name;

    [Tooltip("Sprite for this expression")]
    public Sprite sprite;

    public override string ToString()
    {
        return $"Expression: {name}";
    }
}
