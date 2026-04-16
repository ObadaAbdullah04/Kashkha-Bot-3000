using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// PHASE 18: Centralized audio system with inspector-assigned clips.
///
/// DESIGN:
/// - All clips assigned in inspector via categorized fields
/// - Fallback synthesized tones for unassigned clips
/// - Single PlaySFX(SFXType) call for all game systems
/// - No Resources loading, no naming conventions
///
/// USAGE:
/// 1. Select AudioManager GameObject in scene
/// 2. Drag audio clips into the categorized inspector slots
/// 3. Call: AudioManager.Instance.PlaySFX(SFXType.CorrectAnswer)
///
/// FALLBACK: If a clip is unassigned, a synthesized tone plays automatically.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    #region SFX Catalog

    public enum SFXType
    {
        // UI
        ButtonClick,
        SwipeWhoosh,
        SnapBack,
        TimerPanicTick,
        Transition,

        // Feedback
        CorrectAnswer,
        WrongAnswer,
        StreakBonus,
        Timeout,

        // Gameplay - Catch Mini-Game
        CatchGood,      // Eidia pickup
        CatchBad,       // Maamoul obstacle

        // Gameplay - Path Drawing Mini-Game
        PathDrawStart,
        PathDrawCollision,
        PathDrawSuccess,
        PathDrawFail,

        // Gameplay - Interactions
        InteractionSuccess,
        InteractionFail,
        InteractionShakeRumble,

        // Game State
        BatteryDrained,
        StomachFull,
        GameOver,
        Win,
    }

    #endregion

    #region Inspector Fields - Music

    [Header("Music")]
    [Tooltip("Menu background music")]
    [SerializeField] private AudioClip menuMusic;

    [Tooltip("Gameplay background music")]
    [SerializeField] private AudioClip gameplayMusic;

    [Tooltip("Music fade duration")]
    [SerializeField] private float musicFadeDuration = 0.5f;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    #endregion

    #region Inspector Fields - SFX (Categorized)

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource panicTimerSource;

    [Header("SFX - UI")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip swipeWhoosh;
    [SerializeField] private AudioClip snapBack;
    [SerializeField] private AudioClip timerPanicTick;
    [SerializeField] private AudioClip transition;

    [Header("SFX - Feedback")]
    [SerializeField] private AudioClip correctAnswer;
    [SerializeField] private AudioClip wrongAnswer;
    [SerializeField] private AudioClip streakBonus;
    [SerializeField] private AudioClip timeout;

    [Header("SFX - Catch Mini-Game")]
    [SerializeField] private AudioClip catchGood;
    [SerializeField] private AudioClip catchBad;

    [Header("SFX - Path Drawing Mini-Game")]
    [SerializeField] private AudioClip pathDrawStart;
    [SerializeField] private AudioClip pathDrawCollision;
    [SerializeField] private AudioClip pathDrawSuccess;
    [SerializeField] private AudioClip pathDrawFail;

    [Header("SFX - Interactions")]
    [SerializeField] private AudioClip interactionSuccess;
    [SerializeField] private AudioClip interactionFail;
    [SerializeField] private AudioClip interactionShakeRumble;

    [Header("SFX - Game State")]
    [SerializeField] private AudioClip batteryDrained;
    [SerializeField] private AudioClip stomachFull;
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private AudioClip win;

    #endregion

    #region Fallback Synthesis Settings

    [Header("Fallback Synthesis Settings")]
    [Tooltip("Sample rate for synthesized tones")]
    [SerializeField] private int synthSampleRate = 22050;

    #endregion

    #region Private Fields

    // SFX clip dictionary
    private Dictionary<SFXType, AudioClip> sfxClips = new Dictionary<SFXType, AudioClip>();

    // Track currently playing music
    private AudioClip currentMusicClip;

    // Synthesized fallback cache (avoid regenerating)
    private Dictionary<SFXType, AudioClip> synthesizedClips = new Dictionary<SFXType, AudioClip>();

    // Flag to track if clips have been mapped
    private bool clipsMapped = false;

    // Panic timer state
    private float lastPanicTickTime = 0f;
    private float panicTickInterval = 0.5f;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Validate or create audio sources
            if (musicSource == null) musicSource = CreateOrGetSource("MusicSource");
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            if (sfxSource == null) sfxSource = CreateOrGetSource("SFXSource");
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;

            if (panicTimerSource == null) panicTimerSource = CreateOrGetSource("PanicTimerSource");
            panicTimerSource.loop = false;
            panicTimerSource.volume = sfxVolume * 0.8f; // Relative to SFX

            MapClips();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Creates or retrieves a named AudioSource child.
    /// </summary>
    private AudioSource CreateOrGetSource(string name)
    {
        Transform child = transform.Find(name);
        AudioSource source;

        if (child != null)
        {
            source = child.GetComponent<AudioSource>();
        }
        else
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            source = go.AddComponent<AudioSource>();
        }

        return source;
    }

    #endregion

    #region Clip Mapping

    /// <summary>
    /// Maps inspector fields to the SFXType dictionary.
    /// Logs missing clips in editor.
    /// </summary>
    private void MapClips()
    {
        sfxClips[SFXType.ButtonClick] = buttonClick;
        sfxClips[SFXType.SwipeWhoosh] = swipeWhoosh;
        sfxClips[SFXType.SnapBack] = snapBack;
        sfxClips[SFXType.TimerPanicTick] = timerPanicTick;
        sfxClips[SFXType.Transition] = transition;

        sfxClips[SFXType.CorrectAnswer] = correctAnswer;
        sfxClips[SFXType.WrongAnswer] = wrongAnswer;
        sfxClips[SFXType.StreakBonus] = streakBonus;
        sfxClips[SFXType.Timeout] = timeout;

        sfxClips[SFXType.CatchGood] = catchGood;
        sfxClips[SFXType.CatchBad] = catchBad;

        sfxClips[SFXType.PathDrawStart] = pathDrawStart;
        sfxClips[SFXType.PathDrawCollision] = pathDrawCollision;
        sfxClips[SFXType.PathDrawSuccess] = pathDrawSuccess;
        sfxClips[SFXType.PathDrawFail] = pathDrawFail;

        sfxClips[SFXType.InteractionSuccess] = interactionSuccess;
        sfxClips[SFXType.InteractionFail] = interactionFail;
        sfxClips[SFXType.InteractionShakeRumble] = interactionShakeRumble;

        sfxClips[SFXType.BatteryDrained] = batteryDrained;
        sfxClips[SFXType.StomachFull] = stomachFull;
        sfxClips[SFXType.GameOver] = gameOver;
        sfxClips[SFXType.Win] = win;

        // Log missing clips in editor
#if UNITY_EDITOR
        int missingCount = 0;
        foreach (var kvp in sfxClips)
        {
            if (kvp.Value == null)
            {
                // Debug.LogWarning($"[AudioManager] Missing SFX: {kvp.Key} (assign in inspector). Using synthesized fallback.");
                missingCount++;
            }
        }
        if (menuMusic == null) {} // Debug.LogWarning("[AudioManager] Menu Music not assigned. No menu music will play.");
        if (gameplayMusic == null) {} // Debug.LogWarning("[AudioManager] Gameplay Music not assigned. No gameplay music will play.");
        if (missingCount > 0) {} // Debug.Log($"[AudioManager] {missingCount} SFX clips unassigned. Synthesized fallbacks will be used.");
        // else // Debug.Log("[AudioManager] All SFX clips assigned. Ready!");
#endif

        clipsMapped = true;
    }

    #endregion

    #region Public API - SFX Playback

    /// <summary>
    /// Plays a sound effect by type. Uses inspector-assigned clip or synthesized fallback.
    /// </summary>
    public void PlaySFX(SFXType type)
    {
        if (!clipsMapped) MapClips();

        AudioClip clip = GetClip(type);
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Plays a sound effect with optional volume override.
    /// </summary>
    public void PlaySFX(SFXType type, float volumeScale)
    {
        if (!clipsMapped) MapClips();

        AudioClip clip = GetClip(type);
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    /// <summary>
    /// Updates the music volume at runtime.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Updates the SFX volume at runtime.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (panicTimerSource != null) panicTimerSource.volume = sfxVolume * 0.8f;
    }

    /// <summary>
    /// Plays multiple SFX in rapid succession (for combo effects).
    /// </summary>
    public void PlaySFXCombo(SFXType first, SFXType second, float delay = 0.1f)
    {
        PlaySFX(first);
        DOVirtual.DelayedCall(delay, () => PlaySFX(second));
    }

    #endregion

    #region Public API - Music Playback

    /// <summary>
    /// Plays menu music with fade.
    /// </summary>
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// Plays gameplay music with fade.
    /// </summary>
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    /// <summary>
    /// Stops music with fade out.
    /// </summary>
    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (musicSource == null) return;

        musicSource.DOKill();
        musicSource.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.volume = 1f;
            }
        });
    }

    #endregion

    #region Public API - Panic Timer

    /// <summary>
    /// Plays a panic tick SFX with dynamic pitch based on time remaining.
    /// </summary>
    public void PlayPanicTick(float timeRemaining, float panicThreshold)
    {
        AudioClip clip = GetClip(SFXType.TimerPanicTick);
        if (clip == null || panicTimerSource == null) return;
        if (timeRemaining >= panicThreshold) return;

        // Throttle based on interval
        if (Time.time - lastPanicTickTime < panicTickInterval) return;
        lastPanicTickTime = Time.time;

        // Calculate pitch: 1.0 at panic threshold → 2.0 at 0 time
        float panicProgress = 1f - (timeRemaining / panicThreshold);
        float pitch = Mathf.Lerp(1.0f, 2.0f, panicProgress);

        panicTimerSource.pitch = pitch;
        panicTimerSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Stops the panic timer audio source.
    /// </summary>
    public void StopPanicTicks()
    {
        if (panicTimerSource != null)
        {
            panicTimerSource.Stop();
            panicTimerSource.pitch = 1.0f;
        }
    }

    #endregion

    #region Public API - Legacy Compatibility

    /// <summary>
    /// Legacy method - plays correct answer SFX.
    /// </summary>
    public void PlayCorrectAnswer() => PlaySFX(SFXType.CorrectAnswer);

    /// <summary>
    /// Legacy method - plays wrong answer SFX.
    /// </summary>
    public void PlayWrongAnswer() => PlaySFX(SFXType.WrongAnswer);

    #endregion

    #region Clip Resolution

    /// <summary>
    /// Gets a clip by type, generating a synthesized fallback if unassigned.
    /// </summary>
    private AudioClip GetClip(SFXType type)
    {
        // Check if we have an assigned clip
        if (sfxClips.TryGetValue(type, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        // Generate synthesized fallback
        return GetSynthesizedClip(type);
    }

    /// <summary>
    /// Gets or creates a synthesized tone for the given SFX type.
    /// </summary>
    private AudioClip GetSynthesizedClip(SFXType type)
    {
        if (synthesizedClips.TryGetValue(type, out AudioClip cached))
        {
            return cached;
        }

        // Define tone parameters for each type
        (float frequency, float duration, SynthWaveform waveform) = GetSynthParams(type);

        AudioClip synthClip = CreateTone(frequency, duration, waveform);
        synthesizedClips[type] = synthClip;

        return synthClip;
    }

    /// <summary>
    /// Returns synth parameters for each SFX type.
    /// </summary>
    private (float frequency, float duration, SynthWaveform waveform) GetSynthParams(SFXType type)
    {
        return type switch
        {
            SFXType.ButtonClick => (800f, 0.05f, SynthWaveform.Square),
            SFXType.SwipeWhoosh => (300f, 0.1f, SynthWaveform.Sawtooth),
            SFXType.SnapBack => (400f, 0.08f, SynthWaveform.Square),
            SFXType.TimerPanicTick => (600f, 0.03f, SynthWaveform.Square),
            SFXType.Transition => (330f, 0.15f, SynthWaveform.Sine),
            SFXType.CorrectAnswer => (523f, 0.12f, SynthWaveform.Sine),    // C5
            SFXType.WrongAnswer => (220f, 0.15f, SynthWaveform.Sawtooth),  // A3
            SFXType.StreakBonus => (659f, 0.2f, SynthWaveform.Sine),       // E5
            SFXType.Timeout => (196f, 0.2f, SynthWaveform.Sawtooth),       // G3
            SFXType.CatchGood => (880f, 0.1f, SynthWaveform.Sine),         // A5
            SFXType.CatchBad => (165f, 0.15f, SynthWaveform.Sawtooth),     // E3
            SFXType.PathDrawStart => (440f, 0.05f, SynthWaveform.Sine),    // A4
            SFXType.PathDrawCollision => (220f, 0.15f, SynthWaveform.Square),
            SFXType.PathDrawSuccess => (523f, 0.2f, SynthWaveform.Sine),   // C5
            SFXType.PathDrawFail => (165f, 0.25f, SynthWaveform.Sawtooth), // E3
            SFXType.InteractionSuccess => (523f, 0.12f, SynthWaveform.Sine),
            SFXType.InteractionFail => (220f, 0.15f, SynthWaveform.Sawtooth),
            SFXType.InteractionShakeRumble => (80f, 0.1f, SynthWaveform.Sawtooth),
            SFXType.BatteryDrained => (196f, 0.3f, SynthWaveform.Sawtooth),
            SFXType.StomachFull => (110f, 0.4f, SynthWaveform.Sawtooth),
            SFXType.GameOver => (165f, 0.5f, SynthWaveform.Sawtooth),
            SFXType.Win => (523f, 0.3f, SynthWaveform.Sine),
            _ => (440f, 0.1f, SynthWaveform.Sine),
        };
    }

    /// <summary>
    /// Waveform types for synthesized tones.
    /// </summary>
    private enum SynthWaveform { Sine, Square, Sawtooth }

    /// <summary>
    /// Creates a synthesized AudioClip with the given parameters.
    /// </summary>
    private AudioClip CreateTone(float frequency, float duration, SynthWaveform waveform)
    {
        int sampleRate = synthSampleRate;
        int length = Mathf.RoundToInt(sampleRate * duration);

        float[] samples = new float[length];
        float amplitude = 0.3f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float phase = 2f * Mathf.PI * frequency * t;

            float value = waveform switch
            {
                SynthWaveform.Sine => Mathf.Sin(phase),
                SynthWaveform.Square => Mathf.Sign(Mathf.Sin(phase)),
                SynthWaveform.Sawtooth => 2f * (t * frequency - Mathf.Floor(t * frequency + 0.5f)),
                _ => Mathf.Sin(phase),
            };

            // Apply fade-out envelope to avoid clicks
            float envelope = i < 10 ? i / 10f : (i > length - 10 ? (length - i) / 10f : 1f);
            samples[i] = value * amplitude * envelope;
        }

        AudioClip clip = AudioClip.Create($"Synth_{frequency}Hz", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion

    #region Music Helper

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        // Don't restart if already playing same clip
        if (currentMusicClip == clip && musicSource.isPlaying) return;

        currentMusicClip = clip;

        musicSource.DOKill();
        musicSource.DOFade(0f, musicFadeDuration).OnComplete(() =>
        {
            if (musicSource == null) return;
            musicSource.clip = clip;
            musicSource.Play();
            musicSource.DOFade(musicVolume, musicFadeDuration);
        });
    }

    #endregion
}
