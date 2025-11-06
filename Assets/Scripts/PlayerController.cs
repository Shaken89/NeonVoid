using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float acceleration = 8f; // чем больше — быстрее разгон
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;

    private InputAction moveAction;
    private InputAction fireAction;
    private Rigidbody2D rb;
    private bool isAlive = true;

    public GameObject muzzleFlashPrefab;
    public Transform firePoint;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var gameplayMap = new InputActionMap("Gameplay");

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

        fireAction = gameplayMap.AddAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Keyboard>/space");
        fireAction.AddBinding("<Mouse>/leftButton");

        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;
        fireAction.performed += ctx => Shoot();

        gameplayMap.Enable();
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        // Плавное ускорение и замедление
        Vector2 targetVelocity = moveInput * moveSpeed;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;

        // Плавный разворот к курсору
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;
        Vector2 lookDir = (mousePos - transform.position).normalized;
        float targetAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, 12f * Time.fixedDeltaTime);
    }

    private void Shoot()
    {
        if (!isAlive || bulletPrefab == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0f;
        if (muzzleFlashPrefab && firePoint)
            Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        Vector2 direction = (mousePos - transform.position).normalized;
        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.6f);
        

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
            bulletRb.linearVelocity = direction * bulletSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        Destroy(bullet, 3f);
    }

    public void Kill()
    {
        isAlive = false;
        moveAction.Disable();
        fireAction.Disable();
        rb.linearVelocity = Vector2.zero;
    }
}
