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

---

## Project Overview

**Kashkha-Bot-3000** (كَشْخَة-بوت 3000) is a comedic cultural survival / rogue-lite mobile game built for the **Ramadan Hackathon 2026** (Maysalward GDW Hackathon). The project uses Unity 2022.3.62f3 LTS with the Universal Render Pipeline (URP) 2D template.

### The Pitch

The player is a broke Jordanian developer who builds an AI robot to endure family Eid visits in their place. The player acts as the robot's "social intelligence module," picking dialogue options and performing quick-time events (QTEs) to collect **Eidia** (Eid money) and survive forced hospitality.

**Cultural Trojan Horse:** Every wrong answer teaches real Jordanian etiquette via a feedback card.

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
A **4-House sequence** with escalating difficulty and a push-your-luck climax:
- **Houses 1-3:** Alternating Trivia + Hospitality Offer encounters
- **Inter-House Mini-Games:** Between each house (catch mechanics)
- **Crossroads (After House 3):** Escape with 100+ Eidia OR Risk House 4
- **House 4 (Optional Boss):** Insane mode - fast timers, double QTEs, brutal penalties

#### Win Conditions
1. **Standard Win:** Reach 100 JOD (Eidia), choose "Escape" at Crossroads
2. **Insane Win:** Clear House 4 (bonus prestige)

#### Dual Meters System

| Meter | Behavior | Failure State |
|-------|----------|---------------|
| **Social Battery** | Drains on rude answers, QTE failures, hospitality acceptance | Reaches 0% = **Social Shutdown** (Game Over) |
| **Stomach Meter** | Fills when accepting hospitality offers (scaled by strike count) | Reaches 100% = **Ma'amoul Explosion** (Game Over) |

#### The "Three-Strike" Hospitality System
Inside each house, the game tracks how many times the player **accepts** food/drink offers:

| Strike | Narrative | Eidia | Stomach | Battery |
|--------|-----------|-------|---------|---------|
| **1st (Polite)** | Accepting gracefully | ✅ Full Reward | +Normal (×1.0) | -5 (minimum drain) |
| **2nd (Pushing It)** | Accepting but visibly struggling | ✅ Full Reward | +1.5× Normal | -10 |
| **3rd (Exhausted)** | Completely overwhelmed | ❌ No Reward | +3.0× Normal | -25 |

**Strike counter resets at the start of each new house.**

#### Panic Timer
A shrinking bar (**8s → 6s → 4s** depending on house) forcing quick dialogue choices. When timer reaches panic threshold (3s), URP chromatic aberration pulses.

#### Cultural QTEs
Physical traditions simulated via inputs:
- **Accelerometer shake** → Coffee refusal (threshold: 15, House 4: 22.5)
- **Swipe up** → Hand-on-Heart greeting (time limit: 2s, House 4: 1s + extra taps)
- **Swipe away twice** → Polite Tug-of-War (swipes: 2, House 4: 3)

#### Meta-Progression
**"Tech Scrap"** is the persistent currency awarded after runs (even on death). Used in the **"Wardrobe"** menu to buy permanent outfits that act as stat modifiers:
- Extended Battery (starting battery +10%)
- Titanium Stomach (stomach fill rate -10%)
- Panic Timer Chip (timer duration +1s)

### Data Architecture (CSV Pipeline)

Dialogue and Encounter data parsed from **CSV files** at runtime. All pacing controlled via spreadsheet - **NO HARDCODING**.

#### CSV Columns (23 Total)

