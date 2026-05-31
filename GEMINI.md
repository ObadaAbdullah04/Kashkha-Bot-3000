# Kashkha-Bot-3000 — Project Context & Instructions

Kashkha-Bot-3000 (كَشْخَة-بوت 3000) is a comedic cultural survival / rogue-lite mobile game built for the **Ramadan Hackathon 2026**. The player acts as a robot's "social intelligence module," navigating Jordanian Eid visits through swipe-card interactions, mini-games, and resource management.

## 📐 Project Overview

- **Engine:** Unity 2022.3.62f3 LTS
- **Render Pipeline:** Universal Render Pipeline (URP) 14.0.12 (2D Template)
- **Target Platform:** Android (Mobile)
- **Core Loop:** Wardrobe (Upgrades) → Unified Hub → House Sequences (Questions/Cinematics/Interactions) → Mini-Games → Win/Game Over.
- **Key Technologies:**
    - **DOTween:** Animation and UI tweening.
    - **RTLTMPro:** Arabic RTL text support.
    - **NaughtyAttributes:** Inspector enhancements.
    - **Unity Input System:** New Input System (1.14.2).
    - **Unity Timeline:** Cinematic orchestration.
    - **Cinemachine:** Camera impulses and screen shake.

## 🏗️ Architecture & Systems

The project follows a **Manager-based Clean Architecture** with event-driven communication to maintain loose coupling.

### Core Managers (Singletons)
- **GameManager:** Orchestrates game states (`Wardrobe`, `HouseHub`, `Encounter`, `InterHouseMiniGame`, `GameOver`, `Win`). Manages run lifecycle and streak tracking.
- **HouseFlowController:** Drives the sequence-based house gameplay. Loads `HouseSequenceData` and executes elements (Questions, Cinematics, Interactions) sequentially via coroutines.
- **DataManager:** Handles Regex-based CSV parsing for Questions, Interactions, and Outfits. Manages data pools and registries.
- **MeterManager:** Tracks `Social Battery` and `Stomach Meter`. Fires delta-based events for UI updates.
- **UIManager:** Master UI controller for panels, HUD, and screen effects. Supports "Panic Mode" visuals.
- **SaveManager:** JSON-based persistence for Tech Scrap, Eidia, and unlocked outfits.
- **AudioManager:** Event-driven SFX and music transitions with cross-fade logic.
- **CinematicController:** Unified playback system supporting Unity Timeline assets and DOTween-based typewriter text with smart fallback.

### Key Systems
- **Swipe System:** Tinder-style interaction cards. Includes a streak combo system that rewards players with bonus Eidia.
- **Sequence System:** Each house visit is an ordered list of `SequenceElement` (Question, Cinematic, or Interaction) defined in ScriptableObjects.
- **Interaction System:** Standalone QTE prompts for Shake, Hold, Tap, and Draw inputs, integrated into house sequences and Timelines.
- **Mini-Games:** Inter-house challenges including:
    - **CatchGame:** Time-attack eidia catching.
    - **PathDrawing:** Maze path drawing.
    - **MemorySwap:** Tile matching memory game with hint mechanics.
- **Background System:** Dynamic background management via `HouseBackgroundController` and `MiniGameBackgroundLoader`.
- **Wardrobe System:** Outfit system where players spend Tech Scrap to gain stat modifiers (Battery/Stomach protection).

## 📁 Project Structure

```
Assets/_Project/
├── Art/                # Sprites, UI, Materials
├── Controls/           # Input System Actions (DeviceControls)
├── Data/               # CSV Files (Questions, Outfits, Interactions)
├── Editor/             # Custom Tools (e.g., Prefab Helpers)
├── Prefabs/            # UI, Mini-Games, Environment
├── Resources/          # Runtime-loadable (Sequences, Timelines, Backgrounds)
├── Scripts/
│   ├── Core/           # Manager classes (GameManager, UIManager, etc.)
│   ├── Data/           # ScriptableObjects and Data Models
│   ├── Gameplay/       # Mechanics (MeterManager, SwipeEncounter)
│   ├── UI/             # UI Components (SwipeCard, HUD, Wardrobe)
│   └── Editor/         # Editor-only scripts
└── Scenes/             # Core_Scene (Main Entry Point)
```

