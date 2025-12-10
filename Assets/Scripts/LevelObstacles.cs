using UnityEngine;
using System.Collections;

/// <summary>
/// Destructible obstacles and level objects
/// </summary>
public class LevelObstacle : MonoBehaviour
{
    public enum ObstacleType
    {
        Destructible,   // Can be destroyed
        Bouncy,         // Bounces bullets
        Rotating,       // Rotates over time
        Moving          // Moves in pattern
    }

    [Header("Obstacle Settings")]
    [SerializeField] private ObstacleType obstacleType = ObstacleType.Destructible;
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int scoreValue = 1;

    [Header("Bouncy Settings")]
    [SerializeField] private float bounceForce = 1.5f;

    [Header("Rotating Settings")]
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Moving Settings")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveDistance = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip destroySound;

    [Header("Drop")]
    [SerializeField] private GameObject powerUpPrefab;
    [SerializeField] private float dropChance = 0.3f;

    private int currentHealth;
    private Vector3 startPosition;
    private float moveTimer;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;

        if (obstacleType == ObstacleType.Destructible)
        {
            // Destructible obstacles are static
        }
        else if (obstacleType == ObstacleType.Bouncy)
        {
            // Bouncy obstacles need a collider with specific physics material
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D();
                bouncyMaterial.bounciness = bounceForce;
                col.sharedMaterial = bouncyMaterial;
            }
        }
    }

    private void Update()
    {
        switch (obstacleType)
        {
            case ObstacleType.Rotating:
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                break;

            case ObstacleType.Moving:
                UpdateMovement();
                break;
        }
    }

    private void UpdateMovement()
    {
        moveTimer += Time.deltaTime * moveSpeed;
        float offset = Mathf.PingPong(moveTimer, moveDistance) - (moveDistance / 2f);
        transform.position = startPosition + (Vector3)moveDirection.normalized * offset;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Take damage from bullets
        if (collision.gameObject.CompareTag("Bullet") && obstacleType == ObstacleType.Destructible)
        {
            TakeDamage(1);
            Destroy(collision.gameObject);
        }

        // Bounce bullets
        if (obstacleType == ObstacleType.Bouncy && collision.gameObject.CompareTag("Bullet"))
        {
            Rigidbody2D bulletRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                Vector2 normal = collision.contacts[0].normal;
                bulletRb.linearVelocity = Vector2.Reflect(bulletRb.linearVelocity, normal) * bounceForce;
            }

            if (hitSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hitSound, 0.4f);
        }
    }

    public void TakeDamage(int damage)
    {
        if (obstacleType != ObstacleType.Destructible) return;

        currentHealth -= damage;
        StartCoroutine(DamageFlash());

        if (hitSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hitSound, 0.5f);

        if (currentHealth <= 0)
            Destroy();
    }

    private void Destroy()
    {
        // Update score
        HUDController hud = FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.UpdateScore(scoreValue);

        // Play sound
        if (destroySound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(destroySound, 0.6f);

        // Spawn particles
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.SpawnDeath(transform.position);

        // Drop power-up
        if (powerUpPrefab != null && Random.value < dropChance)
        {
            Instantiate(powerUpPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
}

/// <summary>
/// Environmental hazard that damages player
/// </summary>
public class Hazard : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private AudioClip damageSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null && !health.IsImmune())
            {
                health.TakeDamage(damageAmount);

                if (damageSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(damageSound, 0.5f);
            }
        }
    }
}
