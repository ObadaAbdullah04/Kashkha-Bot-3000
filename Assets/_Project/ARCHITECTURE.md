# Kashkha-Bot-3000 — Architecture Documentation

## 📐 System Overview

Kashkha-Bot-3000 is built using a **Manager-based Clean Architecture** designed for high modularity and event-driven interaction. The system prioritizes data-driven content, allowing gameplay sequences, questions, and cinematics to be modified via external files (CSVs) and ScriptableObjects without code changes.

**Architecture Philosophy:** Singleton Managers + State Machine + Events + ScriptableObjects.

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         GameManager                              │
│  • State Machine (MainMenu → Hub → House → MiniGame → End)      │
│  • Phase 18: Clean State Management (Strict Scene Reloads)      │
│  • Run Lifecycle & Meta-Progression Tracking                    │
│  • Total Eidia Currency Management                              │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
               ▼                                  ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│     UIManager            │          │    MeterManager          │
│  • Unified Hub (Tabs)    │          │  • Battery & Stomach     │
│  • HUD & Panel Control   │          │  • Stat Modifications    │
│  • Panic Mode Effects    │          │  • Delta-based Events    │
└────────────┬─────────────┘          └────────────┬─────────────┘
             │                                     │
             ▼                                     ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│  FloatingTextManager     │          │   HouseFlowController    │
│  • Feedback Object Pool  │          │  • Self-Driving Sequence │
│  • RTL Arabic Support    │          │  • Phase 9.6: Coroutines │
│  • Auto-spawn on Events  │          │  • Interaction Trigger   │
└──────────────────────────┘          └────────┬─────────────────┘
                                               │
                          ┌────────────────────┼─────────────────────┐
                          ▼                    ▼                     ▼
                ┌──────────────┐    ┌──────────────────┐    ┌──────────────────┐
                │SwipeEncounter│    │ CinematicController│   │ InteractionHUD   │
                │  Manager     │    │ • Phase 15: Unified│   │  Controller      │
                │Card Lifecycle │    │ • Timeline/DOTween │   │ QTE Validation   │
                └──────────────┘    └──────────────────┘    └──────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│               Navigation & Visual Context                        │
├──────────────────────────┬──────────────────────────────────────┤
│   TutorialOverlayManager │   Background Management              │
│  • Phase 18: Tracking    │  • HouseBackgroundController         │
│  • UI Masking/Highlight  │  • MiniGameBackgroundLoader          │
│  • Instruction Pointers  │  • Visual Consistency Layer          │
└──────────────────────────┴──────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Data & Persistence Layer                      │
├──────────────────────┬───────────────────────┬──────────────────┤
│   DataManager        │   SaveManager         │  WardrobeManager │
│  • Regex CSV Parsing │  • JSON Serialization │  • Phase 18:     │
│  • Data Pooling      │  • Phase 18: Merged   │    Simplified    │
└──────────────────────┴───────────────────────┴──────────────────┘
```

---

## 📁 Script Responsibilities

### Core Layer
| Script | Responsibility |
|--------|----------------|
| `GameManager.cs` | Main state machine; manages run lifecycle and strict scene reloads (Phase 18). |
| `HouseFlowController.cs` | Self-driving coroutine-based sequence execution (Phase 9.6). |
| `CinematicController.cs` | Unified system for Timeline, DOTween dialogue, and MP4 Video (Phase 15). |
| `DataManager.cs` | Centralized data access, Regex CSV parsing, and asset registries. |
| `MeterManager.cs` | Tracks player resources (Battery/Stomach) and broadcasts changes. |
| `AudioManager.cs` | Event-driven audio system with cross-fading and state-based music (Phase 18). |
| `TransitionPlayer.cs` | Handles polished transitions between houses and mini-games. |

### Gameplay Layer
| Script | Responsibility |
|--------|----------------|
| `SwipeEncounterManager.cs` | Manages swipe-card encounters and timers (hidden during QTEs/Cinematics). |
| `InteractionHUDController.cs` | Handles standalone QTE interactions (Shake, Hold, Tap, Draw). |
| `CatchMiniGame.cs` | Time-attack eidia catching logic. |
| `MemorySwapMiniGame.cs` | Tile-matching memory game with memorization phase (Phase 17). |
| `PathDrawingGame.cs` | Gesture-based maze path drawing (Phase 5C). |

### UI & Juice Layer
| Script | Responsibility |
|--------|----------------|
| `UIManager.cs` | Master controller for the Unified Hub and gameplay panels. |
| `TutorialOverlayManager.cs` | Manages the tutorial FTUE, masking, and pointers. |
| `HouseBackgroundController.cs` | Automatic context-based background switching (Phase 18). |
| `URPPostProcessing.cs` | Dynamic visual effects (Panic Mode, Flashes). |
| `FloatingTextManager.cs` | Object-pooled feedback text system. |

---

## 🔗 Communication Patterns

### Event-Driven Flow
The project utilizes C# Actions to maintain loose coupling:
- **Meters:** `MeterManager.OnBatteryModified` notifies HUD and Post-Processing.
- **Save Data:** `SaveManager.OnEidiaChanged` (Phase 18) updates the unified currency display.
- **Game State:** `GameManager.OnStateChanged` triggers music transitions and panel visibility.

### Sequence Execution Flow (Phase 9.6)
House gameplay is driven by a "Self-Driving" coroutine in `HouseFlowController`:
1. It iterates through `SequenceElement` (Question, Cinematic, or Interaction).
2. It yields control to the specific manager (e.g., `SwipeEncounterManager`).
3. It waits for a completion callback before advancing, allowing for variable-paced gameplay.

### Cinematic Fallback & Video (Phase 15)
The `CinematicController` is a unified entry point for all storytelling:
- **Timeline:** Preferred for choreographed scenes.
- **DOTween:** Used for dynamic typewriter text dialogue.
- **Video:** MP4 playback via `VideoPlayer` for pre-rendered cinematics.
- **UI Management:** Automatically hides meters/HUD during cinematics and restores them after.

---

## 🎮 Game Flow

### 1. Main Menu & Start
Players start in the Main Menu. Starting a run initializes the `GameManager` and resets tutorial states.

### 2. Unified Hub (Phase 10)
Between houses, players return to the Hub. It acts as the "Social Dashboard":
- **House Selection:** Choose the next unlocked house.
- **Wardrobe:** Equip outfits using Total Eidia.
- **Upgrades:** Spend currency on permanent stat boosts.

### 3. House Sequences (Phases 9-19)
Each house visit is a data-driven sequence of events. The `HouseFlowController` ensures smooth transitions between questions and QTEs.

### 4. Inter-House Mini-Games
Transitions are bridged by randomized mini-games. These serve as a "High-Risk, High-Reward" break between houses.

---

## 🛠️ Technical Details

### Phase 18 Optimization
- **Strict Isolation:** Managers clear static events on destroy to prevent references between scene reloads.
- **Instant Transitions:** Support for instant black fades to ensure no NPC/UI "flashes" during state changes.
- **Resource Management:** Smart loading of backgrounds and mini-game assets to minimize memory footprint.

---

**Last Updated:** 2026-05-30
**Maintained By:** Core Development Team
**Status:** ✅ **STABLE (Phase 19)**
