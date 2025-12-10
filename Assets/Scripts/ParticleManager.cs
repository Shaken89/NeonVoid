using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized particle effects manager for spawning and managing visual effects.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [System.Serializable]
    public class ParticleEffect
    {
        public string effectName;
        public GameObject prefab;
        public float defaultLifetime = 2f;
        public bool usePooling = true;
    }

    [Header("Effect Library")]
    [SerializeField] private ParticleEffect[] effects;

    [Header("Common Effects")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private GameObject powerUpEffectPrefab;

    [Header("Pooling Settings")]
    [SerializeField] private bool enablePooling = true;
    [SerializeField] private int poolSize = 20;

    private Dictionary<string, ParticleEffect> effectDictionary;
    private Dictionary<string, Queue<GameObject>> effectPools;

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

        InitializeEffects();
        InitializePools();
    }

    #endregion

    #region Initialization

    private void InitializeEffects()
    {
        effectDictionary = new Dictionary<string, ParticleEffect>();
        
        // Add predefined effects
        if (effects != null)
        {
            foreach (var effect in effects)
            {
                if (!string.IsNullOrEmpty(effect.effectName) && effect.prefab != null)
                {
                    effectDictionary[effect.effectName] = effect;
                }
            }
        }

        // Add common effects
        AddCommonEffect("Explosion", explosionPrefab, 2f);
        AddCommonEffect("Impact", impactPrefab, 1f);
        AddCommonEffect("MuzzleFlash", muzzleFlashPrefab, 0.3f);
        AddCommonEffect("Heal", healEffectPrefab, 1.5f);
        AddCommonEffect("Dash", dashEffectPrefab, 1f);
        AddCommonEffect("PowerUp", powerUpEffectPrefab, 2f);
    }

    private void AddCommonEffect(string name, GameObject prefab, float lifetime)
    {
        if (prefab != null && !effectDictionary.ContainsKey(name))
        {
            effectDictionary[name] = new ParticleEffect
            {
                effectName = name,
                prefab = prefab,
                defaultLifetime = lifetime,
                usePooling = true
            };
        }
    }

    private void InitializePools()
    {
        if (!enablePooling) return;

        effectPools = new Dictionary<string, Queue<GameObject>>();

        foreach (var effect in effectDictionary.Values)
        {
            if (effect.usePooling)
            {
                CreatePool(effect);
            }
        }
    }

    private void CreatePool(ParticleEffect effect)
    {
        Queue<GameObject> pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(effect.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
        }

        effectPools[effect.effectName] = pool;
    }

    #endregion

    #region Effect Spawning

    /// <summary>
    /// Spawns an effect by name at specified position.
    /// </summary>
    public GameObject SpawnEffect(string effectName, Vector3 position, Quaternion rotation = default)
    {
        if (rotation == default)
            rotation = Quaternion.identity;

        if (!effectDictionary.ContainsKey(effectName))
        {
            Debug.LogWarning($"ParticleManager: Effect '{effectName}' not found!");
            return null;
        }

        ParticleEffect effect = effectDictionary[effectName];
        GameObject instance = null;

        if (enablePooling && effect.usePooling && effectPools.ContainsKey(effectName))
        {
            instance = GetFromPool(effectName);
        }
        else
        {
            instance = Instantiate(effect.prefab);
        }

        if (instance != null)
        {
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.SetActive(true);

            // Auto-destroy or return to pool
            if (enablePooling && effect.usePooling)
            {
                StartCoroutine(ReturnToPoolAfterDelay(instance, effectName, effect.defaultLifetime));
            }
            else
            {
                Destroy(instance, effect.defaultLifetime);
            }
        }

        return instance;
    }

    /// <summary>
    /// Spawns an effect with custom lifetime.
    /// </summary>
    public GameObject SpawnEffect(string effectName, Vector3 position, float lifetime)
    {
        GameObject effect = SpawnEffect(effectName, position);
        
        if (effect != null)
        {
            Destroy(effect, lifetime);
        }

        return effect;
    }

    #endregion

    #region Quick Access Methods

    public GameObject SpawnExplosion(Vector3 position) => SpawnEffect("Explosion", position);
    
    public GameObject SpawnExplosion(Vector3 position, float scale)
    {
        GameObject effect = SpawnEffect("Explosion", position);
        if (effect != null)
        {
            effect.transform.localScale = Vector3.one * scale;
        }
        return effect;
    }
    
    public GameObject SpawnDeath(Vector3 position) => SpawnEffect("Impact", position);
    
    public GameObject SpawnImpact(Vector3 position) => SpawnEffect("Impact", position);
    public GameObject SpawnMuzzleFlash(Vector3 position, Quaternion rotation) => SpawnEffect("MuzzleFlash", position, rotation);
    public GameObject SpawnHealEffect(Vector3 position) => SpawnEffect("Heal", position);
    public GameObject SpawnDashEffect(Vector3 position) => SpawnEffect("Dash", position);
    public GameObject SpawnPowerUpEffect(Vector3 position) => SpawnEffect("PowerUp", position);

    #endregion

    #region Pooling

    private GameObject GetFromPool(string effectName)
    {
        if (!effectPools.ContainsKey(effectName) || effectPools[effectName].Count == 0)
        {
            // Pool exhausted, create new instance
            return Instantiate(effectDictionary[effectName].prefab);
        }

        return effectPools[effectName].Dequeue();
    }

    private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject obj, string effectName, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj, effectName);
    }

    private void ReturnToPool(GameObject obj, string effectName)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        
        if (effectPools.ContainsKey(effectName))
        {
            effectPools[effectName].Enqueue(obj);
        }
    }

    #endregion

    #region Screen Shake

    /// <summary>
    /// Triggers camera shake effect.
    /// </summary>
    public void ShakeScreen(float duration = 0.2f, float magnitude = 0.3f)
    {
        StartCoroutine(ScreenShakeRoutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ScreenShakeRoutine(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.position = originalPos;
    }

    #endregion

    #region Public Utilities

    /// <summary>
    /// Registers a new effect at runtime.
    /// </summary>
    public void RegisterEffect(string name, GameObject prefab, float lifetime = 2f, bool usePooling = true)
    {
        if (effectDictionary.ContainsKey(name))
        {
            Debug.LogWarning($"ParticleManager: Effect '{name}' already registered!");
            return;
        }

        ParticleEffect newEffect = new ParticleEffect
        {
            effectName = name,
            prefab = prefab,
            defaultLifetime = lifetime,
            usePooling = usePooling
        };

        effectDictionary[name] = newEffect;

        if (enablePooling && usePooling)
        {
            CreatePool(newEffect);
        }
    }

    /// <summary>
    /// Checks if an effect is registered.
    /// </summary>
    public bool HasEffect(string effectName) => effectDictionary.ContainsKey(effectName);

    #endregion
}
