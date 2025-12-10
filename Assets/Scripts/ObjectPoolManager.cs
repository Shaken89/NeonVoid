using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal object pooling system for bullets, enemies, effects, and other frequently spawned objects.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100;
        public bool expandIfNeeded = true;
        public Transform poolParent;
    }

    [Header("Pool Configuration")]
    [SerializeField] private Pool[] pools;

    [Header("Auto-Create Pools")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject effectPrefab;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;
    private Dictionary<GameObject, string> activeObjects;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    #endregion

    #region Initialization

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();
        activeObjects = new Dictionary<GameObject, string>();

        // Create configured pools
        if (pools != null)
        {
            foreach (var pool in pools)
            {
                CreatePool(pool);
            }
        }

        // Auto-create common pools
        CreateAutoPool("Bullet", bulletPrefab, 50);
        CreateAutoPool("EnemyBullet", enemyBulletPrefab, 50);
        CreateAutoPool("Enemy", enemyPrefab, 20);
        CreateAutoPool("Effect", effectPrefab, 30);
    }

    private void CreatePool(Pool poolConfig)
    {
        if (poolConfig.prefab == null)
        {
            Debug.LogWarning($"ObjectPoolManager: Pool '{poolConfig.poolName}' has no prefab!");
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();

        // Create parent if not assigned
        if (poolConfig.poolParent == null)
        {
            GameObject parent = new GameObject($"Pool_{poolConfig.poolName}");
            parent.transform.SetParent(transform);
            poolConfig.poolParent = parent.transform;
        }

        // Pre-instantiate objects
        for (int i = 0; i < poolConfig.initialSize; i++)
        {
            GameObject obj = CreatePoolObject(poolConfig);
            objectPool.Enqueue(obj);
        }

        poolDictionary[poolConfig.poolName] = objectPool;
        poolConfigs[poolConfig.poolName] = poolConfig;

        Debug.Log($"Created pool '{poolConfig.poolName}' with {poolConfig.initialSize} objects");
    }

    private void CreateAutoPool(string name, GameObject prefab, int size)
    {
        if (prefab == null || poolDictionary.ContainsKey(name)) return;

        Pool autoPool = new Pool
        {
            poolName = name,
            prefab = prefab,
            initialSize = size,
            maxSize = size * 2,
            expandIfNeeded = true
        };

        CreatePool(autoPool);
    }

    private GameObject CreatePoolObject(Pool poolConfig)
    {
        GameObject obj = Instantiate(poolConfig.prefab);
        obj.SetActive(false);
        obj.transform.SetParent(poolConfig.poolParent);
        return obj;
    }

    #endregion

    #region Spawn & Return

    /// <summary>
    /// Spawns an object from the pool at specified position and rotation.
    /// </summary>
    public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogError($"ObjectPoolManager: Pool '{poolName}' doesn't exist!");
            return null;
        }

        GameObject obj = GetObjectFromPool(poolName);
        
        if (obj == null)
        {
            Debug.LogWarning($"ObjectPoolManager: Pool '{poolName}' exhausted!");
            return null;
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Track active objects
        activeObjects[obj] = poolName;

        return obj;
    }

    /// <summary>
    /// Spawns object with default rotation.
    /// </summary>
    public GameObject Spawn(string poolName, Vector3 position)
    {
        return Spawn(poolName, position, Quaternion.identity);
    }

    /// <summary>
    /// Returns an object to its pool.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (!activeObjects.ContainsKey(obj))
        {
            Debug.LogWarning("ObjectPoolManager: Trying to return object that wasn't spawned from pool!");
            Destroy(obj);
            return;
        }

        string poolName = activeObjects[obj];
        activeObjects.Remove(obj);

        // Reset object
        obj.SetActive(false);
        obj.transform.SetParent(poolConfigs[poolName].poolParent);

        // Return to pool
        poolDictionary[poolName].Enqueue(obj);
    }

    /// <summary>
    /// Returns object after a delay.
    /// </summary>
    public void ReturnAfterDelay(GameObject obj, float delay)
    {
        StartCoroutine(ReturnAfterDelayRoutine(obj, delay));
    }

    private System.Collections.IEnumerator ReturnAfterDelayRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(obj);
    }

    private GameObject GetObjectFromPool(string poolName)
    {
        Queue<GameObject> pool = poolDictionary[poolName];
        Pool config = poolConfigs[poolName];

        // Try to get from pool
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
                return obj;
        }

        // Pool empty - try to expand
        if (config.expandIfNeeded && GetActiveCount(poolName) < config.maxSize)
        {
            GameObject newObj = CreatePoolObject(config);
            Debug.Log($"Expanded pool '{poolName}'");
            return newObj;
        }

        return null;
    }

    #endregion

    #region Pool Management

    /// <summary>
    /// Pre-warms a pool by spawning and returning objects.
    /// </summary>
    public void WarmPool(string poolName, int count)
    {
        if (!poolDictionary.ContainsKey(poolName)) return;

        List<GameObject> tempObjects = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Spawn(poolName, Vector3.zero, Quaternion.identity);
            if (obj != null)
                tempObjects.Add(obj);
        }

        foreach (var obj in tempObjects)
        {
            Return(obj);
        }

        Debug.Log($"Warmed pool '{poolName}' with {count} objects");
    }

    /// <summary>
    /// Clears all objects from a pool.
    /// </summary>
    public void ClearPool(string poolName)
    {
        if (!poolDictionary.ContainsKey(poolName)) return;

        Queue<GameObject> pool = poolDictionary[poolName];

        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
                Destroy(obj);
        }

        pool.Clear();
        Debug.Log($"Cleared pool '{poolName}'");
    }

    /// <summary>
    /// Returns all active objects from a specific pool.
    /// </summary>
    public void ReturnAllFromPool(string poolName)
    {
        List<GameObject> toReturn = new List<GameObject>();

        foreach (var kvp in activeObjects)
        {
            if (kvp.Value == poolName)
                toReturn.Add(kvp.Key);
        }

        foreach (var obj in toReturn)
        {
            Return(obj);
        }

        Debug.Log($"Returned {toReturn.Count} objects to pool '{poolName}'");
    }

    /// <summary>
    /// Returns all active objects from all pools.
    /// </summary>
    public void ReturnAll()
    {
        List<GameObject> toReturn = new List<GameObject>(activeObjects.Keys);

        foreach (var obj in toReturn)
        {
            Return(obj);
        }

        Debug.Log($"Returned {toReturn.Count} total objects");
    }

    #endregion

    #region Quick Access Methods

    public GameObject SpawnBullet(Vector3 position, Quaternion rotation) => Spawn("Bullet", position, rotation);
    public GameObject SpawnEnemyBullet(Vector3 position, Quaternion rotation) => Spawn("EnemyBullet", position, rotation);
    public GameObject SpawnEnemy(Vector3 position) => Spawn("Enemy", position, Quaternion.identity);
    public GameObject SpawnEffect(Vector3 position) => Spawn("Effect", position, Quaternion.identity);

    #endregion

    #region Info & Stats

    /// <summary>
    /// Gets the number of available objects in a pool.
    /// </summary>
    public int GetAvailableCount(string poolName)
    {
        return poolDictionary.ContainsKey(poolName) ? poolDictionary[poolName].Count : 0;
    }

    /// <summary>
    /// Gets the number of active objects from a pool.
    /// </summary>
    public int GetActiveCount(string poolName)
    {
        int count = 0;
        foreach (var kvp in activeObjects)
        {
            if (kvp.Value == poolName)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Checks if a pool exists.
    /// </summary>
    public bool HasPool(string poolName) => poolDictionary.ContainsKey(poolName);

    /// <summary>
    /// Creates a new pool at runtime.
    /// </summary>
    public void CreateRuntimePool(string poolName, GameObject prefab, int initialSize = 10)
    {
        if (poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' already exists!");
            return;
        }

        Pool newPool = new Pool
        {
            poolName = poolName,
            prefab = prefab,
            initialSize = initialSize,
            maxSize = initialSize * 2,
            expandIfNeeded = true
        };

        CreatePool(newPool);
    }

    #endregion

    #region Debug

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 200, 300, 400));
        GUILayout.Box("Object Pool Stats");

        foreach (var poolName in poolDictionary.Keys)
        {
            int available = GetAvailableCount(poolName);
            int active = GetActiveCount(poolName);
            GUILayout.Label($"{poolName}: {active} active / {available} available");
        }

        GUILayout.EndArea();
    }
#endif

    #endregion
}
