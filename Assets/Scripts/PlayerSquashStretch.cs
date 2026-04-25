using UnityEngine;

public class PlayerSquashStretch : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform del corpo (PGbody). Riceve rolling, squash/stretch e tilt.")]
    public Transform bodyTransform;

    [Header("Rolling")]
    [Tooltip("Raggio del corpo in unità Unity. Usato per calcolare la rotazione di rolling.")]
    public float bodyRadius = 0.5f;

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

    [Header("Idle Breath")]
    [Tooltip("Ampiezza della deformazione respiratoria (Y). 0.03 = sottile, 0.07 = evidente.")]
    [Range(0f, 0.1f)]
    public float breathAmplitude = 0.035f;

    [Tooltip("Velocità del respiro in cicli al secondo.")]
    [Range(0.2f, 2f)]
    public float breathFrequency = 0.75f;

    private Rigidbody2D rb;
    private PlayerController controller;

    private Vector2 currentScale = Vector2.one;
    private Vector2 targetScale  = Vector2.one;
    private float   currentTilt  = 0f;
    private float   rollAngle    = 0f;

    private bool wasGrounded = false;
    private float jumpTimer  = 0f;

    private enum JumpPhase { None, Squash, Stretch }
    private JumpPhase jumpPhase = JumpPhase.None;

    private float breathPhase  = 0f;
    private float breathFactor = 0f;

    void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        bool isGrounded = controller != null && IsGrounded();
        float vy = rb.linearVelocity.y;
        float vx = rb.linearVelocity.x;

        // --- Rolling (distanza percorsa → angolo) ---
        // Muoversi a destra = rotazione oraria = Z negativo in Unity
        rollAngle -= (vx * Time.deltaTime / bodyRadius) * Mathf.Rad2Deg;

        // --- Gestione fasi di salto ---
        if (!wasGrounded && isGrounded)
        {
            float landIntensity = Mathf.Clamp01(Mathf.Abs(vy) / 15f);
            targetScale = Vector2.Lerp(Vector2.one, squashScale, 0.5f + landIntensity * 0.5f);
            jumpPhase = JumpPhase.None;
            jumpTimer = 0f;
        }
        else if (wasGrounded && !isGrounded && vy > 0f)
        {
            jumpPhase = JumpPhase.Squash;
            jumpTimer = 0f;
        }

        if (jumpPhase == JumpPhase.Squash)
        {
            jumpTimer  += Time.deltaTime;
            targetScale = squashScale;
            if (jumpTimer > 0.06f) { jumpPhase = JumpPhase.Stretch; jumpTimer = 0f; }
        }
        else if (jumpPhase == JumpPhase.Stretch)
        {
            jumpTimer  += Time.deltaTime;
            targetScale = stretchScale;
            if (jumpTimer > 0.12f) jumpPhase = JumpPhase.None;
        }
        else if (!isGrounded && vy < -fallStretchThreshold)
        {
            float t = Mathf.Clamp01((Mathf.Abs(vy) - fallStretchThreshold) / 10f);
            targetScale = Vector2.Lerp(Vector2.one, fallStretchScale, t);
        }
        else if (jumpPhase == JumpPhase.None)
        {
            targetScale = Vector2.one;
        }

        float lerpSpeed = (targetScale != Vector2.one) ? snapSpeed : returnSpeed;
        currentScale = Vector2.Lerp(currentScale, targetScale, Time.deltaTime * lerpSpeed);

        // --- Tilt in corsa ---
        float targetTilt = 0f;
        if (isGrounded && Mathf.Abs(vx) > 0.1f)
            targetTilt = -Mathf.Sign(vx) * runTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // --- Idle breath: attivo solo quando fermo a terra senza fasi di salto ---
        breathPhase += Time.deltaTime * breathFrequency * Mathf.PI * 2f;
        bool isIdle = isGrounded && jumpPhase == JumpPhase.None && Mathf.Abs(vx) < 0.5f;
        breathFactor = Mathf.Lerp(breathFactor, isIdle ? 1f : 0f, Time.deltaTime * 5f);
        float bAmp      = breathAmplitude * breathFactor;
        float sine      = Mathf.Sin(breathPhase);
        float breathX   = 1f - sine * bAmp * 0.5f; // leggera compressione X compensa Y
        float breathY   = 1f + sine * bAmp;

        // --- Applica tutto solo al body ---
        if (bodyTransform != null)
        {
            bodyTransform.localScale    = new Vector3(currentScale.x * breathX, currentScale.y * breathY, 1f);
            bodyTransform.localRotation = Quaternion.Euler(0f, 0f, rollAngle + currentTilt);
        }

        wasGrounded = isGrounded;
    }

    bool IsGrounded()
    {
        var field = typeof(PlayerController).GetField("isGrounded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(controller);
        return false;
    }
}
