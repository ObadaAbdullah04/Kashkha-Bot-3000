using UnityEngine;

/// <summary>
/// PHASE 17: Memory Swap Mini-Game - Tile Value Component.
/// Attached to each tile prefab to store its matching value.
/// </summary>
public class TileValue : MonoBehaviour
{
    /// <summary>
    /// The sprite index/value for this tile (used for matching).
    /// </summary>
    [field: SerializeField] public int Value { get; private set; }

    /// <summary>
    /// Whether this tile is currently face-up (flipped).
    /// </summary>
    public bool IsFlipped { get; private set; }

    /// <summary>
    /// Set the tile's matching value.
    /// </summary>
    /// <param name="value">Sprite index for matching</param>
    public void SetValue(int value) => Value = value;

    /// <summary>
    /// Toggle the tile's flipped state and show/hide the back panel.
    /// </summary>
    public void Flip()
    {
        IsFlipped = !IsFlipped;
        // Child 0 = Front (sprite), Child 1 = Back (cover)
        transform.GetChild(1).gameObject.SetActive(!IsFlipped);
    }
}
