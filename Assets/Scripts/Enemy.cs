using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public float stopDistance = 3f;
    public float retreatDistance = 1.5f;
    public float avoidRadius = 1.2f;
    public float moveSmooth = 5f; // плавность движения
    public float rotateSmooth = 6f; // плавность разворота

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 6f;
    public float shootInterval = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private float shootTimer;
    private HUDController hud;
    private Vector2 currentVelocity;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        hud = FindObjectOfType<HUDController>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 toPlayer = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);

        // Поворачиваем врага плавно
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, rotateSmooth * Time.fixedDeltaTime);

        // Избегаем других врагов
        Vector2 avoidance = AvoidOtherEnemies();
        Vector2 moveDir = toPlayer;

        if (distance > stopDistance)
            moveDir += avoidance;
        else if (distance < retreatDistance)
            moveDir = -toPlayer + avoidance;
        else
        {
            moveDir = avoidance; // на месте, только избегание
            shootTimer -= Time.fixedDeltaTime;
            if (shootTimer <= 0f)
            {
                ShootAtPlayer();
                shootTimer = shootInterval;
            }
        }

        // Плавное движение
        Vector2 targetVelocity = moveDir.normalized * speed;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, moveSmooth * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;
    }

    Vector2 AvoidOtherEnemies()
    {
        Vector2 avoidance = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, avoidRadius);

        foreach (Collider2D col in nearby)
        {
            if (col.CompareTag("Enemy") && col.gameObject != gameObject)
            {
                Vector2 away = (Vector2)(transform.position - col.transform.position);
                float distance = away.magnitude;
                if (distance > 0)
                    avoidance += away.normalized / distance;
            }
        }

        return avoidance * 0.5f;
    }

    void ShootAtPlayer()
    {
        if (player == null || bulletPrefab == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)(direction * 0.5f);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.position = new Vector3(bullet.transform.position.x, bullet.transform.position.y, 0f);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
            bulletRb.freezeRotation = true;
            bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        bullet.tag = "EnemyBullet";
        Destroy(bullet, 3f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Destroy(collision.gameObject);
            DestroyEnemy();
        }
    }

    private void DestroyEnemy()
    {
        if (hud != null)
            hud.UpdateScore(1);

        Destroy(gameObject);
    }
}
