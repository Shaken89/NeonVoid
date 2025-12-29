using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages different weapon types for the player
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public enum WeaponType
    {
        Standard,    // Basic single shot
        Spread,      // 3-way spread
        Rapid,       // High fire rate
        Laser,       // Continuous beam
        Homing       // Tracking bullets
    }

    [System.Serializable]
    public class WeaponConfig
    {
        public WeaponType type;
        public GameObject bulletPrefab;
        public float fireRate = 0.2f;
        public float bulletSpeed = 15f;
        public int bulletCount = 1;
        public float spreadAngle = 15f;
        public AudioClip shootSound;
    }

    [Header("Weapon Configurations")]
    [SerializeField] private WeaponConfig[] weaponConfigs;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerController playerController;

    private WeaponType currentWeaponType = WeaponType.Standard;
    private WeaponConfig currentWeapon;
    private float lastFireTime;

    // Laser weapon
    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float laserMaxDistance = 50f;
    [SerializeField] private int laserDamagePerSecond = 10;
    [SerializeField] private LayerMask laserTargets;
    private bool isFiringLaser;

    private void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        SetWeapon(WeaponType.Standard);

        if (laserLine != null)
            laserLine.enabled = false;
    }

    private void Update()
    {
        // Handle laser weapon separately
        if (currentWeaponType == WeaponType.Laser)
        {
            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                FireLaser();
            else
                StopLaser();
            #else
            if (Input.GetMouseButton(0))
                FireLaser();
            else
                StopLaser();
            #endif
        }
    }

    public void SetWeapon(WeaponType type)
    {
        currentWeaponType = type;
        currentWeapon = GetWeaponConfig(type);

        if (currentWeapon != null)
        {
            Debug.Log($"Switched to {type} weapon");
        }
    }

    private WeaponConfig GetWeaponConfig(WeaponType type)
    {
        foreach (var config in weaponConfigs)
        {
            if (config.type == type)
                return config;
        }
        return null;
    }

    public bool TryShoot(Vector2 direction)
    {
        if (currentWeapon == null) return false;

        // Laser is handled in Update
        if (currentWeaponType == WeaponType.Laser) return true;

        if (Time.time < lastFireTime + currentWeapon.fireRate)
            return false;

        lastFireTime = Time.time;

        switch (currentWeaponType)
        {
            case WeaponType.Standard:
                ShootStandard(direction);
                break;
            case WeaponType.Spread:
                ShootSpread(direction);
                break;
            case WeaponType.Rapid:
                ShootStandard(direction);
                break;
            case WeaponType.Homing:
                ShootHoming(direction);
                break;
        }

        PlayShootSound();
        return true;
    }

    private void ShootStandard(Vector2 direction)
    {
        CreateBullet(direction, 0f);
    }

    private void ShootSpread(Vector2 direction)
    {
        float angleStep = currentWeapon.spreadAngle / (currentWeapon.bulletCount - 1);
        float startAngle = -currentWeapon.spreadAngle / 2f;

        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            CreateBullet(direction, angle);
        }
    }

    private void ShootHoming(Vector2 direction)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * currentWeapon.bulletSpeed;

        // Add homing component
        HomingBullet homing = bullet.AddComponent<HomingBullet>();
        homing.Initialize(10f, 90f); // turnSpeed, detectionRange

        Destroy(bullet, 5f);
    }

    private void CreateBullet(Vector2 direction, float angleOffset)
    {
        if (currentWeapon.bulletPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle += angleOffset;

        Vector2 finalDirection = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = finalDirection * currentWeapon.bulletSpeed;

        Destroy(bullet, 5f);
    }

    private void FireLaser()
    {
        if (laserLine == null) return;

        isFiringLaser = true;
        laserLine.enabled = true;

        Vector2 startPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, laserMaxDistance, laserTargets);

        Vector3 endPos = hit.collider != null ? hit.point : (Vector2)startPos + direction * laserMaxDistance;

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);

        // Deal damage to hit enemy
        if (hit.collider != null)
        {
            EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                int damage = Mathf.CeilToInt(laserDamagePerSecond * Time.deltaTime);
                enemy.TakeDamage(damage);
            }
        }
    }

    private void StopLaser()
    {
        if (laserLine != null && isFiringLaser)
        {
            laserLine.enabled = false;
            isFiringLaser = false;
        }
    }

    private void PlayShootSound()
    {
        if (currentWeapon.shootSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(currentWeapon.shootSound, 0.3f);
    }

    public WeaponType GetCurrentWeaponType() => currentWeaponType;
    public float GetCurrentFireRate() => currentWeapon?.fireRate ?? 0.2f;
}

/// <summary>
/// Makes bullets track towards nearest enemy
/// </summary>
public class HomingBullet : MonoBehaviour
{
    private float turnSpeed;
    private float detectionRange;
    private Transform target;
    private Rigidbody2D rb;

    public void Initialize(float turn, float range)
    {
        turnSpeed = turn;
        detectionRange = range;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        FindTarget();
    }

    private void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            FindTarget();
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        Vector2 currentVelocity = rb.linearVelocity.normalized;

        Vector2 newVelocity = Vector2.Lerp(currentVelocity, direction, turnSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = newVelocity * rb.linearVelocity.magnitude;
    }

    private void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = detectionRange;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                target = enemy.transform;
            }
        }
    }
}
