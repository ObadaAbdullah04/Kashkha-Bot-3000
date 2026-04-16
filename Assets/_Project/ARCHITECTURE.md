# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

This document describes the architecture implemented for Kashkha-Bot-3000, following single-responsibility principles and loose coupling.

**Current State:** Phase 18 - Sequence-Driven Architecture + Wardrobe UI Overhaul + Background System + Memory Swap Mini-Game

**Architecture Philosophy:** Pragmatic Hackathon Approach — Speed and stability over perfect architecture. Singleton Managers + State Machine + Events + ScriptableObjects for data-driven content.

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         GameManager                              │
│  • State Machine (Wardrobe → HouseHub → Encounter → MiniGame)   │
│  • 4-House Progression with House Flow Controller               │
│  • House Sequence Loading (Questions, Cinematics, Interactions) │
│  • Streak Combo Tracking & Meta-Progression                     │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│     UIManager            │          │    MeterManager          │
│  • Display Encounters    │          │  • Battery (0-100+)      │
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
│   UnifiedHubManager      │   TransitionPlayer                   │
│  • Sequential validation │  • DOTween fade in/out               │
│  • Completion tracking   │  • Arabic text overlay               │
│  • Lock/checkmark UI     │  • Skip support                      │
│  • Celebration panel     │  • OnTransitionComplete callback     │
│  • Play Again/Exit btns  │  • Full-screen black panel           │
│  • Wardrobe tab          │                                      │
│  • Upgrades tab          │                                      │
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
├──────────────────────┬───────────────────────┬──────────────────┤
│   DataManager        │   SaveManager         │  WardrobeManager │
│  • Regex CSV Parsing │  • JSON Serialization │  • Outfit purch. │
│  • Question Pools    │  • Tech Scrap/Eidia   │  • Equip/unequip │
│  • Shuffle & Pick    │  • SaveData model     │  • Stat bonuses  │
│  • Wave Assignment   │  • Owned outfits      │  • Mid-run visits│
│  • Cinematic registry│  • Equipped outfit    │                  │
└──────────────────────┴───────────────────────┴──────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Juice & Audio Layer                           │
├──────────────────────┬──────────────────────┬───────────────────┤
│  AudioManager        │  ScreenFlash         │  CameraShakeMgr   │
│  • Event-driven SFX  │  • Full-screen flash │  • Preset shakes  │
│  • Music transitions │  • Correct/wrong     │  • Wrong answer   │
│  • Cross-fade logic  │  • CanvasGroup alpha │  • Explosion      │
└──────────────────────┴──────────────────────┴───────────────────┘
┌──────────────────────┬──────────────────────┐
│  HapticFeedback      │  URPPostProcessing   │
│  • Mobile vibration  │  • Chromatic aberr.  │
│  • Light/Heavy/Expl. │  • Panic mode pulse  │
└──────────────────────┴──────────────────────┘
```

---

## 📁 Script Responsibilities

### Core Layer (17 scripts)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `GameManager.cs` | 4-House state machine, run lifecycle, streak tracking | `StartRun()`, `StartHouse()`, `EndHouse()`, `OnMiniGameComplete()`, `ApplyOutfitBonus()` |
| `GameState.cs` | State enumeration | `Wardrobe`, `HouseHub`, `Encounter`, `InterHouseMiniGame`, `GameOver`, `Win` |
| `HouseFlowController.cs` | **Phase 16:** Self-driving house sequence player via coroutines | `PlayHouseSequence()`, `PlayQuestion()`, `PlayCinematic()`, `PlayInteraction()` |
| `CinematicController.cs` | **Phase 16:** Unified cinematic playback (Timeline + DOTween) | `PlayCinematic()`, `CancelActiveCinematic()`, smart fallback, UI hide/restore |
| `DataManager.cs` | CSV parsing, data pooling, cinematic/interaction registry | `ParseQuestionsCSV()`, `GetQuestionByID()`, `GetCinematicByID()`, `GetInteractionByID()` |
| `WardrobeManager.cs` | Mid-run wardrobe visits, outfit purchasing/equipping | `OpenWardrobe()`, `BuyOutfit()`, `EquipOutfit()`, `ReturnToHub()` |
| `UnifiedHubManager.cs` | House Hub navigation with tabs (Houses, Wardrobe, Upgrades) | `RefreshHub()`, `UnlockHouse()`, `ShowCelebration()`, tab switching |
| `SaveManager.cs` | Persistent JSON serialization | `SaveGame()`, `LoadGame()`, `AddRunRewards()`, `OnScrapChanged` event |
| `AudioManager.cs` | Event-driven music/SFX | `PlayMusic()`, `PlaySFX()`, `HandleStateChanged()` |
| `TransitionPlayer.cs` | House-to-house transition animations | `PlayTransition()`, `SkipTransition()`, `OnTransitionComplete` event |
| `InputManager.cs` | Centralized input handling for DeviceControls | Singleton with `MoveHorizontal`, `ShakeSkip`, `SwipeUp`, `Hold`, `Acceleration`, `Draw`, `Tap` actions |
| `CameraShakeManager.cs` | Screen shake via Cinemachine impulses | `ShakeWrongAnswer()`, `ShakeExplosion()`, `ShakeSocialShutdown()` |
| `MiniGameManager.cs` | Mini-game orchestration, slot assignment | `StartMiniGame()`, `OnMiniGameComplete()`, duration by house |
| `HapticFeedback.cs` | Mobile vibration | `LightTap()`, `HeavyVibration()`, `ExplosionVibration()` |
| `GameConstants.cs` | Shared game constants (avoid magic numbers) | Constant definitions |
| `InteractionSignalEmitter.cs` | Timeline signal emitter for interactions | Emits signals from Timeline to trigger interactions |
| `URPPostProcessing.cs` | Runtime URP post-processing control | Chromatic aberration, panic mode pulse effects |

### Gameplay Layer (9 scripts)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `MeterManager.cs` | Battery/Stomach tracking with direct modifications | `ModifyBattery()`, `ModifyStomach()`, delta-based events (`OnBatteryModified`, `OnStomachModified`) |
| `SwipeEncounterManager.cs` | Single card display with streak tracking, wave system | `ShowSingleCard()`, streak tracking, per-card timer, `PlayWaveIntermission()` |
| `SwipeCardData.cs` | Swipe card data structure | `GetBatteryDelta()`, `GetEidiaReward()`, `GetFeedback()`, `WasSwipeCorrect()` |
| `SwipeCard.cs` | Tinder-style swipe card UI with DOTween | `Setup()`, `OnBeginDrag()`, `OnDrag()`, `OnEndDrag()`, `ShowResultFeedback()` |
| `CatchMiniGame.cs` | Time attack catch mini-game | `Initialize()`, `HandlePlayerMovement()`, `OnItemCaught()` |
| `FallingItem.cs` | Component-based item collision for mini-games | `OnTriggerEnter2D()`, `Update()` |
| `PathDrawingGame.cs` | Path-drawing mini-game mechanics | `Initialize()`, `HandleDrawing()`, `ValidatePath()` |
| `MemorySwapMiniGame.cs` | **Phase 17:** Tile matching memory game | `OnTileClicked()`, `ResolvePair()`, `HintReveal()`, `CompleteGame()` |
| `TileValue.cs` | **Phase 17:** Stores tile matching values | `SetValue()`, `Flip()`, `IsFlipped` |
| `MiniGameType` | Enum for mini-game slot assignment | `CatchGame`, `PathDrawing`, `MemorySwap` |

### UI Layer (11 scripts)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `UIManager.cs` | Master UI manager (panels, meters, wardrobe, HUD) | `DisplayEncounter()`, `RefreshWardrobeUI()`, `SetPanicMode()`, `UpdateMeterSliders()` |
| `FloatingTextManager.cs` | Object pooling for feedback text | `SpawnFeedback()`, `SpawnEidiaReward()`, `GetFromPool()` |
| `FloatingText.cs` | Individual pooled text with CanvasGroup | `Spawn()`, `KillTween()`, `Initialize()` |
| `OutfitSlot.cs` | Wardrobe outfit slot UI | `Initialize()`, `Refresh()`, `UpdateActionButton()` |
| `ScreenFlash.cs` | Full-screen flash effects | `FlashCorrect()`, `FlashWrong()`, `TriggerFlash()` |
| `InteractionHUDController.cs` | Standalone interaction prompts (Shake/Hold/Tap/Draw) | `RunInteraction()`, type-specific prompts |
| `UIScreenShake.cs` | Coroutine-based screen shake | `Shake*()` preset methods |
| `WardrobeUI.cs` | **Phase 18:** Simplified 4-choice wardrobe UI | `RefreshUI()`, `OnOutfitClicked()`, `UpdatePreview()` |
| `PlayerCharacterDisplay.cs` | **Phase 18:** Shows equipped outfit in HUD | `UpdateDisplay()`, subscribes to OnOutfitEquipped |
| `MiniGameBackgroundLoader.cs` | **Phase 18:** Smart background loader for mini-games | `Initialize()`, specific/house-based fallback |
| `HouseBackgroundController.cs` | **Phase 18:** Auto-switches backgrounds by house | `UpdateBackground()`, subscribes to OnHouseStarted |

### Data Layer (8 scripts)

| Script | Responsibility | Key Fields |
|--------|----------------|------------|
| `HouseSequenceData.cs` | **Phase 16:** Ordered element sequences per house (ScriptableObject) | `ElementType` enum (Question/Cinematic/Interaction), `ElementID[]`, `ValidateSequence()` |
| `CinematicData.cs` | **Phase 16:** Cinematic configuration | `ID`, `Type` (Timeline/DOTween), `TextAR`, `Duration`, `TimelineAssetName`, `CharacterName`, `ExpressionName` |
| `InteractionData.cs` | Interaction configuration | `ID`, `HouseLevel`, `InteractionType`, `PromptTextAR`, `Duration`, `Threshold` |
| `InteractionType.cs` | Interaction type enum | `Shake`, `Hold`, `Tap`, `Draw` |
| `CharacterExpressionSO.cs` | **Phase 12:** ScriptableObject for character sprite expressions | `CharacterName`, `Expressions[]` (ExpressionName → Sprite mapping) |
| `SwipeCardData.cs` | Single swipe card data | `CardName`, `QuestionAR`, `OptionCorrectAR`, `OptionWrongAR`, `CorrectSide`, `CorrectFB`, `IncorrectFB`, `CorrectBat`, `IncorrectBat`, `BaseEid`, `WaveNumber` |
| `OutfitData.cs` | Outfit stat bonuses | `OutfitID`, `NameAR`, `Cost`, `StatType`, `StatValue`, `Rarity` |
| `SaveData.cs` | Persistent save structure | `totalScrap`, `totalEidia`, `ownedOutfits[]`, `equippedOutfit` |

### Juice Layer (4 scripts)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `AudioManager.cs` | Event-driven music/SFX with cross-fade | `PlayMusic()`, `PlaySFX()`, `HandleStateChanged()` |
| `ScreenFlash.cs` | Full-screen color flash for correct/wrong feedback | `FlashCorrect()`, `FlashWrong()`, `TriggerFlash()` |
| `CameraShakeManager.cs` | Preset screen shakes via Cinemachine | `ShakeWrongAnswer()`, `ShakeExplosion()`, `ShakeSocialShutdown()` |
| `HapticFeedback.cs` | Mobile haptic vibration | `LightTap()`, `HeavyVibration()`, `ExplosionVibration()` |

### Editor Layer (1 script)

| Script | Responsibility | Key Methods |
|--------|----------------|-------------|
| `MemorySwapPrefabCreator.cs` | **Phase 17:** Quick setup helper for Memory Swap prefab | `ShowWindow()`, opens via `Tools → Kashkha → Memory Swap → Create Prefab Helper` |

---

## 🔗 Communication Patterns

### Event-Driven (Loose Coupling)

```csharp
// GameManager subscribes to SwipeEncounterManager events
private void OnEnable()
{
    SwipeEncounterManager.OnAllCardsSwiped += HandleAllCardsSwiped;
    SwipeEncounterManager.OnCardProcessed += HandleCardProcessed;
    MeterManager.OnBatteryModified += HandleBatteryChanged;
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
→ InteractionSignalEmitter calls HouseFlowController
→ HouseFlowController pauses PlayableDirector
→ Runs Interaction (Shake/Hold/Tap/Draw)
→ Interaction completes
→ HouseFlowController resumes PlayableDirector
→ Timeline continues to next event
```

### House Sequence Flow (Phase 16)

```
HouseFlowController.PlayHouseSequence()
    ↓
For each element in sequence:
    [Question] → SwipeEncounterManager.ShowSingleCard() → yield return wait for swipe
    [Cinematic] → CinematicController.PlayCinematic() → yield return wait for completion
    [Interaction] → InteractionHUDController.RunInteraction() → yield return wait for input
    ↓
Pause between elements (configurable)
    ↓
All elements complete → OnHouseCompleted → GameManager.EndHouse()
```

---

## 🎮 Game Flow

### The 4-House Gauntlet (Phase 16: Sequence-Driven Flow)

```
Wardrobe (Spend Scrap on Outfits)
    ↓
StartRun() → ResetMeters() → ApplyOutfitBonuses()
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
│  HOUSE HUB (UnifiedHubManager)                   │
│  Tab 1: Houses                                   │
│    • House 1: ✅ (completed)                       │
│    • House 2: 🔓 (click to enter)                  │
│    • House 3: 🔒 (locked)                          │
│    • House 4: 🔒 (locked)                          │
│  Tab 2: Wardrobe (mid-run visit)                 │
│  Tab 3: Upgrades                                 │
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
| `eidiaToWin` | `100` | Eidia needed to win the game |
| **House Transition Texts** | | Arabic text shown during transitions |
| `house1TransitionText` | `"السفر إلى بيت خالة أم محمد..."` | |
| `house2TransitionText` | `"الذهاب إلى بيت عمو أبو أحمد..."` | |
| `house3TransitionText` | `"المسار إلى بيت خالة فاطمة..."` | |
| `house4TransitionText` | `"الدخول إلى بيت المدير المجنون..."` | |

### HouseFlowController Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `pauseBetweenElements` | `0.5` | Seconds pause between sequence elements |

### SwipeEncounterManager Component

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

### CinematicController Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `cutscenePanel` | GameObject | Panel for DOTween text display |
| `fallbackTimeout` | `2.0` | Seconds before DOTween fallback |

### InteractionHUDController Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `shakePrompt` | GameObject | Shake interaction UI |
| `holdPrompt` | GameObject | Hold interaction UI |
| `tapPrompt` | GameObject | Tap interaction UI |
| `drawPrompt` | GameObject | Draw interaction UI |

### MiniGameManager Component

| Field | Default | Tuning Notes |
|-------|---------|--------------|
| `miniGamePrefabs` | GameObject[] | Prefab per slot (Catch, Path, etc.) |
| `miniGameDurations` | float[] | Duration per house (10-15s) |

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

### 5. Data-Driven Content
- Questions/encounters from `Questions.csv` (runtime parsed)
- Interactions from `Interactions.csv` (runtime parsed)
- Outfits from `Outfits.csv` (runtime parsed)
- House sequences from `HouseSequenceData` ScriptableObjects (editor configured)

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
| **Phase 17: MemorySwap tiles not matching** | TileValue component missing | Ensure tile prefab has TileValue component attached |
| **Phase 17: MemorySwap grid empty** | Grid or prefab not assigned | Check MemorySwapMiniGame Inspector: Grid, _tilePrefab, _tiles[] |
| **Phase 18: Background not showing** | Resource path incorrect | Check Resources/Backgrounds/ folder exists with HouseX_BG sprites |
| **Phase 18: Outfit preview stuck** | Sprite not found | Verify Resources/CharacterSprites/ folder exists with outfit sprites |
| House 4 not unlocking | Logic bug in UnifiedHubManager | Check House 3 completion triggers unlock |

---

## 📦 Third-Party Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| **DOTween** | UI animations, tweening, cinematic text reveals | Latest |
| **NaughtyAttributes** | Inspector enhancements | Latest |
| **RTLTMPro** | Arabic text rendering | Latest |
| **Unity Timeline** | Cinematic orchestration (optional) | Built-in |
| **Unity Input System** | Touch/shake detection | 1.14.2 |
| **Cinemachine** | Camera impulses and screen shake | Built-in |
| **URP** | Universal Render Pipeline 2D | 14.0.12 |

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
   - Follow MemorySwapMiniGame pattern for consistency

4. **More Backgrounds (Phase 18)**
   - Add sprites to Resources/Backgrounds/
   - Name convention: HouseX_BG or specific names (e.g., Catch_BG)

5. **More Outfits (Phase 18)**
   - Add to Outfits.csv with spriteName pointing to Resources/CharacterSprites/

### Medium Effort (Minor Refactoring)

1. **Conditional Elements**
   - Sequence elements with prerequisites (e.g., "only show if streak >= 3")
   - Requires extending SequenceElement with condition fields

2. **Cinematic Variants**
   - Multiple cinematic versions per house (randomized)
   - HouseFlowController picks at runtime

3. **Memory Swap Difficulty Levels**
   - Configure pair count via Inspector
   - Adjustable reveal duration and hint cooldown

### Advanced (Major Architecture)

1. **Visual Sequence Builder**
   - Custom Editor window to chain elements visually
   - Drag-and-drop questions, cinematics, interactions
   - Auto-generates HouseSequenceData assets

2. **Branching Sequences**
   - Different paths based on player choices
   - Requires state machine expansion

3. **Dynamic Wardrobe Grid**
   - Expand beyond 4 outfits
   - Scrollable grid with pagination

---

**Last Updated:** Phase 18 - Wardrobe UI Overhaul + Background System + Memory Swap Mini-Game
**Maintained By:** Core Development Team
**Status:** ✅ **PRODUCTION READY** - All core systems implemented and tested
