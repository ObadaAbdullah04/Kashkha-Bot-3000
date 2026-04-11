# Phase 14: Sound, Animation & Game Feel — Master Plan

**Created:** April 10, 2026  
**Status:** PLANNING COMPLETE — Ready for implementation  
**Prerequisites:** Phase 13 (Interaction System) must be fully set up and tested

---

## Table of Contents

1. [Current State Inventory](#1-current-state-inventory)
2. [Phase 14A: Sound System Enhancement](#2-phase-14a-sound-system-enhancement)
3. [Phase 14B: Visual Feedback Polish](#3-phase-14b-visual-feedback-polish)
4. [Phase 14C: Cinematic Enhancements](#4-phase-14c-cinematic-enhancements)
5. [Phase 14D: Game Feel Juice](#5-phase-14d-game-feel-juice)
6. [Implementation Checklist](#6-implementation-checklist)
7. [Asset Requirements List](#7-asset-requirements-list)
8. [Recommended Order](#8-recommended-implementation-order)

---

## 1. Current State Inventory

### Existing Systems — Code Complete, Need Assets/Wiring

| System | File(s) | Status | What Works | What's Missing |
|--------|---------|--------|------------|----------------|
| **AudioManager** | `AudioManager.cs` | Code, no assets | PlaySFX, PlayCorrectAnswer, PlayWrongAnswer, PlayMusic with crossfade | Zero audio clips assigned. PlayMusic never called. No SFX for swipe/interaction/button/timer/streak |
| **HapticFeedback** | `HapticFeedback.cs` | Code complete | LightTap, MediumTap, HeavyVibration, ExplosionVibration | Only called on Game Over. Not wired to swipes, answers, interactions, mini-games |
| **CameraShakeManager** | `CameraShakeManager.cs` | Code complete | ShakeWrongAnswer, ShakeSocialShutdown, ShakeMaamoulExplosion. Cinemachine + DOTween fallback | Only used for wrong answer + Game Over |
| **ScreenFlash** | `ScreenFlash.cs` | Code complete | FlashCorrect (green), FlashWrong (red). Called by GameManager | Not used for interaction results, cutscenes, timer warnings |
| **FloatingTextManager** | `FloatingTextManager.cs` + `FloatingText.cs` | Code complete | Object pooled (20). Spawns battery/eidia/scrap text. DOTween animations | Basic spawn. No bounce, glow, or combo-specific effects |
| **URPPostProcessing** | `URPPostProcessing.cs` | Code complete | EnablePanicMode, DisablePanicMode, PulseChromaticAberration, EnableGameOverEffect | **NEVER CALLED** except Game Over. Panic mode should trigger on timer warnings |
| **TransitionPlayer** | `TransitionPlayer.cs` | Code complete | House transition fade with Arabic text (3.1s sequence) | Only used for house entry. No sound or post-processing during transition |
| **CutsceneTrigger** | `CutsceneTrigger.cs` | Code complete | TextReveal (typewriter), CharacterReaction, CameraPan, Dialogue, ReactionShot. CharacterExpressionSO integration | No sound during cutscenes. No camera zoom on reactions. Expression changes are instant swap |
| **InteractionHUDController** | `InteractionHUDController.cs` | Code complete | HUD entrance/exit DOTween, timer bar color changes, result flash | No sound, no particles, no haptic feedback during interactions |
| **SwipeCard** | `SwipeCard.cs` | Code complete | DOTween entrance pop, drag tilt, neutral tint, fly-off | No swipe sound, no haptic, no trail particles |
| **DOTween** | Throughout project | Extensive usage | 70+ tween calls across all scripts | No easing on some tweens. No combined multi-property animations |

### Missing Entirely — Need to Be Created

| Category | What's Needed | Priority | Phase |
|----------|--------------|----------|-------|
| **Audio Files** | ALL sound effects and music clips | CRITICAL | 14A |
| **AudioMixer** | Unity AudioMixer for volume control and ducking | MEDIUM | 14A |
| **Particle Effects** | Success burst, failure burst, Eidia collection, streak combo, timer warning | HIGH | 14B |
| **Panic Mode Wiring** | Connect URPPostProcessing.PanicMode to swipe card timer warnings | MEDIUM | 14B |
| **Typewriter on Questions** | Typewriter text reveal on swipe card questions (currently instant) | MEDIUM | 14B |
| **Expression Transitions** | Smooth crossfade between character expressions (currently instant swap) | MEDIUM | 14B |
| **Timeline Assets** | .playable assets for cinematic house introductions | LOW | 14C |
| **Cinemachine Virtual Cameras** | Camera setups for cutscene close-ups | LOW | 14C |
| **Meter Animations** | Smooth fill/drain with glow effects on sliders | MEDIUM | 14B |
| **Combo Visual Effects** | Streak counter glow, flash, screen effects for 3+ streaks | MEDIUM | 14D |

### Underutilized Systems

| System | Current Usage | Recommended Expansion |
|--------|--------------|----------------------|
| **HapticFeedback** | Only Game Over | Add to: every card swipe, correct/wrong, interaction success/fail, mini-game catches/avoids, streak combos |
| **URP Post-Processing** | Only Game Over | Add to: panic timer (3s left), high streak (4+ = subtle glow), interaction intensity |
| **CameraShakeManager** | Wrong answer + Game Over | Add to: mini-game collisions, streak bonus, cutscene dramatic moments |
| **AudioManager.PlayMusic()** | Never called | Add to: Hub state (menu music), house entry (gameplay music), Game Over/Win (victory/defeat music) |

---

## 2. Phase 14A: Sound System Enhancement

**Goal:** Every player action has audio feedback. Music transitions between game states.

**Estimated Time:** 2-3 hours  
**Files to Create:** 2  
**Files to Modify:** 4

### 2.1 AudioManager.cs — Modifications

**New fields to add:**
```csharp
[Header("Interaction SFX")]
[SerializeField] private AudioClip swipeSfx;
[SerializeField] private AudioClip successSfx;
[SerializeField] private AudioClip failureSfx;
[SerializeField] private AudioClip buttonClickSfx;
[SerializeField] private AudioClip timerTickSfx;
[SerializeField] private AudioClip meterChangeSfx;
[SerializeField] private AudioClip streakAchievedSfx;

[Header("Music Management")]
[SerializeField] private float musicCrossfadeDuration = 0.5f;
```

**New methods to add:**
```csharp
public void PlaySwipe()           → PlaySFX(swipeSfx)
public void PlaySuccess()         → PlaySFX(successSfx)
public void PlayFailure()         → PlaySFX(failureSfx)
public void PlayButtonClick()     → PlaySFX(buttonClickSfx)
public void PlayTimerTick()       → PlaySFX(timerTickSfx)
public void PlayMeterChange()     → PlaySFX(meterChangeSfx)
public void PlayStreakAchieved()  → PlaySFX(streakAchievedSfx)
public void PlayMusicForState(GameState state) → Switches music based on game state
public void StopMusic()           → musicSource.DOFade(0f, 1f)
```

### 2.2 HapticFeedback.cs — New Methods

```csharp
public void SwipeVibration()       → LightTap()
public void SuccessVibration()     → MediumTap()
public void FailVibration()        → HeavyVibration()
public void TimerWarningVibe()     → Quick double-tap
public void StreakVibration()      → Triple tap pattern
```

### 2.3 Integration Points — Where to Wire SFX and Haptics

| Event | Current Trigger | SFX to Add | Haptic to Add |
|-------|----------------|------------|---------------|
| Card Swipe | SwipeCard.OnEndDrag | PlaySwipe() | SwipeVibration() |
| Correct Answer | GameManager.HandleCardProcessed | PlayCorrectAnswer() | SuccessVibration() |
| Wrong Answer | GameManager.HandleCardProcessed | PlayWrongAnswer() | FailVibration() |
| Interaction Success | InteractionHUDController.CompleteInteraction | PlaySuccess() | SuccessVibration() |
| Interaction Fail | InteractionHUDController.CompleteInteraction | PlayFailure() | FailVibration() |
| Timer Warning (3s) | InteractionHUDController.UpdateTimer | PlayTimerTick() | TimerWarningVibe() |
| Streak 3+ | SwipeEncounterManager.UpdateStreak | PlayStreakAchieved() | StreakVibration() |
| Battery Change | MeterManager.ModifyBattery | PlayMeterChange() | LightTap() if > 10 change |
| Button Click | UI buttons | PlayButtonClick() | LightTap() |
| State → Hub | GameManager.ChangeState(HouseHub) | PlayMusic(menu) | None |
| State → Encounter | GameManager.ChangeState(Encounter) | PlayMusic(gameplay) | None |
| State → GameOver | GameManager.ChangeState(GameOver) | StopMusic() + PlayGameOver() | ExplosionVibration() |
| State → Win | GameManager.ChangeState(Win) | StopMusic() + PlayWin() | Celebration vibration |

### 2.4 Files to Create

**AudioMixerSetup.cs** — Editor-only script that generates AudioMixer with groups for Music, SFX, Master. One-time setup tool.

**SFXEventRouter.cs** — Centralizes all SFX trigger calls. Decouples audio from game logic. Subscribes to existing events and calls AudioManager.

---

## 3. Phase 14B: Visual Feedback Polish

**Goal:** Every action has visual particle feedback. Characters feel alive. Timers create tension.

**Estimated Time:** 3-4 hours  
**Files to Create:** 3  
**Files to Modify:** 7

### 3.1 SwipeCard.cs — Modifications

1. **Typewriter question reveal:** Reveal question text character-by-character on card entrance (0.03s per character)
2. **Trail particles on drag:** Spawn small sparkle particles behind card during drag
3. **Enhanced fly-off:** Add rotation and scale to the swipe exit animation

### 3.2 SwipeEncounterManager.cs — Modifications

1. **Panic mode wiring:** When card timer hits 3s, call URPPostProcessing.EnablePanicMode() and PlayTimerWarning()
2. **Streak visual effects:** When streak reaches 3+, spawn rainbow particles and enable screen glow

### 3.3 InteractionHUDController.cs — Modifications

1. **Particle burst on result:** Spawn success or failure particles at HUD position
2. **Timer bar pulse:** When remaining time <= warning threshold, pulse the bar color red
3. **Shake on failure:** Call CameraShakeManager.ShakeWrongAnswer()

### 3.4 CutsceneTrigger.cs — Modifications

1. **Typewriter on ALL text types:** Currently only TextReveal uses typewriter. Apply to CharacterReaction, ReactionShot, Dialogue too
2. **Camera zoom on CharacterReaction:** Subtle DOFieldOfView zoom-in during character reactions
3. **Expression crossfade:** Instead of instant sprite swap, fade out old expression while fading in new

### 3.5 UIManager.cs — Modifications

1. **Meter slider animations:** Use DOValue instead of instant value assignment (0.3s ease)
2. **Meter glow overlay:** Briefly change slider fill color on significant changes
3. **Haptic on meter changes:** Call LightTap() when battery/stomach changes by > 10 units

### 3.6 FloatingText.cs — Modifications

Enhanced spawn animation: bounce scale (1.5 → 1.0), color cycle to gold, longer float distance

### 3.7 Files to Create

**ParticleSpawner.cs** — Code-generated particle effects using Unity ParticleSystem API. No prefabs needed. Methods:
- SpawnSuccessBurst(Vector3) — Golden outward explosion
- SpawnFailureBurst(Vector3) — Red upward drift
- SpawnEidiaCollect(Vector3, int amount) — Gold coins proportional to amount
- SpawnStreakCombo(Vector3, int streak) — Rainbow expanding ring
- SpawnTrailParticle(Vector3) — Small white sparkle behind dragged card
- SpawnTimerWarning(Vector3) — Red orbiting sparks around HUD

**TypewriterText.cs** — Reusable component for typewriter reveal on any TextMeshPro. Handles UTF-16 surrogate pairs for Arabic. Optional skip-on-tap. Optional per-character sound.

**ExpressionTransition.cs** — Smooth crossfade between character expression sprites. Uses two overlapping Image components. Crossfade duration configurable.

### 3.8 Panic Mode Wiring — Currently Dead Code

URPPostProcessing has EnablePanicMode(), DisablePanicMode(), PulseChromaticAberration() but they are NEVER called. Wire them to:
- Swipe card timer when remaining <= 3s
- Interaction HUD timer when remaining <= warning threshold
- Disable on encounter/interaction complete

### 3.9 Meter Animation Enhancements

Replace instant slider.value assignment with DOValue(0.3s, Ease.OutCubic). Add glow overlay that flashes red (drain) or cyan (gain) then fades back to clear.

---

## 4. Phase 14C: Cinematic Enhancements

**Goal:** House introductions feel cinematic. Cutscenes have camera work. Timeline integration for designer sequences.

**Estimated Time:** 2-3 hours (requires Editor setup)  
**Files to Create:** 2  
**Files to Modify:** 3

### 4.1 GameManager.cs — Modifications

Enhanced house entry: Fade to black → camera sweep to house → transition text → character greeting cutscene → begin HouseFlowController sequence

### 4.2 CutsceneTrigger.cs — Modifications

Add PlayTimelineCutscene(PlayableDirector) method. Add sound during cutscenes.

### 4.3 Files to Create

**TimelineController.cs** — Manages Unity Timeline playback for house introductions. Methods: PlayHouseIntro(houseLevel, onComplete), PlayCutsceneTimeline(timelineName, onComplete).

**CinematicCamera.cs** — Simple camera controller for cinematic shots. Methods: PanTo(target, duration), ZoomPunch(target, zoomIn/hold/zoomOut times), HandheldShake(duration, intensity).

### 4.4 Timeline Asset Setup — Manual Editor Steps

Per house: Create .playable asset → Add Activation Track (show background), Animation Track (camera pan), Signal Track (fire events), Audio Track (ambient). Add PlayableDirector component to scene object. Wire in TimelineController.

**Note:** Optional for hackathon. DOTween transitions work fine. Timeline adds designer polish.

---

## 5. Phase 14D: Game Feel Juice

**Goal:** Impacts have weight. Combos feel powerful. Failure has consequences.

**Estimated Time:** 1.5-2 hours  
**Files to Create:** 1  
**Files to Modify:** 4

### 5.1 Files to Create

**HitStopManager.cs** — Brief time freeze on impactful moments. Methods: TriggerHitStop(duration), TriggerLightHitStop(0.05s for correct), TriggerHeavyHitStop(0.15s for wrong). Uses Time.timeScale = 0 with WaitForSecondsRealtime.

### 5.2 GameManager.cs — Modifications

Hit stop on wrong answers (0.15s). Slow-mo fade to black on Game Over (scale down to 0.2 over 0.5s, hold 0.3s, fade to black).

### 5.3 SwipeEncounterManager.cs — Modifications

Streak combo effects: Every 3 streak — spawn particles, play streak SFX, triple haptic vibration. Show streak popup UI.

### 5.4 UIManager.cs — Modifications

Create ShowStreakPopup(int count) — spawns top-center popup with gold text, scale animation, 1s hold, fade out.

Add meter warning effects: When battery < 20% or stomach > 80%, pulse slider color red and trigger subtle chromatic aberration.

### 5.5 InteractionHUDController.cs — Modifications

Add hit stop on failure (0.15s). Light hit stop on success (0.05s).

### 5.6 Juice Effects Summary

| Event | Visual | Audio | Haptic | Time |
|-------|--------|-------|--------|------|
| Card swipe correct | Green flash, gold text | Correct SFX, whoosh | Success vib | Light hit stop |
| Card swipe wrong | Red flash, screen shake | Wrong SFX, thud | Fail vib | Heavy hit stop |
| Streak 3+ | Rainbow particles, glow | Streak SFX | Triple vib | None |
| Interaction success | Green burst, flash | Success SFX | Medium vib | Light hit stop |
| Interaction fail | Red burst, shake | Fail SFX | Heavy vib | Heavy hit stop |
| Timer warning | Red bar pulse, aberration | Timer tick | Quick pulse | None |
| Game Over | Red flash, slow-mo black | Game over SFX | Explosion vib | Heavy hit stop |
| Win | Gold flash, particles | Victory SFX | Celebration vib | Light hit stop |

---

## 6. Implementation Checklist

### Phase 14A: Sound System
- [ ] AudioManager.cs: Add new AudioClip fields (swipe, success, failure, button, timer, meter, streak)
- [ ] AudioManager.cs: Add new playback methods
- [ ] AudioManager.cs: Add PlayMusicForState(GameState) method
- [ ] AudioManager.cs: Wire PlayMusicForState to GameManager.ChangeState
- [ ] HapticFeedback.cs: Add SwipeVibration, SuccessVibration, FailVibration, TimerWarningVibe, StreakVibration
- [ ] Create AudioMixerSetup.cs — auto-generate mixer
- [ ] Create SFXEventRouter.cs — centralize SFX triggers
- [ ] Wire SFX to: swipe, correct/wrong, interaction success/fail, timer warning, streak, meter change, button click
- [ ] Wire Haptic to: swipe, correct/wrong, interaction success/fail, timer warning, streak
- [ ] Wire Music to: Hub, Encounter, GameOver, Win states
- [ ] Import audio files
- [ ] Test all SFX in Editor and on mobile

### Phase 14B: Visual Feedback Polish
- [ ] SwipeCard.cs: Add typewriter question reveal
- [ ] SwipeCard.cs: Add drag trail particles
- [ ] SwipeCard.cs: Enhance fly-off animation
- [ ] SwipeEncounterManager.cs: Wire panic mode to URP post-processing
- [ ] SwipeEncounterManager.cs: Add streak visual effects
- [ ] InteractionHUDController.cs: Add particle burst on result
- [ ] InteractionHUDController.cs: Add timer bar pulse animation
- [ ] InteractionHUDController.cs: Add screen shake on failure
- [ ] CutsceneTrigger.cs: Add typewriter to all text types
- [ ] CutsceneTrigger.cs: Add camera zoom on CharacterReaction
- [ ] CutsceneTrigger.cs: Add expression crossfade
- [ ] UIManager.cs: Add meter slider DOTween animations
- [ ] UIManager.cs: Add meter glow overlay
- [ ] UIManager.cs: Add haptic on meter changes
- [ ] FloatingText.cs: Enhance spawn animation
- [ ] Create ParticleSpawner.cs
- [ ] Create TypewriterText.cs
- [ ] Create ExpressionTransition.cs
- [ ] Wire panic mode to card timer warnings
- [ ] Test all visual effects in Editor

### Phase 14C: Cinematic Enhancements
- [ ] GameManager.cs: Enhance house entry cinematic
- [ ] CutsceneTrigger.cs: Add PlayTimelineCutscene method
- [ ] CutsceneTrigger.cs: Add sound during cutscenes
- [ ] Create TimelineController.cs
- [ ] Create CinematicCamera.cs
- [ ] Create Timeline assets (optional)
- [ ] Test cinematic sequences

### Phase 14D: Game Feel Juice
- [ ] Create HitStopManager.cs
- [ ] GameManager.cs: Add hit stop on wrong answers
- [ ] GameManager.cs: Add slow-mo fade on Game Over
- [ ] SwipeEncounterManager.cs: Add streak combo effects
- [ ] UIManager.cs: Create ShowStreakPopup
- [ ] UIManager.cs: Add meter warning effects
- [ ] InteractionHUDController.cs: Add hit stop on failure
- [ ] Test all juice effects

---

## 7. Asset Requirements List

### Audio Files — SFX (WAV/OGG, mono, 44.1kHz)

| File | Duration | Description | Free Source |
|------|----------|-------------|-------------|
| sfx_card_swipe | 0.2-0.3s | Cardboard whoosh | Freesound.org |
| sfx_correct | 0.3-0.5s | Bright ascending chime | Kenney.nl UI Audio |
| sfx_wrong | 0.3-0.5s | Low descending thud | Kenney.nl UI Audio |
| sfx_interaction_success | 0.3s | Mechanical success ding | Kenney.nl Digital Audio |
| sfx_interaction_fail | 0.4s | Harsh failure buzzer | Kenney.nl Digital Audio |
| sfx_timer_warning | 0.2s | Clock tick or beep | Kenney.nl UI Audio |
| sfx_streak_achieved | 0.5-0.8s | 3-note ascending fanfare | Kenney.nl Digital Audio |
| sfx_button_click | 0.05-0.1s | Short UI click | Kenney.nl UI Audio |
| sfx_meter_change | 0.15s | Subtle blip | Kenney.nl UI Audio |
| sfx_game_over | 1.0-1.5s | Somber descending tones | Kenney.nl |
| sfx_win | 1.5-2.0s | Triumphant fanfare | Kenney.nl |

**Recommended:** Kenney.nl "UI Audio" + "Digital Audio" packs cover 80% of needs. Both CC0 (completely free).

### Audio Files — Music (OGG, stereo, loopable)

| File | Duration | Description | Priority |
|------|----------|-------------|----------|
| music_menu | 60-120s | Calm Middle Eastern melody | LOW |
| music_gameplay | 60-120s | Upbeat tense loop | LOW |
| music_minigame | 60s | Fast energetic loop | LOW |
| music_victory | 15-30s | Short triumphant fanfare | LOW |

Music is optional for hackathon. Focus on SFX first.

### Visual Assets

| Asset | Description | Source |
|-------|-------------|--------|
| Default-Particle.png | 16x16 white soft circle | Built into Unity |
| Particle_Material.mat | Additive blending material | Create in Unity |

That's all. Everything else is code-generated.

---

## 8. Recommended Implementation Order

### Priority 1: Sound (14A) — 2-3 hours
1. Import SFX files (Kenney packs, 15 min)
2. Enhance AudioManager (30 min)
3. Wire SFX to existing events (1 hour)
4. Wire haptic feedback (30 min)
5. Test on mobile (30 min)

**Result:** Game feels 10x more polished immediately.

### Priority 2: Visual Feedback (14B) — 3-4 hours
1. Create ParticleSpawner (1 hour)
2. Wire particles to events (1 hour)
3. Create TypewriterText (45 min)
4. Wire typewriter to questions/cutscenes (30 min)
5. Enhance FloatingText (30 min)
6. Wire panic mode (15 min)

**Result:** Every action has visual feedback. Game feels alive.

### Priority 3: Game Feel (14D) — 1.5-2 hours
1. Create HitStopManager (30 min)
2. Wire hit stop to events (30 min)
3. Create streak combo popup (30 min)
4. Wire streak effects (30 min)

**Result:** Impacts feel heavy, combos feel rewarding.

### Priority 4: Cinematic (14C) — 2-3 hours (Optional)
1. Create TimelineController (45 min)
2. Create CinematicCamera (45 min)
3. Enhance house entry transitions (30 min)
4. Create Timeline assets (30-60 min per house)

**Result:** House introductions feel cinematic. Nice but not essential.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Audio files not found/imported | Low | Medium | Use Kenney.nl CC0 packs — guaranteed compatible |
| Particle performance on mobile | Medium | High | Object pool particles. Limit to 30 on screen |
| Typewriter breaks Arabic text | Low | High | Reuse surrogate pair handling from CutsceneTrigger |
| Hit stop breaks physics | Medium | Medium | Use Time.timeScale = 0. Test mini-games thoroughly |
| Timeline too complex | High | High | Make Phase 14C optional. 14A+14B+14D are sufficient |
| Scope exceeds timeline | Medium | High | Stop after Phase 14B if time is tight |

---

## Testing Checklist

- [ ] All SFX play at appropriate times
- [ ] Music transitions smoothly between states
- [ ] Haptic feedback works on mobile
- [ ] Particles spawn correctly, no frame drops
- [ ] Typewriter reveals Arabic text without breaking
- [ ] Expression crossfades are smooth
- [ ] Hit stop doesn't break mini-game physics
- [ ] Panic mode triggers at correct thresholds
- [ ] Streak combo effects feel rewarding
- [ ] Meter animations are smooth
- [ ] Floating text has satisfying spawn animation
- [ ] Game Over/Win sequences feel impactful
- [ ] No audio/particles leak after scene changes
- [ ] Stable 60 FPS on target mobile device

---

**Maintained By:** Core Development Team  
**Last Updated:** April 10, 2026  
**Status:** PLANNING COMPLETE — Ready for implementation  
**Next Step:** Begin Phase 14A (Sound System) when ready