| Column | Description | Example Values |
|--------|-------------|----------------|
| `ID` | Unique encounter identifier | `H1_T1_MarriageQuestion` |
| `HouseLevel` | Difficulty tier (1-4) | `1`, `2`, `3`, `4` |
| `SequenceOrder` | Order within house (flexible gauntlet) | `1`, `2`, `3`, `4`, `5` |
| `EncounterType` | Type of encounter | `Trivia`, `HospitalityOffer` |
| `MiniGameAfter` | Trigger mini-game after this encounter? | `true`, `false` |
| `QTEType` | Physical gesture required | `None`, `CoffeeShake`, `HandOnHeart`, `TugOfWar` |
| `Speaker` | Character name | `خالة أم محمد` |
| `QuestionAR` | Arabic question/offer text | Arabic string |
| `OfferTextAR` | Hospitality offer display text | Arabic string |
| `Choice1AR` / `Choice2AR` / `Choice3AR` | Arabic choice texts | Arabic strings |
| `Choice1IsCorrect` / `Choice2IsCorrect` / `Choice3IsCorrect` | Boolean correctness flags | `1` or `0` |
| `Choice1Feedback` / `Choice2Feedback` / `Choice3Feedback` | Arabic educational feedback | Arabic strings |
| `BatteryDelta` | Social battery change (always applies) | `-5`, `-15`, `-25` |
| `StomachDelta` | Stomach meter change (Hospitality only) | `0`, `+15`, `+25` |
| `EidiaReward` | Money earned | `0-30` |
| `ScrapReward` | Meta-currency earned | `5-25` |
| `ColorHex` | Floating text color | `#FFD700` |

---

## Technical Architecture

### Architecture Philosophy

**Pragmatic Hackathon Approach:** Speed and stability over perfect architecture.

| Pattern | Usage |
|---------|-------|
| **Singleton Managers** | `GameManager`, `UIManager`, `DataManager`, `MeterManager`, `AudioManager`, `SaveManager` |
| **State Machine** | Enum-based (`GameState { Wardrobe, Encounter, QTE, InterHouseMiniGame, Crossroads, House4Boss, GameOver, Win }`) |
| **Events** | Delta-based events for meters (`OnBatteryModified`, `OnStomachModified`, `OnOfferAccepted`) |
| **Persistence** | JSON serialization for Tech Scrap and Eidia tracking |

### Core Systems

#### GameManager.cs
- State machine orchestration
- 4-House progression with flexible sequencing
- Crossroads decision logic (`EvaluateCrossroads()`, `ChooseEscape()`, `ChooseRiskHouse4()`)
- House 4 Boss Mode activation
- Hospitality strike listener (`HandleOfferAccepted()`)

#### MeterManager.cs
- Battery/Stomach tracking with clamping [0, 100]
- **Three-Strike Hospitality counter** (resets per house)
- Multiplier application via events
- House 4 boss mode flag and multipliers

#### UIManager.cs
- Encounter display (3-choice cards)
- **CrossroadsPanel** (Escape/Risk buttons)
- Feedback animations (DOTween)
- Panic mode visual effects

#### TimerController.cs
- Per-house timer durations (exposed to Inspector)
- Panic mode threshold and pulse effects
- **NO HARDCODING** - all values tunable

#### QTEController.cs
- Multi-input QTE support (shake, tap, swipe)
- Per-QTE-type configuration
- House 4 modifiers (time ×0.5, +1 input, higher thresholds)
- **NO HARDCODING** - all values tunable

### Performance & Optimization

| Technique | Application |
|-----------|-------------|
| **Object Pooling** | Generic Object Pool for UI Feedback Cards (planned) |
| **Canvas Management** | Separate UI Canvases (Static HUD vs. Dynamic Popup); use `CanvasGroup.alpha` instead of `SetActive()` |
| **DOTween** | ALL UI animations, programmatic game juice, elastic card pops, screen shakes |
| **Event-Driven UI** | Meters update via events, not polling |

### DO's and DON'Ts

