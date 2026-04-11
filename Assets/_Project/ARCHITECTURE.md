# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

This document describes the clean architecture implemented for Kashkha-Bot-3000, following single-responsibility principles and loose coupling.

**Phase 16 Update:** Cinematic System Overhaul - Exclusive Playback, Smart Fallback, House Sequence Fixes

**Phase 10 Update:** Signal Router System - Timeline Prefab Spawning with Gameplay Integration (Questions, QTEs, Activations)

**Phase 8 Update:** Wave-Based Question Pool System with Streak Combos, Simplified Meters

**Phase 7 Update:** Swipe Card System with Explicit Correct Answers, CSV Data Pipeline

**Phase 6 Update:** House Hub Navigation, Transition System, Mini-Games

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         GameManager                              │
│  • State Machine (Wardrobe → HouseHub → Encounter → MiniGame)   │
│  • 4-House Progression with House Flow Controller               │
│  • House Sequence Loading (Questions, Cinematics, Interactions) │
│  • Outfit Bonus Application                                     │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│     UIManager            │          │    MeterManager          │
│  • Display Encounters    │          │  • Battery (0-100)       │
│  • WardrobePanel         │          │  • Stomach (0-100)       │
│  • House Hub Panel       │          │  • Direct Modifications  │
│  • Screen Shake          │          │  • Outfit Stat Bonuses   │
│  • Meter Sliders         │          │  • Delta-based Events    │
└────────────┬─────────────┘          └────────────┬─────────────┘
             │                                     │
             ▼                                     ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│  FloatingTextManager     │          │   HouseFlowController    │
│  • Object Pool (20+)     │          │  • Phase 16: Self-driving│
│  • CanvasGroup Alpha     │          │    coroutine sequence    │
│  • RTL Arabic support    │          │  • Plays elements ONE    │
│  • Auto-spawn on events  │          │    at a time, WAITS      │
└──────────────────────────┘          │  • Question/Cinematic/   │
                                       │    Interaction elements  │
                                       └────────┬─────────────────┘
                                                │
                          ┌─────────────────────┼─────────────────────┐
                          ▼                     ▼                     ▼
                ┌──────────────┐    ┌──────────────────┐    ┌──────────────────┐
                │SwipeEncounter│    │ CinematicController│   │ InteractionHUD   │
                │  Manager     │    │ • Timeline/DOTween │   │  Controller      │
                │ShowSingleCard│    │ • Smart Fallback   │   │ RunInteraction   │
                └──────────────┘    │ • UI Hide/Restore  │   │                  │
                                    └─────────┬──────────┘    └──────────────────┘
                                              │
                                    ┌─────────▼──────────┐
                                    │   UI Management     │
                                    │ • Hide gameplay UI  │
                                    │ • Show cinematic UI │
                                    │ • Auto-restore after│
                                    └─────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│               Navigation & Transitions                           │
