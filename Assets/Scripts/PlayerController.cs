using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    [Range(0f, 1f)]
    [Tooltip("Moltiplicatore velocità orizzontale in aria (0 = niente controllo, 1 = stesso del suolo)")]
    public float airControlMultiplier = 0.4f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    public LayerMask placeableLayer;

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteCounter;
    private bool hasJumped;
    private bool isInEditMode;

    // ── Debug ───────────────────────────────────────────────────────
    private bool debugInfiniteJump = false;
    private float _suppressJumpUntil = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isInEditMode) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (Input.GetKeyDown(KeyCode.K))
            debugInfiniteJump = !debugInfiniteJump;

        HandleMovement();
        CheckGround();
        HandleJump();
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float speed = isGrounded ? moveSpeed : moveSpeed * airControlMultiplier;
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

        if (horizontalInput != 0f && isGrounded)
            SoundManager.Instance?.PlayMovementSFX();
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer | placeableLayer
        );

        // Atterraggio: reset del flag di salto
        if (isGrounded && !wasGrounded)
            hasJumped = false;

        if (isGrounded && !hasJumped)
            coyoteCounter = coyoteTime;
        else if (!isGrounded)
            coyoteCounter -= Time.deltaTime;

        wasGrounded = isGrounded;
    }

    void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (Time.time < _suppressJumpUntil) return;

        bool canJump = debugInfiniteJump || (coyoteCounter > 0f && !hasJumped);
        if (!canJump) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteCounter = 0f;
        if (!debugInfiniteJump) hasJumped = true;
        SoundManager.Instance?.PlaySFX(SoundID.PlayerJump);
    }

    void OnGUI()
    {
        if (!debugInfiniteJump) return;
        GUI.color = new Color(1f, 0.85f, 0f);
        GUI.Label(new Rect(10, 10, 300, 30), "[ SALTO UNLOCKED ]");
    }

    public bool IsGrounded => isGrounded;

    // Chiamato da sistemi esterni (JumpPad, OrbJump) per imporre un salto con forza specifica.
    public void ForceJump(float force)
    {
        if (isInEditMode) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        SoundManager.Instance?.PlaySFX(SoundID.PlayerJump);
    }

    /// <summary>
    /// Blocca l'input manuale del salto per <duration> secondi.
    /// Chiamato da JumpPad per impedire che lo Space sovrascriva il lancio.
    /// </summary>
    public void SuppressJump(float duration)
    {
        _suppressJumpUntil = Time.time + duration;
    }

    public void SetEditMode(bool active)
    {
        isInEditMode = active;
        if (active)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
