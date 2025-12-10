using UnityEngine;

/// <summary>
/// Advanced screen wrap system that teleports objects to opposite side when they exit camera bounds.
/// Supports multiple wrap modes, padding, and performance optimization.
/// </summary>
public class ScreenWrap2D : MonoBehaviour
{
    public enum WrapMode
    {
        BothAxes,      // Wrap both X and Y
        HorizontalOnly, // Only wrap X axis
        VerticalOnly,   // Only wrap Y axis
        Disabled        // No wrapping
    }

    [Header("Wrap Configuration")]
    [SerializeField] private WrapMode wrapMode = WrapMode.BothAxes;
    [SerializeField] private bool useCustomCamera = false;
    [SerializeField] private Camera customCamera;
    
    [Header("Boundary Settings")]
    [SerializeField, Range(-5f, 5f)] private float paddingX = 0f;
    [SerializeField, Range(-5f, 5f)] private float paddingY = 0f;
    [SerializeField] private bool accountForObjectSize = true;

    [Header("Performance")]
    [SerializeField] private bool updateInFixedUpdate = false;
    [SerializeField] private bool cacheScreenBounds = true;
    [SerializeField, Range(0.1f, 2f)] private float boundsUpdateInterval = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmosColor = Color.cyan;

    [Header("Events")]
    [SerializeField] private bool invokeWrapEvents = false;

    // Private fields
    private Camera targetCamera;
    private Vector2 screenBounds;
    private float halfWidth;
    private float halfHeight;
    private float objectHalfWidth;
    private float objectHalfHeight;
    private float nextBoundsUpdate;
    private bool isInitialized;

    // Delegates for events
    public delegate void WrapEvent(WrapAxis axis);
    public event WrapEvent OnWrapped;

