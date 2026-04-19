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
    private float coyoteCounter;
    private bool isInEditMode;

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

        HandleMovement();
        CheckGround();
        HandleJump();
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float speed = isGrounded ? moveSpeed : moveSpeed * airControlMultiplier;
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer | placeableLayer
        );

        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteCounter = 0f;
            SoundManager.Instance?.PlaySFX(SoundID.PlayerJump);
        }
    }

    public bool IsGrounded => isGrounded;

    // Chiamato da sistemi esterni (JumpPad, OrbJump) per imporre un salto con forza specifica.
    public void ForceJump(float force)
    {
        if (isInEditMode) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        SoundManager.Instance?.PlaySFX(SoundID.PlayerJump);
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