| ✅ DO | ❌ DON'T |
|-------|----------|
| Use Singleton Managers for global state | Use over-engineered SO-Event Buses |
| Use DOTween for ALL UI animations | Use standard `Update()` animations for UI |
| Use Object Pooling for frequent spawns | Use `Instantiate()`/`Destroy()` at runtime for combat/frequent UI |
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
│   │   ├── Data/                      ← CSV Files (Encounters.csv), Parsed Data Containers
│   │   ├── Fonts/                     ← RTLTMP Font Assets
│   │   ├── Prefabs/
│   │   │   ├── MiniGames/             ← CatchGame_Canvas, Eidia_Pickup, Maamoul_Obstacle
│   │   │   ├── UI/                    ← FeedbackCard, EncounterChoice, CrossroadsPanel
│   │   │   └── Pooled Objects/        ← Object pool prefabs
│   │   ├── Scenes/
│   │   │   └── Core_Scene.unity       ← Main game scene
│   │   └── Scripts/
│   │       ├── Core/                  ← GameManager, UIManager, DataManager, etc.
│   │       ├── Data/                  ← EncounterData, SaveData
│   │       ├── Gameplay/              ← MeterManager, QTEController, TimerController, CatchMiniGame
│   │       └── UI/                    ← UIManager, ChoiceCard, FloatingText
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

