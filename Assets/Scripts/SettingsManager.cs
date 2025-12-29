using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages game settings including audio, graphics, and controls.
/// Automatically saves and loads settings using PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    
    [Header("Gameplay Settings")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle autoFireToggle;
    [SerializeField] private Toggle screenShakeToggle;
    
    [Header("Display")]
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private TextMeshProUGUI sensitivityText;

    // Settings keys
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string QUALITY_KEY = "Quality";
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string RESOLUTION_KEY = "Resolution";
    private const string SENSITIVITY_KEY = "Sensitivity";
    private const string AUTO_FIRE_KEY = "AutoFire";
    private const string SCREEN_SHAKE_KEY = "ScreenShake";

    // Default values
    private const float DEFAULT_MUSIC_VOLUME = 0.7f;
    private const float DEFAULT_SFX_VOLUME = 0.8f;
    private const int DEFAULT_QUALITY = 2; // High
    private const float DEFAULT_SENSITIVITY = 1.0f;
    
    // Current values
    private float musicVolume;
    private float sfxVolume;
    private float sensitivity;
    
    private Resolution[] resolutions;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeResolutions();
        LoadSettings();
        ApplySettings();
        SetupUIListeners();
    }

    #endregion

    #region Initialization

    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio.value.ToString("F0") + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupUIListeners()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        
        if (autoFireToggle != null)
            autoFireToggle.onValueChanged.AddListener(SetAutoFire);
        
        if (screenShakeToggle != null)
            screenShakeToggle.onValueChanged.AddListener(SetScreenShake);
    }

    #endregion

    #region Load & Save

    /// <summary>
    /// Loads all settings from PlayerPrefs.
    /// </summary>
    private void LoadSettings()
    {
        // Audio
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        
        // Graphics
        int quality = PlayerPrefs.GetInt(QUALITY_KEY, DEFAULT_QUALITY);
        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
        int resolution = PlayerPrefs.GetInt(RESOLUTION_KEY, resolutions.Length - 1);
        
        // Gameplay
        sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, DEFAULT_SENSITIVITY);
        bool autoFire = PlayerPrefs.GetInt(AUTO_FIRE_KEY, 0) == 1;
        bool screenShake = PlayerPrefs.GetInt(SCREEN_SHAKE_KEY, 1) == 1;

        // Update UI
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        if (qualityDropdown != null) qualityDropdown.value = quality;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        if (resolutionDropdown != null) resolutionDropdown.value = resolution;
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;
        if (autoFireToggle != null) autoFireToggle.isOn = autoFire;
        if (screenShakeToggle != null) screenShakeToggle.isOn = screenShake;
        
        UpdateVolumeText();
    }

    /// <summary>
    /// Applies all loaded settings to the game.
    /// </summary>
    private void ApplySettings()
    {
        // Apply audio
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(musicVolume, 0.0001f)) * 20f);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sfxVolume, 0.0001f)) * 20f);
        }

        // Apply quality
        if (qualityDropdown != null)
            QualitySettings.SetQualityLevel(qualityDropdown.value);
        
        // Apply fullscreen
        if (fullscreenToggle != null)
            Screen.fullScreen = fullscreenToggle.isOn;
        
        // Apply resolution
        if (resolutionDropdown != null && resolutions.Length > 0)
        {
            int resIndex = Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Length - 1);
            Resolution res = resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }
    }

    #endregion

    #region Audio Settings

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        
        if (audioMixer != null)
        {
            float dbValue = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MusicVolume", dbValue);
        }
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
        
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        UpdateVolumeText();
    }

    /// <summary>
    /// Sets the SFX volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        
        if (audioMixer != null)
        {
            float dbValue = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("SFXVolume", dbValue);
        }
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
        
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        if (musicVolumeText != null)
            musicVolumeText.text = Mathf.RoundToInt(musicVolume * 100f) + "%";
        
        if (sfxVolumeText != null)
            sfxVolumeText.text = Mathf.RoundToInt(sfxVolume * 100f) + "%";
        
        if (sensitivityText != null)
            sensitivityText.text = sensitivity.ToString("F1") + "x";
    }

    #endregion

    #region Graphics Settings

    /// <summary>
    /// Sets the graphics quality level.
    /// </summary>
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QUALITY_KEY, qualityIndex);
    }

    /// <summary>
    /// Sets fullscreen mode.
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
    }

    /// <summary>
    /// Sets the screen resolution.
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutions.Length == 0) return;
        
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(RESOLUTION_KEY, resolutionIndex);
    }

    #endregion

    #region Gameplay Settings

    /// <summary>
    /// Sets the mouse sensitivity.
    /// </summary>
    public void SetSensitivity(float sens)
    {
        sensitivity = sens;
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sens);
        UpdateVolumeText();
    }

    /// <summary>
    /// Sets auto-fire mode.
    /// </summary>
    public void SetAutoFire(bool enabled)
    {
        PlayerPrefs.SetInt(AUTO_FIRE_KEY, enabled ? 1 : 0);
        
        // Notify player controller
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Player controller will need to read this setting
        }
    }

    /// <summary>
    /// Sets screen shake effect.
    /// </summary>
    public void SetScreenShake(bool enabled)
    {
        PlayerPrefs.SetInt(SCREEN_SHAKE_KEY, enabled ? 1 : 0);
    }

    #endregion

    #region Public Getters

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetSensitivity() => sensitivity;
    public bool IsAutoFireEnabled() => PlayerPrefs.GetInt(AUTO_FIRE_KEY, 0) == 1;
    public bool IsScreenShakeEnabled() => PlayerPrefs.GetInt(SCREEN_SHAKE_KEY, 1) == 1;

    #endregion

    #region Reset Settings

    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    public void ResetToDefaults()
    {
        SetMusicVolume(DEFAULT_MUSIC_VOLUME);
        SetSFXVolume(DEFAULT_SFX_VOLUME);
        SetQuality(DEFAULT_QUALITY);
        SetFullscreen(true);
        SetSensitivity(DEFAULT_SENSITIVITY);
        SetAutoFire(false);
        SetScreenShake(true);
        
        LoadSettings();
        ApplySettings();
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Returns to the main menu scene.
    /// </summary>
    public void BackToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    #endregion
}