├──────────────────────────┬──────────────────────────────────────┤
│   HouseHubManager        │   TransitionPlayer                   │
│  • Sequential validation │  • DOTween fade in/out               │
│  • Completion tracking   │  • Arabic text overlay               │
│  • Lock/checkmark UI     │  • Skip support                      │
│  • Celebration panel     │  • OnTransitionComplete callback     │
│  • Play Again/Exit btns  │  • Full-screen black panel           │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Mini-Game Layer                               │
├──────────────────────────┬──────────────────────────────────────┤
│   MiniGameManager        │   CatchMiniGame                      │
│  • Instantiates prefab   │  • Time Attack Mode (10-15s)         │
│  • Duration by house     │  • World Space Player (spawned)      │
│  • OnMiniGameEnded event │  • Screen Halves Touch Input         │
│  • Rewards to FloatingText│ • FallingItem collision (component) │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Data & Persistence Layer                      │
├──────────────────────────┬──────────────────────────────────────┤
│   DataManager            │   SaveManager                        │
│  • Regex CSV Parsing     │  • JSON Serialization                │
│  • Question Pools by House│ • Persistent Tech Scrap & Eidia     │
│  • Shuffle & Pick System │  • SaveData model                    │
│  • Wave Assignment       │  • Owned outfits tracking            │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Juice & Audio Layer                           │
├──────────────────────┬──────────────────────┬───────────────────┤
│  AudioManager        │  ScreenFlash         │  CameraShakeMgr   │
│  • Event-driven SFX  │  • Full-screen flash │  • Preset shakes  │
│  • Music transitions │  • Correct/wrong     │  • Wrong answer   │
│  • Cross-fade logic  │  • CanvasGroup alpha │  • Explosion      │
└──────────────────────┴──────────────────────┴───────────────────┘
```

---

## 📁 Script Responsibilities

### Core Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `GameManager.cs` | 4-House state machine, run lifecycle | `StartRun()`, `StartHouse()`, `EndHouse()`, `OnMiniGameComplete()` |
| `GameState.cs` | State enumeration | `Wardrobe`, `HouseHub`, `Encounter`, `InterHouseMiniGame`, `GameOver`, `Win` |
| `HouseFlowController.cs` | **Phase 16:** Self-driving house sequence player | `PlayHouseSequence()`, `PlayQuestion()`, `PlayCinematic()`, `PlayInteraction()` |
| `CinematicController.cs` | **Phase 16:** Unified cinematic playback (Timeline + DOTween) | `PlayCinematic()`, `CancelActiveCinematic()`, smart fallback, UI management |
| `DataManager.cs` | CSV parsing, data pooling, cinematic registry | `ParseQuestionsCSV()`, `GetQuestionByID()`, `GetCinematicByID()`, `GetInteractionByID()` |
| `SaveManager.cs` | Persistent JSON serialization | `SaveGame()`, `LoadGame()`, `AddRunRewards()` |
| `AudioManager.cs` | Event-driven music/SFX | `PlayMusic()`, `PlaySFX()`, `HandleStateChanged()` |
| `TransitionPlayer.cs` | House-to-house transition animations | `PlayTransition()`, `SkipTransition()`, `OnTransitionComplete` event |

### Gameplay Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `MeterManager.cs` | Battery/Stomach tracking with direct modifications | `ModifyBattery()`, `ModifyStomach()`, delta-based events |
| `SwipeEncounterManager.cs` | Single card display with streak tracking | `ShowSingleCard()`, streak tracking, per-card timer |
| `SwipeCardData.cs` | Swipe card data structure | `GetBatteryDelta()`, `GetEidiaReward()`, `GetFeedback()`, `WasSwipeCorrect()` |
| `SwipeCard.cs` | Tinder-style swipe card UI with DOTween | `Setup()`, `OnBeginDrag()`, `OnDrag()`, `OnEndDrag()`, `ShowResultFeedback()` |
| `InteractionHUDController.cs` | Standalone interaction prompts (Shake/Hold/Tap/Draw) | `RunInteraction()`, type-specific prompts |
| `CatchMiniGame.cs` | Time attack catch mini-game | `Initialize()`, `HandlePlayerMovement()`, `OnItemCaught()` |
| `FallingItem.cs` | Component-based item collision | `OnTriggerEnter2D()`, `Update()` |

### UI Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `UIManager.cs` | UI display, WardrobePanel, HouseHubPanel | `DisplayEncounter()`, `RefreshWardrobeUI()`, `SetPanicMode()` |
| `FloatingTextManager.cs` | **Object pooling for feedback text** | `SpawnFeedback()`, `SpawnEidiaReward()`, `GetFromPool()` |
| `FloatingText.cs` | **Individual pooled text with CanvasGroup** | `Spawn()`, `KillTween()`, `Initialize()` |
| `OutfitSlot.cs` | **Wardrobe outfit slot UI** | `Initialize()`, `Refresh()`, `UpdateActionButton()` |
| `ScreenFlash.cs` | **Full-screen flash effects** | `FlashCorrect()`, `FlashWrong()`, `TriggerFlash()` |
| `UIScreenShake.cs` | Coroutine-based screen shake | `Shake*()` preset methods |

### Data Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `HouseSequenceData.cs` | **Phase 16:** Ordered element sequences per house | `ValidateSequence()`, `GetSequenceSummary()`, `ElementType` enum |
| `CinematicData.cs` | **Phase 16:** Cinematic configuration | `ID`, `Type` (Timeline/DOTween), `TextAR`, `Duration`, `TimelineAssetName` |
| `InteractionData.cs` | Interaction configuration | `ID`, `HouseLevel`, `InteractionType`, `PromptTextAR`, `Duration`, `Threshold` |
| `SwipeCardData.cs` | Single swipe card data | `CardName`, `QuestionAR`, `OptionCorrectAR`, `OptionWrongAR`, `CorrectSide` |
| `OutfitData.cs` | Outfit stat bonuses | `OutfitStatType`, `OutfitRarity` enums |
| `SaveData.cs` | Persistent save structure | `totalScrap`, `totalEidia`, `ownedOutfits`, `equippedOutfit` |

### Juice Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `HapticFeedback.cs` | Mobile vibration | `LightTap()`, `HeavyVibration()`, `ExplosionVibration()` |

---

## 🔗 Communication Patterns

### Event-Driven (Loose Coupling)

```csharp
// GameManager subscribes to SwipeEncounterManager events
private void OnEnable()
{
    SwipeEncounterManager.OnAllCardsSwiped += HandleAllCardsSwiped;
    SwipeEncounterManager.OnCardProcessed += HandleCardProcessed;
}

