# Lean FTUE System Documentation (كَشْخَة-بوت 3000)

This document explains the implementation of the Data-Driven First-Time User Experience (FTUE) system and provides instructions on how to configure and extend it.

## 🏗️ System Architecture

The FTUE system is built to be modular and data-driven, allowing designers to create complex tutorial sequences without writing code.

### Key Components
1. **`TutorialOverlayManager.cs`**: The central Singleton manager that handles the UI overlay, glowing ring positioning, and sequence execution. Now includes a **Manual Targets** list for centralized assignment.
2. **`TutorialTarget`**: A "marker" script (now part of `TutorialOverlayManager.cs`). Attach this to any UI element you want the tutorial to point at if you prefer local assignment. It registers the element with the manager using a unique `TargetID`.
3. **`DataManager.cs`**: Parses the `Tutorials.csv` file into a dictionary of steps.
4. **`TutorialData.cs`**: Contains the data structures used to define tutorial steps.

---

## 📊 CSV Configuration (`Tutorials.csv`)

Tutorials are defined in a CSV file located at `Assets/_Project/Data/Tutorials.csv`.

### Column Definitions
| Column | Description |
| :--- | :--- |
| **TutorialID** | Unique ID for the sequence (e.g., `House1_Intro`). |
| **StepIndex** | The order of the step (starts at 0). |
| **TargetID** | The ID of the target to highlight (mapped in Manager or on `TutorialTarget`). |
| **InstructionAR** | The Arabic text to display in the instruction bubble. |
| **RequireTargetClick** | `1` to wait for the actual UI button to be clicked, `0` to allow clicking anywhere. |
| **TimeScale** | Game speed during this step (`0` = Paused, `0.1` = Slow-mo, `1` = Real-time). |

---

## 🔌 Unity Wiring Guide

### 1. The Global Overlay Setup
1. Create a GameObject named `TutorialOverlayManager` (should be a prefab or in a persistent scene).
2. Attach the `TutorialOverlayManager` script.
3. **Centralized Assignment (Recommended)**:
   - In the **Tutorial Targets** list in the inspector, add items.
   - Enter the **Target ID** (matches CSV) and drag the **RectTransform** into the slot.
4. **UI Hierarchy Requirements**:
   - **Canvas**: Sort Order 100+.
   - **Graphic Raycaster**: Must be assigned to the script slot.
   - **Background**: A full-screen Image (black alpha) to block interaction when needed.
   - **Dismiss Button**: An invisible full-screen button.
   - **Glowing Ring**: A UI Image used as the indicator.
   - **Instruction Container**: A UI Panel/Image with an **RTLTextMeshPro** child.

### 2. Highlighting UI Elements (Alternative)
To make a UI element "tutorial-ready" without adding it to the manager's list:
1. Attach the `TutorialTarget` script to the object.
2. Enter a unique **TargetID** (e.g., `"Timer"`, `"WardrobeTab"`, `"UpgradeBtn"`).
3. Ensure the `TargetID` in your CSV matches this string exactly.

### 3. Manager Assignment
1. Open your `DataManager` object.
2. Drag `Tutorials.csv` into the **Tutorials CSV** slot.

---

## 🚀 Usage from Code

### Triggering a Sequence
To play a sequence defined in the CSV:
```csharp
TutorialOverlayManager.Instance.PlayTutorial("TutorialID", () => {
    // Callback when the entire sequence is finished
    Debug.Log("Tutorial Complete!");
});
```

### Manual Overlay (One-off)
To show a quick instruction without a CSV entry:
```csharp
TutorialOverlayManager.Instance.ShowTutorial(targetTransform, "Arabic Text", true, () => {
    // Callback on dismiss
});
```

---

## 🛠️ Testing & Debugging
- **Force Reset**: Click **"Reset Progress"** in the `SaveManager` to clear the "HasSeen" flags.
- **Debug Toggle**: Use the **"Force Tutorials On"** checkbox in `SaveManager` to repeat tutorials during every play session.
- **Positioning**: If the glowing ring is off-center, ensure the target object's **Pivot** is set to `(0.5, 0.5)` and its `TutorialTarget` is on the correct RectTransform.
