using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager controlling game state, score, waves, and overall flow.
/// Singleton pattern ensures only one instance exists.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool isGameOver = false;
    
    [Header("Score & Progress")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int enemiesKilledThisWave = 0;
    [SerializeField] private int totalEnemiesKilled = 0;
    
    [Header("Difficulty Scaling")]
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField, Range(1.0f, 2.0f)] private float difficultyMultiplier = 1.1f;
    [SerializeField] private int wavesBetweenBosses = 5;
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HUDController hudController;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameOverScreen gameOverScreen;

    // Events
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnWaveChanged;
    public System.Action OnGameOver;
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;

    // Constants
    private const string HIGH_SCORE_KEY = "HighScore";
    private const string GAMEPLAY_SCENE = "SampleScene";
    private const string MAIN_MENU_SCENE = "MainMenu";

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
        
        LoadHighScore();
    }

    private void Start()
    {
        InitializeGame();
        FindReferences();
        SubscribeToEvents();
    }

    private void Update()
    {
        HandlePauseInput();
        CheckWaveCompletion();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    private void InitializeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;
        currentScore = 0;
        currentWave = 1;
        enemiesKilledThisWave = 0;
        totalEnemiesKilled = 0;
        
        UpdateHUD();
    }

    private void FindReferences()
    {
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
        
        if (hudController == null)
            hudController = FindFirstObjectByType<HUDController>();
        
        if (pauseMenu == null)
            pauseMenu = FindFirstObjectByType<PauseMenu>();
        
        if (gameOverScreen == null)
            gameOverScreen = FindFirstObjectByType<GameOverScreen>();
    }

    private void SubscribeToEvents()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += HandlePlayerDeath;
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandlePlayerDeath;
    }

    #endregion

    #region Game Flow

    /// <summary>
    /// Starts a new wave of enemies.
    /// </summary>
    public void StartNewWave()
    {
        currentWave++;
        enemiesKilledThisWave = 0;
        
        // Check if it's a boss wave
        bool isBossWave = currentWave % wavesBetweenBosses == 0;
        
        // Calculate enemy count with difficulty scaling
        int enemyCount = Mathf.RoundToInt(enemiesPerWave * Mathf.Pow(difficultyMultiplier, currentWave - 1));
        enemyCount = Mathf.Clamp(enemyCount, enemiesPerWave, 50); // Cap at 50
        
        if (enemySpawner != null)
        {
            if (isBossWave)
            {
                enemySpawner.SpawnBoss();
            }
            else
            {
                enemySpawner.SetWave(currentWave);
                enemySpawner.StartWave(enemyCount);
            }
        }
        
        OnWaveChanged?.Invoke(currentWave);
        UpdateHUD();
        
        Debug.Log($"Wave {currentWave} started! {(isBossWave ? "BOSS WAVE!" : $"{enemyCount} enemies")}");
    }

    private void CheckWaveCompletion()
    {
        if (isGameOver || isPaused) return;
        
        if (enemySpawner != null && enemySpawner.IsWaveComplete())
        {
            StartNewWave();
        }
    }

    /// <summary>
    /// Pauses the game.
    /// </summary>
    public void PauseGame()
    {
        if (isGameOver) return;
        
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenu != null)
            pauseMenu.ShowPauseMenu();
        
        OnGamePaused?.Invoke();
    }

    /// <summary>
    /// Resumes the game.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenu != null)
            pauseMenu.HidePauseMenu();
        
        OnGameResumed?.Invoke();
    }

    /// <summary>
    /// Restarts the current game.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GAMEPLAY_SCENE);
    }

    /// <summary>
    /// Returns to main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }

    private void HandlePauseInput()
    {
        if (isGameOver) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void HandlePlayerDeath()
    {
        GameOver();
    }

    private void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0f;
        
        // Update high score if necessary
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }
        
        if (gameOverScreen != null)
        {
            gameOverScreen.ShowGameOver(currentScore, highScore, currentWave, totalEnemiesKilled);
        }
        
        OnGameOver?.Invoke();
        
        Debug.Log($"Game Over! Final Score: {currentScore}, Wave: {currentWave}");
    }

    #endregion

    #region Score Management

    /// <summary>
    /// Adds score to the current total.
    /// </summary>
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
        UpdateHUD();
    }

    /// <summary>
    /// Called when an enemy is killed.
    /// </summary>
    public void EnemyKilled(int scoreValue)
    {
        enemiesKilledThisWave++;
        totalEnemiesKilled++;
        AddScore(scoreValue);
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.Save();
    }

    #endregion

    #region HUD Updates

    private void UpdateHUD()
    {
        if (hudController != null)
        {
            hudController.UpdateScore(currentScore);
            hudController.UpdateWave(currentWave);
        }
    }

    #endregion

    #region Public Getters

    public bool IsPaused() => isPaused;
    public bool IsGameOver() => isGameOver;
    public int GetCurrentScore() => currentScore;
    public int GetHighScore() => highScore;
    public int GetCurrentWave() => currentWave;
    public int GetTotalEnemiesKilled() => totalEnemiesKilled;

    #endregion

    #region Public Setters

    /// <summary>
    /// Force sets the game over state (useful for debugging).
    /// </summary>
    public void ForceGameOver()
    {
        GameOver();
    }

    #endregion

    #region Debug

#if UNITY_EDITOR
    [ContextMenu("Add 100 Score")]
    private void DebugAddScore()
    {
        AddScore(100);
    }

    [ContextMenu("Start Next Wave")]
    private void DebugNextWave()
    {
        StartNewWave();
    }

    [ContextMenu("Force Game Over")]
    private void DebugGameOver()
    {
        ForceGameOver();
    }
#endif

    #endregion
}
