using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss enemy with multiple phases and attack patterns
/// </summary>
public class BossEnemy : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private int scoreValue = 100;

    [Header("Phases")]
    [SerializeField] private int phase1HealthThreshold = 66;
    [SerializeField] private int phase2HealthThreshold = 33;

    [Header("Attack Patterns")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float bulletSpeed = 8f;
    
    [Header("Phase 1: Spread Shot")]
    [SerializeField] private int spreadBulletCount = 8;
    [SerializeField] private float spreadInterval = 2f;

    [Header("Phase 2: Spiral")]
    [SerializeField] private float spiralRotationSpeed = 90f;
    [SerializeField] private float spiralInterval = 0.1f;

    [Header("Phase 3: Barrage")]
    [SerializeField] private float barrageInterval = 0.05f;
    [SerializeField] private int barrageBurstCount = 20;

    [Header("Minion Spawn")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private int minionsPerPhase = 3;
    [SerializeField] private float minionSpawnRadius = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color phase2Color = Color.yellow;
    [SerializeField] private Color phase3Color = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip phaseChangeSound;
    [SerializeField] private AudioClip deathSound;

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    
    private int currentHealth;
    private int currentPhase = 1;
    private Transform player;
    private Rigidbody2D rb;
    private HUDController hud;
    private bool isDead;
    private float spiralAngle;
    private GameObject healthBarInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();
        FindHUD();
        CreateHealthBar();
        StartCoroutine(BossAI());
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void FindHUD()
    {
        hud = FindFirstObjectByType<HUDController>();
    }

    private void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab);
            UpdateHealthBar();
        }
    }

    private IEnumerator BossAI()
    {
        while (!isDead)
        {
            // Movement
            if (player != null)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                rb.linearVelocity = direction * moveSpeed;
            }

            // Attack based on phase
            switch (currentPhase)
            {
                case 1:
                    yield return StartCoroutine(Phase1Attack());
                    break;
                case 2:
                    yield return StartCoroutine(Phase2Attack());
                    break;
                case 3:
                    yield return StartCoroutine(Phase3Attack());
                    break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator Phase1Attack()
    {
        // Spread shot in all directions
        for (int i = 0; i < spreadBulletCount; i++)
        {
            float angle = (360f / spreadBulletCount) * i;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ShootBullet(direction);
        }

        yield return new WaitForSeconds(spreadInterval);
    }

    private IEnumerator Phase2Attack()
    {
        // Spiral pattern
        for (int i = 0; i < 5; i++)
        {
            spiralAngle += spiralRotationSpeed * spiralInterval;
            Vector2 direction = new Vector2(Mathf.Cos(spiralAngle * Mathf.Deg2Rad), Mathf.Sin(spiralAngle * Mathf.Deg2Rad));
            ShootBullet(direction);
            yield return new WaitForSeconds(spiralInterval);
        }
    }

    private IEnumerator Phase3Attack()
    {
        // Rapid barrage towards player
        for (int i = 0; i < barrageBurstCount; i++)
        {
            if (player != null)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
                ShootBullet(direction + randomOffset);
            }
            yield return new WaitForSeconds(barrageInterval);
        }

        yield return new WaitForSeconds(1f);
    }

    private void ShootBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = transform.position;
        if (firePoints != null && firePoints.Length > 0)
        {
            Transform firePoint = firePoints[Random.Range(0, firePoints.Length)];
            if (firePoint != null)
                spawnPos = firePoint.position;
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
            bulletRb.linearVelocity = direction.normalized * bulletSpeed;

        Destroy(bullet, 10f);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        UpdateHealthBar();
        StartCoroutine(DamageFlash());

        if (hurtSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hurtSound, 0.6f);

        CheckPhaseChange();

        if (currentHealth <= 0)
            Die();
    }

    private void CheckPhaseChange()
    {
        int healthPercentage = (currentHealth * 100) / maxHealth;

        if (currentPhase == 1 && healthPercentage <= phase1HealthThreshold)
        {
            ChangePhase(2);
        }
        else if (currentPhase == 2 && healthPercentage <= phase2HealthThreshold)
        {
            ChangePhase(3);
        }
    }

    private void ChangePhase(int newPhase)
    {
        currentPhase = newPhase;

        if (phaseChangeSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(phaseChangeSound, 0.8f);

        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 2f);

        // Change color based on phase
        if (spriteRenderer != null)
        {
            spriteRenderer.color = currentPhase == 2 ? phase2Color : phase3Color;
        }

        // Spawn minions
        SpawnMinions();

        // Increase speed and attack rate
        moveSpeed += 0.5f;
    }

    private void SpawnMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        for (int i = 0; i < minionsPerPhase; i++)
        {
            float angle = (360f / minionsPerPhase) * i;
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * minionSpawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            GameObject minionPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarInstance != null)
        {
            BossHealthBar healthBar = healthBarInstance.GetComponent<BossHealthBar>();
            if (healthBar != null)
            {
                healthBar.UpdateHealth((float)currentHealth / maxHealth);
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hud != null)
        {
            hud.UpdateScore(scoreValue);
            hud.RegisterKill();
        }

        if (deathSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound, 1f);

        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 3f);

        if (healthBarInstance != null)
            Destroy(healthBarInstance);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (healthBarInstance != null)
            Destroy(healthBarInstance);
    }
}
