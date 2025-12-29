using UnityEngine;
using System.Collections;

/// <summary>
/// Berserker Enemy - becomes stronger and faster as health decreases
/// Extremely dangerous at low HP, glows red and charges aggressively
/// </summary>
public class BerserkerEnemy : EnemyBase
{
    [Header("Berserker Settings")]
    [SerializeField] private float enrageThreshold = 0.5f; // 50% HP
    [SerializeField] private float berserkThreshold = 0.25f; // 25% HP

    [Header("Rage Bonuses")]
    [SerializeField] private float enragedSpeedMultiplier = 1.5f;
    [SerializeField] private float enragedDamageMultiplier = 1.5f;
    [SerializeField] private float berserkSpeedMultiplier = 2.5f;
    [SerializeField] private float berserkDamageMultiplier = 2f;

    [Header("Charge Attack")]
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float chargeCooldown = 4f;
    [SerializeField] private int contactDamage = 2;

    [Header("Ground Pound")]
    [SerializeField] private float groundPoundRadius = 4f;
    [SerializeField] private int groundPoundDamage = 3;
    [SerializeField] private float groundPoundCooldown = 8f;
    [SerializeField] private float groundPoundWindupTime = 1f;

    [Header("Regeneration")]
    [SerializeField] private bool canRegenerate = true;
    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private int regenAmount = 1;
    [SerializeField] private float regenInterval = 2f;

    [Header("Visual")]
    [SerializeField] private GameObject enrageAuraPrefab;
    [SerializeField] private GameObject chargeTrailPrefab;
    [SerializeField] private ParticleSystem rageParticles;

    private bool isEnraged;
    private bool isBerserk;
    private bool isCharging;
    private bool isGroundPounding;
    private float lastChargeTime;
    private float lastGroundPoundTime;
    private float lastDamageTime;
    private Vector2 chargeDirection;
    private GameObject enrageAuraInstance;
    private GameObject chargeTrailInstance;
    private Coroutine regenCoroutine;

    protected override void Start()
    {
        base.Start();
        maxHealth = 15;
        moveSpeed = 2.5f;
        scoreValue = 10;
        currentHealth = maxHealth;

        lastDamageTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        UpdateRageState();

        if (!isCharging && !isGroundPounding)
        {
            // Try special attacks
            if (CanGroundPound())
            {
                StartCoroutine(GroundPound());
            }
            else if (CanCharge())
            {
                StartCoroutine(ChargeAttack());
            }
            else
            {
                // Normal movement
                MoveTowardsPlayer();
            }
        }
    }

    private void UpdateRageState()
    {
        float healthPercent = (float)currentHealth / maxHealth;

        // Check for berserk state
        if (!isBerserk && healthPercent <= berserkThreshold)
        {
            EnterBerserkState();
        }
        // Check for enraged state
        else if (!isEnraged && healthPercent <= enrageThreshold)
        {
            EnterEnragedState();
        }
    }

