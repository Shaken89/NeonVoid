using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Advanced enemy spawner with wave progression, spawn patterns, and difficulty scaling.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        [Range(0f, 1f)] public float spawnWeight = 1f;
        public int minWave = 1;
        public string enemyName = "Enemy";
    }

    [System.Serializable]
    public class WaveSettings
    {
        [Range(1, 100)] public int startEnemyCount = 5;
        [Range(0, 50)] public int enemiesPerWave = 2;
        [Range(0.1f, 10f)] public float spawnDelay = 1f;
        [Range(0f, 5f)] public float waveBreakTime = 3f;
    }

    [Header("Enemy Configuration")]
    [SerializeField] private EnemySpawnData[] enemyTypes;
    [SerializeField] private bool randomizeSpawnOrder = true;

    [Header("Wave Settings")]
    [SerializeField] private WaveSettings waveSettings;
    [SerializeField, Range(1f, 3f)] private float difficultyScaling = 1.2f;
    [SerializeField] private int maxEnemiesAlive = 50;

    [Header("Spawn Area")]
    [SerializeField, Range(5f, 50f)] private float spawnRadius = 8f;
    [SerializeField, Range(0f, 20f)] private float minSpawnDistance = 2f;
    [SerializeField] private SpawnPattern spawnPattern = SpawnPattern.Circle;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Visual & Audio")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private AudioClip waveStartSound;
    [SerializeField] private AudioClip enemySpawnSound;
    [SerializeField] private bool showSpawnGizmos = true;

    [Header("References")]
    [SerializeField] private HUDController hud;
    [SerializeField] private Transform spawnCenter;

    private Transform player;
    private int currentWave;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool isSpawningWave;
    private bool isPaused;
    private int totalEnemiesSpawned;
    private int totalEnemiesKilled;
    private Coroutine waveCoroutine;

    public enum SpawnPattern
    {
        Circle,
        Random,
        Grid,
        Wave,
        Spiral
    }

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateConfiguration();
        InitializeReferences();
    }

    private void Start()
    {
        FindPlayer();
        StartWaveSystem();
    }

    private void Update()
    {
        if (isPaused) return;

        CleanupDestroyedEnemies();
        CheckWaveCompletion();
    }

    #endregion

    #region Initialization

    private void ValidateConfiguration()
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError("EnemySpawner: No enemy types configured!");
            enabled = false;
            return;
        }

        // Validate enemy prefabs
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i].enemyPrefab == null)
            {
                Debug.LogWarning($"EnemySpawner: Enemy type at index {i} has null prefab!");
            }
        }
    }

    private void InitializeReferences()
    {
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        if (spawnCenter == null)
            spawnCenter = transform;
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: Player not found!");
            enabled = false;
        }
    }

    #endregion

    #region Wave Management

    private void StartWaveSystem()
    {
        if (waveCoroutine != null)
            StopCoroutine(waveCoroutine);
        
        waveCoroutine = StartCoroutine(WaveSequence());
    }

    private IEnumerator WaveSequence()
    {
        while (enabled)
        {
            yield return StartCoroutine(StartNextWave());
            yield return new WaitUntil(() => aliveEnemies.Count == 0 && !isSpawningWave);
            yield return new WaitForSeconds(waveSettings.waveBreakTime);
        }
    }

    private IEnumerator StartNextWave()
    {
        isSpawningWave = true;
        currentWave++;

        UpdateHUD();
        PlayWaveStartEffects();

        int enemyCount = CalculateWaveEnemyCount();
        Debug.Log($"<color=yellow>Wave {currentWave} starting - {enemyCount} enemies</color>");

        yield return SpawnWaveEnemies(enemyCount);

        isSpawningWave = false;
    }

    private int CalculateWaveEnemyCount()
    {
        // Calculate base count
        int baseCount = waveSettings.startEnemyCount + (currentWave - 1) * waveSettings.enemiesPerWave;
        
        // Apply difficulty scaling
        float scaledCount = baseCount * Mathf.Pow(difficultyScaling, (currentWave - 1) * 0.1f);
        
        // Clamp to max
        return Mathf.Min(Mathf.RoundToInt(scaledCount), maxEnemiesAlive);
    }

    private IEnumerator SpawnWaveEnemies(int count)
    {
        List<int> spawnOrder = GenerateSpawnOrder(count);

        for (int i = 0; i < count; i++)
        {
            if (isPaused)
            {
                yield return new WaitUntil(() => !isPaused);
            }

            // Check if we've hit the max alive limit
            if (aliveEnemies.Count >= maxEnemiesAlive)
            {
                yield return new WaitUntil(() => aliveEnemies.Count < maxEnemiesAlive);
            }

            int spawnIndex = randomizeSpawnOrder ? spawnOrder[i] : i;
            SpawnEnemy(spawnIndex);

            yield return new WaitForSeconds(waveSettings.spawnDelay);
        }
    }

    private List<int> GenerateSpawnOrder(int count)
    {
        List<int> order = new List<int>();
        for (int i = 0; i < count; i++)
        {
            order.Add(i);
        }

        // Shuffle if randomized
        if (randomizeSpawnOrder)
        {
            for (int i = 0; i < order.Count; i++)
            {
                int randomIndex = Random.Range(i, order.Count);
                int temp = order[i];
                order[i] = order[randomIndex];
                order[randomIndex] = temp;
            }
        }

        return order;
    }

    private void CheckWaveCompletion()
    {
        if (!isSpawningWave && aliveEnemies.Count == 0 && currentWave > 0)
        {
            // Wave completed
            totalEnemiesKilled += totalEnemiesSpawned;
            totalEnemiesSpawned = 0;
        }
    }

    #endregion

    #region Enemy Spawning

    private void SpawnEnemy(int sequenceIndex)
    {
        if (player == null) return;

        // Select enemy type
        EnemySpawnData enemyData = SelectEnemyType();
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Failed to select valid enemy type");
            return;
        }

        // Calculate spawn position
        Vector2 spawnPos = CalculateSpawnPosition(sequenceIndex);
        
        // Check if position is valid
        if (!IsValidSpawnPosition(spawnPos))
        {
            spawnPos = FindAlternativeSpawnPosition();
        }

        // Spawn the enemy
        GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
        enemy.transform.SetPositionAndRotation(
            new Vector3(spawnPos.x, spawnPos.y, 0f),
            Quaternion.identity
        );

        aliveEnemies.Add(enemy);
        totalEnemiesSpawned++;

        // Spawn effects
        SpawnVisualEffects(spawnPos);
        PlaySpawnSound();
    }

    private EnemySpawnData SelectEnemyType()
    {
        // Filter enemies available for current wave
        var availableEnemies = enemyTypes.Where(e => e.minWave <= currentWave && e.enemyPrefab != null).ToList();
        
        if (availableEnemies.Count == 0)
            return null;

        // Weighted random selection
        float totalWeight = availableEnemies.Sum(e => e.spawnWeight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in availableEnemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
                return enemy;
        }

        return availableEnemies[0];
    }

    private Vector2 CalculateSpawnPosition(int index)
    {
        Vector2 centerPos = spawnCenter != null ? (Vector2)spawnCenter.position : (Vector2)player.position;

        switch (spawnPattern)
        {
            case SpawnPattern.Circle:
                return CalculateCirclePosition(centerPos);

            case SpawnPattern.Grid:
                return CalculateGridPosition(centerPos, index);

            case SpawnPattern.Wave:
                return CalculateWavePosition(centerPos, index);

            case SpawnPattern.Spiral:
                return CalculateSpiralPosition(centerPos, index);

            case SpawnPattern.Random:
            default:
                return CalculateRandomPosition(centerPos);
        }
    }

    private Vector2 CalculateCirclePosition(Vector2 center)
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        float distance = Random.Range(spawnRadius * 0.8f, spawnRadius);
        return center + direction * distance;
    }

    private Vector2 CalculateRandomPosition(Vector2 center)
    {
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        return center + randomOffset;
    }

    private Vector2 CalculateGridPosition(Vector2 center, int index)
    {
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(waveSettings.startEnemyCount));
        float spacing = (spawnRadius * 2f) / gridSize;
        
        int x = index % gridSize;
        int y = index / gridSize;
        
        Vector2 offset = new Vector2(
            (x - gridSize / 2f) * spacing,
            (y - gridSize / 2f) * spacing
        );
        
        return center + offset;
    }

    private Vector2 CalculateWavePosition(Vector2 center, int index)
    {
        float angle = (index * 360f / waveSettings.startEnemyCount) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return center + direction * spawnRadius;
    }

    private Vector2 CalculateSpiralPosition(Vector2 center, int index)
    {
        float angle = index * 137.5f * Mathf.Deg2Rad; // Golden angle
        float distance = Mathf.Sqrt(index) * (spawnRadius / 5f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return center + direction * distance;
    }

    private bool IsValidSpawnPosition(Vector2 position)
    {
        // Check minimum distance from player
        if (Vector2.Distance(position, player.position) < minSpawnDistance)
            return false;

        // Check for obstacles
        if (obstacleLayer != 0)
        {
            Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, obstacleLayer);
            if (hit != null)
                return false;
        }

        return true;
    }

    private Vector2 FindAlternativeSpawnPosition()
    {
        // Try multiple times to find a valid position
        for (int i = 0; i < 10; i++)
        {
            Vector2 pos = CalculateCirclePosition((Vector2)player.position);
            if (IsValidSpawnPosition(pos))
                return pos;
        }

        // Fallback to simple circular spawn
        return (Vector2)player.position + Random.insideUnitCircle.normalized * spawnRadius;
    }

    #endregion

    #region Effects & Feedback

    private void UpdateHUD()
    {
        if (hud != null)
            hud.UpdateWave(currentWave);
    }

    private void PlayWaveStartEffects()
    {
        if (waveStartSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(waveStartSound, 0.8f);
    }

    private void SpawnVisualEffects(Vector2 position)
    {
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void PlaySpawnSound()
    {
        if (enemySpawnSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPosition(enemySpawnSound, transform.position, 0.3f);
    }

    #endregion

    #region Utility

    private void CleanupDestroyedEnemies()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Pauses the spawner.
    /// </summary>
    public void PauseSpawner()
    {
        isPaused = true;
    }

    /// <summary>
    /// Resumes the spawner.
    /// </summary>
    public void ResumeSpawner()
    {
        isPaused = false;
    }

    /// <summary>
    /// Forces the next wave to start immediately.
    /// </summary>
    public void ForceNextWave()
    {
        // Clear remaining enemies
        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        aliveEnemies.Clear();
    }

    /// <summary>
    /// Gets the current wave number.
    /// </summary>
    public int GetCurrentWave() => currentWave;

    /// <summary>
    /// Gets the number of enemies currently alive.
    /// </summary>
    public int GetAliveEnemyCount() => aliveEnemies.Count;

    /// <summary>
    /// Gets whether a wave is currently spawning.
    /// </summary>
    public bool IsSpawningWave() => isSpawningWave;

    /// <summary>
    /// Gets total enemies spawned this wave.
    /// </summary>
    public int GetTotalEnemiesSpawned() => totalEnemiesSpawned;

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

        // Draw spawn radius
        Gizmos.color = Color.yellow;
        DrawCircle(center, spawnRadius, 32);

        // Draw min spawn distance
        Gizmos.color = Color.red;
        if (Application.isPlaying && player != null)
        {
            DrawCircle(player.position, minSpawnDistance, 16);
        }
        else
        {
            DrawCircle(center, minSpawnDistance, 16);
        }

        // Draw spawn pattern preview
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        if (Application.isPlaying)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = CalculateSpawnPosition(i);
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
#endif

    #endregion
}
