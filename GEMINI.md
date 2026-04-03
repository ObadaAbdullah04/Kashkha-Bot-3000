# Kashkha-Bot-3000 — Project Context & Guidelines

Kashkha-Bot-3000 (كَشْخَة-بوت 3000) is a comedic cultural survival / rogue-lite mobile game built for the **Ramadan Hackathon 2026**. The player controls a robot designed to survive Jordanian family Eid visits, managing social battery and stomach capacity while navigating cultural expectations.

---

## 🚀 Quick Start

- **Unity Version:** 2022.3.62f3 LTS
- **Render Pipeline:** Universal Render Pipeline (URP) 14.0.12 (2D)
- **Primary Scene:** `Assets/_Project/Scenes/Core_Scene.unity`
- **Data Source:** `Assets/_Project/Data/Encounters.csv` (CSV parsing via `DataManager`)

---

## 🏗️ Technical Architecture

The project follows a **Singleton Manager** pattern with a centralized **State Machine** in `GameManager`.

### Key Managers
- **GameManager:** Orchestrates the 4-house gauntlet, Crossroads decisions, and House 4 Boss Mode.
- **MeterManager:** Tracks Social Battery and Stomach Meter. Implements the "Three-Strike" hospitality system.
- **DataManager:** Handles Regex-based CSV parsing for encounters and outfits.
- **UIManager:** Manages RTL Arabic text display (RTLTMPro), feedback animations, and panel transitions.
- **QTEController:** Handles 4 types of mobile QTEs (Shake, Tap, Swipe, Hold) with House 4 difficulty scaling.
- **TimerController:** Manages encounter countdowns and panic mode visual pulses.
- **WardrobeManager:** Handles meta-progression, outfit purchases, and stat bonus application.

### Core Data Models
- **EncounterData:** Represents a single trivia or hospitality encounter.
- **SaveData:** Persistent JSON-based state (Tech Scrap, Eidia, Owned Outfits).

---

## 🛠️ Development Conventions

### 1. No Hardcoding
**Mandate:** All gameplay tunables (timers, thresholds, multipliers, offsets) **MUST** be exposed via `[SerializeField]` in the Inspector or loaded via CSV. Do not hardcode magic numbers in logic.

### 2. Coding Standards
- **Naming:** PascalCase for classes/methods, camelCase for private fields (e.g., `private int _myField`).
- **Attributes:** Use `NaughtyAttributes` for cleaner Inspector UI (e.g., `[Button]`, `[ReadOnly]`, `[Header]`).
- **Animation:** Use **DOTween** for ALL UI animations, transitions, and "juice".
- **Decoupling:** Prefer the `public static Action` event pattern for manager communication (e.g., `MeterManager.OnBatteryDrained`).

### 3. Localization & RTL
- All Arabic text MUST use **RTLTMPro** components for proper rendering.
- CSV fields ending in `AR` (e.g., `QuestionAR`) contain Arabic strings.

### 4. Performance
- **Object Pooling:** Use `FloatingTextManager` for frequent UI spawns.
- **Canvas Management:** Use `CanvasGroup.alpha` for fading rather than `SetActive()` where possible.

---

## 📂 Project Structure

- `Assets/_Project/Scripts/Core/`: Central systems (GameManager, DataManager, SaveManager).
- `Assets/_Project/Scripts/Gameplay/`: Mechanics (MeterManager, QTEController, TimerController).
- `Assets/_Project/Scripts/UI/`: UI components and HUD logic.
- `Assets/_Project/Data/`: CSV source files (`Encounters.csv`, `Outfits.csv`).
- `Assets/_Project/Prefabs/`: Mini-games, UI elements, and pooled objects.

---

## 🎮 Building and Running

### Build Target
- **Primary:** Android (APK).
- **Secondary:** Windows (Standalone).

### Build Script
- Commands can be triggered via Unity's batch mode if a `BuildScript` is implemented (see `QWEN.md`).

---

## 📝 Recent Milestones
- **Phase 5 Complete:** Input-Based QTE System, Encounter Shuffling, and Path-Drawing Mini-Game implemented.
- **Phase 4 Complete:** Floating Text System and Wardrobe Meta-Progression implemented.
- **Vertical Slice:** The game is currently in a "Complete Vertical Slice" state, ready for content expansion.
