using System.Collections;
using UnityEngine;

/// <summary>
/// Componente root di Placeables_JumpG.
/// Quando il player entra nel trigger del pad, viene lanciato verso l'alto
/// con forza = jumpForce del player * jumpMultiplier.
/// Usa OnTriggerEnter2D (affidabile su tutti i tipi di Rigidbody2D).
/// </summary>
[RequireComponent(typeof(Placeable))]
public class JumpPad : MonoBehaviour
{
    [Tooltip("Moltiplicatore applicato al jumpForce base del PlayerController.")]
    public float jumpMultiplier = 2f;

    [Tooltip("Tempo minimo tra un lancio e il successivo — evita re-trigger immediato.")]
    public float launchCooldown = 0.4f;

    [Tooltip("Transform del figlio visivo su cui applicare l'effetto squash e l'animazione.")]
    public Transform visualTransform;

    [Header("Trampolino Spritesheet")]
    [Tooltip("Texture dello spritesheet (Read/Write abilitato).")]
    public Texture2D trampolinoSheet;
    [Tooltip("Numero di colonne dello spritesheet.")]
    public int trampolinoColumns = 6;
    [Tooltip("Numero di righe dello spritesheet.")]
    public int trampolinoRows    = 1;
    [Tooltip("Frame al secondo dell'animazione di attivazione.")]
    public float trampolinoFps   = 12f;

    private Placeable placeable;
    private float lastLaunchTime = -10f;
    private Sprite[] frames;
    private SpriteRenderer visualSR;
    private Coroutine animCoroutine;
    private bool wasActive = false; // traccia drag+anchored per cambio sprite

    void Start()
    {
        // Cerca il Placeable sul proprio GameObject, poi in su (es. Jump child di Jump2)
        placeable = GetComponent<Placeable>() ?? GetComponentInParent<Placeable>();

        if (visualTransform != null)
            visualSR = visualTransform.GetComponent<SpriteRenderer>();

        BuildFrames();
        SetIdleSprite();
    }

    void BuildFrames()
    {
        if (trampolinoSheet == null) return;
        int total  = trampolinoColumns * trampolinoRows;
        int frameW = trampolinoSheet.width  / trampolinoColumns;
        int frameH = trampolinoSheet.height / trampolinoRows;
        frames = new Sprite[total];
        for (int i = 0; i < total; i++)
        {
            int col = i % trampolinoColumns;
            int row = trampolinoRows - 1 - (i / trampolinoColumns);
            frames[i] = Sprite.Create(
                trampolinoSheet,
                new Rect(col * frameW, row * frameH, frameW, frameH),
                new Vector2(0.5f, 0.5f),
                frameW
            );
        }
    }

    void Update()
    {
        if (frames == null || visualSR == null || animCoroutine != null || placeable == null) return;
        // Mostra primo frame quando trascinato o ancorato, ultimo frame quando idle/floating
        bool active = placeable.IsAnchored || placeable.IsDragging;
        if (active == wasActive) return;
        wasActive = active;
        if (active) SetPlacedSprite();
        else SetIdleSprite();
    }

    void SetIdleSprite()
    {
        if (visualSR == null || frames == null || frames.Length == 0) return;
        visualSR.sprite = frames[frames.Length - 1]; // ultimo frame = stato idle/floating
    }

    void SetPlacedSprite()
    {
        if (visualSR == null || frames == null || frames.Length == 0) return;
        visualSR.sprite = frames[0]; // primo frame = drag o snappato
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (placeable != null && !placeable.IsAnchored) return;
        if (Time.time - lastLaunchTime < launchCooldown) return;

        // Non rilanciare se il player sta già volando verso l'alto
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.linearVelocity.y > 1f) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        pc.ForceJump(pc.jumpForce * jumpMultiplier);
        pc.SuppressJump(launchCooldown);
        lastLaunchTime = Time.time;
        SoundManager.Instance?.PlaySFX(SoundID.TrampolinoActivate);

        if (visualTransform != null)
            StartCoroutine(SquashEffect());

        if (frames != null && frames.Length > 0)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PlaySheetAnim());
        }
    }

    IEnumerator SquashEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float squashY = 1f - Mathf.Sin(t * Mathf.PI) * 0.35f;
            float squashX = 1f + Mathf.Sin(t * Mathf.PI) * 0.20f;
            visualTransform.localScale = new Vector3(squashX, squashY, 1f);
            yield return null;
        }
        visualTransform.localScale = Vector3.one;
    }

    IEnumerator PlaySheetAnim()
    {
        float frameDuration = trampolinoFps > 0f ? 1f / trampolinoFps : 0.083f;

        // Avanti: 0 → N-1
        for (int i = 0; i < frames.Length; i++)
        {
            if (visualSR != null) visualSR.sprite = frames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        // Indietro: N-2 → 0
        for (int i = frames.Length - 2; i >= 0; i--)
        {
            if (visualSR != null) visualSR.sprite = frames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        SetPlacedSprite(); // dopo l'animazione il pad è piazzato → primo frame
        animCoroutine = null;
    }
}
