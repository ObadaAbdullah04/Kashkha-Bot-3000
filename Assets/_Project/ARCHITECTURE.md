# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

This document describes the clean architecture implemented for Kashkha-Bot-3000, following single-responsibility principles and loose coupling.

**Phase 5 Update:** Input-Based QTE System (4 Types), Encounter Shuffling, Path-Drawing Maze Mini-Game.

**Phase 4 Update:** Floating Text Manager (Object Pooling) + Wardrobe Meta-Progression System.

**Phase 3 Update:** 4-House Gauntlet with Three-Strike Hospitality System, Crossroads Decision, and House 4 Boss Mode.

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         GameManager                              │
│  • State Machine (Wardrobe → Houses 1-3 → Crossroads → H4)      │
│  • 4-House Progression with Flexible Sequencing                 │
│  • Crossroads Decision (Escape vs Risk House 4)                 │
│  • Hospitality Strike Listener (HandleOfferAccepted)            │
│  • House 4 Boss Mode Activation                                 │
│  • Outfit Bonus Application (TimerController, MeterManager)     │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│     UIManager            │          │    MeterManager          │
│  • Display Encounters    │          │  • Battery (0-100)       │
│  • CrossroadsPanel       │          │  • Stomach (0-100)       │
│  • WardrobePanel         │          │  • Strike Counter        │
│  • Screen Shake          │          │  • OnOfferAccepted Event │
│  • Meter Sliders         │          │  • Outfit Stat Bonuses   │
└────────────┬─────────────┘          └────────────┬─────────────┘
             │                                     │
             ▼                                     ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│  FloatingTextManager     │          │   TimerController        │
