using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Nova Drift inspired modular upgrade system with synergies
/// </summary>
public class ModularUpgradeSystem : MonoBehaviour
{
    public static ModularUpgradeSystem Instance { get; private set; }

    #region Enums
    
    public enum UpgradeCategory
    {
        Weapon,      // Оружие
        Hull,        // Корпус
        Shield,      // Щит
        Construct,   // Конструкции (турели, дроны)
        Special,     // Специальные способности
        Gear         // Экипировка
    }

    public enum UpgradeRarity
    {
        Common,      // Белый
        Uncommon,    // Зеленый
        Rare,        // Синий
        Epic,        // Фиолетовый
        Legendary    // Золотой
    }

    public enum StatModifierType
    {
        Flat,        // +5
        Percentage,  // +50%
        Multiplier   // x2
    }

    #endregion

    #region Data Structures

    [System.Serializable]
    public class UpgradeDefinition
    {
        public string id;
        public string displayName;
        [TextArea(3, 6)]
        public string description;
        public UpgradeCategory category;
        public UpgradeRarity rarity;
        public Sprite icon;
        
        [Header("Requirements")]
        public List<string> requiredUpgrades = new List<string>();
        public List<string> incompatibleUpgrades = new List<string>();
        public int minLevel = 1;
        
        [Header("Stats")]
        public List<StatModifier> statModifiers = new List<StatModifier>();
        
        [Header("Special Effects")]
        public bool hasSpecialEffect;
        public string specialEffectId;
        
        [Header("Synergies")]
        public List<SynergyRule> synergies = new List<SynergyRule>();
        
        [Header("Weight")]
        public float weight = 1f;
    }

    [System.Serializable]
    public class StatModifier
    {
        public string statName;
        public StatModifierType type;
        public float value;
        public bool stackable = true;
    }

    [System.Serializable]
    public class SynergyRule
    {
        public string otherUpgradeId;
        public string synergyDescription;
        public List<StatModifier> bonusModifiers;
    }

    [System.Serializable]
    public class PlayerBuild
    {
        public List<string> acquiredUpgrades = new List<string>();
        public Dictionary<string, int> upgradeStacks = new Dictionary<string, int>();
        public Dictionary<string, float> finalStats = new Dictionary<string, float>();
        public List<string> activeSynergies = new List<string>();
    }

    #endregion

    #region Inspector Fields

    [Header("Upgrade Database")]
    [SerializeField] private UpgradeDefinition[] allUpgrades;

    [Header("Level Up Settings")]
    [SerializeField] private int upgradeChoicesPerLevel = 3;
    [SerializeField] private bool allowDuplicates = true;
    [SerializeField] private int maxStacksPerUpgrade = 5;

    [Header("Rarity Weights")]
    [SerializeField] private float commonWeight = 100f;
    [SerializeField] private float uncommonWeight = 50f;
    [SerializeField] private float rareWeight = 20f;
    [SerializeField] private float epicWeight = 5f;
    [SerializeField] private float legendaryWeight = 1f;

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;

    #endregion

    #region Private Fields

    private Dictionary<string, UpgradeDefinition> upgradeDictionary;
    private PlayerBuild currentBuild;
    private int playerLevel = 1;

    #endregion

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

