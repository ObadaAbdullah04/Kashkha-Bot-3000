# Kashkha-Bot-3000 — Comprehensive Project Context

> **AI Persona:** Act as an Expert Unity Lead Developer and Game Architect specializing in mobile 2D games, rapid prototyping, highly performant code, and hackathon-winning development.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Game Design Document](#game-design-document)
3. [Technical Architecture](#technical-architecture)
4. [Project Structure](#project-structure)
5. [Building and Running](#building-and-running)
6. [Development Conventions](#development-conventions)
7. [Asset Pipeline](#asset-pipeline)
8. [Hackathon Deadline](#hackathon-deadline)
9. [Development Phases History](#development-phases-history)

---

## Project Overview

**Kashkha-Bot-3000** (كَشْخَة-بوت 3000) is a comedic cultural survival / rogue-lite mobile game built for the **Ramadan Hackathon 2026** (Maysalward GDW Hackathon). The project uses Unity 2022.3.62f3 LTS with the Universal Render Pipeline (URP) 2D template.

### The Pitch

The player is a broke Jordanian developer who builds an AI robot to endure family Eid visits in their place. The player acts as the robot's "social intelligence module," picking dialogue options via swipe cards to collect **Eidia** (Eid money) and survive forced hospitality.

**Cultural Trojan Horse:** Every wrong answer teaches real Jordanian etiquette via a feedback card.

**Key Mechanic:** Swipe cards with **explicit correctness** (defined in CSV) - players swipe left or right to choose answers, with streak combos rewarding consecutive correct answers.

### Key Technologies

| Technology | Version/Purpose |
|------------|-----------------|
| **Unity Engine** | 2022.3.62f3 LTS |
| **Render Pipeline** | Universal Render Pipeline (URP) 14.0.12 |
| **Scripting Backend** | .NET (IL2CPP) |
| **Input System** | Unity Input System 1.14.2 |

### Third-Party Packages & Assets

| Package/Asset | Purpose |
|---------------|---------|
| **NaughtyAttributes** | Enhanced Unity inspector attributes |
| **DOTween** | High-performance tweening/animation engine |
| **RTLTMPro** | Right-to-left text support for TextMeshPro (Arabic/Persian) |
| **UI Particle Effect** | Particle effects for Unity UGUI elements |
| **Unity UI Rounded Corners** | Rounded corner components for Unity UI |
| **Cinemachine** | Camera impulses and screen shake |

---

## Game Design Document

### Core Mechanics & Game Loop

#### The 4-House Gauntlet Flow
A **4-House sequence** with escalating difficulty:
- **Houses 1-4:** Sequence-driven encounters mixing Questions, Cinematics, and Interactions
- **Inter-House Mini-Games:** Between each house (catch mechanics)
- **House 4 (Insane Mode):** Fast timers, brutal penalties

#### Win Conditions
1. **Standard Win:** Complete all 4 houses
2. **Insane Win:** Clear House 4 with bonus prestige

#### Dual Meters System

| Meter | Behavior | Failure State |
|-------|----------|---------------|
| **Social Battery** | Drains on wrong answers, timeouts | Reaches 0% = **Social Shutdown** (Game Over) |
| **Stomach Meter** | Fills when accepting hospitality | Reaches 100% = **Ma'amoul Explosion** (Game Over) |

**Simplified System:** Meters are straightforward - no complex strike multipliers. Correct answers drain less battery and earn more rewards. Incorrect answers drain more battery and earn less.

#### Panic Timer
Each swipe card has its own per-card timer (default 8s). When timer reaches panic threshold (3s), text turns red and chromatic aberration pulses.

**Note:** Timer system simplified - each card manages its own timer independently. No global timer conflicts.

#### Meta-Progression
**"Tech Scrap"** is the persistent currency awarded after runs (even on death). Used in the **"Wardrobe"** menu to buy permanent outfits that act as stat modifiers:
- Extended Battery (starting battery +10%)
- Titanium Stomach (stomach fill rate -10%)
- Panic Timer Chip (timer duration +1s)

### Streak Combo System

| Streak | Bonus Eidia | Description |
|--------|-------------|-------------|
| 1 | +0 | First correct answer (no bonus yet) |
| 2 | +3 | Two consecutive correct answers |
| 3 | +5 | Three consecutive correct answers |
| 4+ | +8 | Four or more consecutive correct answers |

**Note:** Streak resets on wrong answer or timeout!

### Data Architecture (CSV Pipeline)

Dialogue and Encounter data parsed from **CSV files** at runtime. All pacing controlled via spreadsheet - **NO HARDCODING**.

#### CSV Files

| File | Purpose |
|------|---------|
| `Questions.csv` | Question/encounter data with wave assignments |
| `Interactions.csv` | Interaction element definitions |
| `Outfits.csv` | Wardrobe outfit definitions |

#### Questions.csv Structure

| Column | Description | Example Values |
|--------|-------------|----------------|
| `ID` | Unique question identifier | `Q1`, `Q2`, `Q3` |
| `HouseLevel` | Difficulty tier (1-4) | `1`, `2`, `3`, `4` |
| `Speaker` | Character name | `خالة أم محمد` |
| `CardName` | Card title displayed above question | `خال كريم` |
| `Question` | Arabic question/situation text | Arabic string |
| `OptionCorrect` | Correct answer option text | Arabic string |
| `OptionWrong` | Wrong answer option text | Arabic string |
| `CorrectSide` | Which side is correct | `1` (Right) or `0` (Left) |
| `CorrectFB` | Feedback on correct answer | Arabic string |
| `IncorrectFB` | Feedback on incorrect answer | Arabic string |
| `CorrectBat` | Battery change on correct | `-5`, `-10` |
| `IncorrectBat` | Battery change on incorrect | `-15`, `-25` |
| `BaseEid` | Base Eidia reward (before streak bonus) | `10`, `15` |
| `WaveNumber` | Wave assignment for multi-wave encounters | `1`, `2`, `3` |

**Key Design:** Questions are pooled per house (10 per house in CSV), then shuffled and picked at runtime. Inspector configuration decides how many to pick and how many waves to split them into.

---

## Technical Architecture

### Architecture Philosophy

**Pragmatic Hackathon Approach:** Speed and stability over perfect architecture.

| Pattern | Usage |
|---------|-------|
| **Singleton Managers** | `GameManager`, `UIManager`, `DataManager`, `MeterManager`, `AudioManager`, `SaveManager`, `WardrobeManager`, `MiniGameManager` |
| **State Machine** | Enum-based (`GameState { Wardrobe, HouseHub, Encounter, InterHouseMiniGame, GameOver, Win }`) |
| **Events** | Delta-based events for meters (`OnBatteryModified`, `OnStomachModified`) |
| **Persistence** | JSON serialization for Tech Scrap and Eidia tracking |
| **ScriptableObjects** | House sequences, character expressions |

### Core Systems

#### GameManager.cs
- State machine orchestration
- 4-House progression with House Flow Controller
- Streak combo tracking
- Outfit bonus application
- Mini-game completion handling

#### HouseFlowController.cs
- **Phase 16:** Self-driving coroutine-based sequence player
- Plays elements ONE at a time (Question, Cinematic, Interaction)
- Waits for player input/completion before advancing
- Configurable pause between elements

#### CinematicController.cs
- **Phase 16:** Unified cinematic playback (Timeline + DOTween)
- Exclusive playback (hides all gameplay UI during cinematic)
- Smart fallback: Timeline → DOTween if asset missing
- Auto-restore gameplay UI after cinematic completes
- Safety timeout for timelines (duration + 2s buffer)

#### MeterManager.cs
- **Simplified:** No three-strike hospitality system
- Battery/Stomach tracking with clamping [0, 100]
- Direct modify methods (no multipliers)
- Delta-based events for UI updates

#### UIManager.cs
- Master UI manager (panels, meters, wardrobe, HUD)
- House Hub panel management
- Swipe encounter panel
- Meter slider updates via events
- Wardrobe panel with return-to-hub support

#### SwipeEncounterManager.cs
- Per-card timer system (independent, no conflicts)
- Single card display with streak tracking
- Wave system support
- Floating text spawning for Eidia rewards
- Feedback flash (green/red) after swipe

#### UnifiedHubManager.cs
- **Phase 11+:** Replaced old HouseHubManager
- House Hub navigation with tabs (Houses, Wardrobe, Upgrades)
- Sequential validation (unlock next house only after previous complete)
- Completion tracking with checkmarks
- Celebration panel when all houses complete
- Mid-run wardrobe visits with progress preservation

### Performance & Optimization

| Technique | Application |
|-----------|-------------|
| **Object Pooling** | FloatingTextManager (20+ pooled objects) |
| **Canvas Management** | Separate UI Canvases (Static HUD vs. Dynamic Popup); use `CanvasGroup.alpha` instead of `SetActive()` |
| **DOTween** | ALL UI animations, programmatic game juice, elastic card pops, screen shakes |
| **Event-Driven UI** | Meters update via events, not polling |

### DO's and DON'Ts

| ✅ DO | ❌ DON'T |
|-------|----------|
| Use Singleton Managers for global state | Use over-engineered SO-Event Buses |
| Use DOTween for ALL UI animations | Use standard `Update()` animations for UI |
| Use Object Pooling for frequent spawns | Use `Instantiate()`/`Destroy()` at runtime for frequent UI |
| Use NaughtyAttributes for inspector UI | Write custom editor windows unless absolutely necessary |
| **Expose ALL tunables to Inspector** | **HARDCODE values** (timers, thresholds, multipliers) |

---

## Project Structure

```
Kashkha-Bot-3000/
├── Assets/
│   ├── _Project/
│   │   ├── Art/                       ← Sprites, UI Elements, Materials
│   │   ├── Audio/                     ← Voice, SFX, Music
│   │   ├── Controls/                  ← Input System assets (DeviceControls.inputactions)
│   │   ├── Data/                      ← CSV Files (Questions.csv, Interactions.csv, Outfits.csv)
│   │   │   └── CharacterExpressions/  ← CharacterExpressionSO assets
│   │   ├── Editor/                    ← Custom editor scripts
│   │   ├── Fonts/                     ← RTLTMP Font Assets
│   │   ├── Prefabs/
│   │   │   ├── MiniGames/             ← CatchGame_Canvas, Eidia_Pickup, Maamoul_Obstacle
│   │   │   ├── UI/                    ← SwipeCard, FeedbackCard, HouseHubPanel
│   │   │   └── Pooled Objects/        ← Object pool prefabs
│   │   ├── Resources/
│   │   │   ├── Sequences/             ← House1-4_Sequence.asset (ScriptableObjects)
│   │   │   ├── Timelines/             ← House1_Intro.playable, House1_Outro.playable
│   │   │   │   └── Animations/        ← (Currently empty)
│   │   │   └── InteractionIcons/      ← Icon_Shake/Hold/Tap/Draw.png
│   │   ├── Scenes/
│   │   │   └── Core_Scene.unity       ← Main game scene
│   │   ├── Scripts/
│   │   │   ├── Core/                  ← 17 scripts: GameManager, UIManager, DataManager, etc.
│   │   │   ├── Data/                  ← 8 scripts: SwipeCardData, HouseSequenceData, etc.
│   │   │   ├── Gameplay/              ← 7 scripts: MeterManager, SwipeEncounterManager, etc.
│   │   │   └── UI/                    ← 7 scripts: UIManager, SwipeCard, FloatingText, etc.
│   │   ├── Settings/                  ← URP and project settings
│   │   └── ARCHITECTURE.md            ← Comprehensive architecture documentation
│   ├── Plugins/
│   │   └── Demigiant/                 ← DOTween plugin
│   ├── Resources/                     ← Runtime-loadable assets (DOTweenSettings)
│   ├── RTLTMPro/                      ← RTL text support
│   ├── Settings/                      ← URP and project settings
│   │   ├── UniversalRP.asset
│   │   └── Renderer2D.asset
│   └── TextMesh Pro/                  ← TextMeshPro assets
├── Packages/
│   └── manifest.json                  ← Unity package dependencies
├── ProjectSettings/                   ← Unity project configuration
└── Kashkha-Bot-3000.slnx              ← Visual Studio solution file
```

---

## Building and Running

### Prerequisites

- **Unity Hub** with Unity 2022.3.62f3 LTS installed
- **Visual Studio 2022** or **JetBrains Rider** for C# development

### Opening the Project

1. Open **Unity Hub**
2. Click **Add** → Navigate to the project folder
3. Select the project and open it

### Running the Game

1. Open the project in Unity Editor
2. Open `Assets/_Project/Scenes/Core_Scene.unity`
3. Press **Play** button in the Unity Editor

### Building the Project

**Via Unity Editor:**
- Go to `File` → `Build Settings`
- Select target platform (Android, Windows, WebGL, etc.)
- Click **Build** or **Build and Run**

**Command Line (Unity Batch Mode):**
```bash
Unity -batchmode -quit -projectPath <path> -buildTarget <platform> -executeMethod BuildScript.PerformBuild
```

### Target Platform: Android APK

The final submission must be an **Android APK** demonstrating:
- Creativity
- Technical execution
- High user engagement

---

## Development Conventions

### Code Organization

Scripts are organized into four main categories under `Assets/_Project/Scripts/`:

| Folder | Count | Responsibility |
|--------|-------|----------------|
| **Core/** | 17 | Central systems: `GameManager`, `UIManager`, `DataManager`, `AudioManager`, `CameraShakeManager`, `SaveManager`, `UnifiedHubManager`, `MiniGameManager`, `HouseFlowController`, `CinematicController`, `WardrobeManager`, `TransitionPlayer`, `InputManager`, etc. |
| **Data/** | 8 | Data models: `SwipeCardData`, `HouseSequenceData`, `CinematicData`, `InteractionData`, `CharacterExpressionSO`, `OutfitData`, `SaveData`, `InteractionType` |
| **Gameplay/** | 7 | Game mechanics: `MeterManager`, `SwipeEncounterManager`, `SwipeCard`, `CatchMiniGame`, `PathDrawingGame`, `FallingItem`, `MiniGameType`, `Obstacle` |
| **UI/** | 7 | UI components: `UIManager`, `SwipeCard`, `FloatingText`, `FloatingTextManager`, `OutfitSlot`, `ScreenFlash`, `InteractionHUDController` |

### Coding Standards

- **Language:** C# with Unity conventions
- **Naming:** PascalCase for classes/methods, camelCase for private fields
- **Attributes:** Uses NaughtyAttributes for cleaner inspector configuration
- **DOTween:** Used for ALL animations and tweening sequences
- **Events:** `public static Action` pattern for decoupled communication
- **Inspector Fields:** All tunable values exposed via `[SerializeField]` with `[Tooltip]`
- **XML Documentation:** Summary comments on key classes and methods

### IDE Configuration

The project includes VS Code configuration (`.vscode/`) with:
- Unity debugging setup (`launch.json`)
- Unity-specific file exclusions (`settings.json`)
- Solution reference: `Kashkha-Bot-3000.slnx`

**Debugging:**
- Use the "Attach to Unity" configuration in VS Code
- Or use Unity's built-in script debugging with Visual Studio/Rider

### Version Control

- **Git** is used for version control
- Standard Unity `.gitignore` is in place
- Binary assets (`.unity`, `.prefab`, `.mat`) are tracked
- Generated folders (`Library/`, `Temp/`, `Logs/`) are ignored

---

## Asset Pipeline

| Asset Type | Notes |
|------------|-------|
| **TextMeshPro** | Required for text rendering; fonts stored in `_Project/Fonts/` |
| **RTL Support** | RTLTMPro provides Arabic/Persian text rendering |
| **2D Sprites** | Unity 2D feature package enabled; placeholder sprites generated via Editor tool |
| **URP** | All materials and shaders should be URP-compatible |
| **CSV Data** | Runtime-parsed for dialogue/encounter data (Questions, Interactions, Outfits) |
| **ScriptableObjects** | House sequences (`HouseSequenceData`), character expressions (`CharacterExpressionSO`) |
| **Timeline** | Optional cinematic playback assets (stored in `Resources/Timelines/`) |

### Character Expression System (Phase 12)

Character sprites are managed via `CharacterExpressionSO` ScriptableObjects:
- Each character has one `.asset` file defining their expressions
- Expressions map names (e.g., "Happy", "Angry", "Neutral") to sprites
- CutsceneTrigger reads CSV `CharacterName` and `ExpressionName` columns
- Applies correct sprite dynamically during cutscenes
- Placeholder sprites can be auto-generated via `Tools → Kashkha → Generate Placeholder Sprites`

---

## Known Configuration

| Setting | Value |
|---------|-------|
| **Product Name** | Kashkha-Bot-3000 |
| **Company** | DefaultCompany |
| **Bundle Identifier** | `com.DefaultCompany.KashkhaBot3000` (Android) |
| **Cloud Project ID** | `fd03c60c-7810-4578-bf1a-a9b43763198b` |
| **Active Input Handler** | New Input System |
| **API Compatibility Level** | .NET Standard 2.1 |

---

## Hackathon Deadline

| Milestone | Timeline |
|-----------|----------|
| **Event** | Ramadan Hackathon 2026 |
| **Duration** | 4 Weeks |
| **Deliverable** | 10-minute vertical slice (Android APK) |
| **Goal** | Flawless, fully-juiced submission demonstrating creativity and technical execution |

---

## Notes

- **Robust Data:** CSV parsing uses Regex to safely handle Arabic punctuation and quoted strings.
- **Explicit Correctness:** Swipe cards have correct answers defined in CSV via `CorrectSide` column (1=Right, 0=Left).
- **Simplified Meters:** No three-strike hospitality system - just direct battery/stomach modifications.
- **Per-Card Timers:** Each swipe card has its own independent timer - no global timer conflicts.
- **Anti-Spam:** UI selection lock prevents double-clicking during encounter transitions.
- **Persistence:** High scores and meta-currency are saved to `persistentDataPath/save_data.json`.
- **All Arabic text** uses RTLTMPro for proper rendering.
- **Main scene** `Core_Scene.unity` is the entry point for the game.
- **Mini-Games:** Manually assigned per slot in MiniGameManager inspector (Slot 1, 2, 3).
- **Input System:** DeviceControls.inputactions with MoveHorizontal action (Left/Right Arrow, A/D, Touch Screen Halves)
- **Player Basket:** Spawned at runtime via prefab, uses Rigidbody2D (Kinematic) + BoxCollider2D (Is Trigger)
- **Item Prefabs:** Eidia_Pickup and Maamoul_Obstacle with FallingItem component for self-contained collision
- **House 4 Insane Mode:** Optional fast mode with double stomach/battery penalties
- **NO HARDCODING:** All timers, thresholds, and multipliers exposed to Inspector via [SerializeField]
- **Wardrobe Mid-Run:** Players can visit wardrobe and return to hub without losing progress
- **Unified Hub:** Tab-based navigation (Houses, Wardrobe, Upgrades) with sequential progression
- **Transitions:** DOTween fade animations with Arabic text overlays between houses
- **Screen Flash:** Full-screen color flashes for correct/wrong answer feedback
- **Sequence-Driven:** House sequences defined via ScriptableObject with ordered elements (Question/Cinematic/Interaction)

---

## Development Phases History

> **Note:** The following sections document the evolution of the project through multiple refactoring phases. The **current state** reflects Phase 16+ (Sequence-Driven Architecture).

### Phase 6: QTE Removal + Swipe Card System

#### What Was Removed:
- ❌ All QTE systems (QTEController, QTEEncounterManager, QTEType, etc.)
- ❌ TimerController (converted to per-card timers in SwipeEncounterManager)
- ❌ Three-strike hospitality system (simplified to direct modifications)
- ❌ Timeline integration systems (TimelineEncounterPlayer, CustomSignalReceiver)
- ❌ Crossroads decision logic (simplified game flow)
- ❌ House 4 Boss state (kept as optional insane mode)
- ❌ All ChoiceCard references (replaced by swipe cards)
- ❌ MiniGameAfter column from CSV (mini-games assigned in inspector)

#### What Was Added:
- ✅ **Explicit Correct Answers:** Swipe cards use correct/wrong options
- ✅ **Option Display:** Cards show left/right option texts (not just accept/reject)
- ✅ **Mini-Game Assignment:** Inspector-based slot assignment (no CSV dependency)
- ✅ **Wardrobe Return:** Mid-run wardrobe visits preserve all progress
- ✅ **House Hub Navigation:** Visual node-based progression system
- ✅ **Per-Card Timers:** Independent timers eliminate conflicts
- ✅ **Simplified MeterManager:** Clean, direct modification system

#### Updated GameState Enum:
```csharp
public enum GameState
{
    Wardrobe,           // Pre-run or mid-run visit
    HouseHub,           // Between houses: Navigate to next house
    Encounter,          // Swipe card encounter
    InterHouseMiniGame, // Between houses (1→2, 2→3, 3→4)
    GameOver,
    Win
}
```

---

### Phase 8: Wave-Based Question Pool System

#### What Was Removed:
- ❌ Encounter-based structure (replaced with question pools)
- ❌ SequenceOrder column from CSV (no longer needed)
- ❌ Fixed encounter count per house (replaced with dynamic pool picking)
- ❌ Single-wave encounters (now multi-wave)

#### What Was Added:
- ✅ **Question Pools:** CSV contains 10 questions per house pool, shuffled and picked at runtime
- ✅ **Wave System:** Questions split into waves (Wave 1 → Intermission → Wave 2 → ...)
- ✅ **Streak Combos:** Simple dynamic bonuses (+3 for 2-streak, +5 for 3-streak, +8 for 4+)
- ✅ **Inspector Controls:** Configurable questions-to-pick & waves per house
- ✅ **Intermission Hook:** Placeholder for future QTE/scene between waves
- ✅ **BaseEid Field:** Base reward before streak bonus (simpler reward calculation)
- ✅ **WaveNumber Field:** Assigns questions to specific waves

#### New CSV Structure:
- **14 columns per question:** ID, HouseLevel, Speaker, CardName, Question, OptionCorrect, OptionWrong, CorrectSide, CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, BaseEid, WaveNumber
- **Pool-driven:** 10 questions per house in CSV, inspector decides how many to pick
- **Wave assignments:** Questions tagged with WaveNumber (1, 2, 3, etc.)

---

### Phase 8 Refactoring (Swipe Card Enhancements)

#### What Was Removed:
- ❌ Scrap currency from swipe cards (moved to mini-games only)
- ❌ Stomach meter from swipe cards (moved to QTEs later)
- ❌ ColorHex column from CSV (unnecessary)
- ❌ Randomized correctness (now explicit in CSV via CorrectSide)
- ❌ 12-column card structure (simplified to 10 columns)

#### What Was Added:
- ✅ **Card Name Field:** Displayed as title above question (e.g., "خال كريم")
- ✅ **Explicit Correct Answers:** CSV defines CorrectSide (1=Right, 0=Left) - no randomization
- ✅ **Neutral Drag Tint:** Blue/gray tint during swipe (NO green/red spoilers!)
- ✅ **Result Feedback Flash:** Green flash for correct, Red flash for incorrect AFTER swipe
- ✅ **DOTween Effects:** Elastic entrance animation, smooth drag tilt, fly-off on swipe
- ✅ **Floating Text:** Eidia rewards spawn floating text automatically
- ✅ **Variable Card Count:** Support 1-20 cards per encounter (CSV-driven)
- ✅ **Validated Jordanian Questions:** Culturally accurate Q&A with proper context

#### DOTween Card Effects:
| Effect | Timing | DOTween Code |
|--------|--------|--------------|
| **Entrance Pop** | Card shows | `DOScale(Vector3.zero → Vector3.one, 0.5s).SetEase(Ease.OutBack)` |
| **Drag Tilt** | During drag | `DORotate(0, 0, ±25°)` based on x-delta |
| **Neutral Tint** | During drag | `DOColor(Color.blue, 0.2s)` gradient (no spoilers!) |
| **Result Flash** | After drop | `DOColor(green/red, 0.2s) → DOColor(white, 0.4s)` |
| **Fly Away** | Swipe complete | `DOLocalMove(off-screen, 0.4s).SetEase(Ease.OutBack)` |
| **Snap Back** | Swipe cancelled | `DOLocalMove(center, 0.3s).SetEase(Ease.OutBack)` |

---

### Phase 10: Signal Router System

- ✅ **Timeline Signal Routing:** Timeline signals trigger gameplay elements (Questions, QTEs, Activations)
- ✅ **Intermission Director:** Manages intermission timelines between waves

---

### Phase 11: Critical Bug Fixes + Input System Unification

#### Critical Bugs Fixed:
- ✅ **Battery/Stomach HUD Sliders** - Fixed normalization for maxBattery > 100
- ✅ **Scrap Currency Flow** - Added `SaveManager.OnScrapChanged` event
- ✅ **Card Counter Text** - Added null validation and debug logging

#### Architecture Improvements:
- ✅ **SwipeCard Refactored** - Uses sprite + name (removed duplicate cardNameText/speakerText)
- ✅ **House 4 Unlock Logic** - Fixed bug where House 4 wasn't unlocking after House 3
- ✅ **Mini-Game Replay** - Added `allowMiniGameReplay` toggle for replaying unlocked mini-games
- ✅ **QTEController InputSystem** - Fully migrated to DeviceControls Input Actions
- ✅ **InputManager Created** - Centralized input singleton for unified input handling

---

### Phase 12: Character Expression System + Cutscene Enhancements

#### What Was Added:
- ✅ **CharacterExpressionSO ScriptableObject** - Data-driven expression management for character sprites
- ✅ **Placeholder Sprite Generator** - Editor tool to generate 256x256 colored circle sprites (4 characters × 5 expressions)
- ✅ **Cutscenes.csv Extended** - Added `CharacterName` and `ExpressionName` columns (8 columns total)
- ✅ **DataManager Updated** - Parses new CSV columns into CutsceneData
- ✅ **CutsceneTrigger Expression Lookup** - Reads character/expression from CSV data and applies correct sprite
- ✅ **Unity Timeline Support** - Added `CutsceneType.Timeline` enum and `PlayTimelineCutscene()` method
- ✅ **InputManager Singleton** - Centralized input handling for all DeviceControls actions

---

### Phase 16: Cinematic System Overhaul + House Sequence Fixes

#### What Was Fixed:
- ✅ **Timeline Parallel Execution** - Cinematics now play exclusively (no parallel questions/interactions)
- ✅ **Cutscene Panel Visibility** - Panel hidden during Timeline mode, only shows for DOTween text
- ✅ **Smart Fallback System** - Timeline → DOTween automatic fallback if Timeline asset missing
- ✅ **House Sequence IDs** - All 4 house sequences updated with valid element IDs from CSV
- ✅ **Gameplay UI Isolation** - Swipe/Interaction UI hidden during cinematics, auto-restores after

#### Architecture Improvements:
- ✅ **CinematicController Refactored** - Smart UI management, fallback logic, state tracking, safety timeout
- ✅ **HouseFlowController Enhanced** - Exclusive element execution with debug logging
- ✅ **House Sequences Fixed** - House1-4 now reference valid Q1-Q40, interactions, cinematics
- ✅ **Safety Timeout** - Timelines auto-complete if they exceed expected duration + 2s buffer

#### New Features:
- ✅ **Cinematics Anywhere** - Can add cinematics at beginning, middle, or end of any house sequence
- ✅ **Exclusive Playback** - Only cinematic UI visible, all gameplay UI hidden
- ✅ **Auto-Restore** - Gameplay UI automatically restored after cinematic completes
- ✅ **Fallback Protection** - If Timeline missing, falls back to DOTween text (configurable)

#### House Sequence Structure (Example - House 1):
```
House1_Sequence (9 elements):
  1. [Cinematic] House1_Intro       → Opening cinematic
  2. [Question] Q1                  → "تفضلي معمول مع الشاي!"
  3. [Question] Q2                  → "ليش ما تزورينا أكثر؟"
  4. [Interaction] SHAKE_Cup_1     → "هز الكوب!"
  5. [Question] Q3                  → "شو صار بالدراسة؟"
  6. [Question] Q4                  → "بدك قهوة؟"
  7. [Interaction] HOLD_Hand_1     → "مصافحة اليد!"
  8. [Question] Q5                  → "كيف حالك؟"
  9. [Cinematic] House1_Outro       → Closing cinematic
```

---

### Phase 17: Memory Swap Mini-Game + Background System

#### What Was Added:
- ✅ **MemorySwapMiniGame** - Tile matching memory game as third mini-game type
- ✅ **TileValue Component** - Stores tile matching values and flip state
- ✅ **MemorySwapPrefabCreator** - Editor tool for quick setup (Tools → Kashkha)
- ✅ **MiniGameType.MemorySwap** - Enum entry for slot assignment

#### Memory Swap Gameplay:
- Player flips two tiles at a time to find matching pairs
- All tiles briefly revealed at start for memorization
- Tap tiles to flip and find matches
- Awards Tech Scrap (3 per match + 10 bonus for perfect game)
- Hint button reveals all unmatched tiles (10s cooldown)

#### Editor Tool:
- Menu: `Tools → Kashkha → Memory Swap → Create Prefab Helper`
- Opens setup guide and checklist
- Links to MiniGameManager for slot assignment

---

### Phase 18: Wardrobe UI Overhaul + Background Controllers

#### Wardrobe UI (WardrobeUI.cs):
- ✅ **Super Simplified** - Exactly 4 choices: Default + 3 Outfits
- ✅ **Character Preview** - Shows selected outfit with bounce animation on equip
- ✅ **Lock Overlays** - Visual feedback for locked outfits with cost display
- ✅ **Event-Driven** - Subscribes to WardrobeManager events for real-time updates

#### Player Character Display (PlayerCharacterDisplay.cs):
- ✅ **HUD Integration** - Shows player's currently selected outfit in encounters
- ✅ **Event-Driven Updates** - Syncs with WardrobeManager.OnOutfitEquipped

#### Background System:
- ✅ **MiniGameBackgroundLoader** - Smart background loader for mini-games
  - Supports specific backgrounds (e.g., "Catch_BG")
  - Falls back to house-based backgrounds (HouseX_BG)
  - Nested Canvas with proper sorting (40 = behind world objects)
- ✅ **HouseBackgroundController** - Auto-switches backgrounds based on current house
  - Listens to HouseFlowController.OnHouseStarted
  - Loads Resources/Backgrounds/HouseX_BG dynamically

---

## Current System State

### Script Inventory

| Directory | Count | Key Scripts |
|-----------|-------|-------------|
| **Core/** | 17 | GameManager, UIManager, DataManager, HouseFlowController, CinematicController, WardrobeManager, UnifiedHubManager, SaveManager, AudioManager, InputManager, TransitionPlayer, MiniGameManager, CameraShakeManager, HapticFeedback, GameConstants, InteractionSignalEmitter, URPPostProcessing |
| **Data/** | 8 | SwipeCardData, HouseSequenceData, CinematicData, InteractionData, InteractionType, CharacterExpressionSO, OutfitData, SaveData |
| **Gameplay/** | 9 | MeterManager, SwipeEncounterManager, SwipeCard, CatchMiniGame, PathDrawingGame, FallingItem, Obstacle, MemorySwapMiniGame, TileValue, MiniGameType |
| **UI/** | 11 | UIManager, SwipeCard, FloatingText, FloatingTextManager, OutfitSlot, ScreenFlash, InteractionHUDController, WardrobeUI, PlayerCharacterDisplay, MiniGameBackgroundLoader, HouseBackgroundController |
| **Editor/** | 2 | MemorySwapPrefabCreator, [Placeholder Generator] |
| **Total** | **47** | |

### Resources Inventory

| Directory | Contents |
|-----------|----------|
| **Sequences/** | 4 `.asset` files: House1_Sequence, House2_Sequence, House3_Sequence, House4_Sequence |
| **Timelines/** | 2 `.playable` files: House1_Intro, House1_Outro (Houses 2-4 have no Timeline assets yet) |
| **InteractionIcons/** | 4 `.png` files: Icon_Shake, Icon_Hold, Icon_Tap, Icon_Draw |

### Data Files

| File | Purpose |
|------|---------|
| `Questions.csv` | Question/encounter data with wave assignments (40+ questions) |
| `Interactions.csv` | Interaction element definitions |
| `Outfits.csv` | Wardrobe outfit definitions |
| **CharacterExpressions/** | 4 ScriptableObject assets: Amm_Abu_Mohammed, Grandma, House4_Boss, Khala_Um_Mohammed |

### Known Gaps

1. **Timeline Assets:** Only House 1 has Timeline assets (Intro/Outro). Houses 2-4 rely on DOTween fallback or need Timeline assets created.
2. **Timelines/Animations/:** Subdirectory exists but is empty.

---

**Last Updated:** Phase 18 Complete - Wardrobe UI Overhaul + Background System + Memory Swap Mini-Game
**Maintained By:** Core Development Team
**Status:** ✅ **PRODUCTION READY** - All core systems implemented and tested
