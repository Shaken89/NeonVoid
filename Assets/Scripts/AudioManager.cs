using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all audio in the game including music and sound effects.
/// Implements Singleton pattern and persists across scenes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private float fadeTime = 1f;

    [Header("Sound Effects Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;
    [SerializeField] private int maxSfxSources = 5;

    private List<AudioSource> sfxSourcePool = new List<AudioSource>();
    private Coroutine musicFadeCoroutine;
    private bool isMusicMuted;
    private bool isSfxMuted;

    private void Awake()
    {
        InitializeSingleton();
        InitializeAudioSources();
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // Initialize music source
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
        musicSource.volume = musicVolume;

        // Initialize main SFX source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        sfxSource.volume = sfxVolume;

        // Create SFX source pool for simultaneous sounds
        for (int i = 0; i < maxSfxSources; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = sfxVolume;
            sfxSourcePool.Add(source);
        }
    }

    #region Music Control

    /// <summary>
    /// Plays music with optional fade in effect.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true, bool fade = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempting to play null music clip");
            return;
        }

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        if (fade)
        {
            musicFadeCoroutine = StartCoroutine(FadeMusicTo(clip, loop));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = isMusicMuted ? 0f : musicVolume;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stops music with optional fade out effect.
    /// </summary>
    public void StopMusic(bool fade = false)
    {
        if (musicSource == null) return;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        if (fade)
        {
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Pauses the current music.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    /// <summary>
    /// Resumes paused music.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
            musicSource.UnPause();
    }

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null && !isMusicMuted)
            musicSource.volume = musicVolume;
    }

    /// <summary>
    /// Toggles music mute state.
    /// </summary>
    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        if (musicSource != null)
            musicSource.volume = isMusicMuted ? 0f : musicVolume;
    }

    private IEnumerator FadeMusicTo(AudioClip newClip, bool loop)
    {
        // Fade out current music
        float startVolume = musicSource.volume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();

        // Switch to new clip
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // Fade in new music
        float targetVolume = isMusicMuted ? 0f : musicVolume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = targetVolume;

        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        musicFadeCoroutine = null;
    }

    #endregion

    #region Sound Effects Control

    /// <summary>
    /// Plays a sound effect once.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || isSfxMuted)
        {
            if (clip == null)
                Debug.LogWarning("AudioManager: Attempting to play null SFX clip");
            return;
        }

        AudioSource availableSource = GetAvailableSfxSource();
        availableSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    /// <summary>
    /// Plays a sound effect at a specific 3D position.
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null || isSfxMuted)
        {
            if (clip == null)
                Debug.LogWarning("AudioManager: Attempting to play null SFX clip");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * volumeMultiplier);
    }

    /// <summary>
    /// Plays a random sound from an array of clips.
    /// </summary>
    public void PlayRandomSFX(AudioClip[] clips, float volumeMultiplier = 1f)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("AudioManager: Attempting to play from null or empty clip array");
            return;
        }

        AudioClip randomClip = clips[Random.Range(0, clips.Length)];
        PlaySFX(randomClip, volumeMultiplier);
    }

    /// <summary>
    /// Sets the sound effects volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        foreach (var source in sfxSourcePool)
        {
            source.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Toggles sound effects mute state.
    /// </summary>
    public void ToggleSFXMute()
    {
        isSfxMuted = !isSfxMuted;
    }

    /// <summary>
    /// Stops all currently playing sound effects.
    /// </summary>
    public void StopAllSFX()
    {
        sfxSource.Stop();
        foreach (var source in sfxSourcePool)
        {
            source.Stop();
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        // Try to find an available source from the pool
        foreach (var source in sfxSourcePool)
        {
            if (!source.isPlaying)
                return source;
        }

        // If all sources are busy, use the main SFX source
        return sfxSource;
    }

    #endregion

    #region Public Getters

    public bool IsMusicPlaying() => musicSource != null && musicSource.isPlaying;
    public bool IsMusicMuted() => isMusicMuted;
    public bool IsSFXMuted() => isSfxMuted;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    #endregion
}
