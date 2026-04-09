# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

This document describes the clean architecture implemented for Kashkha-Bot-3000, following single-responsibility principles and loose coupling.

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
│  • 4-House Progression with Sequential Navigation               │
│  • Question Pool Loading & Wave Splitting                       │
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
│  FloatingTextManager     │          │   IntermissionDirector   │
│  • Object Pool (20+)     │          │  • (House,Wave)→Prefab   │
│  • CanvasGroup Alpha     │          │  • Spawns Timeline       │
│  • RTL Arabic support    │          │  • Waits for Completion  │
│  • Auto-spawn on events  │          │  • Fallback Delay        │
└──────────────────────────┘          └────────────┬─────────────┘
                                                   │
                                                   ▼
                                    ┌──────────────────────────┐
                                    │ IntermissionSignalRouter │
                                    │  • OnShowQuestion()      │
                                    │  • OnPauseForQTE()       │
                                    │  • OnActivate()          │
                                    │  • OnDeactivate()        │
                                    │  • Pause/Resume Timeline │
                                    └────────┬─────────────────┘
                                             │
                          ┌──────────────────┼──────────────────┐
                          ▼                  ▼                  ▼
                ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
                │SwipeEncounter│  │ QTEInputCtrl │  │ GameObjects  │
                │  Manager     │  │  Controller  │  │  (NPCs, UI)  │
                │ShowSingleCard│  │  TriggerQTE  │  │  SetActive   │
                └──────────────┘  └──────────────┘  └──────────────┘

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
| `GameManager.cs` | 4-House state machine, question pools, wave splitting | `StartRun()`, `StartHouse()`, `StartSwipeEncounter()`, `EndHouse()` |
| `GameState.cs` | State enumeration | `Wardrobe`, `HouseHub`, `Encounter`, `InterHouseMiniGame`, `GameOver`, `Win` |
| `DataManager.cs` | Robust Regex CSV parsing, question pools | `ParseCSV()`, `GetShuffledQuestionsForHouse()` |
| `SaveManager.cs` | Persistent JSON serialization | `SaveGame()`, `LoadGame()`, `AddRunRewards()` |
| `AudioManager.cs` | Event-driven music/SFX | `PlayMusic()`, `PlaySFX()`, `HandleStateChanged()` |
| `HouseHubManager.cs` | House navigation hub UI | `InitializeHub()`, `MarkHouseComplete()`, `OnHouseSelected` event |
| `TransitionPlayer.cs` | House-to-house transition animations | `PlayTransition()`, `SkipTransition()`, `OnTransitionComplete` event |
| `IntermissionDirector.cs` | **Phase 10:** Timeline prefab spawning | `PlayIntermission()`, `ForceSkipIntermission()`, `(House,Wave)→Prefab` map |
| `IntermissionSignalRouter.cs` | **Phase 10:** Signal-to-gameplay routing | `OnShowQuestion()`, `OnPauseForQTE()`, `OnActivate()`, `OnDeactivate()` |

### Gameplay Layer

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `MeterManager.cs` | Battery/Stomach tracking with direct modifications | `ModifyBattery()`, `ModifyStomach()`, delta-based events |
| `SwipeEncounterManager.cs` | **Phase 8:** Wave-based question pools with streak combos | `StartEncounter()`, `ShowSingleCard()`, streak tracking |
| `SwipeCardData.cs` | **Phase 7:** Swipe card data structure | `GetBatteryDelta()`, `GetEidiaReward()`, `GetFeedback()`, `WasSwipeCorrect()` |
| `SwipeCard.cs` | **Phase 7:** Tinder-style swipe card UI | `Setup()`, `OnBeginDrag()`, `OnDrag()`, `OnEndDrag()`, `ShowResultFeedback()` |
| `QTEInputController.cs` | **Phase 10:** Timeline-paused QTE prompts | `TriggerQTE(callback)`, `SetQTEConfig()`, shake/swipe detection |
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
| `EncounterData.cs` | House/question metadata | `HouseLevel`, `Speaker`, `WaveNumber` |
| `SwipeCardData.cs` | Single swipe card data | `CardName`, `QuestionAR`, `OptionCorrectAR`, `OptionWrongAR`, `RightIsCorrect` |
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

### The 4-House Gauntlet (Phase 10: Timeline Intermissions)

```
Wardrobe (Spend Scrap on Outfits)
    ↓
StartRun() → ResetMeters()
    ↓
Transition: "السفر إلى بيت خالة أم محمد..."
    ↓
┌─────────────────────────────────────────────┐
│  HOUSE 1 (Swipe Encounters + Waves)         │
│  Wave 1: 3 questions → Intermission         │
│  Wave 2: 3 questions → Complete             │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└──────────────┬──────────────────────────────┘
               ↓
┌─────────────────────────────────────────────┐
│  HOUSE HUB (Phase 6+)                       │
│  • House 1: ✅ (completed)                  │
│  • House 2: 🔓 (click to enter)             │
│  • House 3: 🔒 (locked)                     │
│  • House 4: 🔒 (locked)                     │
└──────────────┬──────────────────────────────┘
               ↓ (Click House 2)
    Transition: "الذهاب إلى بيت جدو الحاج..."
               ↓
┌─────────────────────────────────────────────┐
│  HOUSE 2 (Swipe Encounters + Waves)         │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└──────────────┬──────────────────────────────┘
               ↓
┌─────────────────────────────────────────────┐
│  HOUSE 3 (Swipe Encounters + Waves)         │
│  ↓                                          │
│  Mini-Game (Catch)                          │
└──────────────┬──────────────────────────────┘
               ↓
┌─────────────────────────────────────────────┐
│  HOUSE HUB (Celebration Panel)              │
│  • All houses: ✅✅✅✅                     │
│  • Play Again / Exit to Wardrobe buttons    │
└─────────────────────────────────────────────┘
```

