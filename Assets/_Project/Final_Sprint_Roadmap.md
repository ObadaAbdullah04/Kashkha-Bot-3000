# Final Sprint Roadmap: Unity Game Development Checklist

This comprehensive, step-by-step checklist utilizes your existing **Manager-based Clean Architecture** and the new **Lean FTUE (First-Time User Experience)** approach to maximize polish while preventing technical debt before the **May 27 deadline**.

---

## 🛠️ Phase 1: The "Lean FTUE" System
**Objective:** Guide players through the Hub and House 1 using a centralized overlay and decentralized triggers, avoiding complex UI masking.

- [x] **TutorialOverlayManager (Centralized Presentation)**
    - Create a persistent UI Prefab with a high-sorting Canvas (`Sort Order: 999`).
    - Add a full-screen, semi-transparent black background image (**Raycast Target: ON**) to block accidental background taps.
    - Implement `public void ShowTutorial(Transform targetToHighlight, string instructionText, Action onComplete)`.
    - Use **DOTween** to smoothly move a glowing ring sprite to the screen coordinates of the target.
    - Use `transform.DOScale` (with `Ease.OutBack`) on the text container to pop it into view.
- [x] **SaveManager Debug Setup**
    - Add `public bool forceTutorialsOn = true;` to `SaveManager.cs`.
    - Implement `HasSeenTutorial(string key)` that returns `false` if the debug toggle is on, otherwise checks `PlayerPrefs`.
    - Implement `MarkTutorialAsComplete(string key)`.
- [x] **Hub Sequential Sequence**
    - Script a `HubTutorialTrigger.cs` that uses a Coroutine or Queue to chain `ShowTutorial` calls: Greeting -> Houses -> Upgrades -> Wardrobe.
- [x] **Event-Driven Gameplay Tutorials**
    - In `SwipeCard.cs`: Check save data on first spawn. Trigger `Time.timeScale = 0`, call `ShowTutorial`, and resume time in the callback.
    - In `InteractionHUDController.cs`: On first QTE spawn, trigger `Time.timeScale = 0.1f` (Slow-mo) and use DOTween to punch the scale of a localized "SHAKE!" prompt.
- [x] **RTL Arabic Polish**
    - Ensure all text uses `RTLTextMeshPro`.
    - **CRITICAL:** Do NOT use DOTween typewriter effects (`.DOText()`) on Arabic strings. Only animate the CanvasGroup fade or RectTransform scale of the text *container* to prevent broken ligatures.

---

## 📊 Phase 2: HUD & UI Clarity
**Objective:** Communicate clearly when something changes (resources, currency).

- [x] **Persistent Currency Hooks:** Add `public static event Action<int> OnCurrencyChanged` to `SaveManager.cs`. Fire when Eidia or Tech Scrap is modified.
- [x] **Currency UI Animation:** In `UIManager.cs`, subscribe to currency events and use `transform.DOPunchScale` on the currency icon when updated.
- [x] **Dynamic Resource Meters:** Use `Mathf.Lerp` in `UIManager.cs` to smooth slider transitions based on `MeterManager.OnBatteryModified`.
- [x] **Floating Combat Text:** Spawn "+10" / "-15" popups over Battery or Stomach elements using `FloatingTextManager.cs`.

---

## ⚡ Phase 3: Gameplay Pacing & "Juice"
**Objective:** Make core mechanics feel tight, intentional, and fun.

- [x] **CSV Pacing Pass:** Update `Questions.csv` and `Interactions.csv` to smooth difficulty curves for early houses and ramp up for House 4.
- [x] **Coyote Time:** Add a 0.2s grace period to QTE timers in `InteractionHUDController.cs` to improve perceived fairness.
- [x] **Action Validation:** Add low-opacity Green/Red screen flashes and device haptics via `InputManager` for successful/failed swipes.
- [x] **Animator Controller:** Replace static logic in `PlayerCharacterDisplay.cs` with a proper Unity **Animator Controller** triggered by Manager events.

---

## 🎲 Phase 4: Mini-Game Expansion
**Objective:** Remove simple UI panels and create immersive 2D scenes.

- [ ] **Refactor Transitions:** Use `TransitionPlayer.cs` for full-screen wipes with Arabic text overlays during mini-game loading.
- [ ] **Environment Instantiation:** Update `MiniGameManager.cs` to treat mini-games as standalone scenes.

---

## 🎬 Phase 5: Trailer & Final Submission
**Objective:** Secure the prize with a flawless build and compelling presentation.

- [ ] **Intro Cinematic:** Ensure the 15-second Veo AI MP4 plays flawlessly at the start of House 1.
- [ ] **Trailer Capture:** Record 2 minutes of real gameplay highlighting the Lean FTUE, Ghost Hand tutorial, and House 4 VFX.
- [ ] **Final Build Validation:** Perform end-to-end testing to ensure stability for the May 27 deadline.