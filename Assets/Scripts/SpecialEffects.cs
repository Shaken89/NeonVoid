using UnityEngine;
using System.Collections;

/// <summary>
/// Special effect components for Nova Drift style abilities
/// </summary>
/// 
// Ramming Speed - damage on contact
public class ContactDamage : MonoBehaviour
{
    private int damage;
    private float cooldown;
    private float lastDamageTime;

    public void Initialize(int dmg, float cd)
    {
        damage = dmg;
        cooldown = cd;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < lastDamageTime + cooldown) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                lastDamageTime = Time.time;

                if (ParticleManager.Instance != null)
                    ParticleManager.Instance.SpawnImpact(collision.contacts[0].point);
            }
        }
    }
}

// Volatile Shielding - explode when shield breaks
public class VolatileShieldEffect : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int explosionDamage = 5;

    private PlayerHealth health;
    private int lastShield;

    private void Start()
    {
        health = GetComponent<PlayerHealth>();
        lastShield = health != null ? health.GetCurrentShield() : 0;
    }

    private void Update()
    {
        if (health == null) return;

        int currentShield = health.GetCurrentShield();
        
        if (lastShield > 0 && currentShield == 0)
        {
            Explode();
        }

        lastShield = currentShield;
    }

    private void Explode()
    {
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnExplosion(transform.position, 2f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                    enemy.TakeDamage(explosionDamage);
            }
        }

        if (AudioManager.Instance != null)
        {
            // Play explosion sound
        }
    }
}

// Time Slow Effect - slow time on kill
public class TimeSlowEffect : MonoBehaviour
{
    private float slowAmount;
    private float duration;
    private bool isActive;

    public void Initialize(float amount, float dur)
    {
        slowAmount = amount;
        duration = dur;
    }

    private void Start()
    {
        HUDController hud = FindFirstObjectByType<HUDController>();
        if (hud != null)
        {
            // Subscribe to kill events if available
        }
    }

    public void OnEnemyKilled()
    {
        if (!isActive)
        {
            StartCoroutine(SlowTimeCoroutine());
        }
    }

    private IEnumerator SlowTimeCoroutine()
    {
        isActive = true;
        Time.timeScale = slowAmount;
        Time.fixedDeltaTime = 0.02f * slowAmount;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isActive = false;
    }
}

// Overcharge - temporary fire rate boost on kill
public class OverchargeEffect : MonoBehaviour
{
    [SerializeField] private float fireRateBoost = 0.5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private int maxStacks = 5;

    private PlayerController player;
    private int currentStacks = 0;
    private Coroutine overchargeCoroutine;

    private void Start()
    {
        player = GetComponent<PlayerController>();
    }

    public void OnEnemyKilled()
    {
        if (currentStacks < maxStacks)
        {
            currentStacks++;
            ApplyBoost();
        }

        if (overchargeCoroutine != null)
            StopCoroutine(overchargeCoroutine);
        
        overchargeCoroutine = StartCoroutine(OverchargeDuration());
    }

    private void ApplyBoost()
    {
        if (player != null)
        {
            var settings = player.GetShootingSettings();
            if (settings != null)
            {
                // Reduce fire rate (faster shooting)
                float currentRate = settings.fireRate;
                float newRate = currentRate * (1f - fireRateBoost * currentStacks);
                // Apply via ModularUpgradeSystem stats
            }
        }
    }

    private IEnumerator OverchargeDuration()
    {
        yield return new WaitForSeconds(duration);
        currentStacks = 0;
        ApplyBoost();
    }
}

// Hullbreaker - extra damage to high health enemies
public class HullbreakerEffect : MonoBehaviour
{
    [SerializeField] private float healthThreshold = 0.7f;
    [SerializeField] private float damageMultiplier = 1.5f;

    // This would be integrated into bullet damage calculation
    public float GetDamageMultiplier(EnemyBase enemy)
    {
        if (enemy == null) return 1f;

        float healthPercent = (float)enemy.GetCurrentHealth() / 10f; // Assuming max health
        
        if (healthPercent >= healthThreshold)
            return damageMultiplier;
        
        return 1f;
    }
}