│  • Object Pool (20+)     │          │  • Countdown (8/7/6/4s)  │
│  • CanvasGroup Alpha     │          │  • Panic mode (≤3s)      │
│  • RTL Arabic support    │          │  • URP panic effects     │
│  • Auto-spawn on events  │          │  • Outfit timer bonus    │
└──────────────────────────┘          └──────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Wardrobe Meta-Progression                     │
├──────────────────────────┬──────────────────────────────────────┤
│   WardrobeManager        │   OutfitSlot                         │
│  • CSV Parsing (Outfits) │  • Purchase/Equip UI                 │
│  • Purchase System       │  • Owned/Equipped indicators         │
│  • Equip System          │  • Rarity colors                     │
│  • Stat Bonus Provider   │  • Dynamic button text               │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Mini-Game Layer                               │
├──────────────────────────┬──────────────────────────────────────┤
│   MiniGameManager        │   CatchMiniGame                      │
│  • Instantiates prefab   │  • Time Attack Mode (10-15s)         │
│  • Duration by house     │  • World Space Player (spawned)      │
│  • OnMiniGameEnded event │  • Screen Halves Touch Input         │
│  • Rewards to FloatingText│ • FallingItem collision (component) │
│                          │                                      │
│  • StartPathDrawingGame  │   PathDrawingGame (NEW Phase 5)      │
│                          │  • Line drawing mechanic             │
│                          │  • Battery/Hit system (4 hits)       │
│                          │  • Collision cooldown (1s)           │
│                          │  • Obstacle spawn patterns           │
│                          │  • Line rejection on hit             │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Data & Persistence Layer                      │
├──────────────────────────┬──────────────────────────────────────┤
│   DataManager            │   SaveManager                        │
│  • Regex CSV Parsing     │  • JSON Serialization                │
│  • 23-column structure   │  • Persistent Scrap/Eidia            │
│  • List<EncounterData>   │  • SaveData model                    │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Juice & Audio Layer                           │
├──────────────────────┬──────────────────────┬───────────────────┤
│  AudioManager        │  URPPostProcessing   │  QTEController    │
│  • Event-driven SFX  │  • Chromatic Aberr.  │  • 4 Input Types  │
│  • Music transitions │  • Vignette          │     (Shake/Tap/  │
│  • Cross-fade logic  │  • Panic pulse       │     Swipe/Hold)  │
│                      │  • Game Over effect  │  • 8-directional  │
│                      │                      │    swipe detect   │
│                      │                      │  • House 4 mods   │
└──────────────────────┴──────────────────────┴───────────────────┘
```

---

## 📁 Script Responsibilities

### Core Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `GameManager.cs` | 4-House state machine, Crossroads, House 4 Boss Mode | `StartRun()`, `StartHouse()`, `EvaluateCrossroads()`, `ChooseRiskHouse4()`, `HandleOfferAccepted()` |
| `GameState.cs` | State enumeration | `Wardrobe`, `Encounter`, `QTE`, `InterHouseMiniGame`, `Crossroads`, `House4Boss`, `GameOver`, `Win` |
| `DataManager.cs` | Robust Regex CSV parsing (**28 columns**, backward compatible) | `ParseCSV()`, `ParseInt()`, `ParseFloat()`, `CleanCSVField()` |
| `SaveManager.cs` | Persistent JSON saving | `SaveGame()`, `LoadGame()`, `AddRunRewards()` |
| `AudioManager.cs` | Event-driven music/SFX (fail-safe) | `PlayMusic()`, `PlaySFX()`, `HandleStateChanged()` |
| `GameConstants.cs` | Shared constants | (static class) |

### Gameplay Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `MeterManager.cs` | Battery/Stomach + **Three-Strike System** | `ModifyBattery()`, `ModifyStomach()`, `RegisterAcceptedOffer()`, `GetEidiaMultiplier()`, `OnOfferAccepted` event |
| `HospitalityStrike.cs` | Strike enumeration | `First`, `Second`, `Third` |
| `TimerController.cs` | Panic timer with per-house durations | `StartTimer(houseLevel)`, `StopTimer()`, `ApplyOutfitBonus()`, `panicThreshold` |
| `QTEController.cs` | **4 Input Types** (Shake, Tap, Swipe, Hold) + House 4 mods | `StartQTE(inputType, count, time, direction, holdDur)`, `CheckShakeInput()`, `CheckTapInput()`, `CheckSwipeInput()`, `CheckHoldInput()`, `DetectSwipeDirection()` |
| `QTEInputType.cs` | **Input type enumeration** | `Shake`, `Tap`, `Swipe`, `Hold` |
| `SwipeDirection.cs` | **Swipe direction enumeration** | `Up`, `Down`, `Left`, `Right` |
| `CatchMiniGame.cs` | Mini-Game: Time Attack, world space movement | `Initialize()`, `HandlePlayerMovement()`, `OnItemCaught()` |
| `FallingItem.cs` | Mini-Game: Component-based collision | `OnTriggerEnter2D()`, `Update()` |
| `PathDrawingGame.cs` | **Mini-Game: Path-Drawing Maze (Phase 5)** | `InitializeGame()`, `CheckCollisions()`, `OnPathCollision()`, `SpawnObstacles()`, `GetObstaclePositionByPattern()` |
| `Obstacle.cs` | **Obstacle component with auto-collider** | `GetTimePenalty()`, `Start()` |
| `ObstacleSpawnPattern.cs` | **Spawn pattern enumeration** | `Diagonal`, `ZigZag`, `Cluster`, `Spread`, `Custom` |

### UI Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `UIManager.cs` | UI display + **CrossroadsPanel** + **WardrobePanel** | `DisplayEncounter()`, `ShowCrossroadsPanel()`, `RefreshWardrobeUI()`, `SetPanicMode()` |
| `ChoiceCard.cs` | Mobile-friendly scale animations | `AnimateCorrect()`, `AnimateWrong()`, `SetIdleFloating()`, `SetLogicIndex()` |
| `FloatingTextManager.cs` | **Object pooling for feedback text** | `SpawnFeedback()`, `SpawnEidiaReward()`, `GetFromPool()` |
| `FloatingText.cs` | **Individual pooled text with CanvasGroup** | `Spawn()`, `KillTween()`, `Initialize()` |
| `OutfitSlot.cs` | **Wardrobe outfit slot UI** | `Initialize()`, `Refresh()`, `UpdateActionButton()` |
| `UIScreenShake.cs` | Coroutine-based screen shake | `Shake*()` preset methods |

### Wardrobe Layer (NEW - Phase 4)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `WardrobeManager.cs` | Meta-progression system | `ParseOutfitsCSV()`, `PurchaseOutfit()`, `EquipOutfit()`, `GetEquippedStatBonus()` |
| `OutfitData.cs` | Outfit data structure | `OutfitStatType`, `OutfitRarity` enums |
| `OutfitSlot.cs` | UI component for outfit slots | `Initialize()`, `Refresh()`, `OnActionButtonClicked()` |
| `SaveData.cs` | Extended save with wardrobe | `ownedOutfitIDs`, `equippedOutfitID` fields |

### Juice Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `HapticFeedback.cs` | Mobile vibration | `LightTap()`, `HeavyVibration()`, `ExplosionVibration()` |
| `URPPostProcessing.cs` | Post-processing effects | `EnablePanicMode()`, `PulseChromaticAberration()`, `EnableGameOverEffect()` |

---

## 🔗 Communication Patterns

### Event-Driven (Loose Coupling)

```csharp
// GameManager subscribes to MeterManager events
private void OnEnable()
{
    MeterManager.OnOfferAccepted += HandleOfferAccepted;
    MeterManager.OnBatteryDrained += HandleBatteryDrained;
    MeterManager.OnStomachFull += HandleStomachFull;
}

