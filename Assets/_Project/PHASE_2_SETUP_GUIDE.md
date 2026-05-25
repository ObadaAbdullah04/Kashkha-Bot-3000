# Phase 2 & 3 Setup Guide (كَشْخَة-بوت 3000)

This guide covers the manual Unity Editor steps required to activate the new HUD clarity and gameplay pacing systems.

## 📊 Phase 2: HUD & UI Clarity

### 1. Floating Combat Text (+10, -5)
The new `FloatingTextManager` needs to be placed in the scene to show resource changes visually.

1. **Create the Manager**:
    - Create an empty GameObject named `FloatingTextManager` as a child of your **Main Canvas**.
    - Attach the `FloatingTextManager.cs` script.
2. **Create the Text Prefab**:
    - Create a new UI Prefab named `FloatingTextFeedback`.
    - It should have:
        - **RectTransform**: Pivot (0.5, 0.5).
        - **CanvasGroup**: For fading.
        - **RTLTextMeshPro**: For the Arabic text (set alignment to Center).
    - Assign this prefab to the `Text Prefab` slot in the `FloatingTextManager` inspector.
3. **Manager Assignment**:
    - Ensure the `Main Panel` or `HUD Panel` is assigned to `FloatingTextManager`'s parent if you want them to move with the UI.

### 2. Hub Tutorial Trigger
The Hub tutorial now chains multiple steps (Greeting -> Houses -> Upgrades -> Wardrobe).

1. **Attach the Trigger**:
    - Select the `UnifiedHubPanel` in your scene.
    - Attach the `HubTutorialTrigger.cs` script.
2. **Configure (Optional)**:
    - `Delay`: 0.5s (wait for UI to settle).
    - `Tutorial ID`: `TUT_HUB`.
    - `Save Key`: `HasSeenHubTutorial`.

---

## ⚡ Phase 3: Gameplay Pacing & Juice

### 1. Screen Flashes (Swipe Feedback)
The `SwipeEncounterManager` now triggers full-screen flashes via `ScreenFlash.Instance`.

1. **Verify ScreenFlash**:
    - Ensure a GameObject with `ScreenFlash.cs` exists under your Canvas.
    - It must have an **Image** component (full-screen, low alpha) and a **CanvasGroup**.
    - If it's missing, follow the instructions in the `ScreenFlash.cs` header.

### 2. Coyote Time & Grace Period
- This is already active in code (0.2s extra time). No Editor setup required.
- Partial success is also active: If the player reaches **50% of the threshold** but times out, it still counts as a success.

---

## 🎲 Pacing Adjustments (CSVs)
Difficulty has been balanced:
- **House 1**: Very forgiving (Slow timers, low thresholds, minimal penalties).
- **House 4**: Intense (Fast timers, high thresholds, large battery drain on failure).

To test these, simply press Play. If you want to re-watch tutorials, use the **"Reset Tutorials"** button on the `SaveManager`.
