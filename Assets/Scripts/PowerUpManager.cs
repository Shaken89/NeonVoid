using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages power-up drops, pickups, and temporary buffs for the player.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [System.Serializable]
    public class PowerUpDefinition
    {
        public PowerUpType type;
        public string displayName;
        public GameObject prefab;
        public Sprite icon;
        [Range(0f, 1f)] public float dropChance = 0.15f;
        public float duration = 10f;
        public float effectValue = 1.5f;
        public Color glowColor = Color.white;
        public AudioClip pickupSound;
    }

    public enum PowerUpType
    {
        Health,
        Shield,
        Ammo,
        SpeedBoost,
        DamageBoost,
        FireRateBoost,
        Invincibility,
        MultiShot,
        RapidFire,
        ScoreMultiplier
    }

    [Header("Power-Up Configuration")]
    [SerializeField] private PowerUpDefinition[] powerUpDefinitions;
    [SerializeField] private GameObject defaultPowerUpPrefab;

    [Header("Drop Settings")]
    [SerializeField, Range(0f, 1f)] private float globalDropChance = 0.3f;
    [SerializeField] private float powerUpLifetime = 15f;
    [SerializeField] private float magnetRange = 2f;
    [SerializeField] private float magnetSpeed = 10f;

    [Header("Visual Settings")]
    // Visual effects can be added here if needed

    [Header("Audio")]
    [SerializeField] private AudioClip defaultPickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.7f;

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;

    // Active power-ups tracking
    private Dictionary<PowerUpType, Coroutine> activePowerUps = new Dictionary<PowerUpType, Coroutine>();
    private List<GameObject> spawnedPowerUps = new List<GameObject>();

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

        InitializeComponents();
        CreateDefaultDefinitions();
    }

    private void Update()
    {
        UpdatePowerUpMagnetism();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void CreateDefaultDefinitions()
    {
        if (powerUpDefinitions == null || powerUpDefinitions.Length == 0)
        {
            powerUpDefinitions = new PowerUpDefinition[]
            {
                new PowerUpDefinition 
                { 
                    type = PowerUpType.Health, 
                    displayName = "Health Pack",
                    dropChance = 0.25f,
                    effectValue = 25f,
                    glowColor = Color.green
                },
                new PowerUpDefinition 
                { 
                    type = PowerUpType.SpeedBoost, 
                    displayName = "Speed Boost",
                    dropChance = 0.15f,
                    duration = 8f,
                    effectValue = 1.5f,
                    glowColor = Color.cyan
                },
                new PowerUpDefinition 
                { 
                    type = PowerUpType.DamageBoost, 
                    displayName = "Damage Boost",
                    dropChance = 0.12f,
                    duration = 10f,
                    effectValue = 2f,
                    glowColor = Color.red
                },
                new PowerUpDefinition 
                { 
                    type = PowerUpType.Invincibility, 
                    displayName = "Shield",
                    dropChance = 0.08f,
                    duration = 5f,
                    glowColor = Color.yellow
                }
            };
        }
    }

    #endregion

    #region Power-Up Spawning

    /// <summary>
    /// Attempts to spawn a random power-up at the given position.
    /// </summary>
    public void TrySpawnPowerUp(Vector3 position)
    {
        if (Random.value > globalDropChance) return;

        PowerUpDefinition selected = SelectRandomPowerUp();
        if (selected != null)
        {
            SpawnPowerUp(selected, position);
        }
    }

    /// <summary>
    /// Spawns a specific power-up type at the given position.
    /// </summary>
    public GameObject SpawnPowerUp(PowerUpType type, Vector3 position)
    {
        PowerUpDefinition definition = GetPowerUpDefinition(type);
        if (definition != null)
        {
            return SpawnPowerUp(definition, position);
        }
        return null;
    }

    private GameObject SpawnPowerUp(PowerUpDefinition definition, Vector3 position)
    {
        GameObject prefab = definition.prefab != null ? definition.prefab : defaultPowerUpPrefab;
        
        if (prefab == null)
        {
            Debug.LogWarning("PowerUpManager: No prefab available for power-up!");
            return null;
        }

        position.z = 0f;
        GameObject powerUp = Instantiate(prefab, position, Quaternion.identity);
        
        // Setup power-up component
        PowerUpPickup pickup = powerUp.GetComponent<PowerUpPickup>();
        if (pickup == null)
        {
            pickup = powerUp.AddComponent<PowerUpPickup>();
        }
        
        pickup.Initialize(definition, this);
        
        // Setup visuals
        SpriteRenderer sprite = powerUp.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = definition.glowColor;
        }

        spawnedPowerUps.Add(powerUp);
        StartCoroutine(PowerUpLifetimeRoutine(powerUp));
        
        return powerUp;
    }

    private PowerUpDefinition SelectRandomPowerUp()
    {
        float totalChance = 0f;
        foreach (var def in powerUpDefinitions)
        {
            totalChance += def.dropChance;
        }

        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var def in powerUpDefinitions)
        {
            currentChance += def.dropChance;
            if (randomValue <= currentChance)
                return def;
        }

        return powerUpDefinitions[0];
    }

    private IEnumerator PowerUpLifetimeRoutine(GameObject powerUp)
    {
        yield return new WaitForSeconds(powerUpLifetime);
        
        if (powerUp != null)
        {
            spawnedPowerUps.Remove(powerUp);
            Destroy(powerUp);
        }
    }

    #endregion

    #region Power-Up Collection

    /// <summary>
    /// Called when player collects a power-up.
    /// </summary>
    public void CollectPowerUp(PowerUpDefinition definition)
    {
        if (definition == null) return;

        ApplyPowerUpEffect(definition);
        PlayPickupSound(definition);
        
        Debug.Log($"<color=cyan>Collected {definition.displayName}!</color>");
    }

    private void ApplyPowerUpEffect(PowerUpDefinition definition)
    {
        switch (definition.type)
        {
            case PowerUpType.Health:
                ApplyHealthPowerUp(definition);
                break;
            
            case PowerUpType.Shield:
                ApplyShieldPowerUp(definition);
                break;
            
            case PowerUpType.Ammo:
                ApplyAmmoPowerUp(definition);
                break;
            
            case PowerUpType.SpeedBoost:
                ApplyTemporaryBuff(definition, () => ApplySpeedBoost(definition.effectValue));
                break;
            
            case PowerUpType.DamageBoost:
                ApplyTemporaryBuff(definition, () => ApplyDamageBoost(definition.effectValue));
                break;
            
            case PowerUpType.FireRateBoost:
                ApplyTemporaryBuff(definition, () => ApplyFireRateBoost(definition.effectValue));
                break;
            
            case PowerUpType.Invincibility:
                ApplyTemporaryBuff(definition, () => ApplyInvincibility());
                break;
            
            case PowerUpType.ScoreMultiplier:
                ApplyTemporaryBuff(definition, () => ApplyScoreMultiplier(definition.effectValue));
                break;
        }
    }

    #endregion

    #region Individual Power-Up Effects

    private void ApplyHealthPowerUp(PowerUpDefinition definition)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal((int)definition.effectValue);
        }
    }

    private void ApplyShieldPowerUp(PowerUpDefinition definition)
    {
        if (playerHealth != null)
        {
            playerHealth.RestoreShield((int)definition.effectValue);
        }
    }

    private void ApplyAmmoPowerUp(PowerUpDefinition definition)
    {
        if (player != null)
        {
            player.AddAmmo((int)definition.effectValue);
        }
    }

    private void ApplySpeedBoost(float multiplier)
    {
        if (player == null) return;
        float currentSpeed = player.GetMoveSpeed();
        player.SetMoveSpeed(currentSpeed * multiplier);
    }

    private void RemoveSpeedBoost(float multiplier)
    {
        if (player == null) return;
        float currentSpeed = player.GetMoveSpeed();
        player.SetMoveSpeed(currentSpeed / multiplier);
    }

    private void ApplyDamageBoost(float multiplier)
    {
        // Would need damage system implementation
        Debug.Log($"Damage boosted by {multiplier}x!");
    }

    private void RemoveDamageBoost(float multiplier)
    {
        Debug.Log("Damage boost ended");
    }

    private void ApplyFireRateBoost(float multiplier)
    {
        if (player == null) return;
        var shooting = player.GetShootingSettings();
        shooting.fireRate /= multiplier;
    }

    private void RemoveFireRateBoost(float multiplier)
    {
        if (player == null) return;
        var shooting = player.GetShootingSettings();
        shooting.fireRate *= multiplier;
    }

    private void ApplyInvincibility()
    {
        // Invincibility would be handled by PlayerHealth
        Debug.Log("Invincibility activated!");
    }

    private void RemoveInvincibility()
    {
        Debug.Log("Invincibility ended");
    }

    private void ApplyScoreMultiplier(float multiplier)
    {
        Debug.Log($"Score multiplier: {multiplier}x!");
    }

    private void RemoveScoreMultiplier(float multiplier)
    {
        Debug.Log("Score multiplier ended");
    }

    #endregion

    #region Temporary Buffs

    private void ApplyTemporaryBuff(PowerUpDefinition definition, System.Action applyEffect)
    {
        // Stop existing buff of same type
        if (activePowerUps.ContainsKey(definition.type))
        {
            StopCoroutine(activePowerUps[definition.type]);
        }

        applyEffect?.Invoke();
        
        Coroutine buffCoroutine = StartCoroutine(TemporaryBuffRoutine(definition));
        activePowerUps[definition.type] = buffCoroutine;
    }

    private IEnumerator TemporaryBuffRoutine(PowerUpDefinition definition)
    {
        yield return new WaitForSeconds(definition.duration);
        
        // Remove effect
        switch (definition.type)
        {
            case PowerUpType.SpeedBoost:
                RemoveSpeedBoost(definition.effectValue);
                break;
            case PowerUpType.DamageBoost:
                RemoveDamageBoost(definition.effectValue);
                break;
            case PowerUpType.FireRateBoost:
                RemoveFireRateBoost(definition.effectValue);
                break;
            case PowerUpType.Invincibility:
                RemoveInvincibility();
                break;
            case PowerUpType.ScoreMultiplier:
                RemoveScoreMultiplier(definition.effectValue);
                break;
        }
        
        activePowerUps.Remove(definition.type);
        Debug.Log($"{definition.displayName} effect ended");
    }

    #endregion

    #region Magnetism

    private void UpdatePowerUpMagnetism()
    {
        if (player == null) return;

        for (int i = spawnedPowerUps.Count - 1; i >= 0; i--)
        {
            if (spawnedPowerUps[i] == null)
            {
                spawnedPowerUps.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(spawnedPowerUps[i].transform.position, player.transform.position);
            
            if (distance <= magnetRange)
            {
                Vector3 direction = (player.transform.position - spawnedPowerUps[i].transform.position).normalized;
                spawnedPowerUps[i].transform.position += direction * magnetSpeed * Time.deltaTime;
            }
        }
    }

    #endregion

    #region Audio

    private void PlayPickupSound(PowerUpDefinition definition)
    {
        AudioClip sound = definition.pickupSound != null ? definition.pickupSound : defaultPickupSound;
        
        if (sound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sound, pickupVolume);
        }
    }

    #endregion

    #region Public Getters

    public bool IsPowerUpActive(PowerUpType type) => activePowerUps.ContainsKey(type);
    public int GetActivePowerUpCount() => activePowerUps.Count;
    public PowerUpDefinition GetPowerUpDefinition(PowerUpType type)
    {
        foreach (var def in powerUpDefinitions)
        {
            if (def.type == type) return def;
        }
        return null;
    }

    #endregion
}

/// <summary>
/// Component for individual power-up pickups.
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    private PowerUpManager.PowerUpDefinition definition;
    private PowerUpManager manager;
    private float startY;

    public void Initialize(PowerUpManager.PowerUpDefinition def, PowerUpManager mgr)
    {
        definition = def;
        manager = mgr;
        startY = transform.position.y;
    }

    private void Update()
    {
        // Floating animation
        float newY = startY + Mathf.Sin(Time.time * 2f) * 0.3f;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Rotation
        transform.Rotate(Vector3.forward * 90f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (manager != null && definition != null)
            {
                manager.CollectPowerUp(definition);
                Destroy(gameObject);
            }
        }
    }
}