## 🛠️ Building and Running

- **Main Scene:** `Assets/_Project/Scenes/Core_Scene.unity`
- **Entry Point:** Press Play in the editor. `GameManager` initializes the run.
- **Build Target:** Android. 
- **Dependencies:** Ensure DOTween is initialized (`Tools -> Demigiant -> DOTween Setup`).

## 📜 Development Conventions

### ✅ DO
- **Expose Tunables:** Use `[SerializeField]` with `[Tooltip]` for all magic numbers (timers, thresholds).
- **Use DOTween:** All UI animations and "juice" must use DOTween.
- **Event-Driven UI:** UI should listen to manager events (e.g., `MeterManager.OnBatteryModified`) rather than polling.
- **RTL Support:** Use `RTLTextMeshPro` components for all Arabic text.
- **Data-Driven:** Keep gameplay content in CSVs or ScriptableObjects.

### ❌ DON'T
- **Hardcode Values:** Never hardcode gameplay values; use the Data Pipeline.
- **Direct References:** Avoid tight coupling between managers. Use static events or the Singleton pattern carefully.
- **Legacy Input:** Do not use `Input.GetKeyDown`. Use `InputManager.Instance` or `DeviceControls` actions.

## 🔗 Data Pipeline

Data is managed via CSVs in `Assets/_Project/Data/` and ScriptableObjects in `Resources/Sequences/`:
- **Questions.csv:** Pooled swipe-card questions.
- **Interactions.csv:** QTE interaction configurations.
- **Outfits.csv:** Wardrobe items and stat modifiers.
- **HouseSequenceData:** ScriptableObjects defining the flow of each house visit.

---
*For deep technical details, refer to `Assets/_Project/ARCHITECTURE.md`.*

## 🚀 Recent Implementations (Phases 1-19)

The project has undergone significant expansion, reaching **Phase 19** with a focus on polished transitions, unified UI, and refined gameplay loops.

### 🏛️ 1. Unified Hub Architecture (Phase 6-10)
- **Single Source of Truth:** Replaced multiple scenes/panels with a **Unified Hub** containing three tabs: Houses, Wardrobe, and Upgrades.
- **Persistent Progress:** The Hub tracks house completion and manages the mid-run loop.

### 🎬 2. Unified Cinematic System (Phase 15)
- **Hybrid Playback:** `CinematicController` now supports both **Unity Timeline** (for complex scenes) and **DOTween Typewriter** (for quick dialogue) with automatic UI management.
- **Full-Screen Video:** Integrated MP4 playback support for high-fidelity intros and transitions.

### 🎮 3. Expanded Mini-Games (Phase 5C, 17)
- **Path-Drawing Maze:** A gesture-based maze navigation challenge.
- **Memory Swap:** A tile-matching memory game with memorization phases.
- **Catch Game:** Optimized time-attack eidia catching.

### 🧹 4. Phase 18 Optimization Pass (The "Polish" Phase)
- **Currency Consolidation:** Merged all currencies into **Total Eidia** for a cleaner economy.
- **Simplified Wardrobe:** Streamlined outfit selection and stat modifiers.
- **Audio Overhaul:** Centralized `AudioManager` with event-driven SFX and cross-fading music.
- **Smart Backgrounds:** `HouseBackgroundController` and `MiniGameBackgroundLoader` automatically handle visual context switching.
- **Clean State Management:** Implemented strict scene reload patterns and event clearing to prevent memory leaks and "ghost" references.

### ⏱️ 5. Interaction Refinement (Phase 13-19)
- **Standalone QTEs:** Integrated Shake, Hold, Tap, and Draw interactions directly into house sequences.
- **Smart Timer:** Phase 19 logic ensures the timer is only visible during active questions, hiding it during cinematics and interactions for better immersion.
- **Tutorial Overlay:** Enhanced `TutorialOverlayManager` with persistent completion tracking and visual pointers.
