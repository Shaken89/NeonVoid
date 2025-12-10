using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Advanced player controller with smooth movement, shooting mechanics, and enhanced input handling.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [System.Serializable]
    public class MovementSettings
    {
        [Range(1f, 30f)] public float moveSpeed = 10f;
        [Range(1f, 20f)] public float acceleration = 8f;
        [Range(1f, 20f)] public float deceleration = 10f;
        [Range(1f, 30f)] public float rotationSpeed = 12f;
        public bool smoothRotation = true;
    }

    [System.Serializable]
    public class ShootingSettings
    {
        public GameObject bulletPrefab;
        [Range(5f, 50f)] public float bulletSpeed = 20f;
        [Range(0.05f, 2f)] public float fireRate = 0.2f;
        [Range(0f, 5f)] public float bulletSpawnDistance = 0.6f;
        [Range(1f, 10f)] public float bulletLifetime = 3f;
        public bool autoFire = false;
        public int maxAmmo = -1; // -1 = infinite
    }

    [System.Serializable]
    public class VisualSettings
    {
        public Transform firePoint;
        public GameObject muzzleFlashPrefab;
        public GameObject deathEffectPrefab;
        public SpriteRenderer spriteRenderer;
        [Range(0f, 1f)] public float muzzleFlashLifetime = 0.1f;
        public bool showTrail = true;
    }

    [System.Serializable]
    public class AudioSettings
    {
        public AudioClip shootSound;
        public AudioClip emptyAmmoSound;
        public AudioClip deathSound;
        [Range(0f, 1f)] public float shootVolume = 0.5f;
    }

    [Header("Movement Configuration")]
    [SerializeField] private MovementSettings movement;

    [Header("Shooting Configuration")]
    [SerializeField] private ShootingSettings shooting;

    [Header("Visual Configuration")]
    [SerializeField] private VisualSettings visuals;

    [Header("Audio Configuration")]
    [SerializeField] private AudioSettings audioSettings;

    [Header("Boundaries")]
    [SerializeField] private bool constrainToBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Input
    private InputAction moveAction;
    private InputAction fireAction;
    private Vector2 moveInput;

    // Physics
    private Rigidbody2D rb;
    private Vector2 currentVelocity;

    // State
    private bool isAlive = true;
    private bool canShoot = true;
    private float nextFireTime;
    private int currentAmmo;

    // Mouse aiming
    private Vector2 lastMousePosition;
    private Vector2 aimDirection;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        SetupInput();
        InitializeAmmo();
    }

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void Update()
    {
        if (!isAlive) return;

        UpdateAiming();
        HandleAutoFire();
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        UpdateMovement();
        UpdateRotation();
        EnforceBoundaries();
    }

    private void OnDestroy()
    {
        CleanupInput();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (visuals.spriteRenderer == null)
            visuals.spriteRenderer = GetComponent<SpriteRenderer>();

        if (visuals.firePoint == null)
            visuals.firePoint = transform;
    }

    private void SetupInput()
    {
        var gameplayMap = new InputActionMap("Gameplay");

        // Movement input
        moveAction = gameplayMap.AddAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Fire input
        fireAction = gameplayMap.AddAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Keyboard>/space");
        fireAction.AddBinding("<Mouse>/leftButton");

        // Bind callbacks
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        fireAction.performed += OnFire;

        gameplayMap.Enable();
    }

    private void InitializeAmmo()
    {
        currentAmmo = shooting.maxAmmo;
    }

    #endregion

    #region Input Handling

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
            TryShoot();
    }

    private void EnableInput()
    {
        moveAction?.Enable();
        fireAction?.Enable();
    }

    private void DisableInput()
    {
        moveAction?.Disable();
        fireAction?.Disable();
    }

    private void CleanupInput()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction.Disable();
            moveAction.Dispose();
        }

        if (fireAction != null)
        {
            fireAction.performed -= OnFire;
            fireAction.Disable();
            fireAction.Dispose();
        }
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        Vector2 targetVelocity = moveInput.normalized * movement.moveSpeed;
        
        // Choose acceleration or deceleration
        float smoothing = moveInput.magnitude > 0.1f ? movement.acceleration : movement.deceleration;
        
        currentVelocity = Vector2.Lerp(
            currentVelocity,
            targetVelocity,
            smoothing * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;
    }

    private void UpdateRotation()
    {
        if (aimDirection.magnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;

        if (movement.smoothRotation)
        {
            float newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, movement.rotationSpeed * Time.fixedDeltaTime);
            rb.SetRotation(newAngle);
        }
        else
        {
            rb.SetRotation(targetAngle);
        }
    }

    private void EnforceBoundaries()
    {
        if (!constrainToBounds) return;

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        rb.position = pos;
    }

    #endregion

    #region Aiming

    private void UpdateAiming()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Only update if mouse moved
        if (mousePos != lastMousePosition)
        {
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
            worldMousePos.z = 0f;
            
            aimDirection = ((Vector2)worldMousePos - rb.position).normalized;
            lastMousePosition = mousePos;
        }
    }

    #endregion

    #region Shooting

    private void HandleAutoFire()
    {
        if (shooting.autoFire && fireAction.IsPressed())
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (!isAlive || !canShoot || shooting.bulletPrefab == null)
            return;

        if (Time.time < nextFireTime)
            return;

        // Check ammo
        if (shooting.maxAmmo >= 0)
        {
            if (currentAmmo <= 0)
            {
                PlayEmptyAmmoSound();
                return;
            }
            currentAmmo--;
        }

        Shoot();
        nextFireTime = Time.time + shooting.fireRate;
    }

    private void Shoot()
    {
        Vector2 direction = aimDirection.magnitude > 0.01f ? aimDirection : Vector2.up;
        Vector3 spawnPos = transform.position + (Vector3)(direction * shooting.bulletSpawnDistance);

        // Spawn bullet
        GameObject bullet = Instantiate(shooting.bulletPrefab, spawnPos, Quaternion.identity);
        
        // Set bullet rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.SetPositionAndRotation(
            new Vector3(spawnPos.x, spawnPos.y, 0f),
            Quaternion.Euler(0f, 0f, angle - 90f)
        );

        // Apply velocity
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * shooting.bulletSpeed;
            bulletRb.gravityScale = 0f;
        }

        // Tag as player bullet
        bullet.tag = "Bullet";

        // Auto-destroy
        Destroy(bullet, shooting.bulletLifetime);

        // Visual and audio feedback
        SpawnMuzzleFlash();
        PlayShootSound();
    }

    private void SpawnMuzzleFlash()
    {
        if (visuals.muzzleFlashPrefab == null || visuals.firePoint == null)
            return;

        GameObject flash = Instantiate(
            visuals.muzzleFlashPrefab,
            visuals.firePoint.position,
            visuals.firePoint.rotation
        );

        Destroy(flash, visuals.muzzleFlashLifetime);
    }

    private void PlayShootSound()
    {
        if (audioSettings.shootSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.shootSound, audioSettings.shootVolume);
        }
    }

    private void PlayEmptyAmmoSound()
    {
        if (audioSettings.emptyAmmoSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.emptyAmmoSound, 0.4f);
        }
    }

    #endregion

    #region Player State

    /// <summary>
    /// Kills the player and disables controls.
    /// </summary>
    public void Kill()
    {
        if (!isAlive) return;

        isAlive = false;
        DisableInput();
        rb.linearVelocity = Vector2.zero;

        // Spawn death effect
        if (visuals.deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(visuals.deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Play death sound
        if (audioSettings.deathSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSettings.deathSound, 0.8f);
        }

        // Hide player sprite
        if (visuals.spriteRenderer != null)
        {
            StartCoroutine(FadeOutSprite());
        }
    }

    private IEnumerator FadeOutSprite()
    {
        if (visuals.spriteRenderer == null) yield break;

        Color color = visuals.spriteRenderer.color;
        float elapsed = 0f;
        float fadeTime = 0.5f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            visuals.spriteRenderer.color = color;
            yield return null;
        }

        color.a = 0f;
        visuals.spriteRenderer.color = color;
    }

    /// <summary>
    /// Revives the player and re-enables controls.
    /// </summary>
    public void Revive()
    {
        isAlive = true;
        EnableInput();
        InitializeAmmo();

        // Restore sprite
        if (visuals.spriteRenderer != null)
        {
            Color color = visuals.spriteRenderer.color;
            color.a = 1f;
            visuals.spriteRenderer.color = color;
        }
    }

    #endregion

    #region Ammo Management

    /// <summary>
    /// Adds ammo to the player.
    /// </summary>
    public void AddAmmo(int amount)
    {
        if (shooting.maxAmmo < 0) return; // Infinite ammo

        currentAmmo += amount;
        currentAmmo = Mathf.Min(currentAmmo, shooting.maxAmmo);
    }

    /// <summary>
    /// Sets the player's ammo to a specific value.
    /// </summary>
    public void SetAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(amount, 0, shooting.maxAmmo);
    }

    /// <summary>
    /// Gets the current ammo count.
    /// </summary>
    public int GetCurrentAmmo() => currentAmmo;

    /// <summary>
    /// Gets the max ammo capacity.
    /// </summary>
    public int GetMaxAmmo() => shooting.maxAmmo;

    #endregion

    #region Public Getters

    public bool IsAlive() => isAlive;
    public Vector2 GetVelocity() => rb.linearVelocity;
    public Vector2 GetAimDirection() => aimDirection;
    public float GetSpeed() => currentVelocity.magnitude;

    /// <summary>
    /// Gets the movement settings for external access.
    /// </summary>
    public MovementSettings GetMovementSettings() => movement;

    /// <summary>
    /// Gets the shooting settings for external access.
    /// </summary>
    public ShootingSettings GetShootingSettings() => shooting;

    /// <summary>
    /// Sets the move speed.
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        movement.moveSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// Sets the bullet speed.
    /// </summary>
    public void SetBulletSpeed(float speed)
    {
        shooting.bulletSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// Gets the current move speed.
    /// </summary>
    public float GetMoveSpeed() => movement.moveSpeed;

    /// <summary>
    /// Gets the current bullet speed.
    /// </summary>
    public float GetBulletSpeed() => shooting.bulletSpeed;

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw movement boundaries
        if (constrainToBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) / 2f,
                (minBounds.y + maxBounds.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                maxBounds.y - minBounds.y,
                0f
            );
            Gizmos.DrawWireCube(center, size);
        }

        // Draw aim direction
        if (Application.isPlaying && isAlive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, aimDirection * 2f);
        }

        // Draw velocity
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.5f);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 150));
        GUILayout.Box("Player Debug Info");
        GUILayout.Label($"Alive: {isAlive}");
        GUILayout.Label($"Speed: {GetSpeed():F2}");
        GUILayout.Label($"Position: {transform.position}");
        GUILayout.Label($"Aim: {aimDirection}");
        if (shooting.maxAmmo >= 0)
            GUILayout.Label($"Ammo: {currentAmmo}/{shooting.maxAmmo}");
        GUILayout.EndArea();
    }
#endif

    #endregion
}