// SwipeEncounterManager fires event when card is answered
OnCardProcessed?.Invoke(batteryDelta, eidia, wasCorrect);

// GameManager responds by applying stats and spawning floating text
private void HandleCardProcessed(float batteryDelta, int eidia, bool wasCorrect)
{
    // Floating text spawns automatically via events
    if (eidia > 0 && FloatingTextManager.Instance != null)
        FloatingTextManager.Instance.SpawnEidiaReward(eidia);
}
```

### Timeline Signal Flow (Phase 10)

```
Timeline plays → SignalEmitter fires
→ SignalReceiver calls IntermissionSignalRouter.OnShowQuestion()
→ SignalRouter pauses PlayableDirector
→ SignalRouter calls SwipeEncounterManager.ShowSingleCard(cardData, callback)
→ Player answers card
→ SwipeEncounterManager invokes callback
→ SignalRouter resumes PlayableDirector
→ Timeline continues to next event
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

### The 4-House Gauntlet (Phase 16: Sequence-Driven Flow)

```
Wardrobe (Spend Scrap on Outfits)
    ↓
StartRun() → ResetMeters()
    ↓
Transition: "السفر إلى بيت خالة أم محمد..."
    ↓
┌─────────────────────────────────────────────────┐
│  HOUSE 1 (HouseFlowController Sequence)          │
│  [Cinematic] House1_Intro                        │
│  [Question] Q1 → [Question] Q2                  │
│  [Interaction] SHAKE_Cup_1                       │
│  [Question] Q3 → [Question] Q4                  │
│  [Interaction] HOLD_Hand_1                       │
│  [Question] Q5                                   │
│  [Cinematic] House1_Outro                        │
│  ↓                                               │
│  Mini-Game (Catch)                               │
└──────────────┬──────────────────────────────────┘
               ↓
┌─────────────────────────────────────────────────┐
│  HOUSE HUB (Phase 6+)                            │
│  • House 1: ✅ (completed)                       │
│  • House 2: 🔓 (click to enter)                  │
│  • House 3: 🔒 (locked)                          │
│  • House 4: 🔒 (locked)                          │
└──────────────┬──────────────────────────────────┘
               ↓ (Click House 2)
    Transition: "الذهاب إلى بيت عمو أبو أحمد..."
               ↓
┌─────────────────────────────────────────────────┐
│  HOUSE 2 (Sequence with 10 elements)             │
│  Cinematic → Q11 → Q12 → SHAKE_Phone_2          │
│  Q13 → Q14 → HOLD_Cup_2 → Q15 → Q16 → Cinematic│
│  ↓                                               │
│  Mini-Game (Catch)                               │
└──────────────┬──────────────────────────────────┘
               ↓
┌─────────────────────────────────────────────────┐
│  HOUSE 3 (Sequence with 11 elements)             │
│  Cinematic → Q21 → Q22 → SHAKE_Hand_3           │
│  Q23 → Q24 → HOLD_Gift_3 → Q25 → Q26            │
│  DRAW_Path_3 → Cinematic                         │
│  ↓                                               │
│  Mini-Game (Catch)                               │
└──────────────┬──────────────────────────────────┘
               ↓
┌─────────────────────────────────────────────────┐
│  HOUSE 4 (Sequence with 11 elements - INSANE)    │
│  Cinematic → Q31 → Q32 → SHAKE_Insane_4         │
│  Q33 → Q34 → HOLD_Strong_4 → Q35 → Q36          │
│  TAP_Fast_4 → Q37 → Cinematic                    │
│  ↓                                               │
│  Mini-Game (Catch)                               │
└──────────────┬──────────────────────────────────┘
               ↓
┌─────────────────────────────────────────────────┐
│  HOUSE HUB (Celebration Panel)                   │
│  • All houses: ✅✅✅✅                          │
│  • Play Again / Exit to Wardrobe buttons         │
└─────────────────────────────────────────────────┘
```

### House Sequence Element Flow (Phase 16)

