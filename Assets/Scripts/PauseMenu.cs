using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

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
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction pauseAction;
#else
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
#endif

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
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (inputActionsAsset == null)
        {
            Debug.LogError("InputActionAsset not assigned in PauseMenu.");
        }
        else
        {
            var playerMap = inputActionsAsset.FindActionMap("Player", true);
            pauseAction = playerMap.FindAction("Pause", true);
            pauseAction.performed += ctx => TogglePause();
        }
    #endif
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        pauseAction?.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        pauseAction?.Disable();
#endif
    }

    private void Start()
    {
        LoadSettings();
        HidePauseMenu();
    }

    private void Update()
    {
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
#endif
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

        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (inputActionsAsset != null)
        {
            var playerMap = inputActionsAsset.FindActionMap("Player", true);
            var uiMap = inputActionsAsset.FindActionMap("UI", true);
            playerMap.Disable();
            uiMap.Enable();
            Debug.Log("PauseMenu: UI map enabled");
        }
        #endif
        ShowPauseMenu();
        PlaySound(pauseSound);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = previousTimeScale;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (inputActionsAsset != null)
        {
            var playerMap = inputActionsAsset.FindActionMap("Player", true);
            var uiMap = inputActionsAsset.FindActionMap("UI", true);
            uiMap.Disable();
            playerMap.Enable();
        }
#endif
        HidePauseMenu();
        PlaySound(resumeSound);
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (resumeButton != null)
        {
            resumeButton.Select();
            Debug.Log("PauseMenu: Resume button selected");
        }
    }

    public void HidePauseMenu()
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
        Debug.Log("PauseMenu: Settings button clicked");
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        PlaySound(buttonClickSound);
    }

    private void CloseSettings()
    {
        Debug.Log("PauseMenu: Back button clicked");
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
        Debug.Log("PauseMenu: Main Menu button clicked");
        PlaySound(buttonClickSound);
        Time.timeScale = 1f;
        isPaused = false;
        // Остановить игровую музыку перед переходом на меню
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();
        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        Debug.Log("PauseMenu: Exit button clicked");
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
