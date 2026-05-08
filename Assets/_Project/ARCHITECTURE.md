# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

Kashkha-Bot-3000 is built using a **Manager-based Clean Architecture** designed for high modularity and event-driven interaction. The system prioritizes data-driven content, allowing gameplay sequences, questions, and cinematics to be modified via external files (CSVs) and ScriptableObjects without code changes.

**Architecture Philosophy:** Singleton Managers + State Machine + Events + ScriptableObjects.

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         GameManager                              │
│  • State Machine (Wardrobe → HouseHub → Encounter → MiniGame)   │
│  • 4-House Progression Orchestration                            │
│  • Run Lifecycle & Meta-Progression                             │
│  • Streak Combo Tracking                                        │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│     UIManager            │          │    MeterManager          │
│  • HUD & Panel Control   │          │  • Battery & Stomach     │
│  • Wardrobe & Hub UI     │          │  • Stat Modifications    │
│  • Panic Mode Effects    │          │  • Delta-based Events    │
└────────────┬─────────────┘          └────────────┬─────────────┘
             │                                     │
             ▼                                     ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│  FloatingTextManager     │          │   HouseFlowController    │
│  • Feedback Object Pool  │          │  • Sequence Execution    │
│  • RTL Arabic Support    │          │  • Element Coroutines    │
│  • Auto-spawn on Events  │          │  • Interaction Trigger   │
└──────────────────────────┘          └────────┬─────────────────┘
                                               │
                          ┌────────────────────┼─────────────────────┐
                          ▼                    ▼                     ▼
                ┌──────────────┐    ┌──────────────────┐    ┌──────────────────┐
                │SwipeEncounter│    │ CinematicController│   │ InteractionHUD   │
                │  Manager     │    │ • Unified Playback │   │  Controller      │
                │Card Lifecycle │    │ • Fallback Logic   │   │ Input Validation │
                └──────────────┘    └──────────────────┘    └──────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│               Navigation & Transitions                           │
