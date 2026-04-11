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

The player is a broke Jordanian developer who builds an AI robot to endure family Eid visits in their place. The player acts as the robot's "social intelligence module," picking dialogue options via swipe cards to collect **Eidia** (Eid money) and survive forced hospitality.

**Cultural Trojan Horse:** Every wrong answer teaches real Jordanian etiquette via a feedback card.

**Key Mechanic:** Swipe cards with **randomized correctness** - players don't know which option is correct until after swiping, making each encounter unpredictable and replayable.

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
| **Social Battery** | Drains on rude answers, wrong swipes, hospitality acceptance | Reaches 0% = **Social Shutdown** (Game Over) |
| **Stomach Meter** | Fills when accepting hospitality offers | Reaches 100% = **Ma'amoul Explosion** (Game Over) |

**Simplified System:** Meters are straightforward - no complex strike multipliers. Correct answers drain less battery and earn more rewards. Incorrect answers drain more battery and earn less.

#### Panic Timer
Each swipe card has its own per-card timer (default 8s). When timer reaches panic threshold (3s), text turns red and chromatic aberration pulses.

**Note:** Timer system simplified - each card manages its own timer independently. No global timer conflicts.

#### Meta-Progression
**"Tech Scrap"** is the persistent currency awarded after runs (even on death). Used in the **"Wardrobe"** menu to buy permanent outfits that act as stat modifiers:
- Extended Battery (starting battery +10%)
- Titanium Stomach (stomach fill rate -10%)
- Panic Timer Chip (timer duration +1s)

### Data Architecture (CSV Pipeline)

Dialogue and Encounter data parsed from **CSV files** at runtime. All pacing controlled via spreadsheet - **NO HARDCODING**.

#### CSV Columns - New Simplified Structure (41-53 columns depending on card count)

| Column Range | Description | Example Values |
|--------------|-------------|----------------|
| `ID` | Unique encounter identifier | `1`, `2`, `3` |
| `HouseLevel` | Difficulty tier (1-4) | `1`, `2`, `3`, `4` |
| `SequenceOrder` | Order within house | `1`, `2`, `3` |
| `Speaker` | Character name | `خالة أم محمد` |
| `CardCount` | Number of swipe cards (2-3) | `2`, `3` |
| **Per Card (12 columns each)** | | |
| `Cn_Question` | Arabic question/situation text | Arabic string |
| `Cn_OptionR` | Right swipe option text | Arabic string |
| `Cn_OptionL` | Left swipe option text | Arabic string |
| `Cn_IsRightCorrect` | Which side is correct (randomized at load) | `1` or `0` |
| `Cn_CorrectFB` | Feedback on correct answer | Arabic string |
| `Cn_IncorrectFB` | Feedback on incorrect answer | Arabic string |
| `Cn_CorrectBat` | Battery change on correct | `-5`, `-10` |
| `Cn_IncorrectBat` | Battery change on incorrect | `-15`, `-25` |
| `Cn_CorrectEid` | Eidia reward on correct | `10`, `15` |
| `Cn_IncorrectEid` | Eidia reward on incorrect | `5`, `8` |
| `Cn_CorrectScr` | Scrap reward on correct | `3`, `5` |
| `Cn_IncorrectScr` | Scrap reward on incorrect | `2`, `3` |
| `ColorHex` | UI accent color | `#FFD700` |

**Key Change:** Correctness is **randomized at CSV load time** (50/50 chance), so players can't memorize "right = correct" - each playthrough feels different!

---

## Technical Architecture

### Architecture Philosophy

**Pragmatic Hackathon Approach:** Speed and stability over perfect architecture.

| Pattern | Usage |
|---------|-------|
| **Singleton Managers** | `GameManager`, `UIManager`, `DataManager`, `MeterManager`, `AudioManager`, `SaveManager` |
| **State Machine** | Enum-based (`GameState { Wardrobe, HouseHub, Encounter, InterHouseMiniGame, GameOver, Win }`) |
| **Events** | Delta-based events for meters (`OnBatteryModified`, `OnStomachModified`) |
| **Persistence** | JSON serialization for Tech Scrap and Eidia tracking |

