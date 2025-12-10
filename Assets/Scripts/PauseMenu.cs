using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu system with settings and controls.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Button settingsBackButton;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Audio")]
    [SerializeField] private AudioClip pauseSound;
    [SerializeField] private AudioClip resumeSound;
    [SerializeField] private AudioClip buttonClickSound;

    private bool isPaused;
    private float previousTimeScale = 1f;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeUI();
    }

    private void Start()
    {
        LoadSettings();
        HidePauseMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        // Setup buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);

        // Setup sliders
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void LoadSettings()
    {
        if (SaveManager.Instance != null)
        {
            float musicVolume = SaveManager.Instance.GetMusicVolume();
            float sfxVolume = SaveManager.Instance.GetSFXVolume();

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = musicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = sfxVolume;

            ApplyVolumeSettings(musicVolume, sfxVolume);
        }
    }

    #endregion

    #region Pause/Resume

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        ShowPauseMenu();
        PlaySound(pauseSound);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = previousTimeScale;

        HidePauseMenu();
        PlaySound(resumeSound);
    }

    private void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public bool IsPaused() => isPaused;

    #endregion

    #region Settings

    private void OpenSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        PlaySound(buttonClickSound);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        SaveSettings();
        PlaySound(buttonClickSound);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);

        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void ApplyVolumeSettings(float music, float sfx)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }

        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(music * 100)}%";

        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(sfx * 100)}%";
    }

    private void SaveSettings()
    {
        if (SaveManager.Instance != null && musicVolumeSlider != null && sfxVolumeSlider != null)
        {
            SaveManager.Instance.SaveSettings(musicVolumeSlider.value, sfxVolumeSlider.value);
        }
    }

    #endregion

    #region Navigation

    private void ReturnToMainMenu()
    {
        PlaySound(buttonClickSound);
        Time.timeScale = 1f;
        isPaused = false;
        
        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        PlaySound(buttonClickSound);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, 0.5f);
        }
    }

    #endregion
}