// MeterManager fires event when player accepts offer
public void RegisterAcceptedOffer()
{
    acceptedOffersThisHouse++;
    HospitalityStrike strike = (HospitalityStrike)(acceptedOffersThisHouse - 1);
    OnOfferAccepted?.Invoke(strike); // GameManager applies multipliers
}

// GameManager applies strike-based multipliers
private void HandleOfferAccepted(HospitalityStrike strike)
{
    float eidiaMult = MeterManager.Instance.GetEidiaMultiplier(strike);
    float stomachMult = MeterManager.Instance.GetStomachMultiplier(strike);
    float batteryDrain = MeterManager.Instance.GetBatteryDrain(strike);
    // Apply adjusted values...
}
```

### Direct Reference (Tight Coupling - Avoided)

```csharp
// ❌ BAD: GameManager knows about UI implementation
UIManager.Instance.feedbackPanel.SetActive(true);

// ✅ GOOD: GameManager calls method, UIManager decides implementation
UIManager.Instance.ShowFeedback(text);
```

---

## 🎮 Game Flow

### The 4-House Gauntlet

```
Wardrobe (Spend Scrap on Outfits)
    ↓
StartRun() → ResetMeters() → ResetStrikeCounter()
    ↓
┌─────────────────────────────────────────────┐
│  HOUSE 1 (5 encounters)                     │
│  Sequence: HandOnHeart QTE → Trivia →       │
│          Hospitality Offer → Trivia → Offer │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└─────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────┐
│  HOUSE 2 (5 encounters)                     │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└─────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────┐
│  HOUSE 3 (5 encounters)                     │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└─────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────┐
│  CROSSROADS                                 │
│  Eidia ≥ 100?                               │
│  ┌──────────────┬───────────────────────┐   │
│  │ ESCAPE       │ RISK HOUSE 4          │   │
│  │ WinGame()    │ StartHouse4()         │   │
│  │              │ Boss Mode Multipliers │   │
│  └──────────────┴───────────────────────┘   │
└─────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────┐
│  HOUSE 4 (Optional Boss - 8 encounters)     │
│  • Fast timers (4s vs 8s)                   │
│  • Extra QTE inputs (+1 tap/swipe)          │
│  • Higher shake thresholds (×1.5)           │
│  • Double stomach/battery penalties         │
└─────────────────────────────────────────────┘
    ↓
Game Over / Win (Standard or Insane)
```

### Three-Strike Hospitality System

```
Player Accepts Hospitality Offer (Choice 1 = Correct)
    ↓
MeterManager.RegisterAcceptedOffer()
    ↓
Increment acceptedOffersThisHouse (1, 2, or 3)
    ↓
Fire OnOfferAccepted(strike)
    ↓
GameManager.HandleOfferAccepted(strike)
    ↓
