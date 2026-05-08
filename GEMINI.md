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

## 🚀 Recent Implementation: Phase 1 (Onboarding & FTUE)

We have successfully implemented the core onboarding systems. **Manual Editor setup is required to activate visuals.**

> [!IMPORTANT]
> Follow the **[PHASE_1_UNITY_SETUP.md](./PHASE_1_UNITY_SETUP.md)** checklist for step-by-step instructions on Hierarchy and Inspector assignments.

### 🎥 1. Video Intro System
Allows playing full-screen MP4 cinematics (e.g., Intro, Prologue) as part of any house sequence.

### ✋ 2. "Ghost Hand" Swipe Tutorial
An animated hand that teaches the core swipe mechanic on the first card of House 1. Includes background dimming.

### ⚡ 3. QTE Micro-Tutorials (Shake, Hold, Tap, Draw)
Pauses the timer the first time a player encounters a new interaction type. Loads gesture icons from `Resources/TutorialIcons/`.

### 🔄 4. Global Tutorial Reset
Tutorials are automatically reset at the start of every new run via `GameManager.StartRun()`.
