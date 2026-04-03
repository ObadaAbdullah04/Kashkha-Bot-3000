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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            sfxSource.loop = false;
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
}