```
GameManager.StartHouse(houseLevel)
    ↓
Resources.Load<HouseSequenceData>("Sequences/House{N}_Sequence")
    ↓
HouseFlowController.PlayHouseSequence(houseLevel, sequence)
    ↓
┌─────────────────────────────────────────────────┐
│  ITERATE THROUGH ELEMENTS (ONE AT A TIME):       │
│                                                  │
│  For each element:                               │
│    1. [Question] → ShowSingleCard()              │
│       • Wait for player swipe/timeout            │
│       • Apply battery/eidia rewards              │
│       • Spawn floating text                      │
│                                                  │
│    2. [Cinematic] → PlayCinematic()              │
│       • Hide ALL gameplay UI                     │
│       • Play Timeline OR DOTween text            │
│       • Smart fallback if Timeline missing       │
│       • Auto-restore gameplay UI after           │
│                                                  │
│    3. [Interaction] → RunInteraction()           │
│       • Show Shake/Hold/Tap/Draw prompt          │
│       • Wait for player input/timeout            │
│       • Apply rewards/penalties                  │
│                                                  │
│  Pause between elements (0.5s default)           │
└──────────────┬──────────────────────────────────┘
               ↓
All Elements Complete!
    ↓
OnHouseCompleted → GameManager.EndHouse()
```

### Cinematic Playback Flow (Phase 16)

```
HouseFlowController.PlayCinematic(cinematicID)
    ↓
DataManager.GetCinematicByID(cinematicID)
    ↓
Check: Pre-defined in DataManager?
    ├─ YES → Return CinematicData
    └─ NO  → Create UnityTimeline wrapper
              (TimelineAssetName = cinematicID)
    ↓
CinematicController.PlayCinematic(cinematicData, onComplete)
    ↓
┌─────────────────────────────────────────────────┐
│  CINEMATIC PLAYBACK:                             │
│  1. Hide all gameplay UI                         │
│     • Swipe encounter panel                      │
│     • Interaction HUD                            │
│     • Timer slider                               │
│  2. Check cinematic type:                        │
│     ┌────────────────────────────────────┐       │
│     │ Timeline Mode:                     │       │
│     │ • Has text? → Show panel + text    │       │
│     │ • No text? → Hide panel            │       │
│     │ • Play Timeline asset              │       │
│     │ • Safety timeout (duration + 2s)   │       │
│     └────────────────────────────────────┘       │
│     ┌────────────────────────────────────┐       │
│     │ DOTween Mode:                      │       │
│     │ • Show panel + typewriter text     │       │
│     │ • Animate character by character   │       │
│     │ • Arabic RTL support               │       │
│     └────────────────────────────────────┘       │
│  3. Wait for completion                          │
│  4. Hide cutscene UI                             │
│  5. Restore gameplay UI                          │
│  6. Call onComplete callback                     │
└──────────────┬──────────────────────────────────┘
               ↓
HouseFlowController resumes → Next element
```

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
| `BatteryMax` | `100` | Maximum battery percentage |
| `StomachMax` | `100` | Maximum stomach percentage |

### GameManager Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **Question Pool Configuration** | | |
| `house1QuestionsToPick` | `6` | Pick 6 from pool of 10 |
| `house1Waves` | `2` | Split into 2 waves (3 per wave) |
| `house2QuestionsToPick` | `8` | Pick 8 from pool of 10 |
| `house2Waves` | `2` | Split into 2 waves (4 per wave) |
| `house3QuestionsToPick` | `9` | Pick 9 from pool of 10 |
| `house3Waves` | `3` | Split into 3 waves (3 per wave) |
| `house4QuestionsToPick` | `10` | Pick 10 from pool of 10 (all) |
| `house4Waves` | `2` | Split into 2 waves (5 per wave) |

### IntermissionDirector Component (Phase 10)

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **Intermission Timeline Map** | | |
| Entry 0: House Level | `1` | House that just completed |
| Entry 0: Wave Index | `1` | Wave that just completed |
| Entry 0: Timeline Prefab | `[Assign]` | Prefab with PlayableDirector + SignalRouter |
| **Fallback Behavior** | | |
| `fallbackDelay` | `0.5` | Seconds to wait if no timeline assigned |

### SwipeEncounterManager Component (Phase 8/10)

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **Card Display** | | |
| `swipeCardPrefab` | SwipeCard prefab | Instantiate per card |
| `cardParent` | Transform | Parent for card instances |
| **Timer UI** | | |
| `timerSlider` | Slider | Visual timer bar |
| `timerText` | RTLTextMeshPro | Seconds remaining text |
| **Timing Settings** | | |
| `timePerCard` | `8` | Seconds per card decision |
| `feedbackDuration` | `1.5` | Feedback display seconds |
| `panicThreshold` | `3` | Timer turns red below this |
| **Animation Settings** | | |
| `cardEntranceDuration` | `0.5` | Card spawn animation |