┌─────────────────────────────────────────────┐
│  Apply Multipliers Based on Strike:         │
│                                             │
│  1st Strike (Polite):                       │
│  • Eidia: ×1.0 (Full Reward)                │
│  • Stomach: ×1.0 (Normal)                   │
│  • Battery: -5 (Minimum drain)              │
│                                             │
│  2nd Strike (Pushing It):                   │
│  • Eidia: ×1.0 (Full Reward)                │
│  • Stomach: ×1.5 (Dangerous)                │
│  • Battery: -10                             │
│                                             │
│  3rd Strike (Exhausted):                    │
│  • Eidia: ×0.0 (NO REWARD)                  │
│  • Stomach: ×3.0 (CATASTROPHIC)             │
│  • Battery: -25                             │
└─────────────────────────────────────────────┘
    ↓
Continue Encounter Loop
```

**Strike counter resets at `StartHouse()`** (new house = fresh hosts = fresh start)

### Game Over Conditions

```
┌─────────────────────────┐
│  MeterManager detects:  │
│  • Battery <= 0         │
│  • Stomach >= 100       │
└───────────┬─────────────┘
            │
    ┌───────┴────────┐
    │                │
    ▼                ▼
Battery:         Stomach:
Social Shutdown  Ma'amoul Explosion
    │                │
    ▼                ▼
Shake: Social    Shake: Explosion
Shutdown         Haptic: Explosion
Haptic: Heavy    URP: Game Over
    │                │
    └───────┬────────┘
            │
            ▼
    ChangeState(GameOver)
            │
            ▼
    SaveManager.AddRunRewards()
