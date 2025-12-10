using UnityEngine;

/// <summary>
/// Enemy projectile that damages the player and has configurable behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField, Range(1, 100)] private int damage = 1;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Lifetime Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool autoDestroy = true;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color bulletColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.5f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private bool ignoreEnemies = true;
    [SerializeField] private bool destroyOnWalls = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool hasHit;
    private float spawnTime;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        ApplyVisualSettings();
    }

    private void Start()
    {
        spawnTime = Time.time;
        
        if (autoDestroy && lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Update()
    {
        // Optional: Rotate bullet to face movement direction
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Configure Rigidbody2D
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // Configure Collider
        if (col != null)
        {
            col.isTrigger = false; // Use physics collision for better detection
            col.enabled = true;
        }

        // Get visual components if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }

    private void ApplyVisualSettings()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = bulletColor;
        }

        if (trailRenderer != null)
        {
            trailRenderer.startColor = bulletColor;
            trailRenderer.endColor = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0f);
        }
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return; // Prevent multiple hits

        GameObject hitObject = collision.gameObject;
        string hitTag = hitObject.tag;

        #if UNITY_EDITOR
        Debug.Log($"EnemyBullet hit: {hitObject.name} (Tag: {hitTag})");
        #endif

        // Handle player collision
        if (hitTag == "Player")
        {
            HandlePlayerHit(hitObject);
            return;
        }

        // Ignore other enemies
        if (ignoreEnemies && hitTag == "Enemy")
        {
            Physics2D.IgnoreCollision(col, collision.collider);
            return;
        }

        // Handle wall/obstacle collision
        if (destroyOnWalls && (hitTag == "Wall" || hitTag == "Obstacle" || hitTag == "Untagged"))
        {
            HandleWallHit(collision.GetContact(0).point);
            return;
        }

        // Handle any other damageable object
        if (damageableLayers != 0 && ((1 << hitObject.layer) & damageableLayers) != 0)
        {
            HandleGenericHit(hitObject, collision.GetContact(0).point);
        }
    }

    private void HandlePlayerHit(GameObject player)
    {
        hasHit = true;

        // Apply damage to player
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning("EnemyBullet: Player has no PlayerHealth component!");
        }

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            SpawnHitEffect(player.transform.position);
        }

        // Play hit sound
        PlaySound(hitSound);

        // Destroy bullet
        if (destroyOnHit)
        {
            DestroyBullet();
        }
    }

    private void HandleWallHit(Vector2 hitPoint)
    {
        hasHit = true;

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            SpawnHitEffect(hitPoint);
        }

        // Play destroy sound
        PlaySound(destroySound);

        DestroyBullet();
    }

    private void HandleGenericHit(GameObject target, Vector2 hitPoint)
    {
        hasHit = true;

        // Try to apply damage to any IDamageable interface
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            SpawnHitEffect(hitPoint);
        }

        // Play hit sound
        PlaySound(hitSound);

        if (destroyOnHit)
        {
            DestroyBullet();
        }
    }

    #endregion

    #region Effects & Audio

    private void SpawnHitEffect(Vector2 position)
    {
        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        effect.transform.SetPositionAndRotation(
            new Vector3(position.x, position.y, 0f),
            Quaternion.identity
        );
        
        Destroy(effect, 2f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, soundVolume);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the bullet damage value.
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(0, newDamage);
    }

    /// <summary>
    /// Sets the bullet velocity.
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Sets the bullet color.
    /// </summary>
    public void SetColor(Color color)
    {
        bulletColor = color;
        ApplyVisualSettings();
    }

    /// <summary>
    /// Gets the current damage value.
    /// </summary>
    public int GetDamage() => damage;

    /// <summary>
    /// Gets the time this bullet has been alive.
    /// </summary>
    public float GetAliveTime() => Time.time - spawnTime;

    #endregion

    #region Destruction

    private void DestroyBullet()
    {
        // Disable collision to prevent multiple hits
        if (col != null)
            col.enabled = false;

        // Stop movement
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Disable trail
        if (trailRenderer != null)
            trailRenderer.emitting = false;

        // Destroy the gameobject
        Destroy(gameObject, 0.05f);
    }

    private void OnDestroy()
    {
        // Clean up trail if it exists
        if (trailRenderer != null && trailRenderer.transform.parent == transform)
        {
            trailRenderer.autodestruct = true;
        }
    }

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw velocity direction
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f);
        }
    }
#endif

    #endregion
}

/// <summary>
/// Interface for objects that can take damage.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