### QTEInputController Component (Phase 10)

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| **Timeline Integration** | | |
| `controlledDirector` | PlayableDirector | Timeline this controller pauses/resumes |
| `qteType` | Shake | Shake (accelerometer) or Swipe (touch) |
| **QTE Settings** | | |
| `qteDuration` | `5.0` | Seconds before timeout |
| `shakeThreshold` | `15.0` | Accelerometer magnitude |
| `swipeMinDistance` | `100.0` | Pixels for touch swipe |
| **QTE Stat Changes** | | |
| `onSuccessStomachDelta` | `+20.0` | Fill stomach as reward |
| `onFailStomachDelta` | `+40.0` | Double penalty for failing |

---

## 🎯 Design Principles Applied

### 1. Single Responsibility
Each script does ONE thing well:
- `GameManager`: Game state and loop
- `HouseFlowController`: Sequence playback (questions, cinematics, interactions)
- `CinematicController`: Cinematic playback (Timeline + DOTween)
- `MeterManager`: Math, thresholds, events
- `UIManager`: Visual display only

### 2. Loose Coupling
Communication via events, not direct references:
- `SwipeEncounterManager.OnCardProcessed` → GameManager spawns floating text
- `CinematicController.OnCinematicCompleted` → HouseFlowController continues sequence
- UI listens to events, doesn't poll state

### 3. Inspector-Configurable (NO HARDCODING)
All magic numbers exposed as `[SerializeField]`:
- Timer durations, thresholds, panic settings
- Interaction settings (shake/hold/tap/draw)
- Feedback display durations
- Cinematic fallback timeout settings

### 4. Sequence-Driven Architecture (Phase 16)
Each house is defined by a `HouseSequenceData` ScriptableObject with:
- Ordered elements (Question, Cinematic, Interaction)
- Explicit IDs that map to CSV data or cinematic registry
- Designer notes for documentation
- Fully configurable in Unity Editor

---

## 🐛 Troubleshooting

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "No questions parsed" | CSV not assigned | Drag Questions.csv to DataManager.questionsCSV |
| "Screen shake not working" | Component missing | Ensure CameraShakeManager exists in scene |
| **Phase 16: Cinematic doesn't play** | Timeline asset missing OR no DOTween text | Check Resources/Timelines/ folder OR add DOTween text to DataManager |
| **Phase 16: Both Timeline + text showing** | Cutscene panel not hidden | Check CinematicController.cutscenePanel assignment |
| **Phase 16: Gameplay UI missing after cinematic** | UI not restored | Check ShowGameplayUI() called in CinematicController |
| **Phase 16: Element not found** | Invalid ID in sequence | Verify ElementID matches CSV ID or cinematic ID |
| **Phase 16: Sequence skips element** | Element ID null/empty | Check HouseSequenceData in inspector for empty fields |
| Streak bonus not applying | Streak not tracked | Check SwipeEncounterManager.currentStreak increments |

---

## 📦 Third-Party Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| **DOTween** | UI animations, tweening, cinematic text reveals | Latest |
| **NaughtyAttributes** | Inspector enhancements | Latest |
| **RTLTMPro** | Arabic text rendering | Latest |
| **Unity Timeline** | Cinematic orchestration (optional) | Built-in |
| **Unity Input System** | Touch/shake detection | 1.14.2 |

---

## 📋 Future Extensions

### Easy to Add (No Architecture Changes)

1. **More Cinematics**
   - Add DOTween cinematics to DataManager pre-defined array
   - Create Timeline assets in Resources/Timelines/
   - Reference in house sequences

2. **More Interactions**
   - Add to Interactions.csv with new IDs
   - Reference in house sequences
   - Supports Shake/Hold/Tap/Draw types

3. **More Mini-Game Types**
   - New script: `NewMiniGame.cs`
   - MiniGameManager instantiates different prefab per slot

### Medium Effort (Minor Refactoring)

1. **Conditional Elements**
   - Sequence elements with prerequisites (e.g., "only show if streak >= 3")
   - Requires extending SequenceElement with condition fields

2. **Cinematic Variants**
   - Multiple cinematic versions per house (randomized)
   - HouseFlowController picks at runtime

### Advanced (Major Architecture)

1. **Visual Sequence Builder**
   - Custom Editor window to chain elements visually
   - Drag-and-drop questions, cinematics, interactions
   - Auto-generates HouseSequenceData assets

2. **Branching Sequences**
   - Different paths based on player choices
   - Requires state machine expansion

---

**Last Updated:** Phase 16 - Cinematic System Overhaul + House Sequence Fixes
**Maintained By:** Core Development Team
**Status:** ✅ **SEQUENCE-DRIVEN ARCHITECTURE** - Questions, Cinematics, Interactions unified