```

---

## ⚙️ Inspector Configuration Guide

### MeterManager Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **Hospitality Strike Multipliers - Eidia** | | |
| `First Strike Eidia Multiplier` | `1.0` | Polite acceptance = full reward |
| `Second Strike Eidia Multiplier` | `1.0` | Pushing it = still full reward |
| `Third Strike Eidia Multiplier` | `0.0` | Exhausted = NO REWARD |
| **Hospitality Strike Multipliers - Stomach** | | |
| `First Strike Stomach Multiplier` | `1.0` | Normal fill |
| `Second Strike Stomach Multiplier` | `1.5` | 50% more dangerous |
| `Third Strike Stomach Multiplier` | `3.0` | Triple danger! |
| **Hospitality Strike Multipliers - Battery** | | |
| `First Strike Battery Drain` | `5` | Minimum drain for realism |
| `Second Strike Battery Drain` | `10` | Moderate drain |
| `Third Strike Battery Drain` | `25` | Massive drain |
| **House 4 Boss Mode Multipliers** | | |
| `House 4 Stomach Multiplier` | `2.0` | Double stomach fill in boss mode |
| `House 4 Battery Drain Multiplier` | `1.5` | 50% more battery drain |

### GameManager Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `Encounters Per House` | `5` | Trivia + Offer pairs per house |
| `Eidia To Win` | `100` | Threshold to unlock Crossroads |
| `Crossroads Decision Time` | `10` | Seconds to choose (if using timer) |
| `House 4 Encounters` | `8` | Boss mode length |
| `House 4 Is Optional` | `true` | Enable Crossroads choice |

### TimerController Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `House 1 Duration` | `8` | Seconds per encounter |
| `House 2 Duration` | `7` | Faster |
| `House 3 Duration` | `6` | Even faster |
| `House 4 Duration` | `4` | Insane mode speed (half of House 1) |
| `Panic Threshold` | `3` | Seconds remaining for panic effects |
| `Pulse Cooldown` | `0.3` | Chromatic aberration pulse rate |

### QTEController Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **QTE Type: Coffee Shake** | | |
| `Coffee Shake Threshold` | `15` | Accelerometer sensitivity |
| `Coffee Shake Duration` | `3` | Seconds to shake |
| `Coffee Shake Cooldown` | `0.3` | Input cooldown after success |
| **QTE Type: Hand On Heart** | | |
| `Hand On Heart Time Limit` | `2` | Seconds to tap |
| `Hand On Heart Required Taps` | `1` | Taps needed |
| `Hand On Heart Swipe Distance` | `50` | Minimum swipe pixels |
| **QTE Type: Tug Of War** | | |
| `Tug Of War Required Swipes` | `2` | Swipes needed |
| `Tug Of War Time Limit` | `4` | Seconds to complete |
| `Tug Of War Swipe Distance` | `50` | Minimum swipe pixels |
| **House 4 Boss Mode Modifiers** | | |
| `House 4 Time Multiplier` | `0.5` | Half time in boss mode |
| `House 4 Extra Inputs` | `1` | +1 tap/swipe in boss mode |
| `House 4 Shake Threshold Multiplier` | `1.5` | Harder shake detection |
| **Global Settings** | | |
| `Default Time Limit` | `4` | Fallback duration |
| `Global Input Cooldown` | `0.15` | Cooldown between all inputs |

### UIManager Component

| Field | Assignment |
|-------|------------|
| **Encounter UI** | |
| `questionText` | QuestionText (RTLTextMeshPro) |
| `choiceTexts[]` | [Choice1Text, Choice2Text, Choice3Text] |
| `choiceCards[]` | [ChoiceButton1, ChoiceButton2, ChoiceButton3] |
| **Feedback UI** | |
| `feedbackText` | FeedbackText |
| `feedbackPanel` | FeedbackPanel GameObject |
| **QTE Warning UI** | |
| `qteWarningPanel` | QTEWarningPanel |
| `qteWarningText` | QTEWarningText |
| **Meter UI** | |
| `batterySlider` | BatterySlider |
| `stomachSlider` | StomachSlider |
| **Game State Panels** | |
| `encounterPanel` | EncounterPanel |
| `gameOverPanel` | GameOverPanel |
| `winPanel` | WinPanel |
| **Crossroads UI (NEW)** | |
| `crossroadsPanel` | CrossroadsPanel GameObject |
| `crossroadsTitleText` | Title Text (RTLTextMeshPro) |
| `crossroadsStatusText` | Status Text (RTLTextMeshPro) |
| `escapeButton` | Escape Button (calls GameManager.ChooseEscape()) |
| `riskButton` | Risk Button (calls GameManager.ChooseRiskHouse4()) |
| **Screen Shake** | |
| `mainPanel` | UIParent RectTransform (with UIScreenShake) |

---

## 🎯 Design Principles Applied

### 1. Single Responsibility
Each script does ONE thing well:
- `GameManager`: Game state and loop
- `MeterManager`: Math, thresholds, AND strike tracking
- `UIManager`: Visual display only

### 2. Loose Coupling
Communication via events, not direct references:
- `MeterManager.OnOfferAccepted` → GameManager applies multipliers
- `MeterManager.OnBatteryDrained` → GameManager triggers Game Over
- UI listens to events, doesn't poll state

### 3. Inspector-Configurable (NO HARDCODING)
All magic numbers exposed as `[SerializeField]`:
- Timer durations per house
- QTE thresholds, time limits, input counts
- Strike multipliers (Eidia, Stomach, Battery)
- House 4 boss mode modifiers
- Panic threshold and pulse cooldown

### 4. Clear Documentation
Every script has:
- XML summary at top
- Responsibility section
- Method-level comments explaining WHAT and WHY

### 5. Testability
Every manager has `[Button]` test methods:
- Test in isolation without playing full game
- Quick iteration during development

---

## 📋 Future Extensions

### Easy to Add (No Architecture Changes)

1. **Floating Combat Text**
   - New script: `FloatingText.cs`
   - UIManager calls `ShowFloatingText("+10", position)`
   - No GameManager changes needed

2. **Sound Effects**
   - New script: `AudioManager.cs`
   - Subscribe to same events as HapticFeedback
   - Play sounds on correct/wrong/QTE

3. **More QTE Types**
   - Add to `QTEType` enum
   - Add input check method in `QTEController`
   - Add CSV rows with new `QTEType` value

4. **Wardrobe/Outfit System**
   - New script: `WardrobeManager.cs`
   - Outfits with stat modifiers (Battery +10%, Stomach -10%, etc.)
   - GameManager reads modifiers at `StartRun()`

---

## 🐛 Troubleshooting

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "No encounters parsed" | CSV not assigned | Drag Encounters.csv to DataManager.csvFile |
| "Only first button works" | ChoiceCard logicIndex not set | Check UIManager.DisplayEncounter() calls SetLogicIndex() |
| "Strike counter not resetting" | StartHouse() not calling ResetHouseCounters() | Verify GameManager.StartHouse() calls MeterManager.ResetHouseCounters() |
| "Double Eidia on offers" | HandleOfferAccepted + ProcessChoice both adding | Ensure ProcessChoice skips Eidia for HospitalityOffer + isCorrect |
| "Crossroads not appearing" | MiniGameAfter not set on last encounter | Set MiniGameAfter = true on House 3, SequenceOrder = 5 |
| "House 4 not activating" | house4IsOptional = false OR Eidia < 100 | Check GameManager inspector, verify accumulatedEidia |
| Screen shake not working | mainPanel not assigned | Assign UIParent RectTransform to UIManager.mainPanel |
| QTE not triggering | QTEType column empty | Ensure CSV has "CoffeeShake", "HandOnHeart", etc. |
| Post-processing not working | No Global Volume | Create: GameObject > Volume > Global Volume |

---

## 🎮 Mini-Game Architecture

### Inter-House Catch Mini-Game

**When:** After completing each house (House 1, 2, 3), before the next house begins.

**Game Mode:** Time Attack - Catch as many Eidia as possible in 10-15 seconds.

---

### Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    MiniGameManager                               │
│  • Singleton, persists across scenes                            │
│  • Instantiates CatchGame_Canvas prefab                         │
│  • Sets duration: House 1 = 10s, House 2 = 12s, House 3 = 15s  │
│  • Calls CatchMiniGame.Initialize(duration)                     │
│  • On complete: calls GameManager.OnMiniGameComplete()          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CatchMiniGame (MonoBehaviour)                 │
│  • Spawns PlayerBasket prefab at runtime (world space)          │
│  • Reads MoveHorizontal input (New Input System)                │
│  • Spawns Eidia/Ma'amoul items every 0.8s                       │
│  • Tracks score, calls OnItemCaught() via FallingItem           │
│  • Ends game when timer reaches 0                               │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    FallingItem (Component)                       │
│  • Attached to each Eidia/Ma'amoul prefab                       │
│  • Falls via transform.Translate (world space)                  │
│  • OnTriggerEnter2D → calls CatchMiniGame.Instance.OnItemCaught │
│  • Self-destructs when below camera bounds                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Code Audit Checklist

### Known Issues Fixed

| Issue | Status | Fix Location |
|-------|--------|--------------|
| CSV column count mismatch | ✅ Fixed | All 38 rows now have 28 columns |
| Button input only registers first choice | ✅ Fixed | UIManager.DisplayEncounter() calls `SetLogicIndex(i)` |
| Double Eidia on hospitality offers | ✅ Fixed | ProcessChoice skips Eidia for HospitalityOffer + isCorrect |
| Strike counter not resetting | ✅ Fixed | GameManager.StartHouse() calls MeterManager.ResetHouseCounters() |
| Ghosting (multiple QTE triggers) | ✅ Fixed | QTEController input cooldown system |
| QTE type hardcoded | ✅ Fixed | Input-based system (Shake, Tap, Swipe, Hold) |
| Encounter order same every run | ✅ Fixed | Fisher-Yates shuffle with run seed |
| Path-Drawing collision unreliable | ✅ Fixed | OverlapCircleAll with multiple check points |
| Path-Drawing instant redraw after hit | ✅ Fixed | 1-second cooldown + line clear + must restart from green |
| Obstacle spawn too random | ✅ Fixed | 5 spawn patterns (Diagonal, ZigZag, Cluster, Spread, Custom) |

### Potential Future Issues to Watch

| Issue | Prevention |
|-------|------------|
| NullReference on MeterManager.Instance | Always check `if (MeterManager.Instance != null)` before calls |
| Event subscription leaks | Unsubscribe in `OnDisable()` (already implemented) |
| DOTween memory leaks | Kill tweens in `OnDisable()` (already implemented) |
| CSV parsing fails on Arabic punctuation | Use Regex with quoted field handling (already implemented) |
| House 4 multipliers stacking | `isHouse4Active` flag prevents double-application |
| Path-Drawing performance with many segments | `maxLinePoints` limit + `collisionCheckInterval` optimization |
| QTE input detection too sensitive | Tune thresholds in Inspector (shakeThreshold, swipeDistance) |

---

**Last Updated:** Phase 5 - Enhanced Gameplay & Replayability Complete
**Maintained By:** Core Development Team
**Status:** ✅ Complete Vertical Slice - Ready for Android Build / Content Expansion / Visual Polish
