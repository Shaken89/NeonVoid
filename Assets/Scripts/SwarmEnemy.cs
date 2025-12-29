using UnityEngine;
using System.Collections;

/// <summary>
/// Swarm Enemy - small, fast enemies that attack in coordinated groups
/// Слабые по отдельности, но опасны в группе
/// </summary>
public class SwarmEnemy : EnemyBase
{
    [Header("Swarm Settings")]
    [SerializeField] private float groupRadius = 3f;
    [SerializeField] private float separationForce = 2f;
    [SerializeField] private float cohesionForce = 1f;
    [SerializeField] private float alignmentForce = 1f;
    [SerializeField] private int maxNearbySwarm = 10;

    [Header("Pack Bonus")]
    [SerializeField] private float damagePerAlly = 0.2f;
    [SerializeField] private float speedPerAlly = 0.1f;
    [SerializeField] private int maxPackBonus = 5;

    [Header("Dive Attack")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float diveDistance = 10f;
    [SerializeField] private float diveCooldown = 5f;
    [SerializeField] private int diveContactDamage = 1;

    private Vector2 swarmVelocity;
    private int nearbyAlliesCount;
    private float lastDiveTime;
    private bool isDiving;
    private Vector2 diveTarget;

    protected override void Start()
    {
        base.Start();
        maxHealth = 1;
        moveSpeed = 3f;
        scoreValue = 1;
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        UpdateNearbyAlliesCount();
        ApplyPackBonus();

        if (!isDiving && CanDive())
        {
            StartCoroutine(DiveAttack());
        }
        else if (!isDiving)
        {
            SwarmBehavior();
        }
    }

    private void SwarmBehavior()
    {
        Vector2 separation = CalculateSeparation();
        Vector2 cohesion = CalculateCohesion();
        Vector2 alignment = CalculateAlignment();
        Vector2 targetDirection = GetDirectionToPlayer();

        swarmVelocity = (separation * separationForce) + 
                        (cohesion * cohesionForce) + 
                        (alignment * alignmentForce) + 
                        (targetDirection * 2f);

        swarmVelocity = swarmVelocity.normalized;
        rb.linearVelocity = swarmVelocity * GetEffectiveSpeed();
    }

    private Vector2 CalculateSeparation()
    {
        Vector2 separationVector = Vector2.zero;
        int count = 0;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, groupRadius);
        
        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                SwarmEnemy otherSwarm = col.GetComponent<SwarmEnemy>();
                if (otherSwarm != null && !otherSwarm.IsDead())
                {
                    Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
                    float distance = diff.magnitude;
                    
                    if (distance > 0 && distance < 1f)
                    {
                        separationVector += diff / distance;
                        count++;
                    }
                }
            }
        }

        return count > 0 ? separationVector / count : Vector2.zero;
    }

    private Vector2 CalculateCohesion()
    {
        Vector2 centerOfMass = Vector2.zero;
        int count = 0;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, groupRadius);
        
        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                SwarmEnemy otherSwarm = col.GetComponent<SwarmEnemy>();
                if (otherSwarm != null && !otherSwarm.IsDead())
                {
                    centerOfMass += (Vector2)col.transform.position;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            centerOfMass /= count;
            return (centerOfMass - (Vector2)transform.position).normalized;
        }

        return Vector2.zero;
    }

    private Vector2 CalculateAlignment()
    {
        Vector2 averageVelocity = Vector2.zero;
        int count = 0;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, groupRadius);
        
        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                SwarmEnemy otherSwarm = col.GetComponent<SwarmEnemy>();
                if (otherSwarm != null && !otherSwarm.IsDead())
                {
                    Rigidbody2D otherRb = col.GetComponent<Rigidbody2D>();
                    if (otherRb != null)
                    {
                        averageVelocity += otherRb.linearVelocity;
                        count++;
                    }
                }
            }
        }

        return count > 0 ? (averageVelocity / count).normalized : Vector2.zero;
    }

    private Vector2 GetDirectionToPlayer()
    {
        return player != null ? ((Vector2)(player.position - transform.position)).normalized : Vector2.zero;
    }

    private void UpdateNearbyAlliesCount()
    {
        nearbyAlliesCount = 0;
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, groupRadius);
        
        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                SwarmEnemy otherSwarm = col.GetComponent<SwarmEnemy>();
                if (otherSwarm != null && !otherSwarm.IsDead())
                {
                    nearbyAlliesCount++;
                    if (nearbyAlliesCount >= maxNearbySwarm)
                        break;
                }
            }
        }
    }

    private void ApplyPackBonus()
    {
        int bonusCount = Mathf.Min(nearbyAlliesCount, maxPackBonus);
        
        // Visual feedback for pack bonus
        if (spriteRenderer != null)
        {
            float intensity = 1f + (bonusCount * 0.1f);
            spriteRenderer.color = originalColor * intensity;
        }
    }

    private float GetEffectiveSpeed()
    {
        int bonusCount = Mathf.Min(nearbyAlliesCount, maxPackBonus);
        return moveSpeed * (1f + (bonusCount * speedPerAlly));
    }

    private bool CanDive()
    {
        if (player == null) return false;
        
        float distance = GetDistanceToPlayer();
        return distance < diveDistance && 
               distance > 3f && 
               Time.time > lastDiveTime + diveCooldown &&
               nearbyAlliesCount >= 2; // Need pack support
    }

    private IEnumerator DiveAttack()
    {
        isDiving = true;
        lastDiveTime = Time.time;
        diveTarget = player.position;

        // Warning flash
        if (spriteRenderer != null)
        {
            Color originalColorSaved = spriteRenderer.color;
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColorSaved;
        }

        // Dive!
        float diveTime = 0f;
        float diveDuration = 0.5f;
        Vector2 startPos = transform.position;

        while (diveTime < diveDuration)
        {
            diveTime += Time.fixedDeltaTime;
            float t = diveTime / diveDuration;
            
            Vector2 direction = (diveTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * diveSpeed;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity *= 0.5f;
        isDiving = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDiving && collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                int damage = Mathf.CeilToInt(diveContactDamage * (1f + nearbyAlliesCount * damagePerAlly));
                health.TakeDamage(damage);
            }
        }
    }

    protected override void Die()
    {
        // When one dies, others become more aggressive
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, groupRadius * 2f);
        
        foreach (var col in nearbyEnemies)
        {
            SwarmEnemy otherSwarm = col.GetComponent<SwarmEnemy>();
            if (otherSwarm != null && !otherSwarm.IsDead() && otherSwarm != this)
            {
                otherSwarm.OnAllyDeath();
            }
        }

        base.Die();
    }

    public void OnAllyDeath()
    {
        // Boost speed temporarily
        moveSpeed *= 1.2f;
        StartCoroutine(ResetSpeedBoost());
    }

    private IEnumerator ResetSpeedBoost()
    {
        yield return new WaitForSeconds(3f);
        moveSpeed = 3f; // Reset to base
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, groupRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, diveDistance);
    }
}
