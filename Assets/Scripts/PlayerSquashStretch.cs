using UnityEngine;

public class PlayerSquashStretch : MonoBehaviour
{
    [Header("Squash & Stretch")]
    [Tooltip("Scala target durante lo schiacciamento (salto/atterraggio)")]
    public Vector2 squashScale = new Vector2(1.3f, 0.7f);

    [Tooltip("Scala target durante l'allungamento (salto verso l'alto)")]
    public Vector2 stretchScale = new Vector2(0.75f, 1.3f);

    [Tooltip("Stretch massimo durante la caduta veloce")]
    public Vector2 fallStretchScale = new Vector2(0.85f, 1.2f);

    [Tooltip("Velocità verticale oltre la quale inizia lo stretch in caduta")]
    public float fallStretchThreshold = 4f;

    [Tooltip("Velocità con cui la scala torna a normale (lerp)")]
    public float returnSpeed = 12f;

    [Tooltip("Velocità con cui applica squash/stretch istantaneo")]
    public float snapSpeed = 25f;

    [Header("Tilt in corsa")]
    [Tooltip("Gradi di inclinazione massima mentre si corre")]
    public float runTiltAngle = 5f;

    [Tooltip("Velocità con cui l'inclinazione segue il movimento")]
    public float tiltSpeed = 10f;

    private Rigidbody2D rb;
    private PlayerController controller;

    private Vector2 currentScale = Vector2.one;
    private Vector2 targetScale = Vector2.one;
    private float currentTilt = 0f;

    private bool wasGrounded = false;
    private bool jumpSquashDone = false;
    private float jumpTimer = 0f;

    // Fasi del salto
    private enum JumpPhase { None, Squash, Stretch }
    private JumpPhase jumpPhase = JumpPhase.None;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        bool isGrounded = controller != null && IsGrounded();
        float vy = rb.linearVelocity.y;
        float vx = rb.linearVelocity.x;

        // --- Gestione fasi di salto ---
        if (!wasGrounded && isGrounded)
        {
            // Atterraggio
            float landIntensity = Mathf.Clamp01(Mathf.Abs(vy) / 15f);
            targetScale = Vector2.Lerp(Vector2.one, squashScale, 0.5f + landIntensity * 0.5f);
            jumpPhase = JumpPhase.None;
            jumpTimer = 0f;
        }
        else if (wasGrounded && !isGrounded && vy > 0f)
        {
            // Inizio salto: prima schiaccia, poi allunga
            jumpPhase = JumpPhase.Squash;
            jumpTimer = 0f;
        }

        // Timer per transizione squash → stretch durante il salto
        if (jumpPhase == JumpPhase.Squash)
        {
            jumpTimer += Time.deltaTime;
            targetScale = squashScale;
            if (jumpTimer > 0.06f)
            {
                jumpPhase = JumpPhase.Stretch;
                jumpTimer = 0f;
            }
        }
        else if (jumpPhase == JumpPhase.Stretch)
        {
            jumpTimer += Time.deltaTime;
            targetScale = stretchScale;
            if (jumpTimer > 0.12f)
                jumpPhase = JumpPhase.None;
        }
        else if (!isGrounded && vy < -fallStretchThreshold)
        {
            // Caduta veloce: stretch proporzionale alla velocità
            float t = Mathf.Clamp01((Mathf.Abs(vy) - fallStretchThreshold) / 10f);
            targetScale = Vector2.Lerp(Vector2.one, fallStretchScale, t);
        }
        else if (jumpPhase == JumpPhase.None)
        {
            // Torna a normale
            targetScale = Vector2.one;
        }

        // Lerp della scala verso il target
        float lerpSpeed = (targetScale != Vector2.one) ? snapSpeed : returnSpeed;
        currentScale = Vector2.Lerp(currentScale, targetScale, Time.deltaTime * lerpSpeed);

        // --- Tilt in corsa ---
        float targetTilt = 0f;
        if (isGrounded && Mathf.Abs(vx) > 0.1f)
            targetTilt = -Mathf.Sign(vx) * runTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // Applica scala e rotazione al transform
        transform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);

        wasGrounded = isGrounded;
    }

    bool IsGrounded()
    {
        // Legge lo stato di grounded tramite reflection sul PlayerController
        // (coyoteCounter > 0 significa a terra o appena lasciato il suolo)
        var field = typeof(PlayerController).GetField("isGrounded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(controller);
        return false;
    }
}
