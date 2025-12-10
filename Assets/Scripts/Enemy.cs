using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy AI controller with intelligent movement, shooting, and collision avoidance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Range(1f, 10f)] private float speed = 2f;
    [SerializeField, Range(0f, 10f)] private float stopDistance = 3f;
    [SerializeField, Range(0f, 5f)] private float retreatDistance = 1.5f;
    [SerializeField, Range(0.5f, 3f)] private float avoidRadius = 1.2f;
    [SerializeField, Range(1f, 20f)] private float moveSmooth = 5f;
    [SerializeField, Range(1f, 20f)] private float rotateSmooth = 6f;

    [Header("Combat Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Range(1f, 20f)] private float bulletSpeed = 6f;
    [SerializeField, Range(0.1f, 5f)] private float shootInterval = 1.5f;
    [SerializeField, Range(0f, 10f)] private float shootRange = 15f;
    [SerializeField] private float bulletLifetime = 3f;

    [Header("Health Settings")]
    [SerializeField, Range(1, 100)] private int maxHealth = 3;
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Scoring & Effects")]
    [SerializeField, Range(1, 100)] private int scoreValue = 1;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 2f)] private float hitFlashDuration = 0.1f;

    [Header("Optimization")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask bulletLayer;

    // Private references
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private HUDController hud;
    
    // State tracking
    private int currentHealth;
    private float shootTimer;
    private Vector2 currentVelocity;
    private bool isDead;
    private Coroutine flashCoroutine;
    private Color originalColor;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeHealth();
    }

    private void Start()
    {
        FindPlayer();
        FindHUD();
        shootTimer = Random.Range(0f, shootInterval); // Random start timing
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;
        
        UpdateMovement();
        UpdateCombat();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        
        if (showHealthBar && healthBarPrefab != null)
        {
            // Create health bar UI above enemy
            GameObject healthBar = Instantiate(healthBarPrefab, transform);
            healthBar.transform.localPosition = Vector3.up * 0.5f;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning($"Enemy '{name}': Player not found!");
    }

    private void FindHUD()
    {
        hud = FindFirstObjectByType<HUDController>();
        if (hud == null)
            Debug.LogWarning($"Enemy '{name}': HUDController not found!");
    }

    #endregion

    #region Movement & AI

    private void UpdateMovement()
    {
        Vector2 toPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Smooth rotation towards player
        RotateTowardsPlayer(toPlayer);

        // Calculate movement direction with collision avoidance
        Vector2 avoidance = AvoidOtherEnemies();
        Vector2 moveDir = CalculateMovementDirection(toPlayer, distanceToPlayer, avoidance);

        // Apply smooth movement
        ApplyMovement(moveDir);
    }

    private void RotateTowardsPlayer(Vector2 toPlayer)
    {
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = rb.rotation;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotateSmooth * Time.fixedDeltaTime);
        rb.SetRotation(newAngle);
    }

    private Vector2 CalculateMovementDirection(Vector2 toPlayer, float distance, Vector2 avoidance)
    {
        if (distance > stopDistance)
        {
            // Move towards player
            return toPlayer + avoidance;
        }
        else if (distance < retreatDistance)
        {
            // Retreat from player
            return -toPlayer + avoidance;
        }
        else
        {
            // Stay in optimal range, only apply avoidance
            return avoidance;
        }
    }

    private void ApplyMovement(Vector2 moveDir)
    {
        Vector2 targetVelocity = moveDir.normalized * speed;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, moveSmooth * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;
    }

    private Vector2 AvoidOtherEnemies()
    {
        Vector2 avoidance = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, avoidRadius, enemyLayer);

        foreach (Collider2D col in nearby)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                Vector2 away = (Vector2)(transform.position - col.transform.position);
                float distance = away.magnitude;
                
                if (distance > 0f && distance < avoidRadius)
                {
                    // Stronger avoidance when closer
                    avoidance += (away.normalized / distance) * avoidRadius;
                }
            }
        }

        return avoidance.normalized * 0.5f;
    }

    #endregion

    #region Combat

    private void UpdateCombat()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Only shoot if in range and optimal distance
        if (distanceToPlayer <= shootRange && distanceToPlayer >= retreatDistance && distanceToPlayer <= stopDistance)
        {
            shootTimer -= Time.fixedDeltaTime;
            
            if (shootTimer <= 0f)
            {
                ShootAtPlayer();
                shootTimer = shootInterval;
            }
        }
    }

    private void ShootAtPlayer()
    {
        if (player == null || bulletPrefab == null)
        {
            Debug.LogWarning($"Enemy '{name}': Cannot shoot - missing player or bulletPrefab");
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)(direction * 0.5f);

        // Spawn bullet
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.SetPositionAndRotation(
            new Vector3(bullet.transform.position.x, bullet.transform.position.y, 0f),
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
        );

        // Configure bullet physics
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
            bulletRb.gravityScale = 0f;
            bulletRb.freezeRotation = true;
            bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        bullet.tag = "EnemyBullet";
        Destroy(bullet, bulletLifetime);

        // Play shoot sound
        if (shootSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootSound, 0.6f);
    }

    #endregion

    #region Health & Damage

    /// <summary>
    /// Damages the enemy by the specified amount.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Visual feedback
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashOnHit());

        // Audio feedback
        if (hurtSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hurtSound, 0.7f);

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator FlashOnHit()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Award score
        if (hud != null)
            hud.UpdateScore(scoreValue);

        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Play death sound
        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound, 0.8f);

        Destroy(gameObject);
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Handle bullet collision
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            TakeDamage(1);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current health of the enemy.
    /// </summary>
    public int GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Gets the max health of the enemy.
    /// </summary>
    public int GetMaxHealth() => maxHealth;

    /// <summary>
    /// Checks if the enemy is dead.
    /// </summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Gets the distance to the player.
    /// </summary>
    public float GetDistanceToPlayer()
    {
        return player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
    }

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw stop distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Draw retreat distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);

        // Draw avoid radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, avoidRadius);

        // Draw shoot range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
#endif

    #endregion
}