├──────────────────────────┬──────────────────────────────────────┤
│   UnifiedHubManager      │   TransitionPlayer                   │
│  • House Selection       │  • Cross-house Fades                 │
│  • Completion Tracking   │  • Arabic Text Overlays              │
│  • Tabbed Navigation     │  • Async Sequence Control            │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Data & Persistence Layer                      │
├──────────────────────┬───────────────────────┬──────────────────┤
│   DataManager        │   SaveManager         │  WardrobeManager │
│  • CSV Regex Parsing │  • JSON Serialization │  • Outfit Shop   │
│  • Data Pooling      │  • Persistence Logic  │  • Stat Bonuses  │
└──────────────────────┴───────────────────────┴──────────────────┘
```

---

## 📁 Script Responsibilities

### Core Layer
| Script | Responsibility |
|--------|----------------|
| `GameManager.cs` | Main state machine and run lifecycle management. |
| `HouseFlowController.cs` | Executes the sequential flow of questions, cinematics, and interactions in a house. |
| `CinematicController.cs` | Unified system for playing Timelines or DOTween-based text sequences with fallback support. |
| `DataManager.cs` | Centralized data access, CSV parsing, and asset registries. |
| `MeterManager.cs` | Tracks player resources (Battery/Stomach) and broadcasts changes via events. |
| `InputManager.cs` | Centralized wrapper for the Unity Input System, handling touch, shake, and gestures. |
| `MiniGameManager.cs` | Orchestrates the instantiation and completion of inter-house mini-games. |
| `SaveManager.cs` | Handles persistent storage of player progress and currency. |
| `UnifiedHubManager.cs` | Manages the navigation between houses, wardrobe, and upgrades. |

### Gameplay Layer
| Script | Responsibility |
|--------|----------------|
| `SwipeEncounterManager.cs` | Manages the logic of swipe-card encounters, including timers and streak tracking. |
| `SwipeCard.cs` | UI interaction logic for individual Tinder-style cards. |
| `InteractionHUDController.cs` | Handles QTE interactions (Shake, Hold, Tap, Draw) and provides visual prompts. |
| `CatchMiniGame.cs` | Logic for the eidia-catching mini-game. |
| `MemorySwapMiniGame.cs` | Logic for the tile-matching memory mini-game. |
| `PathDrawingGame.cs` | Logic for the maze-path drawing mini-game. |

### UI & Juice Layer
| Script | Responsibility |
|--------|----------------|
| `UIManager.cs` | Controls the visibility and state of all major UI panels. |
| `FloatingTextManager.cs` | Object-pooled system for spawning feedback and reward text. |
| `AudioManager.cs` | Event-driven audio system with cross-fading and state-based music. |
| `CameraShakeManager.cs` | Manages Cinemachine-based screen shakes. |
| `HouseBackgroundController.cs` | Automatically switches backgrounds based on the current house. |
| `PlayerCharacterDisplay.cs` | Displays the player character with their currently equipped outfit. |

---

## 🔗 Communication Patterns

### Event-Driven Flow
The project heavily utilizes C# Actions to maintain loose coupling. 
- **Meters:** `MeterManager.OnBatteryModified` notifies the HUD to update sliders.
- **Encounters:** `SwipeEncounterManager.OnCardProcessed` triggers feedback text and resource changes in the `GameManager`.
- **Game State:** `GameManager.OnStateChanged` allows systems like `AudioManager` to transition music based on the current context.

### Sequence Execution Flow
House gameplay is driven by `HouseSequenceData` assets:
1. `HouseFlowController` iterates through the sequence.
2. For **Questions**, it hands control to `SwipeEncounterManager` and waits for completion.
3. For **Cinematics**, it triggers `CinematicController` and waits for the sequence to end.
4. For **Interactions**, it activates `InteractionHUDController` and waits for successful input or timeout.

### Cinematic Fallback Logic
The `CinematicController` provides a robust playback system:
- **Timeline Mode:** If a Timeline asset exists, it plays it for visual storytelling.
- **DOTween Mode:** If no Timeline is found or text is provided, it uses a typewriter-style UI reveal.
- **Auto-UI Management:** The system automatically hides gameplay UI during cinematics and restores it after.

---

## 🎮 Game Flow

### 1. Wardrobe & Preparation
Players start in the Wardrobe to equip outfits that provide protection against battery drain or stomach fill.

### 2. House Visits (The Gauntlet)
The game consists of 4 distinct houses, each with increasing difficulty and unique sequences:
- **House 1-3:** Standard progression with mixed elements.
- **House 4:** High-intensity "Insane" mode with rapid-fire questions and interactions.

### 3. House Hub
Between houses, players return to the **Unified Hub**. Here they can:
- Select the next unlocked house.
- Visit the mid-run Wardrobe.
- View upgrades and progression.

### 4. Mini-Games
Transitions between houses are bridged by mini-games (`Catch`, `Path`, `Memory`). Successful completion rewards players with Eidia or Tech Scrap.

---

## ⚙️ Data Architecture

### CSV Data Pipeline
- **Questions:** Parsed into `SwipeCardData`, containing text, values, and correct swipe directions.
- **Interactions:** Parsed into `InteractionData`, defining the type (Shake/Hold/Tap/Draw) and thresholds.
- **Outfits:** Parsed into `OutfitData`, defining costs and stat multipliers.

### ScriptableObject Sequences
Houses are configured using `HouseSequenceData` ScriptableObjects. This allows designers to:
- Reorder gameplay elements easily.
- Assign specific IDs to questions or cinematics.
- Validate sequence integrity within the Inspector.

---

## 🛠️ Technical Details

### Input Handling
The `InputManager` abstracts the Unity Input System, providing simple events for:
- **Shake:** Detected via accelerometer.
- **Hold:** Detected via press interactions.
- **Draw:** Detected via screen-space path tracking.
- **Swipe:** Detected via delta movements in `SwipeCard`.

### Post-Processing & Juice
Visual feedback is enhanced via:
- **Chromatic Aberration:** Pulses when the player enters "Panic Mode" (low battery/high stomach).
- **Screen Flash:** Color-coded flashes (Green for correct, Red for wrong).
- **Haptic Feedback:** Contextual vibrations for actions and errors.

---

**Last Updated:** 2026-05-04
**Maintained By:** Core Development Team
**Status:** ✅ **STABLE**