| Folder | Responsibility |
|--------|----------------|
| **Core/** | Central systems: `GameManager`, `UIManager`, `DataManager`, `AudioManager`, `CameraShakeManager`, `SaveManager`, `HapticFeedback`, `URPPostProcessing` |
| **Data/** | Data models: `EncounterData`, `SaveData` |
| **Gameplay/** | Game mechanics: `MeterManager`, `QTEController`, `TimerController`, `CatchMiniGame`, `FallingItem`, `HospitalityStrike` |
| **UI/** | UI components: `UIManager`, `ChoiceCard`, `FloatingText`, `FloatingTextManager` |

### Coding Standards

- **Language:** C# with Unity conventions
- **Naming:** PascalCase for classes/methods, camelCase for private fields
- **Attributes:** Uses NaughtyAttributes for cleaner inspector configuration
- **DOTween:** Used for ALL animations and tweening sequences
- **Events:** `public static Action` pattern for decoupled communication
- **Inspector Fields:** All tunable values exposed via `[SerializeField]` with `[Tooltip]`

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
| **2D Sprites** | Unity 2D feature package enabled |
| **URP** | All materials and shaders should be URP-compatible |
| **CSV Data** | Runtime-parsed for dialogue/encounter data |

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
- **Physical QTEs:** Real accelerometer shake and swipe gesture detection implemented for mobile.
- **Anti-Spam:** UI selection lock prevents double-clicking during encounter transitions.
- **Persistence:** High scores and meta-currency are saved to `persistentDataPath/save_data.json`.
- **All Arabic text** uses RTLTMPro for proper rendering.
- **Main scene** `Core_Scene.unity` is the entry point for the game.
- **Mini-Game:** World Space 2D Physics approach with Time Attack mode (10-15s per house)
- **Input System:** DeviceControls.inputactions with MoveHorizontal action (Left/Right Arrow, A/D, Touch Screen Halves)
- **Player Basket:** Spawned at runtime via prefab, uses Rigidbody2D (Kinematic) + BoxCollider2D (Is Trigger)
- **Item Prefabs:** Eidia_Pickup and Maamoul_Obstacle with FallingItem component for self-contained collision
- **Three-Strike Hospitality:** Per-house tracking, resets at StartHouse(), multipliers applied via event
- **Crossroads Decision:** After House 3, player chooses Escape (Win) or Risk House 4 (Boss Mode)
- **House 4 Boss Mode:** Fast timers (×0.5), extra QTE inputs (+1), higher shake thresholds (×1.5), double stomach/battery penalties
- **NO HARDCODING:** All timers, thresholds, and multipliers exposed to Inspector via [SerializeField]

---

## Recent Changes (Phase 5 Complete)

### Phase 5: Enhanced Gameplay & Replayability ✅

#### 5A: Input-Based QTE System (MVP - 4 Types)
| Feature | Implementation | Inspector Tunable |
|---------|---------------|-------------------|
| **Shake QTE** | Accelerometer detection (threshold: 15) | `shakeThreshold`, `shakeDuration`, `shakeCount` |
| **Tap QTE** | Touch/spacebar tap detection | `tapTimeWindow`, `tapCount` |
| **Swipe QTE** | 8-directional swipe detection | `swipeTimeLimit`, `swipeDistance`, `QTEDirection` |
| **Hold QTE** | Hold-and-release timing | `holdDuration`, `holdReleaseWindow` |

**New Files:**
- `QTEInputType.cs` - Enum: `Shake`, `Tap`, `Swipe`, `Hold`
- `SwipeDirection.cs` - Enum: `Up`, `Down`, `Left`, `Right`
- `EncounterData.cs` - 5 new fields: `QTEInputType`, `QTECount`, `QTETimeLimit`, `QTEDirection`, `QTEHoldDuration`
- `DataManager.cs` - 28-column CSV parsing with backward compatibility

**CSV Format (28 columns):**
```csv
ID,HouseLevel,SequenceOrder,EncounterType,MiniGameAfter,QTEType,QTEInputType,QTECount,QTETimeLimit,QTEDirection,QTEHoldDuration,Speaker,...
3,1,3,HospitalityOffer,false,CoffeeRefuse,Shake,3,3,_,0,عمو أبو أحمد,...
```

**House 4 Boss Modifiers:**
- Time: ×0.5
- Inputs: +1 extra
- Shake threshold: ×1.5
- Hold duration: ×1.5

#### 5B: Encounter Shuffling System
| Feature | Implementation |
|---------|---------------|
| **Algorithm** | Fisher-Yates shuffle with run seed |
| **Reproducibility** | Same seed = same order (daily challenge ready) |
| **Per-House Limits** | House 1: 5, House 2: 6, House 3: 7, House 4: 8 (all) |
| **Backward Compatible** | Works with 23-col and 28-col CSV |

**Code Location:** `GameManager.cs` → `LoadNextEncounter()`, `GetEncountersPerHouse()`

#### 5C: Path-Drawing Maze Mini-Game
| Feature | Implementation | Inspector Field |
|---------|---------------|-----------------|
| **Line Drawing** | Touch/mouse input with LineRenderer | `lineWidth`, `lineColor` |
| **Collision Detection** | OverlapCircleAll along line segments | `collisionCheckInterval` |
| **Battery System** | 4 hits maximum (❤️ display) | `maxHits` |
| **Cooldown** | 1-second freeze after collision | `collisionCooldown` |
| **Line Rejection** | Clear entire line on hit | Auto (no field) |
| **Visual Markers** | Green circle (start), Red circle (end) | `startPoint`, `endPoint` |
| **Spawn Patterns** | 5 predictable patterns | `spawnPattern` enum |

**New Files:**
- `PathDrawingGame.cs` - Main mini-game logic
- `Obstacle.cs` - Auto-collider setup, time penalty
- `ObstacleSpawnPattern.cs` - Enum: `Diagonal`, `ZigZag`, `Cluster`, `Spread`, `Custom`

**Spawn Patterns:**
| Pattern | Description | Difficulty |
|---------|-------------|------------|
| **Diagonal** | Alternating left/right across path | Medium |
| **ZigZag** | Sine wave pattern | Hard |
| **Cluster** | Middle bottleneck (40% of path) | Strategic |
| **Spread** | Even distribution | Fair |
| **Custom** | Manual positioning (editor) | Designer choice |

**Gameplay Flow:**
1. Click GREEN circle to start drawing
2. Draw path to RED circle (goal)
3. Avoid PURPLE obstacles
4. Hit obstacle → Line clears + 1s cooldown + lose 1 heart
5. After cooldown → Must return to GREEN circle
6. Lose 4 hearts OR time = 0 → Game Over

**Integration:** `MiniGameManager.cs` → `StartPathDrawingGame()`, difficulty scaling per house

### Phase 4: Polish & Juice (Complete)

#### Floating Text System (NEW)
1. **FloatingTextManager.cs** - Object pooling manager with 20+ pooled objects
2. **FloatingText.cs** - Individual text prefab with CanvasGroup alpha fading
3. **Event Integration** - Auto-spawns on meter changes and mini-game rewards
4. **Performance** - Zero runtime allocations, DOTween recycling enabled

#### Wardrobe Meta-Progression (NEW)
1. **WardrobeManager.cs** - Outfit purchase, equip, and stat application
2. **OutfitData.cs** - Data structure for outfit definitions
3. **Outfits.csv** - 3 launch outfits (Battery, Stomach, Timer bonuses)
4. **OutfitSlot.cs** - UI slot component with purchase/equip logic
5. **UIManager.cs** - Wardrobe panel integration with refresh system
6. **SaveData.cs** - Extended to track owned outfits and equipped state

### Phase 3: Core Architecture (Complete)

#### Core Architecture Updates
1. **GameState enum** - Added `Wardrobe`, `Crossroads`, `House4Boss`
2. **HospitalityStrike enum** - New enum for three-strike system
3. **MeterManager.cs** - Strike counter, multipliers, House 4 mode, `OnOfferAccepted` event
4. **GameManager.cs** - Full 4-house flow, Crossroads logic, strike-based reward system
5. **UIManager.cs** - CrossroadsPanel with Escape/Risk buttons, Wardrobe panel support
6. **TimerController.cs** - Per-house durations, panic mode settings, outfit bonus integration
7. **QTEController.cs** - Per-QTE-type config, House 4 modifiers (all inspector-tunable)
8. **MiniGameManager.cs** - Added `OnMiniGameEnded` event for floating text
9. **CatchMiniGame.cs** - Passes rewards to MiniGameManager for UI feedback

### System Features

#### Three-Strike Hospitality Logic
| Strike | Narrative | Eidia | Stomach | Battery |
|--------|-----------|-------|---------|---------|
| **1st (Polite)** | Accepting gracefully | ✅ Full Reward | +Normal (×1.0) | -5 |
| **2nd (Pushing)** | Accepting but struggling | ✅ Full Reward | +1.5× Normal | -10 |
| **3rd (Exhausted)** | Completely overwhelmed | ❌ NO REWARD | +3.0× Normal | -25 |

#### Outfit Stat Bonuses
| Outfit | Cost | Bonus | Arabic Name |
|--------|------|-------|-------------|
| **Extended Battery** | 50 Scrap | Starting battery +10% | بطارية موسعة |
| **Titanium Stomach** | 75 Scrap | Stomach fill rate -10% | معدة تيتانيوم |
| **Panic Timer Chip** | 100 Scrap | Timer duration +1s | رقاقة التريث |

#### Floating Text Colors
| Type | Gain Color | Loss Color |
|------|------------|------------|
| **Battery** | Cyan (0.2, 0.8, 1) | Red (1, 0.3, 0.3) |
| **Stomach** | Red (bad) | Cyan (good) |
| **Eidia** | Gold (1, 0.84, 0) | N/A |
| **Scrap** | Bronze (0.8, 0.6, 0.4) | N/A |

### Inspector Tunables (No Hardcoding)
- Timer durations per house (8s/7s/6s/4s)
- QTE thresholds, time limits, input counts (Shake, Tap, Swipe, Hold)
- Panic mode threshold and pulse cooldown
- Hospitality strike multipliers (Eidia, Stomach, Battery)
- House 4 boss mode modifiers (time ×0.5, inputs +1, thresholds ×1.5)
- Floating text pool size, offsets, colors, animation durations
- Outfit stat values and costs (via CSV)
- **NEW:** Encounter shuffle limits per house (5/6/7/8)
- **NEW:** Path-Drawing settings (maxHits, collisionCooldown, spawnPattern)
- **NEW:** Path-Drawing collision check interval, line width, goal distance

---

**Last Updated:** Phase 5 - Enhanced Gameplay & Replayability Complete
**Maintained By:** Core Development Team
**Status:** ✅ Complete Vertical Slice - Ready for Android Build / Content Expansion / Visual Polish