    private void EnterEnragedState()
    {
        isEnraged = true;

        // Visual changes
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, 0.5f);
        }

        // Spawn aura
        if (enrageAuraPrefab != null)
        {
            enrageAuraInstance = Instantiate(enrageAuraPrefab, transform);
            enrageAuraInstance.transform.localPosition = Vector3.zero;
        }

        if (rageParticles != null)
            rageParticles.Play();

        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnEffect("PowerUp", transform.position);

        Debug.Log("Berserker ENRAGED!");
    }

    private void EnterBerserkState()
    {
        isBerserk = true;

        // More intense visual changes
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            transform.localScale *= 1.2f; // Grow slightly
        }

        // Upgrade aura
        if (enrageAuraInstance != null)
        {
            enrageAuraInstance.transform.localScale *= 1.5f;
        }

        if (rageParticles != null)
        {
            var main = rageParticles.main;
            main.startSize = main.startSize.constant * 1.5f;
        }

        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 2f);

        Debug.Log("Berserker BERSERK!");
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        float speed = GetEffectiveSpeed();
        rb.linearVelocity = direction * speed;
    }

    private float GetEffectiveSpeed()
    {
        float speed = moveSpeed;

        if (isBerserk)
            speed *= berserkSpeedMultiplier;
        else if (isEnraged)
            speed *= enragedSpeedMultiplier;

        return speed;
    }

    private int GetEffectiveDamage()
    {
        int damage = contactDamage;

        if (isBerserk)
            damage = Mathf.RoundToInt(damage * berserkDamageMultiplier);
        else if (isEnraged)
            damage = Mathf.RoundToInt(damage * enragedDamageMultiplier);

        return damage;
    }

    private bool CanCharge()
    {
        if (player == null) return false;
        
        float distance = GetDistanceToPlayer();
        return distance < 12f && 
               distance > 3f && 
               Time.time > lastChargeTime + chargeCooldown;
    }

    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        lastChargeTime = Time.time;

        // Aim at player
        chargeDirection = (player.position - transform.position).normalized;

        // Warning flash
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.yellow;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = currentColor;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Spawn trail
        if (chargeTrailPrefab != null)
        {
            chargeTrailInstance = Instantiate(chargeTrailPrefab, transform);
        }

        // Charge!
        float chargeTime = 0f;
        while (chargeTime < chargeDuration)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            chargeTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Cleanup
        if (chargeTrailInstance != null)
            Destroy(chargeTrailInstance);

        rb.linearVelocity *= 0.5f;
        isCharging = false;
    }

    private bool CanGroundPound()
    {
        if (player == null) return false;
        if (!isEnraged) return false; // Only when enraged or berserk

        float distance = GetDistanceToPlayer();
        return distance < 5f && Time.time > lastGroundPoundTime + groundPoundCooldown;
    }

    private IEnumerator GroundPound()
    {
        isGroundPounding = true;
        lastGroundPoundTime = Time.time;

        // Stop moving and wind up
        rb.linearVelocity = Vector2.zero;

        // Wind up animation
        Vector3 originalScale = transform.localScale;
        float windupTime = 0f;

        while (windupTime < groundPoundWindupTime)
        {
            windupTime += Time.deltaTime;
            float t = windupTime / groundPoundWindupTime;
            
            // Grow and lift
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
            
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.white, t);

            yield return null;
        }

        // SLAM!
        transform.localScale = originalScale;
        
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 3f);

        // Damage all nearby enemies
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundPoundRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth health = hit.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(groundPoundDamage);
                }
            }
        }

        // Screen shake would go here

        isGroundPounding = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                int damage = GetEffectiveDamage();
                if (isCharging)
                    damage = Mathf.RoundToInt(damage * 1.5f);
                
                health.TakeDamage(damage);
            }
        }
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        lastDamageTime = Time.time;

        // Stop regeneration when taking damage
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        // Start regeneration timer
        if (canRegenerate && !isDead)
        {
            regenCoroutine = StartCoroutine(RegenerationRoutine());
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        yield return new WaitForSeconds(regenDelay);

        while (!isDead && currentHealth < maxHealth)
        {
            // Check if took damage recently
            if (Time.time - lastDamageTime < regenDelay)
                yield break;

            currentHealth = Mathf.Min(currentHealth + regenAmount, maxHealth);

            // Visual feedback
            if (ParticleManager.Instance != null)
                ParticleManager.Instance.SpawnEffect("Heal", transform.position);

            yield return new WaitForSeconds(regenInterval);
        }
    }

    protected override void Die()
    {
        if (enrageAuraInstance != null)
            Destroy(enrageAuraInstance);

        if (chargeTrailInstance != null)
            Destroy(chargeTrailInstance);

        if (rageParticles != null)
            rageParticles.Stop();

        // Massive explosion
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 4f);

        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundPoundRadius);
    }
}
