/// <summary>
/// Represents the "Three-Strike" hospitality pressure system.
/// Tracks how many times the player has accepted food/drink offers within a single house.
/// </summary>
public enum HospitalityStrike
{
    First = 0,   // Polite acceptance: +Eidia, +Stomach (normal), 0 Battery drain
    Second = 1,  // Mad/pushing it: +Eidia (50%), +Stomach (150%), -10 Battery
    Third = 2    // Exhausted: 0 Eidia, +Stomach (300%), -25 Battery
}
