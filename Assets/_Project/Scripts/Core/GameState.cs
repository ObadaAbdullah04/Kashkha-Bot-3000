/// <summary>
/// Game state enumeration for the core state machine.
/// Covers the full 4-House Gauntlet flow including Wardrobe and Crossroads.
/// </summary>
public enum GameState
{
    Wardrobe,           // Pre-run: Spend Tech Scrap on outfits (stat modifiers)
    Encounter,          // Trivia or Hospitality Offer encounter
    QTE,                // Action-based QTE sequence
    InterHouseMiniGame, // Between houses (1→2, 2→3, 3→4)
    Crossroads,         // After House 3: Escape/Win or Risk House 4
    House4Boss,         // Optional insane mode (fast timers, double QTEs)
    GameOver,
    Win
}
