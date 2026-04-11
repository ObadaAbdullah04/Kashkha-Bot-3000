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

## 🏗️ Architecture & Systems

The project follows a **Manager-based Clean Architecture** with event-driven communication to maintain loose coupling.

### Core Managers (Singletons)
- **GameManager:** Orchestrates game states (`Wardrobe`, `HouseHub`, `Encounter`, `InterHouseMiniGame`, `GameOver`, `Win`).
- **HouseFlowController (Phase 16):** Drives the "self-driving" house sequences. Loads `HouseSequenceData` and executes elements (Questions, Cinematics, Interactions) one by one.
- **DataManager:** Handles Regex-based CSV parsing for Questions, Interactions, and Outfits. Manages question pools and randomization.
- **MeterManager:** Tracks `Social Battery` (0-100) and `Stomach Meter` (0-100).
- **UIManager:** Manages UI panels, HUD, and screen effects.
- **SaveManager:** JSON-based persistence for Tech Scrap, Eidia, and unlocked outfits.
- **AudioManager:** Event-driven SFX and music transitions.

### Key Systems
- **Swipe System:** Tinder-style cards with explicit correct/incorrect sides defined in CSV. Includes a streak combo system (+3, +5, +8 Eidia).
- **Sequence System:** Each house is an ordered list of `SequenceElement` (Question, Cinematic, or Interaction).
- **Cinematic System:** Unified playback supporting both Unity Timeline assets and DOTween-based typewriter text with smart fallback logic.
- **Mini-Games:** Inter-house "Catch" games and other mini-interactions.

## 📁 Project Structure

```
Assets/_Project/
├── Art/                # Sprites, UI, Materials
├── Controls/           # Input System Actions (DeviceControls)
├── Data/               # CSV Files (Questions, Outfits, Interactions)
├── Editor/             # Custom Tools (e.g., Placeholder Generator)
├── Prefabs/            # UI, Mini-Games, Obstacles
├── Resources/          # Runtime-loadable (Sequences, Timelines)
├── Scripts/
│   ├── Core/           # Manager classes (GameManager, UIManager, etc.)
│   ├── Data/           # ScriptableObjects and Data Models
│   ├── Gameplay/       # Mechanics (MeterManager, SwipeEncounter)
│   └── UI/             # UI Components (SwipeCard, FloatingText)
└── Scenes/             # Core_Scene (Main Entry Point)
```

## 🛠️ Building and Running

- **Main Scene:** `Assets/_Project/Scenes/Core_Scene.unity`
- **Entry Point:** Press Play in the editor. `GameManager` initializes via `Awake`.
- **Build Target:** Android. Ensure `UniversalRenderPipelineGlobalSettings` are correctly assigned in Project Settings.
- **Dependencies:** Ensure DOTween is initialized (`Tools -> Demigiant -> DOTween Setup`).

## 📜 Development Conventions

### ✅ DO
- **Expose Tunables:** Use `[SerializeField]` with `[Tooltip]` for all magic numbers (timers, thresholds).
- **Use DOTween:** All UI animations and "juice" must use DOTween. Avoid legacy Animation components for UI.
- **Event-Driven UI:** UI should listen to manager events (e.g., `MeterManager.OnBatteryModified`) rather than polling in `Update`.
- **RTL Support:** Use `RTLTextMeshPro` components for all Arabic text.
- **Surgical Edits:** When modifying systems, respect the "Phase" updates documented in `ARCHITECTURE.md` and `QWEN.md`.

### ❌ DON'T
- **Hardcode Values:** Never hardcode gameplay values; use CSVs or ScriptableObjects.
- **Direct References:** Avoid tight coupling between managers. Use `static Action` events where possible.
- **Legacy Input:** Do not use `Input.GetKeyDown`. Use `InputManager.Instance` or `DeviceControls` actions.

## 🔗 Data Pipeline

Data is managed via CSVs in `Assets/_Project/Data/`:
- **Questions.csv:** Pooled questions (10 per house).
- **Interactions.csv:** QTE interactions (Shake, Hold, Tap, Draw).
- **Outfits.csv:** Wardrobe items and stat modifiers.

**Note:** Correctness is explicit in CSV (`CorrectSide: 1=Right, 0=Left`).

---
*For deep technical details, refer to `Assets/_Project/ARCHITECTURE.md`.*
