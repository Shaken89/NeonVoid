using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all enemy types with common functionality.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Enemy Settings")]
    [SerializeField] protected int maxHealth = 3;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected int scoreValue = 1;
    [SerializeField] protected float detectionRange = 15f;

    [Header("Visual")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Color damageColor = Color.red;

    [Header("Audio")]
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;

    protected Transform player;
    protected Rigidbody2D rb;
    protected HUDController hud;
    protected int currentHealth;
    protected bool isDead;
    protected Color originalColor;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected virtual void Start()
    {
        FindPlayer();
        FindHUD();
        currentHealth = maxHealth;
    }

    protected void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected void FindHUD()
    {
        hud = FindFirstObjectByType<HUDController>();
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(DamageFlash());

        if (hurtSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hurtSound, 0.5f);

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hud != null)
        {
            hud.UpdateScore(scoreValue);
            hud.RegisterKill();
        }

        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound, 0.6f);

        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnDeath(transform.position);

        Destroy(gameObject);
    }

    protected IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    protected float GetDistanceToPlayer()
    {
        return player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
    }

    public int GetCurrentHealth() => currentHealth;
    public bool IsDead() => isDead;
}

/// <summary>
/// Tank enemy - slow but high health
/// </summary>
public class TankEnemy : EnemyBase
{
    [Header("Tank Settings")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDistance = 10f;
    [SerializeField] private float chargeCooldown = 5f;

    private float lastChargeTime;
    private bool isCharging;

    protected override void Start()
    {
        base.Start();
        maxHealth = 10;
        moveSpeed = 1f;
        scoreValue = 5;
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distance = GetDistanceToPlayer();

        if (!isCharging && distance < chargeDistance && Time.time > lastChargeTime + chargeCooldown)
        {
            StartCoroutine(ChargeAttack());
        }
        else if (!isCharging)
        {
            MoveTowardsPlayer();
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        lastChargeTime = Time.time;

        Vector2 chargeDirection = (player.position - transform.position).normalized;
        rb.linearVelocity = chargeDirection * chargeSpeed;

        yield return new WaitForSeconds(1f);

        rb.linearVelocity *= 0.5f;
        isCharging = false;
    }
}

/// <summary>
/// Sniper enemy - long range attacks
/// </summary>
public class SniperEnemy : EnemyBase
{
    [Header("Sniper Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float shootRange = 20f;
    [SerializeField] private float shootInterval = 3f;
    [SerializeField] private float keepDistance = 12f;

    private float shootTimer;

    protected override void Start()
    {
        base.Start();
        maxHealth = 2;
        moveSpeed = 3f;
        scoreValue = 3;
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distance = GetDistanceToPlayer();

        // Keep distance
        if (distance < keepDistance)
        {
            Vector2 direction = (transform.position - player.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Shoot
        if (distance <= shootRange)
        {
            shootTimer -= Time.fixedDeltaTime;
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootInterval;
            }
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
            bulletRb.linearVelocity = direction * bulletSpeed;

        Destroy(bullet, 5f);
    }
}

/// <summary>
/// Kamikaze enemy - rushes and explodes
/// </summary>
public class KamikazeEnemy : EnemyBase
{
    [Header("Kamikaze Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int explosionDamage = 2;
    [SerializeField] private float rushSpeed = 6f;
    [SerializeField] private float activationRange = 8f;

    private bool isActivated;

    protected override void Start()
    {
        base.Start();
        maxHealth = 1;
        moveSpeed = 2f;
        scoreValue = 2;
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        float distance = GetDistanceToPlayer();

        if (distance < activationRange && !isActivated)
        {
            isActivated = true;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        float speed = isActivated ? rushSpeed : moveSpeed;
        rb.linearVelocity = direction * speed;
    }

    protected override void Die()
    {
        Explode();
        base.Die();
    }

    private void Explode()
    {
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 1.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(explosionDamage);
            }
        }
    }
}
