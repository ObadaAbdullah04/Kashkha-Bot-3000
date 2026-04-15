/// <summary>
/// Game state enumeration for the core state machine.
/// Covers the 4-House Gauntlet flow: MainMenu → Wardrobe → House Hub → Encounters → Mini-Games → Win/GameOver.
/// </summary>
public enum GameState
{
    MainMenu,           // Start Screen
    Wardrobe,           // Pre-run: Spend Tech Scrap on outfits (stat modifiers)
    HouseHub,           // Between houses: Navigate to next house, shop wardrobe, tech pit stop
    Encounter,          // Swipe card encounter (Tinder-style)
    InterHouseMiniGame, // Between houses (1→2, 2→3, 3→4)
    GameOver,
    Win
}
