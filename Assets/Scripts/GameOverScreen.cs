using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Game Over screen with statistics and options.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI accuracyText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDelay = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip newHighScoreSound;

    private CanvasGroup canvasGroup;
    private bool isNewHighScore;

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
        HideGameOver();
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        canvasGroup = gameOverPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && gameOverPanel != null)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Setup buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    #endregion

    #region Display

    public void ShowGameOver(int score, int wave, float time, int kills = 0, float accuracy = 0f)
    {
        StartCoroutine(ShowGameOverRoutine(score, wave, time, kills, accuracy));
    }

    private System.Collections.IEnumerator ShowGameOverRoutine(int score, int wave, float time, int kills, float accuracy)
    {
        yield return new WaitForSeconds(displayDelay);

        // Check for high score
        int highScore = SaveManager.Instance != null ? SaveManager.Instance.GetHighScore() : 0;
        isNewHighScore = score > highScore;

        if (isNewHighScore && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveHighScore(score);
            PlaySound(newHighScoreSound);
        }
        else
        {
            PlaySound(gameOverSound);
        }

        // Update UI
        UpdateStatistics(score, highScore, wave, time, kills, accuracy);

        // Show panel with fade in
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (canvasGroup != null)
            {
                yield return StartCoroutine(FadeIn());
            }
        }
    }

    private void UpdateStatistics(int score, int highScore, int wave, float time, int kills, float accuracy)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
            if (isNewHighScore)
                scoreText.text += " <color=yellow>NEW RECORD!</color>";
        }

        if (highScoreText != null)
            highScoreText.text = $"Best: {highScore}";

        if (waveText != null)
            waveText.text = $"Wave: {wave}";

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        if (killsText != null)
            killsText.text = $"Kills: {kills}";

        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {Mathf.RoundToInt(accuracy * 100)}%";
    }

    private System.Collections.IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    #endregion

    #region Navigation

    private void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
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
            AudioManager.Instance.PlaySFX(clip, 0.7f);
        }
    }

    #endregion
}
