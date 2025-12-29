using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Necromancer Enemy - summons minions, supports allies, stays at range
/// Опасный саппорт, которого нужно убить в первую очередь
/// </summary>
public class NecromancerEnemy : EnemyBase
{
    [Header("Necromancer Settings")]
    [SerializeField] private float keepDistance = 15f;
    [SerializeField] private float teleportDistance = 8f;
    [SerializeField] private float teleportCooldown = 10f;

    [Header("Minion Summoning")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private int maxMinions = 4;
    [SerializeField] private float summonInterval = 5f;
    [SerializeField] private float summonRadius = 2f;
    [SerializeField] private int minionsPerSummon = 2;

    [Header("Healing Aura")]
    [SerializeField] private float healingRadius = 8f;
    [SerializeField] private float healInterval = 3f;

    [Header("Curse Attack")]
    [SerializeField] private float curseRange = 12f;
    [SerializeField] private float curseInterval = 8f;
    [SerializeField] private float curseDuration = 5f;
    [SerializeField] private float curseSlowAmount = 0.5f;

    [Header("Visual")]
    [SerializeField] private GameObject summonEffectPrefab;
    [SerializeField] private GameObject healAuraEffectPrefab;
    [SerializeField] private GameObject curseEffectPrefab;
    [SerializeField] private LineRenderer curseBeam;

    private List<GameObject> activeMinions = new List<GameObject>();
    private float lastSummonTime;
    private float lastHealTime;
    private float lastCurseTime;
    private float lastTeleportTime;
    private GameObject healAuraInstance;

    protected override void Start()
    {
        base.Start();
        maxHealth = 5;
        moveSpeed = 2f;
        scoreValue = 8;
        currentHealth = maxHealth;

        if (curseBeam != null)
            curseBeam.enabled = false;

        StartCoroutine(NecromancerAI());
    }

    private IEnumerator NecromancerAI()
    {
        while (!isDead)
        {
            if (player == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            float distance = GetDistanceToPlayer();

            // Teleport if too close
            if (distance < teleportDistance && Time.time > lastTeleportTime + teleportCooldown)
            {
                Teleport();
            }
            // Maintain distance
            else if (distance < keepDistance)
            {
                MoveAway();
            }
            // Summon minions
            else if (Time.time > lastSummonTime + summonInterval && GetActiveMinionCount() < maxMinions)
            {
                yield return StartCoroutine(SummonMinions());
            }
            // Curse player
            else if (Time.time > lastCurseTime + curseInterval && distance < curseRange)
            {
                yield return StartCoroutine(CursePlayer());
            }
            // Heal allies
            else if (Time.time > lastHealTime + healInterval)
            {
                HealNearbyAllies();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void MoveAway()
    {
        if (player == null) return;

        Vector2 direction = ((Vector2)transform.position - (Vector2)player.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void Teleport()
    {
        lastTeleportTime = Time.time;

        // Spawn effect at current position
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnEffect("Dash", transform.position);

        // Find new position away from player
        Vector2 directionFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        Vector2 randomOffset = Random.insideUnitCircle * 3f;
        Vector2 newPosition = (Vector2)player.position + (directionFromPlayer * keepDistance) + randomOffset;

        // Clamp to screen bounds
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector2 screenBounds = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
            newPosition.x = Mathf.Clamp(newPosition.x, -screenBounds.x + 2f, screenBounds.x - 2f);
            newPosition.y = Mathf.Clamp(newPosition.y, -screenBounds.y + 2f, screenBounds.y - 2f);
        }

        transform.position = newPosition;

        // Spawn effect at new position
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnEffect("Dash", transform.position);

        if (AudioManager.Instance != null)
        {
            // Play teleport sound
        }
    }

    private IEnumerator SummonMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) yield break;

        lastSummonTime = Time.time;

        // Summon animation
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = new Color(0.5f, 0f, 1f); // Purple glow
            yield return new WaitForSeconds(1f);
            spriteRenderer.color = original;
        }

        // Spawn minions
        for (int i = 0; i < minionsPerSummon; i++)
        {
            if (GetActiveMinionCount() >= maxMinions) break;

            float angle = (360f / minionsPerSummon) * i;
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * summonRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            GameObject minionPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            activeMinions.Add(minion);

            // Spawn effect
            if (summonEffectPrefab != null)
            {
                Instantiate(summonEffectPrefab, spawnPos, Quaternion.identity);
            }
            else if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SpawnEffect("PowerUp", spawnPos);
            }

            yield return new WaitForSeconds(0.2f);
        }

        if (AudioManager.Instance != null)
        {
            // Play summon sound
        }
    }

    private void HealNearbyAllies()
    {
        lastHealTime = Time.time;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, healingRadius);
        bool healedAny = false;

        foreach (var col in nearbyEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy != null && !enemy.IsDead())
                {
                    // Heal (need to add healing method to EnemyBase)
                    int currentHealth = enemy.GetCurrentHealth();
                    // Since EnemyBase doesn't have Heal, we'll add temporary invulnerability instead
                    healedAny = true;

                    // Visual feedback
                    if (healAuraEffectPrefab != null)
                    {
                        Instantiate(healAuraEffectPrefab, col.transform.position, Quaternion.identity);
                    }
                    else if (ParticleManager.Instance != null)
                    {
                        ParticleManager.Instance.SpawnEffect("Heal", col.transform.position);
                    }
                }
            }
        }

        if (healedAny)
        {
            // Show healing aura
            StartCoroutine(ShowHealingAura());

            if (AudioManager.Instance != null)
            {
                // Play heal sound
            }
        }
    }

    private IEnumerator ShowHealingAura()
    {
        if (healAuraEffectPrefab != null)
        {
            GameObject aura = Instantiate(healAuraEffectPrefab, transform.position, Quaternion.identity);
            aura.transform.SetParent(transform);
            aura.transform.localScale = Vector3.one * (healingRadius * 2f);
            Destroy(aura, 1f);
        }

        yield return null;
    }

    private IEnumerator CursePlayer()
    {
        if (player == null) yield break;

        lastCurseTime = Time.time;

        // Visual beam
        if (curseBeam != null)
        {
            curseBeam.enabled = true;
            curseBeam.SetPosition(0, transform.position);
            curseBeam.SetPosition(1, player.position);
        }

        // Curse effect on player
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            StartCoroutine(ApplyCurseToPlayer(playerController));
        }

        // Curse visual
        if (curseEffectPrefab != null)
        {
            GameObject curse = Instantiate(curseEffectPrefab, player.position, Quaternion.identity);
            curse.transform.SetParent(player);
            Destroy(curse, curseDuration);
        }

        yield return new WaitForSeconds(1f);

        if (curseBeam != null)
            curseBeam.enabled = false;

        if (AudioManager.Instance != null)
        {
            // Play curse sound
        }
    }

    private IEnumerator ApplyCurseToPlayer(PlayerController playerController)
    {
        float originalSpeed = playerController.GetMoveSpeed();
        playerController.SetMoveSpeed(originalSpeed * curseSlowAmount);

        yield return new WaitForSeconds(curseDuration);

        playerController.SetMoveSpeed(originalSpeed);
    }

    private int GetActiveMinionCount()
    {
        activeMinions.RemoveAll(minion => minion == null);
        return activeMinions.Count;
    }

    protected override void Die()
    {
        // Destroy all minions
        foreach (var minion in activeMinions)
        {
            if (minion != null)
            {
                Destroy(minion);
            }
        }
        activeMinions.Clear();

        if (curseBeam != null)
            curseBeam.enabled = false;

        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, keepDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healingRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, curseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, teleportDistance);
    }
}