### Core Systems

#### GameManager.cs
- State machine orchestration
- 4-House progression with House Hub navigation
- Wardrobe mid-run visit with return-to-hub support
- Mini-game slot assignment system
- **Simplified:** Removed all QTE logic, crossroads, House 4 boss state

#### MeterManager.cs
- **Simplified:** Removed three-strike hospitality system
- Battery/Stomach tracking with clamping [0, 100]
- Direct modify methods (no multipliers)
- House 4 insane mode multipliers (optional)

#### UIManager.cs
- **Simplified:** Removed all ChoiceCard dead code
- Wardrobe panel with return-to-hub button support
- House Hub panel management
- Swipe encounter panel

#### SwipeEncounterManager.cs
- Per-card timer system (independent, no conflicts)
- Card stack management
- Randomized correctness resolution
- Feedback display

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
│   │   ├── Data/                      ← CSV Files (SampleEncounters.csv, Outfits.csv)
│   │   ├── Editor/                    ← Custom editor scripts
│   │   ├── Fonts/                     ← RTLTMP Font Assets
│   │   ├── Prefabs/
│   │   │   ├── MiniGames/             ← CatchGame_Canvas, Eidia_Pickup, Maamoul_Obstacle
│   │   │   ├── UI/                    ← SwipeCard, FeedbackCard, HouseHubPanel
│   │   │   └── Pooled Objects/        ← Object pool prefabs
│   │   ├── Resources/                 ← Runtime-loadable assets
│   │   ├── Scenes/
│   │   │   └── Core_Scene.unity       ← Main game scene
│   │   ├── Scripts/
│   │   │   ├── Core/                  ← GameManager, UIManager, DataManager, etc.
│   │   │   ├── Data/                  ← EncounterData, SwipeCardData, SaveData
│   │   │   ├── Gameplay/              ← MeterManager, SwipeEncounterManager, MiniGameManager
│   │   │   └── UI/                    ← UIManager, SwipeCard, FloatingText
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

