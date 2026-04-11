using UnityEngine;
using DG.Tweening;

/// <summary>
/// Simple audio manager for music and SFX.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip correctAnswerSfx;
    [SerializeField] private AudioClip wrongAnswerSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip winSfx;
    
    [Header("Panic Timer")]
    [Tooltip("Ticking clock SFX that plays during panic threshold")]
    [SerializeField] private AudioClip panicTickSfx;
    [SerializeField] private AudioSource panicTimerSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (panicTimerSource == null) panicTimerSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            sfxSource.loop = false;
            panicTimerSource.loop = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayCorrectAnswer()
    {
        if (correctAnswerSfx != null) PlaySFX(correctAnswerSfx);
    }

    public void PlayWrongAnswer()
    {
        if (wrongAnswerSfx != null) PlaySFX(wrongAnswerSfx);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.DOKill(); // Kill existing fade
        musicSource.DOFade(0f, 0.5f).OnComplete(() =>
        {
            if (musicSource == null) return; // Guard for scene cleanup
            musicSource.clip = clip;
            musicSource.Play();
            musicSource.DOFade(1f, 0.5f);
        });
    }
    
    /// <summary>
    /// Plays a panic tick SFX with dynamic pitch based on time remaining.
    /// Pitch increases as time runs out (1.0 → 2.0) to create psychological pressure.
    /// </summary>
    /// <param name="timeRemaining">Current time remaining on timer</param>
    /// <param name="panicThreshold">Threshold below which panic ticks start</param>
    public void PlayPanicTick(float timeRemaining, float panicThreshold)
    {
        if (panicTickSfx == null || panicTimerSource == null) return;
        if (timeRemaining >= panicThreshold) return; // Only play during panic
        
        // Calculate pitch: 1.0 at panic threshold → 2.0 at 0 time
        float panicProgress = 1f - (timeRemaining / panicThreshold); // 0 → 1
        float pitch = Mathf.Lerp(1.0f, 2.0f, panicProgress);
        
        panicTimerSource.pitch = pitch;
        panicTimerSource.PlayOneShot(panicTickSfx);
    }
    
    /// <summary>
    /// Stops the panic timer audio source.
    /// </summary>
    public void StopPanicTicks()
    {
        if (panicTimerSource != null)
        {
            panicTimerSource.Stop();
            panicTimerSource.pitch = 1.0f; // Reset pitch
        }
    }
}
