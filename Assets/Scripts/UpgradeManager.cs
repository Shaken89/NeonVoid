using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Advanced upgrade manager with weighted selection, upgrade tracking, and visual feedback.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType type;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Range(0f, 1f)] public float weight = 1f;
        public int maxLevel = 10;
        public bool enabled = true;
    }

    [System.Serializable]
    public class UpgradeValues
    {
        [Header("Movement Upgrades")]
        [Range(0.1f, 10f)] public float moveSpeedIncrease = 1f;
        [Range(0.1f, 10f)] public float accelerationIncrease = 0.5f;

        [Header("Combat Upgrades")]
        [Range(1f, 20f)] public float bulletSpeedIncrease = 2f;
        [Range(0.01f, 1f)] public float fireRateDecrease = 0.05f;
        [Range(1, 100)] public int maxAmmoIncrease = 10;
        [Range(1, 50)] public int damageIncrease = 1;

        [Header("Health Upgrades")]
        [Range(1, 100)] public int maxHealthIncrease = 10;
        [Range(1, 100)] public int maxShieldIncrease = 10;
        [Range(0.1f, 5f)] public float regenRateIncrease = 0.1f;
    }

    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Upgrade Configuration")]
    [SerializeField] private UpgradeDefinition[] availableUpgrades;
    [SerializeField] private UpgradeValues upgradeValues;

    [Header("Upgrade System Settings")]
    [SerializeField] private bool preventDuplicates = true;
    [SerializeField] private int maxRecentUpgrades = 3;
    [SerializeField] private bool useWeightedSelection = true;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject upgradeEffectPrefab;
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField, Range(0f, 1f)] private float upgradeSoundVolume = 0.8f;

    [Header("UI References")]
    [SerializeField] private HUDController hud;

    // Tracking
    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    private Queue<UpgradeType> recentUpgrades = new Queue<UpgradeType>();
    private int totalUpgradesApplied;

    public enum UpgradeType
    {
        MoveSpeed,
        Acceleration,
        BulletSpeed,
        FireRate,
        MaxAmmo,
        Damage,
        MaxHealth,
        MaxShield,
        HealthRegen,
        ShieldRegen
    }

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeUpgrades();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        // Initialize default upgrades if none assigned
        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            CreateDefaultUpgrades();
        }
    }

    private void InitializeUpgrades()
    {
        // Initialize upgrade levels dictionary
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            upgradeLevels[type] = 0;
        }
    }

    private void CreateDefaultUpgrades()
    {
        availableUpgrades = new UpgradeDefinition[]
        {
            new UpgradeDefinition 
            { 
                type = UpgradeType.MoveSpeed, 
                displayName = "Movement Speed",
                description = "Increase your movement speed",
                weight = 1f,
                maxLevel = 10
            },
            new UpgradeDefinition 
            { 
                type = UpgradeType.BulletSpeed, 
                displayName = "Bullet Speed",
                description = "Increase bullet velocity",
                weight = 1f,
                maxLevel = 10
            },
            new UpgradeDefinition 
            { 
                type = UpgradeType.FireRate, 
                displayName = "Fire Rate",
                description = "Shoot faster",
                weight = 1f,
                maxLevel = 15
            },
            new UpgradeDefinition 
            { 
                type = UpgradeType.MaxHealth, 
                displayName = "Max Health",
                description = "Increase maximum health",
                weight = 0.8f,
                maxLevel = 20
            }
        };
    }

    #endregion

    #region Upgrade Application

    /// <summary>
    /// Applies a random weighted upgrade to the player.
    /// </summary>
    public void ApplyRandomUpgrade()
    {
        if (player == null)
        {
            Debug.LogWarning("UpgradeManager: Player reference is null!");
            return;
        }

        UpgradeType selectedType = SelectRandomUpgrade();
        ApplyUpgrade(selectedType);
    }

    /// <summary>
    /// Applies a specific upgrade type.
    /// </summary>
    public void ApplyUpgrade(UpgradeType type)
    {
        if (player == null && playerHealth == null)
        {
            Debug.LogWarning("UpgradeManager: No valid targets for upgrade!");
            return;
        }

        // Check if upgrade is at max level
        var upgrade = GetUpgradeDefinition(type);
        if (upgrade != null && upgradeLevels[type] >= upgrade.maxLevel)
        {
            Debug.LogWarning($"Upgrade {type} is already at max level!");
            return;
        }

        // Apply the upgrade
        bool success = ApplyUpgradeInternal(type);

        if (success)
        {
            // Track upgrade
            upgradeLevels[type]++;
            totalUpgradesApplied++;
            TrackRecentUpgrade(type);

            // Feedback
            PlayUpgradeFeedback(type);
            
            Debug.Log($"<color=green>Upgrade Applied: {type} (Level {upgradeLevels[type]})</color>");
        }
    }

    private bool ApplyUpgradeInternal(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.MoveSpeed:
                return UpgradeMoveSpeed();
            
            case UpgradeType.Acceleration:
                return UpgradeAcceleration();
            
            case UpgradeType.BulletSpeed:
                return UpgradeBulletSpeed();
            
            case UpgradeType.FireRate:
                return UpgradeFireRate();
            
            case UpgradeType.MaxAmmo:
                return UpgradeMaxAmmo();
            
            case UpgradeType.Damage:
                return UpgradeDamage();
            
            case UpgradeType.MaxHealth:
                return UpgradeMaxHealth();
            
            case UpgradeType.MaxShield:
                return UpgradeMaxShield();
            
            case UpgradeType.HealthRegen:
                return UpgradeHealthRegen();
            
            case UpgradeType.ShieldRegen:
                return UpgradeShieldRegen();
            
            default:
                Debug.LogWarning($"Unknown upgrade type: {type}");
                return false;
        }
    }

    #endregion

    #region Individual Upgrades

    private bool UpgradeMoveSpeed()
    {
        if (player == null) return false;
        float currentSpeed = player.GetMoveSpeed();
        player.SetMoveSpeed(currentSpeed + upgradeValues.moveSpeedIncrease);
        return true;
    }

    private bool UpgradeAcceleration()
    {
        if (player == null) return false;
        var movement = player.GetMovementSettings();
        movement.acceleration += upgradeValues.accelerationIncrease;
        return true;
    }

    private bool UpgradeBulletSpeed()
    {
        if (player == null) return false;
        float currentSpeed = player.GetBulletSpeed();
        player.SetBulletSpeed(currentSpeed + upgradeValues.bulletSpeedIncrease);
        return true;
    }

    private bool UpgradeFireRate()
    {
        if (player == null) return false;
        var shooting = player.GetShootingSettings();
        shooting.fireRate = Mathf.Max(0.05f, shooting.fireRate - upgradeValues.fireRateDecrease);
        return true;
    }

    private bool UpgradeMaxAmmo()
    {
        if (player == null) return false;
        var shooting = player.GetShootingSettings();
        if (shooting.maxAmmo > 0)
        {
            shooting.maxAmmo += upgradeValues.maxAmmoIncrease;
            player.AddAmmo(upgradeValues.maxAmmoIncrease);
            return true;
        }
        return false;
    }

    private bool UpgradeDamage()
    {
        if (player == null) return false;
        // This would require a damage system in PlayerController
        Debug.LogWarning("Damage upgrade not yet implemented");
        return false;
    }

    private bool UpgradeMaxHealth()
    {
        if (playerHealth == null) return false;
        playerHealth.IncreaseMaxHealth(upgradeValues.maxHealthIncrease);
        return true;
    }

    private bool UpgradeMaxShield()
    {
        if (playerHealth == null) return false;
        playerHealth.IncreaseMaxShield(upgradeValues.maxShieldIncrease);
        return true;
    }

    private bool UpgradeHealthRegen()
    {
        if (playerHealth == null) return false;
        // Would require access to regen settings
        Debug.LogWarning("Health regen upgrade not yet implemented");
        return false;
    }

    private bool UpgradeShieldRegen()
    {
        if (playerHealth == null) return false;
        // Would require access to regen settings
        Debug.LogWarning("Shield regen upgrade not yet implemented");
        return false;
    }

    #endregion

    #region Selection Logic

    private UpgradeType SelectRandomUpgrade()
    {
        // Get available upgrades
        var available = GetAvailableUpgrades();

        if (available.Count == 0)
        {
            Debug.LogWarning("No available upgrades!");
            return UpgradeType.MoveSpeed; // Fallback
        }

        if (useWeightedSelection)
        {
            return SelectWeightedUpgrade(available);
        }
        else
        {
            return available[Random.Range(0, available.Count)].type;
        }
    }

    private List<UpgradeDefinition> GetAvailableUpgrades()
    {
        var available = new List<UpgradeDefinition>();

        foreach (var upgrade in availableUpgrades)
        {
            // Skip disabled upgrades
            if (!upgrade.enabled) continue;

            // Skip max level upgrades
            if (upgradeLevels[upgrade.type] >= upgrade.maxLevel) continue;

            // Skip recent duplicates
            if (preventDuplicates && recentUpgrades.Contains(upgrade.type)) continue;

            available.Add(upgrade);
        }

        return available;
    }

    private UpgradeType SelectWeightedUpgrade(List<UpgradeDefinition> upgrades)
    {
        float totalWeight = upgrades.Sum(u => u.weight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var upgrade in upgrades)
        {
            currentWeight += upgrade.weight;
            if (randomValue <= currentWeight)
                return upgrade.type;
        }

        return upgrades[0].type; // Fallback
    }

    private void TrackRecentUpgrade(UpgradeType type)
    {
        recentUpgrades.Enqueue(type);
        
        while (recentUpgrades.Count > maxRecentUpgrades)
        {
            recentUpgrades.Dequeue();
        }
    }

    #endregion

    #region Feedback

    private void PlayUpgradeFeedback(UpgradeType type)
    {
        // Visual effect
        if (upgradeEffectPrefab != null && player != null)
        {
            GameObject effect = Instantiate(upgradeEffectPrefab, player.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Audio
        if (upgradeSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(upgradeSound, upgradeSoundVolume);
        }

        // HUD notification (if available)
        var upgrade = GetUpgradeDefinition(type);
        if (upgrade != null)
        {
            Debug.Log($"📈 {upgrade.displayName} upgraded to level {upgradeLevels[type]}!");
        }
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Gets the current level of an upgrade.
    /// </summary>
    public int GetUpgradeLevel(UpgradeType type)
    {
        return upgradeLevels.ContainsKey(type) ? upgradeLevels[type] : 0;
    }

    /// <summary>
    /// Gets the total number of upgrades applied.
    /// </summary>
    public int GetTotalUpgrades() => totalUpgradesApplied;

    /// <summary>
    /// Gets the upgrade definition for a specific type.
    /// </summary>
    public UpgradeDefinition GetUpgradeDefinition(UpgradeType type)
    {
        return availableUpgrades?.FirstOrDefault(u => u.type == type);
    }

    /// <summary>
    /// Gets all available upgrade definitions.
    /// </summary>
    public UpgradeDefinition[] GetAllUpgrades() => availableUpgrades;

    /// <summary>
    /// Checks if an upgrade can be applied.
    /// </summary>
    public bool CanApplyUpgrade(UpgradeType type)
    {
        var upgrade = GetUpgradeDefinition(type);
        if (upgrade == null || !upgrade.enabled) return false;
        return upgradeLevels[type] < upgrade.maxLevel;
    }

    /// <summary>
    /// Resets all upgrades (for new game).
    /// </summary>
    public void ResetUpgrades()
    {
        foreach (var key in upgradeLevels.Keys.ToList())
        {
            upgradeLevels[key] = 0;
        }
        recentUpgrades.Clear();
        totalUpgradesApplied = 0;
    }

    #endregion
}