### Wave & Intermission Flow (Phase 8/10)

```
GameManager.StartSwipeEncounter(houseLevel)
    ↓
DataManager.GetShuffledQuestionsForHouse(houseLevel)
    ↓
Pick N questions → Split into waves by WaveNumber
    ↓
SwipeEncounterManager.StartEncounter(questionsByWave, totalWaves)
    ↓
┌─────────────────────────────────────────────┐
│  WAVE 1:                                    │
│  • Question 1: Swipe card → Answer → Float  │
│  • Question 2: Swipe card → Answer → Float  │
│  • Question 3: Swipe card → Answer → Float  │
│  ↓                                          │
│  Wave 1 Complete!                           │
└──────────────┬──────────────────────────────┘
               ↓
┌─────────────────────────────────────────────┐
│  INTERMISSION (Phase 10)                    │
│  IntermissionDirector spawns Timeline prefab│
│  Timeline plays → Signals pause for:        │
│    - ShowQuestion (card shown mid-timeline) │
│    - PauseForQTE (QTE prompt shown)         │
│    - OnActivate/OnDeactivate (NPCs, props)  │
│  Timeline completes → Next wave starts      │
└──────────────┬──────────────────────────────┘
               ↓
┌─────────────────────────────────────────────┐
│  WAVE 2: More questions...                  │
└──────────────┬──────────────────────────────┘
               ↓
All Waves Complete!
    ↓
EndEncounter() → OnAllCardsSwiped → GameManager.EndHouse()
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
- `MeterManager`: Math, thresholds, events
- `UIManager`: Visual display only
- `IntermissionSignalRouter`: Signal-to-gameplay routing

### 2. Loose Coupling
Communication via events, not direct references:
- `SwipeEncounterManager.OnCardProcessed` → GameManager spawns floating text
- `IntermissionDirector.OnIntermissionComplete` → SwipeEncounterManager starts next wave
- UI listens to events, doesn't poll state

### 3. Inspector-Configurable (NO HARDCODING)
All magic numbers exposed as `[SerializeField]`:
- Timer durations, thresholds, panic settings
- QTE settings, shake/swipe thresholds
- Questions-to-pick and waves per house
- Fallback delay for missing timelines

### 4. Timeline-First Architecture (Phase 10)
Each intermission is a **prefab** with:
- PlayableDirector component (plays Timeline asset)
- IntermissionSignalRouter component (routes signals to gameplay)
- SignalReceiver component (Unity's built-in, receives Timeline signals)
- Fully configurable in Unity Editor

---

## 🐛 Troubleshooting

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "No encounters parsed" | CSV not assigned | Drag Encounters.csv to DataManager.csvFile |
| "Screen shake not working" | mainPanel not assigned | Assign UIParent RectTransform to UIManager.mainPanel |
| **Phase 10: Timeline doesn't play** | Prefab missing PlayableDirector | Check prefab has PlayableDirector + Timeline asset assigned |
| **Phase 10: Signals don't fire** | SignalReceiver not wired | Add SignalReceiver to prefab, wire to SignalRouter methods |
| **Phase 10: Timeline doesn't pause** | SignalRouter missing director ref | Check IntermissionSignalRouter.ControlledDirector is assigned |
| **Phase 10: Card doesn't show** | QuestionToShow not assigned | Assign SwipeCardData in SignalRouter inspector |
| **Phase 10: QTE doesn't trigger** | QTEInputController missing in scene | Ensure QTEInputController exists and is accessible |
| Wave intermission not firing | No timeline mapped for (house, wave) | Add entry in IntermissionDirector inspector |
| Streak bonus not applying | Questions not tagged with WaveNumber | Check CSV WaveNumber column |

---

## 📦 Third-Party Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| **DOTween** | UI animations, tweening | Latest |
| **NaughtyAttributes** | Inspector enhancements | Latest |
| **RTLTMPro** | Arabic text rendering | Latest |
| **Unity Timeline** | Cutscene orchestration | Built-in |
| **Unity Input System** | Touch/shake detection | 1.14.2 |

---

## 📋 Future Extensions

### Easy to Add (No Architecture Changes)

1. **Custom Signal Types**
   - Create new Signal assets (MiniGame, Dialogue, etc.)
   - Add methods to IntermissionSignalRouter
   - Wire in SignalReceiver

2. **Voice Lines**
   - Add Audio tracks to Timeline prefabs
   - Sync with animation tracks

3. **More Mini-Game Types**
   - New script: `PathDrawingGame.cs`
   - MiniGameManager instantiates different prefab per slot

### Medium Effort (Minor Refactoring)

1. **Dynamic Question Injection**
   - SignalRouter requests questions from pool at runtime
   - Requires passing houseLevel to SignalRouter

2. **Timeline Chaining**
   - One Timeline prefab spawns next Timeline
   - IntermissionDirector manages sequence

### Advanced (Major Architecture)

1. **Visual Scenario Builder**
   - Custom Editor window to chain events visually
   - Export to Timeline prefabs

---

**Last Updated:** Phase 10 - Signal Router System (Timeline Prefab Spawning)
**Maintained By:** Core Development Team
**Status:** ✅ Complete - Timeline Prefab Architecture with Full Gameplay Integration
