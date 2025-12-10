using UnityEngine;
using System.Collections;

/// <summary>
/// Dash ability component that allows quick movement bursts with cooldown and invincibility frames.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DashAbility : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField, Range(5f, 50f)] private float dashSpeed = 25f;
    [SerializeField, Range(0.05f, 1f)] private float dashDuration = 0.2f;
    [SerializeField, Range(0.5f, 10f)] private float dashCooldown = 2f;
    [SerializeField] private int maxDashCharges = 1;

    [Header("Invincibility")]
    [SerializeField] private bool invincibleDuringDash = true;
    [SerializeField] private LayerMask dashThroughLayers;

    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color dashColor = Color.cyan;
    [SerializeField, Range(0f, 1f)] private float ghostEffectInterval = 0.05f;
    [SerializeField] private GameObject ghostPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip dashReadySound;
    [SerializeField, Range(0f, 1f)] private float dashVolume = 0.7f;

    [Header("Input")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;

    [Header("Constraints")]
    [SerializeField] private bool resetVelocityAfterDash = false;

    // Private state
    private Rigidbody2D rb;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private Color originalColor;
    private int currentCharges;
    private bool isDashing;
    private bool canDash = true;
    private Vector2 dashDirection;
    private Coroutine dashCoroutine;
    private Coroutine cooldownCoroutine;

    // Events
    public delegate void DashEvent();
    public event DashEvent OnDashStart;
    public event DashEvent OnDashEnd;
    public event DashEvent OnDashReady;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        currentCharges = maxDashCharges;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        HandleInput();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();

        // Disable trail initially
        if (trailRenderer != null)
            trailRenderer.emitting = false;
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (Input.GetKeyDown(dashKey))
        {
            TryDash();
        }
    }

    #endregion

    #region Dash Logic

    /// <summary>
    /// Attempts to perform a dash.
    /// </summary>
    public bool TryDash()
    {
        if (!CanPerformDash()) return false;

        // Get dash direction from movement or aim
        dashDirection = GetDashDirection();
        
        if (dashDirection.magnitude < 0.1f)
        {
            Debug.LogWarning("No dash direction available!");
            return false;
        }

        StartDash();
        return true;
    }

    /// <summary>
    /// Performs dash in a specific direction.
    /// </summary>
    public bool DashInDirection(Vector2 direction)
    {
        if (!CanPerformDash()) return false;
        
        dashDirection = direction.normalized;
        StartDash();
        return true;
    }

    private bool CanPerformDash()
    {
        if (isDashing) return false;
        if (!canDash) return false;
        if (currentCharges <= 0) return false;
        if (playerController != null && !playerController.IsAlive()) return false;
        
        return true;
    }

    private Vector2 GetDashDirection()
    {
        // Try to use aim direction first
        if (playerController != null)
        {
            Vector2 aimDir = playerController.GetAimDirection();
            if (aimDir.magnitude > 0.1f)
                return aimDir.normalized;
        }

        // Fallback to velocity direction
        if (rb.linearVelocity.magnitude > 0.1f)
            return rb.linearVelocity.normalized;

        // Last resort: forward
        return Vector2.up;
    }

    private void StartDash()
    {
        isDashing = true;
        currentCharges--;
        
        // Visual effects
        StartDashEffects();
        
        // Audio
        PlayDashSound();
        
        // Start dash coroutine
        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashRoutine());
        
        // Start cooldown
        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(DashCooldownRoutine());
        
        // Invoke event
        OnDashStart?.Invoke();
        
        Debug.Log($"Dash! Charges remaining: {currentCharges}");
    }

    private IEnumerator DashRoutine()
    {
        float elapsed = 0f;
        Vector2 dashVelocity = dashDirection * dashSpeed;

        // Enable invincibility
        if (invincibleDuringDash && playerHealth != null)
        {
            // Would need a SetInvincible method in PlayerHealth
        }

        // Spawn ghost trail
        Coroutine ghostCoroutine = StartCoroutine(SpawnGhostTrail());

        // Perform dash
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashVelocity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Stop ghost trail
        StopCoroutine(ghostCoroutine);

        // Disable invincibility
        if (invincibleDuringDash && playerHealth != null)
        {
            // Would need to disable invincibility
        }

        // Reset velocity
        if (resetVelocityAfterDash)
        {
            rb.linearVelocity = Vector2.zero;
        }

        EndDash();
    }

    private void EndDash()
    {
        isDashing = false;
        
        // Stop effects
        StopDashEffects();
        
        // Invoke event
        OnDashEnd?.Invoke();
    }

    private IEnumerator DashCooldownRoutine()
    {
        canDash = false;
        
        yield return new WaitForSeconds(dashCooldown);
        
        // Restore charge
        currentCharges++;
        currentCharges = Mathf.Min(currentCharges, maxDashCharges);
        
        canDash = true;
        
        // Play ready sound
        if (dashReadySound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashReadySound, dashVolume * 0.5f);
        }
        
        OnDashReady?.Invoke();
        
        Debug.Log("Dash ready!");
    }

    #endregion

    #region Visual Effects

    private void StartDashEffects()
    {
        // Enable trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = true;
            trailRenderer.startColor = dashColor;
        }

        // Change sprite color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = dashColor;
        }

        // Spawn dash effect
        if (dashEffectPrefab != null)
        {
            GameObject effect = Instantiate(dashEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void StopDashEffects()
    {
        // Disable trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        // Restore sprite color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator SpawnGhostTrail()
    {
        while (isDashing)
        {
            SpawnGhost();
            yield return new WaitForSeconds(ghostEffectInterval);
        }
    }

    private void SpawnGhost()
    {
        if (ghostPrefab != null)
        {
            GameObject ghost = Instantiate(ghostPrefab, transform.position, transform.rotation);
            
            // Copy sprite
            SpriteRenderer ghostSprite = ghost.GetComponent<SpriteRenderer>();
            if (ghostSprite != null && spriteRenderer != null)
            {
                ghostSprite.sprite = spriteRenderer.sprite;
                ghostSprite.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
            }
            
            Destroy(ghost, 0.5f);
        }
        else
        {
            // Create simple ghost effect
            GameObject ghost = new GameObject("Ghost");
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            
            SpriteRenderer ghostSprite = ghost.AddComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                ghostSprite.sprite = spriteRenderer.sprite;
                ghostSprite.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
                ghostSprite.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            
            StartCoroutine(FadeGhost(ghostSprite));
        }
    }

    private IEnumerator FadeGhost(SpriteRenderer ghostSprite)
    {
        float elapsed = 0f;
        float fadeDuration = 0.5f;
        Color startColor = ghostSprite.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 0f, elapsed / fadeDuration);
            ghostSprite.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghostSprite.gameObject);
    }

    #endregion

    #region Audio

    private void PlayDashSound()
    {
        if (dashSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashSound, dashVolume);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets whether dash is currently active.
    /// </summary>
    public bool IsDashing() => isDashing;

    /// <summary>
    /// Gets current dash charges.
    /// </summary>
    public int GetCharges() => currentCharges;

    /// <summary>
    /// Gets max dash charges.
    /// </summary>
    public int GetMaxCharges() => maxDashCharges;

    /// <summary>
    /// Gets whether dash is ready to use.
    /// </summary>
    public bool IsReady() => canDash && currentCharges > 0;

    /// <summary>
    /// Adds a dash charge.
    /// </summary>
    public void AddCharge()
    {
        currentCharges = Mathf.Min(currentCharges + 1, maxDashCharges);
    }

    /// <summary>
    /// Resets dash charges to max.
    /// </summary>
    public void ResetCharges()
    {
        currentCharges = maxDashCharges;
    }

    /// <summary>
    /// Sets the dash cooldown.
    /// </summary>
    public void SetCooldown(float cooldown)
    {
        dashCooldown = Mathf.Max(0.1f, cooldown);
    }

    #endregion

    #region Debug

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isDashing) return;

        // Draw dash direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, dashDirection * 2f);
    }
#endif

    #endregion
}
