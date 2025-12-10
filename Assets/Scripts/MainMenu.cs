using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main menu controller with navigation and settings.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject statsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Button settingsBackButton;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI gamesPlayedText;
    [SerializeField] private TextMeshProUGUI totalKillsText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    [SerializeField] private Button statsBackButton;
    [SerializeField] private Button resetStatsButton;

    [Header("Credits")]
    [SerializeField] private Button creditsBackButton;

    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Audio")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip buttonClickSound;

    private void Start()
    {
        InitializeUI();
        LoadSettings();
        ShowMainMenu();
        PlayMenuMusic();
    }

    #region Initialization

    private void InitializeUI()
    {
        // Main menu buttons
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel));

        if (statsButton != null)
            statsButton.onClick.AddListener(() => { ShowPanel(statsPanel); UpdateStats(); });

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => ShowPanel(creditsPanel));

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Back buttons
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(() => { SaveSettings(); ShowMainMenu(); });

        if (statsBackButton != null)
            statsBackButton.onClick.AddListener(ShowMainMenu);

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(ShowMainMenu);

        if (resetStatsButton != null)
            resetStatsButton.onClick.AddListener(ResetStats);

        // Sliders
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

    #region Navigation

    private void PlayGame()
    {
        PlayButtonSound();
        
        if (SaveManager.Instance != null)
            SaveManager.Instance.IncrementGamesPlayed();

        SceneManager.LoadScene(gameSceneName);
    }

    private void ShowMainMenu()
    {
        PlayButtonSound();
        ShowPanel(mainPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        // Hide all panels
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);

        // Show target panel
        if (panel != null)
            panel.SetActive(true);
    }

    private void QuitGame()
    {
        PlayButtonSound();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #endregion

    #region Settings

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

        // Play test sound
        PlayButtonSound();
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

    #region Statistics

    private void UpdateStats()
    {
        if (SaveManager.Instance == null) return;

        if (highScoreText != null)
            highScoreText.text = $"High Score: {SaveManager.Instance.GetHighScore()}";

        if (gamesPlayedText != null)
            gamesPlayedText.text = $"Games Played: {SaveManager.Instance.GetTotalGamesPlayed()}";

        if (totalKillsText != null)
            totalKillsText.text = $"Total Kills: {SaveManager.Instance.GetTotalKills()}";

        if (playTimeText != null)
        {
            float totalSeconds = SaveManager.Instance.GetTotalPlayTime();
            int hours = Mathf.FloorToInt(totalSeconds / 3600f);
            int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
            playTimeText.text = $"Play Time: {hours}h {minutes}m";
        }
    }

    private void ResetStats()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            UpdateStats();
            PlayButtonSound();
        }
    }

    #endregion

    #region Audio

    private void PlayMenuMusic()
    {
        if (menuMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(menuMusic, loop: true, fade: true);
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound, 0.5f);
        }
    }

    #endregion
}
