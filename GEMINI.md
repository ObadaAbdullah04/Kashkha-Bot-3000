# Kashkha-Bot-3000 — Project Context

## Project Overview
**Kashkha-Bot-3000** (كَشْخَة-بوت 3000) is a comedic cultural survival / rogue-lite mobile game developed for the **Ramadan Hackathon 2026**. The player takes on the role of a Jordanian developer who builds an AI robot to handle family Eid visits. The core gameplay revolves around managing social encounters through a swipe-card system while balancing survival meters.

### Core Mechanics
- **Swipe-Card Dialogue:** Pick dialogue options via Tinder-style swipes to navigate social situations.
- **Dual Meter System:**
    - **Social Battery:** Drains on rude or incorrect answers.
    - **Stomach Meter:** Fills when accepting hospitality (don't let it explode from too much Ma'amoul!).
- **Eidia Collection:** Earn "Eidia" (Eid money) to reach the win condition (standard: 100 JOD).
- **Meta-Progression:** Collect "Tech Scrap" to buy permanent outfits in the Wardrobe, which provide stat modifiers.
- **Mini-Games:** Inter-house games (e.g., catching falling items) to earn extra rewards.

### Technical Stack
- **Engine:** Unity 2022.3.62f3 LTS
- **Render Pipeline:** Universal Render Pipeline (URP) 2D
- **Language:** C# (.NET IL2CPP)
- **Input:** Unity Input System
- **Key Libraries:**
    - **DOTween:** High-performance tweening for all UI animations and game juice.
    - **NaughtyAttributes:** Enhanced inspector attributes.
    - **RTLTMPro:** Support for Right-to-Left (Arabic) text in TextMeshPro.
    - **Cinemachine:** Camera effects and screen shake.

---

## Building and Running

### Prerequisites
- **Unity Hub** with version **2022.3.62f3 LTS** installed.
- **Visual Studio 2022** or **JetBrains Rider** for C# development.
- Target platform: **Android** (primary) or PC.

### Opening the Project
1. Open Unity Hub and add the project folder.
2. Ensure the Unity version matches **2022.3.62f3**.
3. Open the main scene: `Assets/_Project/Scenes/Core_Scene.unity`.

### Running the Game
- Press the **Play** button in the Unity Editor while `Core_Scene` is open.

### Building
- Go to `File > Build Settings`.
- Select **Android** as the platform.
- Click **Build** to generate the APK.

---

## Development Conventions

### Coding Standards
- **Naming:** PascalCase for classes/methods, camelCase for private fields.
- **Patterns:**
    - **Singletons:** Used for global managers (`GameManager`, `UIManager`, `DataManager`, etc.).
    - **Event-Driven:** Decoupled communication using `Action` events (e.g., `OnBatteryModified`).
    - **State Machine:** Centralized game flow management in `GameManager`.
- **UI:** Uses `CanvasGroup.alpha` and DOTween for transitions rather than `SetActive` where possible for performance.
- **Data:** All dialogue, questions, and outfits are driven by **CSV files** in `Assets/_Project/Data/`. **Do not hardcode gameplay values.**

### Asset Pipeline
- **Arabic Text:** Always use `RTLTMPro` components for Arabic rendering.
- **Animations:** Prefer DOTween code-based animations over the legacy Animator for simple UI/Juice effects.
- **CSV Parsing:** Handled by `DataManager.cs` using Regex to support Arabic punctuation and quoted strings.

---

## Key Files & Directories

- `Assets/_Project/Scripts/Core/`:
    - `GameManager.cs`: Main state machine and run orchestration.
    - `UIManager.cs`: Centralized UI controller.
    - `DataManager.cs`: CSV loading and parsing logic.
    - `InputManager.cs`: Centralized input handling for the new Input System.
- `Assets/_Project/Scripts/Gameplay/`:
    - `SwipeEncounterManager.cs`: Logic for the card-based encounters and wave system.
    - `MeterManager.cs`: Handles battery and stomach meter logic.
    - `MiniGameManager.cs`: Manages the inter-house mini-games.
- `Assets/_Project/Scripts/UI/`:
    - `SwipeCard.cs`: Visual and interaction logic for the swipeable cards.
- `Assets/_Project/Data/`:
    - `Questions.csv`: Primary database for dialogue and social encounters.
    - `Outfits.csv`: Configuration for wardrobe items and stat bonuses.
    - `Cutscenes.csv`: Dialogue for story moments.
- `Assets/_Project/ARCHITECTURE.md`: Comprehensive technical documentation.
- `QWEN.md`: High-level project context and design notes.

---

## Workflow Notes
- **Testing Changes:** Always test in the `Core_Scene` as it initializes all singleton managers.
- **Adding Content:** To add new dialogue or houses, update the corresponding CSV in `Assets/_Project/Data/`.
- **UI Animation:** Use `transform.DOPunchScale` or `transform.DOShakePosition` for "juice" effects on UI elements.
