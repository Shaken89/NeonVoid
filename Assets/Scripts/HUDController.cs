using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Advanced HUD controller with smooth animations, notifications, and comprehensive UI management.
/// </summary>
public class HUDController : MonoBehaviour
{
    [System.Serializable]
    public class UIElements
    {
        [Header("Health UI")]
        public Slider healthBar;
        public Image healthBarFill;
        public TextMeshProUGUI healthText;
        public Gradient healthColorGradient;

        [Header("Score UI")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;

        [Header("Wave UI")]
        public TextMeshProUGUI waveText;
        public GameObject waveNotification;
        public TextMeshProUGUI waveNotificationText;

        [Header("Game Stats")]
        public TextMeshProUGUI enemiesText;
        public TextMeshProUGUI fpsText;
        public TextMeshProUGUI timerText;

        [Header("Combat Feedback")]
        public TextMeshProUGUI comboText;
        public Image damageVignette;
        public GameObject criticalHealthWarning;
    }

    [Header("UI Configuration")]
    [SerializeField] private UIElements ui;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimations = true;
    [SerializeField, Range(0.1f, 2f)] private float healthBarSmoothTime = 0.3f;
    [SerializeField, Range(0.1f, 2f)] private float scoreAnimationTime = 0.5f;
    [SerializeField, Range(0.5f, 5f)] private float waveNotificationDuration = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showTimer = true;
    [SerializeField, Range(0f, 1f)] private float criticalHealthThreshold = 0.25f;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color fullHealthColor = Color.green;

    [Header("Combo System")]
    [SerializeField] private bool enableComboSystem = true;
    [SerializeField, Range(1f, 10f)] private float comboTimeout = 3f;
    [SerializeField, Range(1, 100)] private int comboMultiplierInterval = 5;

    [Header("Audio")]
    [SerializeField] private AudioClip scoreSound;
    [SerializeField] private AudioClip waveCompleteSound;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip lowHealthSound;

    // Private state
    private int currentScore;
    private int highScore;
    private int currentWave;
    private int killCombo;
    private float currentHealth;
    private float maxHealth = 100f;
    private float targetHealthBarValue;
    private float healthBarVelocity;
    private float gameStartTime;
    private float lastKillTime;
    private bool isLowHealth;
    private Coroutine damageFlashCoroutine;
    private Coroutine waveNotificationCoroutine;

    // FPS tracking
    private float deltaTime;
    private const string HIGH_SCORE_KEY = "HighScore";

    #region Unity Lifecycle

    private void Awake()
    {
        LoadHighScore();
        InitializeUI();
    }

    private void Start()
    {
        gameStartTime = Time.time;
        ResetUI();
    }

