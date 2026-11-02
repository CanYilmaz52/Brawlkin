using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Tuşlar (Inspector'dan seç)")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dashKey = KeyCode.LeftShift;

    [Header("Yürüme")]
    public float moveSpeed = 6f;

    [Header("Zıplama")]
    public float jumpForce = 12f;
    [Tooltip("2 yaparsan çift zıplama olur")]
    public int maxJumps = 1;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.6f;

    [Header("Zemin Kontrolü")]
    [Tooltip("Karakterin ayaklarının altına koyduğun boş child obje")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Fırlatma scripti bu değeri okuyabilir: 1 = sağ, -1 = sol
    public int FacingDirection { get; private set; } = 1;
    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }

    Rigidbody2D rb;
    float moveInput;
    bool jumpRequested;
    int jumpsLeft;
    float dashTimer;
    float dashCooldownTimer;
    int dashDir;
    float defaultGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
        jumpsLeft = maxJumps;
    }

    void Update()
    {
        // Yatay input
        moveInput = 0f;
        if (Input.GetKey(leftKey)) moveInput -= 1f;
        if (Input.GetKey(rightKey)) moveInput += 1f;

        // Yön çevirme (dash sırasında çevirme)
        if (!IsDashing && moveInput != 0f)
        {
            FacingDirection = moveInput > 0 ? 1 : -1;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * FacingDirection;
            transform.localScale = s;
        }

        // Zıplama isteği
        if (Input.GetKeyDown(jumpKey) && jumpsLeft > 0 && !IsDashing)
            jumpRequested = true;

        // Dash isteği
        dashCooldownTimer -= Time.deltaTime;
        if (Input.GetKeyDown(dashKey) && !IsDashing && dashCooldownTimer <= 0f)
            StartDash();

        // Dash süresi
        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
        }
    }

    void FixedUpdate()
    {
        CheckGround();

        if (IsDashing)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsLeft--;
            jumpRequested = false;
        }
    }

    void CheckGround()
    {
        if (groundCheck == null) return;

        bool wasGrounded = IsGrounded;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Yukarı çıkarken zıplama hakkını sıfırlama
        if (IsGrounded && rb.linearVelocity.y <= 0.1f)
            jumpsLeft = maxJumps;
    }

    void StartDash()
    {
        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDir = moveInput != 0f ? (moveInput > 0 ? 1 : -1) : FacingDirection;
        rb.gravityScale = 0f;
    }

    void EndDash()
    {
        IsDashing = false;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = new Vector2(0f, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}