        InitializeSystem();
    }

    private void Start()
    {
        FindReferences();
    }

    #endregion

    #region Initialization

    private void InitializeSystem()
    {
        upgradeDictionary = new Dictionary<string, UpgradeDefinition>();
        currentBuild = new PlayerBuild();

        foreach (var upgrade in allUpgrades)
        {
            if (!string.IsNullOrEmpty(upgrade.id))
            {
                upgradeDictionary[upgrade.id] = upgrade;
            }
        }

        InitializeBaseStats();
    }

    private void FindReferences()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        
        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();
    }

    private void InitializeBaseStats()
    {
        currentBuild.finalStats["moveSpeed"] = 5f;
        currentBuild.finalStats["maxHealth"] = 10f;
        currentBuild.finalStats["maxShield"] = 5f;
        currentBuild.finalStats["fireRate"] = 0.2f;
        currentBuild.finalStats["bulletDamage"] = 1f;
        currentBuild.finalStats["bulletSpeed"] = 15f;
        currentBuild.finalStats["critChance"] = 0.05f;
        currentBuild.finalStats["critMultiplier"] = 2f;
        currentBuild.finalStats["experienceGain"] = 1f;
        currentBuild.finalStats["pickupRange"] = 2f;
    }

    #endregion

    #region Level Up System

    public void LevelUp()
    {
        playerLevel++;
        
        List<UpgradeDefinition> availableUpgrades = GetAvailableUpgrades();
        List<UpgradeDefinition> choices = SelectUpgradeChoices(availableUpgrades, upgradeChoicesPerLevel);
        
        ShowUpgradeChoice(choices);
    }

    private List<UpgradeDefinition> GetAvailableUpgrades()
    {
        List<UpgradeDefinition> available = new List<UpgradeDefinition>();

        foreach (var upgrade in allUpgrades)
        {
            if (IsUpgradeAvailable(upgrade))
            {
                available.Add(upgrade);
            }
        }

        return available;
    }

    private bool IsUpgradeAvailable(UpgradeDefinition upgrade)
    {
        // Check level requirement
        if (playerLevel < upgrade.minLevel)
            return false;

        // Check if already acquired (if duplicates not allowed)
        if (!allowDuplicates && currentBuild.acquiredUpgrades.Contains(upgrade.id))
            return false;

        // Check stack limit
        if (currentBuild.upgradeStacks.ContainsKey(upgrade.id) && 
            currentBuild.upgradeStacks[upgrade.id] >= maxStacksPerUpgrade)
            return false;

        // Check required upgrades
        foreach (var required in upgrade.requiredUpgrades)
        {
            if (!currentBuild.acquiredUpgrades.Contains(required))
                return false;
        }

        // Check incompatible upgrades
        foreach (var incompatible in upgrade.incompatibleUpgrades)
        {
            if (currentBuild.acquiredUpgrades.Contains(incompatible))
                return false;
        }

        return true;
    }

    private List<UpgradeDefinition> SelectUpgradeChoices(List<UpgradeDefinition> available, int count)
    {
        List<UpgradeDefinition> choices = new List<UpgradeDefinition>();
        
        if (available.Count <= count)
            return available;

        // Weighted random selection
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            UpgradeDefinition selected = SelectWeightedUpgrade(available);
            choices.Add(selected);
            available.Remove(selected);
        }

        return choices;
    }

    private UpgradeDefinition SelectWeightedUpgrade(List<UpgradeDefinition> upgrades)
    {
        float totalWeight = 0f;

        foreach (var upgrade in upgrades)
        {
            totalWeight += GetUpgradeWeight(upgrade);
        }

        float random = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var upgrade in upgrades)
        {
            currentWeight += GetUpgradeWeight(upgrade);
            if (random <= currentWeight)
                return upgrade;
        }

        return upgrades[upgrades.Count - 1];
    }

    private float GetUpgradeWeight(UpgradeDefinition upgrade)
    {
        float rarityWeight = upgrade.rarity switch
        {
            UpgradeRarity.Common => commonWeight,
            UpgradeRarity.Uncommon => uncommonWeight,
            UpgradeRarity.Rare => rareWeight,
            UpgradeRarity.Epic => epicWeight,
            UpgradeRarity.Legendary => legendaryWeight,
            _ => 1f
        };

        return rarityWeight * upgrade.weight;
    }

    #endregion

    #region Upgrade Application

    public void ApplyUpgrade(string upgradeId)
    {
        if (!upgradeDictionary.ContainsKey(upgradeId))
        {
            Debug.LogError($"Upgrade {upgradeId} not found!");
            return;
        }

        UpgradeDefinition upgrade = upgradeDictionary[upgradeId];

        // Add to build
        if (!currentBuild.acquiredUpgrades.Contains(upgradeId))
            currentBuild.acquiredUpgrades.Add(upgradeId);

        // Track stacks
        if (!currentBuild.upgradeStacks.ContainsKey(upgradeId))
            currentBuild.upgradeStacks[upgradeId] = 0;
        currentBuild.upgradeStacks[upgradeId]++;

        // Apply stat modifiers
        ApplyStatModifiers(upgrade.statModifiers);

        // Apply special effects
        if (upgrade.hasSpecialEffect)
        {
            ApplySpecialEffect(upgrade.specialEffectId);
        }

        // Check synergies
        CheckSynergies(upgrade);

        // Recalculate all stats
        RecalculateStats();

        Debug.Log($"Applied upgrade: {upgrade.displayName} (Stack: {currentBuild.upgradeStacks[upgradeId]})");
    }

    private void ApplyStatModifiers(List<StatModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (!currentBuild.finalStats.ContainsKey(modifier.statName))
                currentBuild.finalStats[modifier.statName] = 0f;

            switch (modifier.type)
            {
                case StatModifierType.Flat:
                    currentBuild.finalStats[modifier.statName] += modifier.value;
                    break;
                case StatModifierType.Percentage:
                    currentBuild.finalStats[modifier.statName] *= (1f + modifier.value / 100f);
                    break;
                case StatModifierType.Multiplier:
                    currentBuild.finalStats[modifier.statName] *= modifier.value;
                    break;
            }
        }
    }

    private void CheckSynergies(UpgradeDefinition newUpgrade)
    {
        foreach (var synergy in newUpgrade.synergies)
        {
            if (currentBuild.acquiredUpgrades.Contains(synergy.otherUpgradeId))
            {
                string synergyKey = $"{newUpgrade.id}_{synergy.otherUpgradeId}";
                
                if (!currentBuild.activeSynergies.Contains(synergyKey))
                {
                    currentBuild.activeSynergies.Add(synergyKey);
                    ApplyStatModifiers(synergy.bonusModifiers);
                    
                    Debug.Log($"Synergy activated: {synergy.synergyDescription}");
                }
            }
        }
    }

    private void RecalculateStats()
    {
        // Apply to player
        if (player != null)
        {
            var settings = player.GetMovementSettings();
            if (settings != null)
            {
                player.SetMoveSpeed(GetStat("moveSpeed"));
            }
            
            player.SetBulletSpeed(GetStat("bulletSpeed"));
        }

        // Apply to health
        if (playerHealth != null)
        {
            int newMaxHealth = Mathf.RoundToInt(GetStat("maxHealth"));
            int newMaxShield = Mathf.RoundToInt(GetStat("maxShield"));
            
            playerHealth.SetMaxHealth(newMaxHealth);
            playerHealth.SetMaxShield(newMaxShield);
        }
    }

    #endregion

    #region Special Effects

    private void ApplySpecialEffect(string effectId)
    {
        switch (effectId)
        {
            case "ramming_speed":
                ApplyRammingSpeed();
                break;
            case "volatile_shielding":
                ApplyVolatileShielding();
                break;
            case "temporal_flux":
                ApplyTemporalFlux();
                break;
            case "antimatter_torpedo":
                ApplyAntimatterTorpedo();
                break;
            case "shield_booster":
                ApplyShieldBooster();
                break;
            case "overcharge":
                ApplyOvercharge();
                break;
            case "hullbreaker":
                ApplyHullbreaker();
                break;
            case "adaptive_armor":
                ApplyAdaptiveArmor();
                break;
        }
    }

    private void ApplyRammingSpeed()
    {
        // Damage enemies on contact
        var damageOnContact = player.gameObject.AddComponent<ContactDamage>();
        damageOnContact.Initialize(2, 0.5f);
    }

    private void ApplyVolatileShielding()
    {
        // Explode when shield breaks
        if (playerHealth != null)
        {
            playerHealth.gameObject.AddComponent<VolatileShieldEffect>();
        }
    }

    private void ApplyTemporalFlux()
    {
        // Slow time on kill
        var timeEffect = player.gameObject.AddComponent<TimeSlowEffect>();
        timeEffect.Initialize(0.5f, 1f);
    }

    private void ApplyAntimatterTorpedo()
    {
        // Add special weapon
        if (weaponManager != null)
        {
            // Implementation depends on weapon system
        }
    }

    private void ApplyShieldBooster()
    {
        if (playerHealth != null)
        {
            playerHealth.SetShieldRegenRate(playerHealth.GetShieldRegenRate() * 1.5f);
        }
    }

    private void ApplyOvercharge()
    {
        // Increase fire rate temporarily after kill
        player.gameObject.AddComponent<OverchargeEffect>();
    }

    private void ApplyHullbreaker()
    {
        // Extra damage to high health enemies
        player.gameObject.AddComponent<HullbreakerEffect>();
    }

    private void ApplyAdaptiveArmor()
    {
        // Reduce damage from repeated sources
        playerHealth.gameObject.AddComponent<AdaptiveArmorEffect>();
    }

    #endregion

    #region UI Integration

    private void ShowUpgradeChoice(List<UpgradeDefinition> choices)
    {
        UpgradeChoiceUI ui = FindFirstObjectByType<UpgradeChoiceUI>();
        if (ui != null)
        {
            ui.ShowChoices(choices);
        }
        else
        {
            Debug.LogWarning("UpgradeChoiceUI not found! Auto-selecting random upgrade.");
            if (choices.Count > 0)
            {
                ApplyUpgrade(choices[Random.Range(0, choices.Count)].id);
            }
        }
    }

    #endregion

    #region Getters

    public float GetStat(string statName)
    {
        return currentBuild.finalStats.ContainsKey(statName) ? currentBuild.finalStats[statName] : 0f;
    }

    public PlayerBuild GetCurrentBuild() => currentBuild;
    public int GetPlayerLevel() => playerLevel;
    public int GetUpgradeStack(string upgradeId)
    {
        return currentBuild.upgradeStacks.ContainsKey(upgradeId) ? currentBuild.upgradeStacks[upgradeId] : 0;
    }

    public bool HasUpgrade(string upgradeId) => currentBuild.acquiredUpgrades.Contains(upgradeId);
    
    public List<string> GetActiveSynergies() => currentBuild.activeSynergies;

    #endregion
}