    private void Update()
    {
        UpdateHealthBar();
        UpdateComboSystem();
        
        if (showFPS)
            UpdateFPS();
        
        if (showTimer)
            UpdateTimer();
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        // Setup health bar
        if (ui.healthBar != null)
        {
            ui.healthBar.minValue = 0f;
            ui.healthBar.maxValue = 1f;
            ui.healthBar.value = 1f;
        }

        // Setup health gradient if not assigned
        if (ui.healthColorGradient == null)
        {
            ui.healthColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(lowHealthColor, 0f);
            colorKeys[1] = new GradientColorKey(fullHealthColor, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            ui.healthColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        // Hide notifications initially
        if (ui.waveNotification != null)
            ui.waveNotification.SetActive(false);

        if (ui.criticalHealthWarning != null)
            ui.criticalHealthWarning.SetActive(false);

        if (ui.damageVignette != null)
        {
            Color c = ui.damageVignette.color;
            c.a = 0f;
            ui.damageVignette.color = c;
        }
    }

    private void ResetUI()
    {
        UpdateHealth(maxHealth, maxHealth);
        UpdateScore(0);
        UpdateWave(1);
        UpdateEnemyCount(0);
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (ui.highScoreText != null)
            ui.highScoreText.text = $"Best: {highScore}";
    }

    #endregion

    #region Health Management

    /// <summary>
    /// Updates the health bar with smooth animation.
    /// </summary>
    public void UpdateHealth(float current, float max)
    {
        currentHealth = Mathf.Clamp(current, 0f, max);
        maxHealth = Mathf.Max(1f, max);
        
        targetHealthBarValue = currentHealth / maxHealth;

        // Update health text
        if (ui.healthText != null)
        {
            ui.healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        // Check for low health
        CheckLowHealth();
    }

    private void UpdateHealthBar()
    {
        if (ui.healthBar == null) return;

        // Smooth health bar animation
        if (useAnimations)
        {
            ui.healthBar.value = Mathf.SmoothDamp(
                ui.healthBar.value,
                targetHealthBarValue,
                ref healthBarVelocity,
                healthBarSmoothTime
            );
        }
        else
        {
            ui.healthBar.value = targetHealthBarValue;
        }

        // Update health bar color
        if (ui.healthBarFill != null && ui.healthColorGradient != null)
        {
            ui.healthBarFill.color = ui.healthColorGradient.Evaluate(ui.healthBar.value);
        }
    }

    private void CheckLowHealth()
    {
        bool wasLowHealth = isLowHealth;
        isLowHealth = targetHealthBarValue <= criticalHealthThreshold;

        // Show/hide critical health warning
        if (ui.criticalHealthWarning != null)
        {
            ui.criticalHealthWarning.SetActive(isLowHealth);
        }

        // Play low health sound once
        if (isLowHealth && !wasLowHealth)
        {
            if (lowHealthSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(lowHealthSound, 0.7f);
        }
    }

    /// <summary>
    /// Shows damage feedback effect.
    /// </summary>
    public void ShowDamageEffect()
    {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        
        damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (ui.damageVignette == null) yield break;

        Color c = ui.damageVignette.color;
        c.a = 0.5f;
        ui.damageVignette.color = c;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, elapsed / duration);
            ui.damageVignette.color = c;
            yield return null;
        }

        c.a = 0f;
        ui.damageVignette.color = c;
    }

    #endregion

    #region Score Management

    /// <summary>
    /// Updates the score and checks for high score.
    /// </summary>
    public void UpdateScore(int points)
    {
        if (points > 0)
        {
            // Apply combo multiplier
            if (enableComboSystem && killCombo > 0)
            {
                int multiplier = 1 + (killCombo / comboMultiplierInterval);
                points *= multiplier;
            }

            // Play score sound
            if (scoreSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(scoreSound, 0.4f);
        }

        currentScore += points;
        currentScore = Mathf.Max(0, currentScore);

        // Update score text
        if (ui.scoreText != null)
        {
            if (useAnimations)
                StartCoroutine(AnimateScoreText(currentScore));
            else
                ui.scoreText.text = $"Score: {currentScore}";
        }

        // Check and update high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();

            if (ui.highScoreText != null)
                ui.highScoreText.text = $"Best: {highScore}";
        }
    }

    private IEnumerator AnimateScoreText(int targetScore)
    {
        if (ui.scoreText == null) yield break;

        int startScore = targetScore - Mathf.Abs(targetScore - currentScore);
        float elapsed = 0f;

        while (elapsed < scoreAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scoreAnimationTime;
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
            ui.scoreText.text = $"Score: {displayScore}";
            yield return null;
        }

        ui.scoreText.text = $"Score: {targetScore}";
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int GetScore() => currentScore;

    /// <summary>
    /// Gets the high score.
    /// </summary>
    public int GetHighScore() => highScore;

    #endregion

    #region Wave Management

    /// <summary>
    /// Updates the wave number with notification.
    /// </summary>
    public void UpdateWave(int waveNumber)
    {
        currentWave = waveNumber;

        if (ui.waveText != null)
            ui.waveText.text = $"Wave: {waveNumber}";

        // Show wave notification
        if (waveNumber > 1)
        {
            ShowWaveNotification(waveNumber);
            
            if (waveCompleteSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(waveCompleteSound, 0.6f);
        }
    }

    private void ShowWaveNotification(int waveNumber)
    {
        if (ui.waveNotification == null) return;

        if (waveNotificationCoroutine != null)
            StopCoroutine(waveNotificationCoroutine);

        waveNotificationCoroutine = StartCoroutine(WaveNotificationRoutine(waveNumber));
    }

    private IEnumerator WaveNotificationRoutine(int waveNumber)
    {
        ui.waveNotification.SetActive(true);

        if (ui.waveNotificationText != null)
            ui.waveNotificationText.text = $"WAVE {waveNumber}";

        // Fade in
        CanvasGroup canvasGroup = ui.waveNotification.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float fadeTime = 0.5f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(waveNotificationDuration);

        // Fade out
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float fadeTime = 0.5f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        ui.waveNotification.SetActive(false);
    }

    /// <summary>
    /// Gets the current wave number.
    /// </summary>
    public int GetWave() => currentWave;

    #endregion

    #region Combo System

    /// <summary>
    /// Registers a kill for combo tracking.
    /// </summary>
    public void RegisterKill()
    {
        killCombo++;
        lastKillTime = Time.time;

        UpdateComboDisplay();

        // Play combo sound at intervals
        if (killCombo % comboMultiplierInterval == 0)
        {
            if (comboSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(comboSound, 0.5f);
        }
    }

    private void UpdateComboSystem()
    {
        if (!enableComboSystem) return;

        // Reset combo if timeout expired
        if (killCombo > 0 && Time.time - lastKillTime > comboTimeout)
        {
            killCombo = 0;
            UpdateComboDisplay();
        }
    }

    private void UpdateComboDisplay()
    {
        if (ui.comboText == null) return;

        if (killCombo > 1)
        {
            int multiplier = 1 + (killCombo / comboMultiplierInterval);
            ui.comboText.text = $"x{killCombo} COMBO\n{multiplier}x MULTIPLIER";
            ui.comboText.gameObject.SetActive(true);
        }
        else
        {
            ui.comboText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gets the current combo count.
    /// </summary>
    public int GetCombo() => killCombo;

    #endregion

    #region Enemy & Stats Display

    /// <summary>
    /// Updates the enemy count display.
    /// </summary>
    public void UpdateEnemyCount(int count)
    {
        if (ui.enemiesText != null)
            ui.enemiesText.text = $"Enemies: {count}";
    }

    private void UpdateFPS()
    {
        if (ui.fpsText == null) return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        ui.fpsText.text = $"FPS: {Mathf.Ceil(fps)}";

        // Color code FPS
        if (fps >= 50f)
            ui.fpsText.color = Color.green;
        else if (fps >= 30f)
            ui.fpsText.color = Color.yellow;
        else
            ui.fpsText.color = Color.red;
    }

    private void UpdateTimer()
    {
        if (ui.timerText == null) return;

        float elapsed = Time.time - gameStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        
        ui.timerText.text = $"Time: {minutes:00}:{seconds:00}";
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Resets all HUD values to default.
    /// </summary>
    public void ResetHUD()
    {
        currentScore = 0;
        killCombo = 0;
        gameStartTime = Time.time;
        ResetUI();
    }

    /// <summary>
    /// Shows or hides the entire HUD.
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Gets the current game time in seconds.
    /// </summary>
    public float GetGameTime() => Time.time - gameStartTime;

    #endregion
}