    public enum WrapAxis
    {
        Horizontal,
        Vertical
    }

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeCamera();
    }

    private void Start()
    {
        InitializeObjectSize();
        UpdateScreenBounds();
        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || updateInFixedUpdate || wrapMode == WrapMode.Disabled) return;
        
        UpdateBoundsIfNeeded();
        CheckAndWrap();
    }

    private void FixedUpdate()
    {
        if (!isInitialized || !updateInFixedUpdate || wrapMode == WrapMode.Disabled) return;
        
        UpdateBoundsIfNeeded();
        CheckAndWrap();
    }

    #endregion

    #region Initialization

    private void InitializeCamera()
    {
        if (useCustomCamera && customCamera != null)
        {
            targetCamera = customCamera;
        }
        else
        {
            targetCamera = Camera.main;
            
            if (targetCamera == null)
            {
                Debug.LogWarning($"ScreenWrap2D on '{name}': No camera found! Disabling wrap.");
                wrapMode = WrapMode.Disabled;
            }
        }
    }

    private void InitializeObjectSize()
    {
        if (!accountForObjectSize) return;

        // Try to get object bounds from renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            objectHalfWidth = bounds.extents.x;
            objectHalfHeight = bounds.extents.y;
        }
        else
        {
            // Fallback to collider
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Bounds bounds = collider.bounds;
                objectHalfWidth = bounds.extents.x;
                objectHalfHeight = bounds.extents.y;
            }
        }
    }

    #endregion

    #region Screen Bounds Management

    private void UpdateScreenBounds()
    {
        if (targetCamera == null) return;

        // Calculate screen bounds in world space
        Vector3 screenBottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, targetCamera.nearClipPlane));
        Vector3 screenTopRight = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, targetCamera.nearClipPlane));

        halfWidth = (screenTopRight.x - screenBottomLeft.x) / 2f + paddingX;
        halfHeight = (screenTopRight.y - screenBottomLeft.y) / 2f + paddingY;

        screenBounds = new Vector2(halfWidth, halfHeight);

        if (cacheScreenBounds)
        {
            nextBoundsUpdate = Time.time + boundsUpdateInterval;
        }
    }

    private void UpdateBoundsIfNeeded()
    {
        if (!cacheScreenBounds)
        {
            UpdateScreenBounds();
        }
        else if (Time.time >= nextBoundsUpdate)
        {
            UpdateScreenBounds();
        }
    }

    #endregion

    #region Wrapping Logic

    private void CheckAndWrap()
    {
        Vector3 pos = transform.position;
        bool wrapped = false;

        // Horizontal wrapping
        if (wrapMode == WrapMode.BothAxes || wrapMode == WrapMode.HorizontalOnly)
        {
            float wrapThreshold = halfWidth + objectHalfWidth;
            
            if (pos.x > wrapThreshold)
            {
                pos.x = -wrapThreshold;
                wrapped = true;
                InvokeWrapEvent(WrapAxis.Horizontal);
            }
            else if (pos.x < -wrapThreshold)
            {
                pos.x = wrapThreshold;
                wrapped = true;
                InvokeWrapEvent(WrapAxis.Horizontal);
            }
        }

        // Vertical wrapping
        if (wrapMode == WrapMode.BothAxes || wrapMode == WrapMode.VerticalOnly)
        {
            float wrapThreshold = halfHeight + objectHalfHeight;
            
            if (pos.y > wrapThreshold)
            {
                pos.y = -wrapThreshold;
                wrapped = true;
                InvokeWrapEvent(WrapAxis.Vertical);
            }
            else if (pos.y < -wrapThreshold)
            {
                pos.y = wrapThreshold;
                wrapped = true;
                InvokeWrapEvent(WrapAxis.Vertical);
            }
        }

        if (wrapped)
        {
            transform.position = pos;
        }
    }

    private void InvokeWrapEvent(WrapAxis axis)
    {
        if (invokeWrapEvents && OnWrapped != null)
        {
            OnWrapped.Invoke(axis);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually updates the screen bounds.
    /// </summary>
    public void ForceUpdateBounds()
    {
        UpdateScreenBounds();
    }

    /// <summary>
    /// Sets the wrap mode at runtime.
    /// </summary>
    public void SetWrapMode(WrapMode mode)
    {
        wrapMode = mode;
    }

    /// <summary>
    /// Gets the current wrap mode.
    /// </summary>
    public WrapMode GetWrapMode() => wrapMode;

    /// <summary>
    /// Enables or disables wrapping.
    /// </summary>
    public void SetWrappingEnabled(bool enabled)
    {
        wrapMode = enabled ? WrapMode.BothAxes : WrapMode.Disabled;
    }

    /// <summary>
    /// Checks if position is within screen bounds.
    /// </summary>
    public bool IsWithinBounds(Vector2 position)
    {
        return Mathf.Abs(position.x) <= halfWidth && Mathf.Abs(position.y) <= halfHeight;
    }

    /// <summary>
    /// Gets the screen bounds (half-width, half-height).
    /// </summary>
    public Vector2 GetScreenBounds() => screenBounds;

    /// <summary>
    /// Sets custom padding for wrap boundaries.
    /// </summary>
    public void SetPadding(float x, float y)
    {
        paddingX = x;
        paddingY = y;
        ForceUpdateBounds();
    }

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos || targetCamera == null) return;

        Gizmos.color = gizmosColor;

        // Draw screen bounds
        Vector3 center = targetCamera.transform.position;
        center.z = 0f;

        float totalWidth = (halfWidth + objectHalfWidth) * 2f;
        float totalHeight = (halfHeight + objectHalfHeight) * 2f;

        // Draw rectangle
        Vector3 topLeft = center + new Vector3(-totalWidth / 2f, totalHeight / 2f, 0f);
        Vector3 topRight = center + new Vector3(totalWidth / 2f, totalHeight / 2f, 0f);
        Vector3 bottomLeft = center + new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0f);
        Vector3 bottomRight = center + new Vector3(totalWidth / 2f, -totalHeight / 2f, 0f);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Draw axis indicators
        if (wrapMode == WrapMode.HorizontalOnly || wrapMode == WrapMode.BothAxes)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center + Vector3.left * totalWidth / 2f, center + Vector3.right * totalWidth / 2f);
        }

        if (wrapMode == WrapMode.VerticalOnly || wrapMode == WrapMode.BothAxes)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center + Vector3.down * totalHeight / 2f, center + Vector3.up * totalHeight / 2f);
        }
    }

    private void OnValidate()
    {
        // Update bounds when values change in inspector
        if (Application.isPlaying && isInitialized)
        {
            ForceUpdateBounds();
        }
    }
#endif

    #endregion

    #region Static Utilities

    /// <summary>
    /// Wraps a position to screen bounds (static utility).
    /// </summary>
    public static Vector2 WrapPosition(Vector2 position, Vector2 bounds)
    {
        Vector2 wrappedPos = position;

        if (position.x > bounds.x)
            wrappedPos.x = -bounds.x;
        else if (position.x < -bounds.x)
            wrappedPos.x = bounds.x;

        if (position.y > bounds.y)
            wrappedPos.y = -bounds.y;
        else if (position.y < -bounds.y)
            wrappedPos.y = bounds.y;

        return wrappedPos;
    }

    /// <summary>
    /// Calculates screen bounds for a given camera.
    /// </summary>
    public static Vector2 CalculateScreenBounds(Camera camera)
    {
        if (camera == null) return Vector2.zero;

        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.nearClipPlane));
        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.nearClipPlane));

        float halfWidth = (topRight.x - bottomLeft.x) / 2f;
        float halfHeight = (topRight.y - bottomLeft.y) / 2f;

        return new Vector2(halfWidth, halfHeight);
    }

    #endregion
}