| Folder | Responsibility |
|--------|----------------|
| **Core/** | Central systems: `GameManager`, `UIManager`, `DataManager`, `AudioManager`, `CameraShakeManager`, `SaveManager`, `HouseHubManager`, `MiniGameManager` |
| **Data/** | Data models: `EncounterData`, `SwipeCardData`, `SaveData`, `OutfitData` |
| **Gameplay/** | Game mechanics: `MeterManager`, `SwipeEncounterManager`, `CatchMiniGame`, `PathDrawingGame`, `MiniGameType` |
| **UI/** | UI components: `UIManager`, `SwipeCard`, `FloatingText`, `FloatingTextManager`, `OutfitSlot` |

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
- **Randomized Correctness:** Swipe cards randomly assign correct answer to left or right (50/50) at CSV load time - players can't memorize patterns!
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
- **House Hub:** Sequential navigation with mini-game nodes between houses
- **Transitions:** DOTween fade animations with Arabic text overlays between houses
- **Screen Flash:** Full-screen color flashes for correct/wrong answer feedback

---

## Phase 6 Refactoring Summary

### What Was Removed:
- ❌ All QTE systems (QTEController, QTEEncounterManager, QTEType, etc.)
- ❌ TimerController (converted to per-card timers in SwipeEncounterManager)
- ❌ Three-strike hospitality system (simplified to direct modifications)
- ❌ Timeline integration systems (TimelineEncounterPlayer, CustomSignalReceiver)
- ❌ Crossroads decision logic (simplified game flow)
- ❌ House 4 Boss state (kept as optional insane mode)
- ❌ All ChoiceCard references (replaced by swipe cards)
- ❌ MiniGameAfter column from CSV (mini-games assigned in inspector)

### What Was Added:
- ✅ **Randomized Correctness:** Swipe cards randomly assign correct answers
- ✅ **Option Display:** Cards show left/right option texts (not just accept/reject)
- ✅ **Mini-Game Assignment:** Inspector-based slot assignment (no CSV dependency)
- ✅ **Wardrobe Return:** Mid-run wardrobe visits preserve all progress
- ✅ **House Hub Navigation:** Visual node-based progression system
- ✅ **Per-Card Timers:** Independent timers eliminate conflicts
- ✅ **Simplified MeterManager:** Clean, direct modification system
- ✅ **SampleEncounters.csv:** Test data with new structure

### New CSV Structure:
- **41-53 columns** (depending on card count: 2 or 3)
- **12 columns per card:** Question, OptionR, OptionL, IsRightCorrect, CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, CorrectEid, IncorrectEid, CorrectScr, IncorrectScr
- **Randomized at load:** IsRightCorrect is overridden by 50/50 randomization

### Updated GameState Enum:
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

### Files Deleted:
1. QTEEncounterManager.cs
2. QTEEncounterData.cs
3. TimelineEncounterPlayer.cs
4. CustomSignalReceiver.cs
5. QTEController.cs
6. QTEType.cs
7. TimerController.cs

### Files Modified:
- SwipeCardData.cs - Complete restructure with randomized correctness
- SwipeCard.cs - Updated UI fields for option display
- SwipeEncounterManager.cs - Uses IsRightCorrect for reward resolution
- MeterManager.cs - Simplified (removed strike system)
- DataManager.cs - New CSV parser (12 cols per card)
- EncounterData.cs - Removed MiniGameAfter field
- GameManager.cs - Removed QTE/crossroads logic, added wardrobe return
- UIManager.cs - Removed dead ChoiceCard code
- HouseHubManager.cs - Added mini-game slot assignment support
- MiniGameManager.cs - Added manual slot assignment system

---

## Phase 8 Refactoring Summary

### What Was Removed:
- ❌ Encounter-based structure (replaced with question pools)
- ❌ SequenceOrder column from CSV (no longer needed)
- ❌ Fixed encounter count per house (replaced with dynamic pool picking)
- ❌ Single-wave encounters (now multi-wave)

### What Was Added:
- ✅ **Question Pools:** CSV contains 10 questions per house pool, shuffled and picked at runtime
- ✅ **Wave System:** Questions split into waves (Wave 1 → Intermission → Wave 2 → ...)
- ✅ **Streak Combos:** Simple dynamic bonuses (+3 for 2-streak, +5 for 3-streak, +8 for 4+)
- ✅ **Inspector Controls:** Configurable questions-to-pick & waves per house
- ✅ **Intermission Hook:** Placeholder for future QTE/scene between waves
- ✅ **BaseEid Field:** Base reward before streak bonus (simpler reward calculation)
- ✅ **WaveNumber Field:** Assigns questions to specific waves

### New CSV Structure:
- **14 columns per question:** ID, HouseLevel, Speaker, CardName, Question, OptionCorrect, OptionWrong, CorrectSide, CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, BaseEid, WaveNumber
- **Pool-driven:** 10 questions per house in CSV, inspector decides how many to pick
- **Wave assignments:** Questions tagged with WaveNumber (1, 2, 3, etc.)

### Inspector Configuration (GameManager):
```csharp
house1QuestionsToPick = 6, house1Waves = 2    // Pick 6 from 10, split into 2 waves
house2QuestionsToPick = 8, house2Waves = 2    // Pick 8 from 10, split into 2 waves
house3QuestionsToPick = 9, house3Waves = 3    // Pick 9 from 10, split into 3 waves
house4QuestionsToPick = 10, house4Waves = 2   // Pick 10 from 10, split into 2 waves
```

### Files Modified:
- **SwipeCardData.cs** — Added WaveNumber, BaseEid; removed CorrectEidiaReward/IncorrectEidiaReward
- **DataManager.cs** — Parse into pools by HouseLevel, shuffle support, GetShuffledQuestionsForHouse()
- **SwipeEncounterManager.cs** — Wave system, streak tracking, intermission hook, PlayWaveIntermission()
- **GameManager.cs** — Inspector controls for questions/waves per house, wave splitting logic
- **SampleEncounters.csv** — 40 questions total (10 per house), wave assignments, culturally validated

### Wave Flow Example (House 1):
```
[Enter House 1]
→ Load pool of 10 questions (HouseLevel = 1)
→ Shuffle & pick 6 questions
→ Split into 2 waves (3 questions each)

=== WAVE 1 ===
Question 1: "تفضلي معمول مع الشاي!" (Random from pick)
→ Timer: 8s → Swipe → Green/Red flash + Floating text
Question 2: "ليش ما تزورينا أكثر؟" (Random from pick)
→ Timer: 8s → Swipe → Green/Red flash + Floating text
Question 3: "شو صار بالدراسة؟" (Random from pick)
→ Timer: 8s → Swipe → ✅ Correct! Streak: 3 → Bonus: +5 Eidia

[INTERMISSION: Placeholder for future QTE/scene]

=== WAVE 2 ===
Question 4: "العيد إيد؟" (Random from pick)
→ Timer: 8s → Swipe → Green/Red flash + Floating text
Question 5: "بدك شاي؟" (Random from pick)
→ Timer: 8s → Swipe → ✅ Correct! Streak: 2
Question 6: "كيف حالك؟" (Random from pick)
→ Timer: 8s → Swipe → ❌ Wrong! Streak reset

[HOUSE 1 COMPLETE!]
Total Eidia: 48 (Base: 43 + Streak Bonus: 5)
```

### Streak Combo System:
| Streak | Bonus Eidia | Description |
|--------|-------------|-------------|
| 1 | +0 | First correct answer (no bonus yet) |
| 2 | +3 | Two consecutive correct answers |
| 3 | +5 | Three consecutive correct answers |
| 4+ | +8 | Four or more consecutive correct answers |

**Note:** Streak resets on wrong answer or timeout!

---

---

## Recent Changes (Phase 8 Refactoring Complete)

### What Was Removed:
- ❌ Scrap currency from swipe cards (moved to mini-games only)
- ❌ Stomach meter from swipe cards (moved to QTEs later)
- ❌ ColorHex column from CSV (unnecessary)
- ❌ Randomized correctness (now explicit in CSV via CorrectSide)
- ❌ 12-column card structure (simplified to 10 columns)

### What Was Added:
- ✅ **Card Name Field:** Displayed as title above question (e.g., "خال كريم")
- ✅ **Explicit Correct Answers:** CSV defines CorrectSide (1=Right, 0=Left) - no randomization
- ✅ **Neutral Drag Tint:** Blue/gray tint during swipe (NO green/red spoilers!)
- ✅ **Result Feedback Flash:** Green flash for correct, Red flash for incorrect AFTER swipe
- ✅ **DOTween Effects:** Elastic entrance animation, smooth drag tilt, fly-off on swipe
- ✅ **Floating Text:** Eidia rewards spawn floating text automatically
- ✅ **Variable Card Count:** Support 1-20 cards per encounter (CSV-driven)
- ✅ **Validated Jordanian Questions:** Culturally accurate Q&A with proper context

### New CSV Structure:
- **10 columns per card** (down from 12):
  - Name, Question, OptionCorrect, OptionWrong, CorrectSide, CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, CorrectEid, IncorrectEid
- **Total columns:** 5 (header) + (10 × CardCount)
  - 4 cards = 45 columns
  - 5 cards = 55 columns
  - 10 cards = 105 columns
- **No ColorHex, Scrap, or Stomach fields**

### Updated Swipe Flow:
1. Card shows with **name + question + two options** (player sees BOTH)
2. Player drags card → **Neutral blue/gray tint** (no spoilers!)
3. Player drops card → **Green/Red flash** based on correctness
4. **Feedback text** appears with explanation
5. **Floating text** spawns for Eidia reward
6. Next card shows → Repeat

### Files Modified:
- **SwipeCardData.cs** — Added CardName, removed scrap/stomach, renamed option fields
- **DataManager.cs** — New CSV parser (10 cols/card), removed randomization
- **SwipeCard.cs** — Added DOTween effects, neutral drag tint, result feedback flash
- **SwipeEncounterManager.cs** — Removed randomization, added floating text, updated event signature
- **GameManager.cs** — Removed scrap references, simplified card processing
- **SaveManager.cs** — Updated AddRunRewards signature (eidia only)
- **SampleEncounters.csv** — Complete rewrite with validated Jordanian questions (5 encounters, 5 cards each)

### DOTween Card Effects:
| Effect | Timing | DOTween Code |
|--------|--------|--------------|
| **Entrance Pop** | Card shows | `DOScale(Vector3.zero → Vector3.one, 0.5s).SetEase(Ease.OutBack)` |
| **Drag Tilt** | During drag | `DORotate(0, 0, ±25°)` based on x-delta |
| **Neutral Tint** | During drag | `DOColor(Color.blue, 0.2s)` gradient (no spoilers!) |
| **Result Flash** | After drop | `DOColor(green/red, 0.2s) → DOColor(white, 0.4s)` |
| **Fly Away** | Swipe complete | `DOLocalMove(off-screen, 0.4s).SetEase(Ease.OutBack)` |
| **Snap Back** | Swipe cancelled | `DOLocalMove(center, 0.3s).SetEase(Ease.OutBack)` |

### Cultural Validation:
All questions now follow Jordanian etiquette:
- ✅ Accepting hospitality (coffee, food) = **Correct**
- ✅ Respecting elders = **Correct**
- ✅ Polite excuses = **Better than direct rejection**
- ✅ Family bonds = **Highly valued**
- ✅ Traditional customs = **Properly represented**

---

---

## Recent Changes (Phase 8 Refactoring Complete)

### Phase 8 REFACTORED: Wave-Based Question Pool System with Streak Combos

#### Summary
- **Question Pools:** 10 questions per house in CSV, shuffled and picked at runtime
- **Wave System:** Questions split into waves with intermission hooks between them
- **Streak Combos:** Simple dynamic bonuses for consecutive correct answers
- **Inspector Controls:** Configure questions-to-pick & waves per house independently

#### Key Features
| Feature | Description |
|---------|-------------|
| **Pool & Pick** | CSV has 10 questions per house, runtime picks N and shuffles |
| **Multi-Wave** | Questions split into waves (Wave 1 → Intermission → Wave 2) |
| **Streak Bonus** | +3 for 2-streak, +5 for 3-streak, +8 for 4+ consecutive correct |
| **Intermission Hook** | Placeholder for future QTE/scene between waves |
| **Configurable** | Inspector controls for pick count & waves per house |

#### Updated Architecture
```
Wardrobe → House Hub → House 1 → Wave 1 → Intermission → Wave 2 → House Hub → House 2
                            ↓                        ↓
                      Shuffle & Pick           Streak Bonus
```

**Key Insight:** Questions are now pooled and shuffled per house, split into waves with intermissions - giving you full control over pacing and future QTE/scene integration!

---

**Last Updated:** Phase 12 Complete - Character Expression System + Critical Bug Fixes + Input Unification
**Maintained By:** Core Development Team
**Status:** 🔄 **REQUIRES COMPREHENSIVE REVIEW** - All major systems refactored, needs cleanup and validation

---

---

## Phase 12: Character Expression System + Cutscene Enhancements (COMPLETE)

### What Was Added:
- ✅ **CharacterExpressionSO ScriptableObject** - Data-driven expression management for character sprites
- ✅ **Placeholder Sprite Generator** - Editor tool to generate 256x256 colored circle sprites (4 characters × 5 expressions)
- ✅ **Cutscenes.csv Extended** - Added `CharacterName` and `ExpressionName` columns (8 columns total)
- ✅ **DataManager Updated** - Parses new CSV columns into CutsceneData
- ✅ **CutsceneTrigger Expression Lookup** - Reads character/expression from CSV data and applies correct sprite
- ✅ **Unity Timeline Support** - Added `CutsceneType.Timeline` enum and `PlayTimelineCutscene()` method
- ✅ **InputManager Singleton** - Centralized input handling for all DeviceControls actions

### New Files:
1. `Assets/_Project/Scripts/Data/CharacterExpressionSO.cs` - ScriptableObject for character expressions
2. `Assets/_Project/Editor/PlaceholderSpriteGenerator.cs` - Editor tool to generate placeholder sprites
3. `Assets/_Project/Data/CharacterExpressions/SETUP_GUIDE.md` - Step-by-step setup instructions
4. `Assets/_Project/Scripts/Core/InputManager.cs` - Centralized input management

### Modified Files:
1. `CutsceneData.cs` - Added `CharacterName`, `ExpressionName` fields; Added `Timeline` to CutsceneType enum
2. `DataManager.cs` - Updated CSV parser for 8-column format
3. `Cutscenes.csv` - Extended with CharacterName/ExpressionName, 16 entries updated
4. `CutsceneTrigger.cs` - Expression system integration, Timeline support, GetCharacterSprite() helper

### Manual Steps Required (Unity Editor - 30 min):
1. Run **Tools → Kashkha → Generate Placeholder Sprites**
2. Create 4 CharacterExpressionSO assets (see SETUP_GUIDE.md)
3. Assign expressions to CutsceneTrigger inspector

---

## Phase 11: Critical Bug Fixes + Input System Unification (COMPLETE)

### Critical Bugs Fixed:
- ✅ **Battery/Stomach HUD Sliders** - Fixed normalization for maxBattery > 100
- ✅ **Scrap Currency Flow** - Added `SaveManager.OnScrapChanged` event
- ✅ **Card Counter Text** - Added null validation and debug logging

### Architecture Improvements:
- ✅ **SwipeCard Refactored** - Uses sprite + name (removed duplicate cardNameText/speakerText)
- ✅ **House 4 Unlock Logic** - Fixed bug where House 4 wasn't unlocking after House 3
- ✅ **Mini-Game Replay** - Added `allowMiniGameReplay` toggle for replaying unlocked mini-games
- ✅ **QTEController InputSystem** - Fully migrated to DeviceControls Input Actions
- ✅ **InputManager Created** - Centralized input singleton for unified input handling

### Modified Files:
1. `UIManager.cs` - Fixed slider normalization, uses MeterManager.MaxBattery
2. `SaveManager.cs` - Added `OnScrapChanged` event
3. `UnifiedHubManager.cs` - Subscribed to scrap events, fixed House 4 unlock, added mini-game replay
4. `SwipeEncounterManager.cs` - Card counter validation
5. `SwipeCard.cs` - Removed duplicate text, uses sprite + speaker name
6. `QTEController.cs` - Replaced legacy Input with DeviceControls actions
7. `CutsceneData.cs` - Added Timeline enum value

### New Files:
1. `Assets/_Project/Scripts/Core/InputManager.cs` - Centralized input management

### Input Actions Now Unified:
- `MoveHorizontal` - CatchMiniGame movement
- `ShakeSkip` - QTE Shake type
- `SwipeUp` - QTE Swipe type  
- `Hold` - QTE Hold type
- `Acceleration` - Mobile shake detection
- `Draw` - PathDrawingGame
- `Tap` - Mini-game interactions

---

## Phase 10: Data Flow & House Progression Fixes (COMPLETE)

### What Was Fixed:
- ✅ **MeterManager → UIManager** - Slider normalization bug (0-100 values into 0-1 slider)
- ✅ **Scrap Not Updating** - Mini-game scrap now fires events to refresh Wardrobe/Upgrades UI
- ✅ **House Progression 1→2→3→4** - House 4 unlock logic fixed
- ✅ **Card Counter** - Added validation and debug logging

---
