using UnityEngine;
using System.Collections;

/// <summary>
/// Experience and leveling system for Nova Drift style progression
/// </summary>
public class ExperienceSystem : MonoBehaviour
{
    public static ExperienceSystem Instance { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private int baseExperienceRequired = 10;
    [SerializeField] private float levelScaling = 1.5f;

    [Header("Experience Sources")]
    [SerializeField] private int enemyKillXP = 5;
    [SerializeField] private int bossKillXP = 50;
    [SerializeField] private int waveCompleteXP = 20;

    [Header("Visual")]
    [SerializeField] private bool showLevelUpEffect = true;
    [SerializeField] private AudioClip levelUpSound;

    [Header("References")]
    [SerializeField] private ExperienceBarUI experienceBar;

    // Events
    public delegate void LevelUpEvent(int newLevel);
    public event LevelUpEvent OnLevelUp;

    private int experienceRequired;

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

        CalculateExperienceRequired();
    }

    private void Start()
    {
        if (experienceBar == null)
            experienceBar = FindFirstObjectByType<ExperienceBarUI>();

        UpdateUI();
    }

    private void CalculateExperienceRequired()
    {
        experienceRequired = Mathf.RoundToInt(baseExperienceRequired * Mathf.Pow(levelScaling, currentLevel - 1));
    }

    public void AddExperience(int amount)
    {
        // Apply experience multiplier from upgrades
        if (ModularUpgradeSystem.Instance != null)
        {
            float multiplier = ModularUpgradeSystem.Instance.GetStat("experienceGain");
            amount = Mathf.RoundToInt(amount * multiplier);
        }

        currentExperience += amount;

        // Check for level up
        while (currentExperience >= experienceRequired)
        {
            LevelUp();
        }

        UpdateUI();
    }

    private void LevelUp()
    {
        currentExperience -= experienceRequired;
        currentLevel++;
        CalculateExperienceRequired();

        // Visual and audio feedback
        if (showLevelUpEffect)
        {
            ShowLevelUpEffect();
        }

        if (levelUpSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(levelUpSound, 0.8f);
        }

        // Trigger upgrade selection
        if (ModularUpgradeSystem.Instance != null)
        {
            ModularUpgradeSystem.Instance.LevelUp();
        }

        OnLevelUp?.Invoke(currentLevel);

        Debug.Log($"Level Up! Now level {currentLevel}");
    }

    private void ShowLevelUpEffect()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && ParticleManager.Instance != null)
        {
            ParticleManager.Instance.SpawnEffect("PowerUp", player.transform.position);
        }
    }

    private void UpdateUI()
    {
        if (experienceBar != null)
        {
            float progress = (float)currentExperience / experienceRequired;
            experienceBar.UpdateExperience(currentLevel, currentExperience, experienceRequired, progress);
        }
    }

    // Called by enemies/game events
    public void OnEnemyKilled()
    {
        AddExperience(enemyKillXP);
    }

    public void OnBossKilled()
    {
        AddExperience(bossKillXP);
    }

    public void OnWaveComplete()
    {
        AddExperience(waveCompleteXP);
    }

    // Getters
    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceRequired() => experienceRequired;
    public float GetExperienceProgress() => (float)currentExperience / experienceRequired;
}

/// <summary>
/// UI for displaying experience bar
/// </summary>
public class ExperienceBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UnityEngine.UI.Image fillImage;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;
    [SerializeField] private TMPro.TextMeshProUGUI experienceText;

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool animateBar = true;

    private float targetFill;
    private float currentFill;

    public void UpdateExperience(int level, int current, int required, float progress)
    {
        if (levelText != null)
            levelText.text = $"LVL {level}";

        if (experienceText != null)
            experienceText.text = $"{current}/{required}";

        targetFill = progress;

        if (!animateBar && fillImage != null)
            fillImage.fillAmount = progress;
    }

    private void Update()
    {
        if (animateBar && fillImage != null)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
            fillImage.fillAmount = currentFill;
        }
    }
}

/// <summary>
/// Experience gem that player picks up (Nova Drift style)
/// </summary>
public class ExperienceGem : MonoBehaviour
{
    [SerializeField] private int experienceValue = 1;
    [SerializeField] private float magnetRange = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private float lifetime = 30f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color[] gemColors;

    private Transform player;
    private bool isBeingPulled;
    private float spawnTime;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        spawnTime = Time.time;

        // Set random color
        if (spriteRenderer != null && gemColors.Length > 0)
        {
            spriteRenderer.color = gemColors[Random.Range(0, gemColors.Length)];
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Get magnet range from upgrades
        float effectiveMagnetRange = magnetRange;
        if (ModularUpgradeSystem.Instance != null)
        {
            effectiveMagnetRange *= ModularUpgradeSystem.Instance.GetStat("pickupRange");
        }

        if (distance < effectiveMagnetRange)
        {
            isBeingPulled = true;
        }

        if (isBeingPulled)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * magnetSpeed * Time.deltaTime);

            if (distance < 0.5f)
            {
                Collect();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (ExperienceSystem.Instance != null)
        {
            ExperienceSystem.Instance.AddExperience(experienceValue);
        }

        if (AudioManager.Instance != null)
        {
            // Play pickup sound
        }

        Destroy(gameObject);
    }

    public void SetExperienceValue(int value)
    {
        experienceValue = value;
    }
}

/// <summary>
/// Spawns experience gems when enemies die
/// </summary>
public class ExperienceDropper : MonoBehaviour
{
    [SerializeField] private GameObject experienceGemPrefab;
    [SerializeField] private int minGems = 1;
    [SerializeField] private int maxGems = 3;
    [SerializeField] private float spawnRadius = 1f;

    public void DropExperience()
    {
        if (experienceGemPrefab == null) return;

        int gemCount = Random.Range(minGems, maxGems + 1);

        for (int i = 0; i < gemCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)randomOffset;

            GameObject gem = Instantiate(experienceGemPrefab, spawnPos, Quaternion.identity);
            
            // Add random initial velocity
            Rigidbody2D rb = gem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = randomOffset.normalized * 2f;
            }
        }
    }

    private void OnDestroy()
    {
        // Drop gems when enemy dies
        if (gameObject.scene.isLoaded) // Only if not scene unload
        {
            DropExperience();
        }
    }
}