// Adaptive Armor - reduce damage from same source
public class AdaptiveArmorEffect : MonoBehaviour
{
    [SerializeField] private float damageReduction = 0.1f;
    [SerializeField] private int maxStacks = 5;

    private PlayerHealth health;
    private string lastDamageSource;
    private int adaptiveStacks = 0;

    private void Start()
    {
        health = GetComponent<PlayerHealth>();
    }

    public int ModifyDamage(int incomingDamage, string source)
    {
        if (source == lastDamageSource)
        {
            adaptiveStacks = Mathf.Min(adaptiveStacks + 1, maxStacks);
        }
        else
        {
            adaptiveStacks = 0;
            lastDamageSource = source;
        }

        float reduction = 1f - (damageReduction * adaptiveStacks);
        return Mathf.Max(1, Mathf.RoundToInt(incomingDamage * reduction));
    }
}

// Shield Drain - leech shield from enemies
public class ShieldDrainEffect : MonoBehaviour
{
    [SerializeField] private float drainPercent = 0.1f;

    private PlayerHealth health;

    private void Start()
    {
        health = GetComponent<PlayerHealth>();
    }

    public void OnEnemyHit(int damageDealt)
    {
        if (health != null)
        {
            int shieldGain = Mathf.Max(1, Mathf.RoundToInt(damageDealt * drainPercent));
            health.RestoreShield(shieldGain);
        }
    }
}

// Piercing Rounds - bullets penetrate enemies
public class PiercingRoundsEffect : MonoBehaviour
{
    [SerializeField] private int maxPenetrations = 3;

    // This would modify bullet behavior
    public int GetMaxPenetrations() => maxPenetrations;
}

// Split Shot - bullets split on hit
public class SplitShotEffect : MonoBehaviour
{
    [SerializeField] private int splitCount = 2;
    [SerializeField] private GameObject splitBulletPrefab;

    public void OnBulletHit(Vector3 position, Vector2 direction)
    {
        if (splitBulletPrefab == null) return;

        float angleStep = 360f / splitCount;
        
        for (int i = 0; i < splitCount; i++)
        {
            float angle = angleStep * i;
            Vector2 splitDirection = Quaternion.Euler(0, 0, angle) * direction;
            
            GameObject split = Instantiate(splitBulletPrefab, position, Quaternion.identity);
            Rigidbody2D rb = split.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = splitDirection * 10f;

            Destroy(split, 3f);
        }
    }
}

// Chain Lightning - arc between enemies
public class ChainLightningEffect : MonoBehaviour
{
    [SerializeField] private int maxChains = 3;
    [SerializeField] private float chainRange = 5f;
    [SerializeField] private int chainDamage = 2;

    public void OnEnemyHit(Vector3 position)
    {
        StartCoroutine(ChainToNearbyEnemies(position, maxChains));
    }

    private IEnumerator ChainToNearbyEnemies(Vector3 startPos, int chainsLeft)
    {
        if (chainsLeft <= 0) yield break;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(startPos, chainRange);
        EnemyBase closestEnemy = null;
        float closestDistance = chainRange;

        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(startPos, col.transform.position);
                if (distance < closestDistance)
                {
                    EnemyBase enemy = col.GetComponent<EnemyBase>();
                    if (enemy != null && !enemy.IsDead())
                    {
                        closestEnemy = enemy;
                        closestDistance = distance;
                    }
                }
            }
        }

        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(chainDamage);
            
            // Visual effect
            DrawLightningArc(startPos, closestEnemy.transform.position);

            yield return new WaitForSeconds(0.1f);
            
            StartCoroutine(ChainToNearbyEnemies(closestEnemy.transform.position, chainsLeft - 1));
        }
    }

    private void DrawLightningArc(Vector3 from, Vector3 to)
    {
        LineRenderer line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.cyan;
        line.endColor = Color.white;

        Destroy(line, 0.1f);
    }
}
