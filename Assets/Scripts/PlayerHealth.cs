using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Advanced player health system with damage immunity, regeneration, shields, and visual feedback.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public class HealthSettings
    {
        [Range(1, 1000)] public int maxHealth = 100;
        [Range(0, 100)] public int startingHealth = 100;
        public bool canRegenerate = false;
        [Range(0.1f, 10f)] public float regenRate = 1f;
        [Range(1, 50)] public int regenAmount = 1;
        [Range(0f, 10f)] public float regenDelay = 3f;
    }

    [System.Serializable]
    public class ShieldSettings
    {
        public bool hasShield = false;
        [Range(1, 500)] public int maxShield = 50;
        [Range(0, 500)] public int startingShield = 50;
        [Range(0.1f, 10f)] public float shieldRegenRate = 2f;
        [Range(1, 50)] public int shieldRegenAmount = 1;
        [Range(0f, 10f)] public float shieldRegenDelay = 5f;
    }

    [System.Serializable]
    public class ImmunitySettings
    {
        public bool enableDamageImmunity = true;
        [Range(0.1f, 5f)] public float immunityDuration = 1f;
        [Range(0.1f, 1f)] public float flashInterval = 0.1f;
    }

    [System.Serializable]
    public class VisualSettings
    {
        public SpriteRenderer spriteRenderer;
        public Color normalColor = Color.white;
        public Color damageColor = Color.red;
        [Range(0f, 1f)] public float damageFlashDuration = 0.1f;
        public GameObject damageEffectPrefab;
        public GameObject deathEffectPrefab;
        public GameObject healEffectPrefab;
    }

    [System.Serializable]
    public class AudioSettings
    {
        public AudioClip damageSound;
        public AudioClip deathSound;
        public AudioClip healSound;
        public AudioClip shieldBreakSound;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    [Header("Health Configuration")]
    [SerializeField] private HealthSettings health;

    [Header("Shield Configuration")]
    [SerializeField] private ShieldSettings shield;

    [Header("Immunity Configuration")]
    [SerializeField] private ImmunitySettings immunity;

    [Header("Visual Configuration")]
    [SerializeField] private VisualSettings visuals;

    [Header("Audio Configuration")]
    [SerializeField] private AudioSettings audioSettings;

    [Header("References")]
    [SerializeField] private HUDController hud;
    [SerializeField] private PlayerController playerController;

    [Header("Death Behavior")]
    [SerializeField] private bool respawnOnDeath = false;
    [SerializeField, Range(0f, 10f)] private float respawnDelay = 2f;
    [SerializeField] private bool reloadSceneOnDeath = true;
    [SerializeField] private string gameOverSceneName = "";

    // Events
    public System.Action OnDeath;
    public System.Action<int> OnHealthChanged;
    public System.Action<int> OnShieldChanged;

    // State
    private int currentHealth;
    private int currentShield;
    private bool isDead;
    private bool isImmune;
    private float lastDamageTime;
    private Coroutine immunityCoroutine;
    private Coroutine healthRegenCoroutine;
    private Coroutine shieldRegenCoroutine;
    private Color originalColor;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeHealth();
    }

    private void Start()
    {
        UpdateHUD();
        StartRegeneration();
    }

    private void Update()
    {
        if (isDead) return;

        CheckRegeneration();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (visuals.spriteRenderer == null)
            visuals.spriteRenderer = GetComponent<SpriteRenderer>();

        if (visuals.spriteRenderer != null)
            originalColor = visuals.spriteRenderer.color;
    }

    private void InitializeHealth()
    {
        currentHealth = health.startingHealth > 0 ? health.startingHealth : health.maxHealth;
        currentShield = shield.hasShield ? (shield.startingShield > 0 ? shield.startingShield : shield.maxShield) : 0;
        
        currentHealth = Mathf.Clamp(currentHealth, 1, health.maxHealth);
        currentShield = Mathf.Clamp(currentShield, 0, shield.maxShield);
    }

    #endregion

    #region Damage System

    /// <summary>
    /// Applies damage to the player.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead || isImmune || amount <= 0) return;

        lastDamageTime = Time.time;
        int damageRemaining = amount;

        // Apply damage to shield first
        if (currentShield > 0)
        {
            int shieldDamage = Mathf.Min(currentShield, damageRemaining);
            currentShield -= shieldDamage;
            damageRemaining -= shieldDamage;

            if (currentShield <= 0)
            {
                PlayShieldBreakSound();
            }
        }

        // Apply remaining damage to health
        if (damageRemaining > 0)
        {
            currentHealth -= damageRemaining;
            currentHealth = Mathf.Max(0, currentHealth);

            PlayDamageEffects();
        }

        UpdateHUD();

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        else if (immunity.enableDamageImmunity)
        {
            StartImmunity();
        }

        Debug.Log($"Player took {amount} damage! HP: {currentHealth}/{health.maxHealth}, Shield: {currentShield}/{shield.maxShield}");
    }

    private void StartImmunity()
    {
        if (immunityCoroutine != null)
            StopCoroutine(immunityCoroutine);
        
        immunityCoroutine = StartCoroutine(ImmunityRoutine());
    }

    private IEnumerator ImmunityRoutine()
    {
        isImmune = true;
        float elapsed = 0f;

        // Flash sprite during immunity
        while (elapsed < immunity.immunityDuration)
        {
            if (visuals.spriteRenderer != null)
            {
                visuals.spriteRenderer.enabled = !visuals.spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(immunity.flashInterval);
            elapsed += immunity.flashInterval;
        }

        // Restore sprite
        if (visuals.spriteRenderer != null)
        {
            visuals.spriteRenderer.enabled = true;
        }

        isImmune = false;
        immunityCoroutine = null;
    }

    #endregion

    #region Healing System

    /// <summary>
    /// Heals the player by the specified amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;

        int healthBefore = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, health.maxHealth);
        int actualHealed = currentHealth - healthBefore;

        if (actualHealed > 0)
        {
            PlayHealEffects();
            UpdateHUD();
            Debug.Log($"Player healed {actualHealed} HP! Current: {currentHealth}/{health.maxHealth}");
        }
    }

    /// <summary>
    /// Restores shield by the specified amount.
    /// </summary>
    public void RestoreShield(int amount)
    {
        if (!shield.hasShield || isDead || amount <= 0) return;

        int shieldBefore = currentShield;
        currentShield = Mathf.Min(currentShield + amount, shield.maxShield);
        int actualRestored = currentShield - shieldBefore;

        if (actualRestored > 0)
        {
            UpdateHUD();
            Debug.Log($"Shield restored {actualRestored}! Current: {currentShield}/{shield.maxShield}");
        }
    }

    /// <summary>
    /// Fully restores health and shield.
    /// </summary>
    public void FullHeal()
    {
        currentHealth = health.maxHealth;
        currentShield = shield.maxShield;
        UpdateHUD();
        PlayHealEffects();
    }

    #endregion

    #region Regeneration

    private void StartRegeneration()
    {
        if (health.canRegenerate)
        {
            healthRegenCoroutine = StartCoroutine(HealthRegenRoutine());
        }

        if (shield.hasShield)
        {
            shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
        }
    }

    private void CheckRegeneration()
    {
        // Health regen check
        if (health.canRegenerate && currentHealth < health.maxHealth)
        {
            if (healthRegenCoroutine == null)
            {
                healthRegenCoroutine = StartCoroutine(HealthRegenRoutine());
            }
        }

        // Shield regen check
        if (shield.hasShield && currentShield < shield.maxShield)
        {
            if (shieldRegenCoroutine == null)
            {
                shieldRegenCoroutine = StartCoroutine(ShieldRegenRoutine());
            }
        }
    }

    private IEnumerator HealthRegenRoutine()
    {
        yield return new WaitForSeconds(health.regenDelay);

        while (currentHealth < health.maxHealth && !isDead)
        {
            // Don't regen if recently damaged
            if (Time.time - lastDamageTime < health.regenDelay)
            {
                yield return new WaitForSeconds(health.regenDelay);
                continue;
            }

            currentHealth = Mathf.Min(currentHealth + health.regenAmount, health.maxHealth);
            UpdateHUD();

            yield return new WaitForSeconds(health.regenRate);
        }

        healthRegenCoroutine = null;
    }

    private IEnumerator ShieldRegenRoutine()
    {
        yield return new WaitForSeconds(shield.shieldRegenDelay);

        while (currentShield < shield.maxShield && !isDead)
        {
            // Don't regen if recently damaged
            if (Time.time - lastDamageTime < shield.shieldRegenDelay)
            {
                yield return new WaitForSeconds(shield.shieldRegenDelay);
                continue;
            }

            currentShield = Mathf.Min(currentShield + shield.shieldRegenAmount, shield.maxShield);
            UpdateHUD();

            yield return new WaitForSeconds(shield.shieldRegenRate);
        }

        shieldRegenCoroutine = null;
    }

    #endregion

    #region Death & Respawn

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 Player died!");

        // Invoke death event
        OnDeath?.Invoke();

        // Disable player controller
        if (playerController != null)
        {
            playerController.Kill();
        }

        // Play death effects
        PlayDeathEffects();

        // Handle respawn or game over
        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
        else if (reloadSceneOnDeath)
        {
            StartCoroutine(ReloadSceneRoutine());
        }
        else if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            StartCoroutine(LoadGameOverSceneRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private IEnumerator ReloadSceneRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private IEnumerator LoadGameOverSceneRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SceneManager.LoadScene(gameOverSceneName);
    }

    /// <summary>
    /// Respawns the player at current position.
    /// </summary>
    public void Respawn()
    {
        isDead = false;
        InitializeHealth();
        UpdateHUD();

        if (playerController != null)
        {
            playerController.Revive();
        }

        // Restore sprite
        if (visuals.spriteRenderer != null)
        {
            Color c = visuals.spriteRenderer.color;
            c.a = 1f;
            visuals.spriteRenderer.color = c;
            visuals.spriteRenderer.enabled = true;
        }

        StartRegeneration();
        Debug.Log("Player respawned!");
    }

    #endregion

    #region Effects & Feedback

    private void PlayDamageEffects()
    {
        // Visual effects
        if (visuals.damageEffectPrefab != null)
        {
            GameObject effect = Instantiate(visuals.damageEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        StartCoroutine(DamageFlashRoutine());

        // Audio
        if (audioSettings.damageSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.damageSound, audioSettings.volume);
        }

        // HUD feedback
        if (hud != null)
        {
            hud.ShowDamageEffect();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (visuals.spriteRenderer == null) yield break;

        visuals.spriteRenderer.color = visuals.damageColor;
        yield return new WaitForSeconds(visuals.damageFlashDuration);
        visuals.spriteRenderer.color = originalColor;
    }

    private void PlayHealEffects()
    {
        if (visuals.healEffectPrefab != null)
        {
            GameObject effect = Instantiate(visuals.healEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        if (audioSettings.healSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.healSound, audioSettings.volume);
        }
    }

    private void PlayDeathEffects()
    {
        if (visuals.deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(visuals.deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        if (audioSettings.deathSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.deathSound, audioSettings.volume);
        }
    }

    private void PlayShieldBreakSound()
    {
        if (audioSettings.shieldBreakSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.shieldBreakSound, audioSettings.volume);
        }
    }

    private void UpdateHUD()
    {
        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, health.maxHealth);
        }

        // Invoke events
        OnHealthChanged?.Invoke(currentHealth);
        OnShieldChanged?.Invoke(currentShield);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current health value.
    /// </summary>
    public int GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Gets the max health value.
    /// </summary>
    public int GetMaxHealth() => health.maxHealth;

    /// <summary>
    /// Gets the current shield value.
    /// </summary>
    public int GetCurrentShield() => currentShield;

    /// <summary>
    /// Gets the max shield value.
    /// </summary>
    public int GetMaxShield() => shield.maxShield;

    /// <summary>
    /// Gets the health percentage (0-1).
    /// </summary>
    public float GetHealthPercentage() => (float)currentHealth / health.maxHealth;

    /// <summary>
    /// Gets the shield percentage (0-1).
    /// </summary>
    public float GetShieldPercentage() => shield.hasShield ? (float)currentShield / shield.maxShield : 0f;

    /// <summary>
    /// Checks if the player is dead.
    /// </summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Checks if the player is immune to damage.
    /// </summary>
    public bool IsImmune() => isImmune;

    /// <summary>
    /// Increases max health.
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        health.maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, health.maxHealth);
        UpdateHUD();
    }

    /// <summary>
    /// Increases max shield.
    /// </summary>
    public void IncreaseMaxShield(int amount)
    {
        if (!shield.hasShield) return;
        shield.maxShield += amount;
        currentShield = Mathf.Min(currentShield + amount, shield.maxShield);
        UpdateHUD();
    }

    /// <summary>
    /// Sets the max health value.
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        health.maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, health.maxHealth);
        UpdateHUD();
    }

    /// <summary>
    /// Sets the max shield value.
    /// </summary>
    public void SetMaxShield(int newMaxShield)
    {
        if (!shield.hasShield) return;
        shield.maxShield = newMaxShield;
        currentShield = Mathf.Min(currentShield, shield.maxShield);
        UpdateHUD();
    }

    /// <summary>
    /// Gets the shield regeneration rate.
    /// </summary>
    public float GetShieldRegenRate() => shield.shieldRegenRate;

    /// <summary>
    /// Sets the shield regeneration rate.
    /// </summary>
    public void SetShieldRegenRate(float newRate)
    {
        shield.shieldRegenRate = Mathf.Max(0.1f, newRate);
    }

    #endregion
}
