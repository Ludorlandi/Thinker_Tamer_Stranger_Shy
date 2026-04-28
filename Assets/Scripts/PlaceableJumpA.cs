using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Placeable custom per il "Orb Jump" (stile orb rosa di Geometry Dash).
///
/// - Solo visivo e non fisico: il player ci attraversa, non ci collide fisicamente.
/// - Può essere piazzato ovunque, purché un box 1x1 centrato su di esso
///   non compenetri con oggetti in scena (eccetto il player).
/// - Se il player entra nel trigger e preme Salto mentre è in aria,
///   esegue un salto con forza = jumpForce * jumpForceFraction (default 0.7).
/// - Un solo utilizzo per entrata: il player deve uscire e rientrare per riusarlo.
/// - L'orb rimane in scena (non si consuma).
/// - Animazione: bob idle quando non piazzato, pulse lento quando piazzato.
///
/// NOTA: L'input mouse è gestito manualmente con OverlapPoint in Update()
/// per evitare che il collider del player blocchi gli eventi OnMouse* di Unity.
/// </summary>
public class PlaceableJumpA : MonoBehaviour
{
    // ── Unlock ────────────────────────────────────────────────────
    [Header("Unlock")]
    [Tooltip("Il tipo di questo Placeable. Deve corrispondere al PlaceableUnlockItem nella scena.")]
    public PlaceableTypeSO placeableType;

    [Tooltip("Tinta applicata quando il Placeable è bloccato.")]
    public Color lockedTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    // ── References ───────────────────────────────────────────────
    [Header("References")]
    public GameObject player;

    // ── Sprites (modalità Orb) ────────────────────────────────────
    [Header("Sprites (modalità Orb — lasciare vuoti se si usa il Trampolino)")]
    [Tooltip("Frames animazione normale dell'orb.")]
    public Sprite[] saltoIdleSprites;
    [Tooltip("Frames animazione all'attivazione dell'orb.")]
    public Sprite[] saltoActivatedSprites;

    // ── Trampolino Spritesheet (modalità Trampolino) ──────────────
    [Header("Trampolino Spritesheet (lasciare vuoto se si usa l'Orb)")]
    [Tooltip("Texture dello spritesheet del trampolino (Read/Write abilitato). Se assegnata, attiva la modalità trampolino.")]
    public Texture2D trampolinoSheet;
    [Tooltip("Numero di colonne dello spritesheet.")]
    public int trampolinoColumns = 6;
    [Tooltip("Numero di righe dello spritesheet.")]
    public int trampolinoRows    = 1;
    [Tooltip("Frame al secondo dell'animazione di attivazione.")]
    public float trampolinoFps  = 12f;

    // ── Placement ────────────────────────────────────────────────
    [Header("Placement")]
    [Tooltip("Dimensione del box usato per il check compenetrazione (1x1 = un blocco).")]
    public Vector2 overlapCheckSize = Vector2.one;

    [Tooltip("Layer da controllare per la compenetrazione. Default: tutto tranne Player.")]
    public LayerMask overlapCheckMask = ~0 & ~(1 << 9); // tutto tranne Player[9]

    public float snapSpeed = 15f;

    // ── Orb ──────────────────────────────────────────────────────
    [Header("Orb")]
    [Tooltip("Frazione del jumpForce del player applicata dall'orb. 0.7 = 70% del salto base.")]
    [Range(0f, 2f)]
    public float jumpForceFraction = 0.7f;

    // ── Hint Label ───────────────────────────────────────────────
    [Header("Hint Label")]
    [Tooltip("Font da usare per la scritta che appare sopra al player.")]
    public TMP_FontAsset hintFont;
    [Tooltip("Dimensione della scritta in unità Unity.")]
    [Range(0.1f, 1f)]
    public float hintFontSize = 0.28f;
    [Tooltip("Offset rispetto al centro del player.")]
    public Vector2 hintOffset = new Vector2(0f, 1.1f);

    // ── Idle Bob ─────────────────────────────────────────────────
    [Header("Idle Bob (solo quando non piazzato)")]
    [Range(0f, 0.3f)]
    public float bobAltezza = 0.08f;
    [Range(0.5f, 5f)]
    public float bobVelocita = 1.8f;
    [Range(0f, 15f)]
    public float bobInclinazione = 3f;

    // ── Hover ────────────────────────────────────────────────────
    [Header("Hover Effect")]
    [Range(1f, 1.5f)]
    public float hoverScale = 1.15f;
    [Range(1f, 20f)]
    public float hoverScaleSpeed = 10f;
    [Range(1f, 4f)]
    public float hoverBobMultiplier = 2f;

    // ── Pulse (quando piazzato) ───────────────────────────────────
    [Header("Pulse (quando piazzato)")]
    [Tooltip("Ampiezza della pulsazione di scala quando l'orb è piazzato.")]
    [Range(0f, 0.2f)]
    public float pulseAmplitude = 0.07f;
    [Tooltip("Frequenza della pulsazione in Hz.")]
    [Range(0.5f, 5f)]
    public float pulseFrequency = 1.8f;

    // ── Float (quando piazzato) ───────────────────────────────────
    [Header("Float (quando piazzato)")]
    [Tooltip("Altezza della fluttuazione verticale quando piazzato.")]
    [Range(0f, 0.3f)]
    public float floatAltezza = 0.06f;
    [Tooltip("Velocità della fluttuazione quando piazzato.")]
    [Range(0.5f, 5f)]
    public float floatVelocita = 1.2f;

    // ── Stato interno ────────────────────────────────────────────
    private bool isUnlocked = false;
    private bool isDragging = false;
    private bool isPlaced = false;

    private Vector3 startPosition;
    private Vector3 originalPosition; // posizione spawn, mai modificata
    private Vector3 dragOffset;
    private Camera cam;
    private SpriteRenderer[] spriteRenderers;
    private Collider2D col;

    private float bobPhase = 0f;
    private bool isHovered = false;
    private float currentScaleFactor = 1f;
    private float pulsePhase = 0f;
    private float floatPhase = 0f;
    private SpriteRenderer mainSpriteRenderer;
    private FloorTileAnimator tileAnimator;
    private Vector3 baseScale;

    // Trampolino animation state
    private Sprite[]  trampolinoFrames;
    private Coroutine trampolinoAnim;

    // Orb state
    private bool playerInOrb = false;
    private bool orbUsed = false;
    private PlayerController playerController;

    // Hint label
    private TMP_Text hintLabel;

    // ── Unity ────────────────────────────────────────────────────

    void Start()
    {
        cam = Camera.main;
        col = GetComponent<Collider2D>();
        startPosition = transform.position;
        originalPosition = transform.position;
        baseScale = transform.localScale;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        mainSpriteRenderer = GetComponent<SpriteRenderer>();
        if (mainSpriteRenderer == null && spriteRenderers.Length > 0)
            mainSpriteRenderer = spriteRenderers[0];
        tileAnimator = GetComponent<FloorTileAnimator>();
        if (trampolinoSheet != null)
        {
            BuildTrampolinoFrames();
            SetIdleSprite();
        }
        else
        {
            ApplySprites(saltoIdleSprites);
        }

        if (player != null)
            playerController = player.GetComponent<PlayerController>();

        if (PlaceableUnlockManager.Instance != null && PlaceableUnlockManager.Instance.IsUnlocked(placeableType))
            SetUnlocked();
        else
            ApplyLockedVisual();

        CreateHintLabel();
    }

    void CreateHintLabel()
    {
        var go = new GameObject("HintLabel_Jump");
        go.transform.SetParent(transform);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "Jump";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = hintFontSize;
        tmp.color = Color.white;
        tmp.sortingOrder = 200;
        if (hintFont != null) tmp.font = hintFont;
        tmp.gameObject.SetActive(false);
        hintLabel = tmp;
    }

    void BuildTrampolinoFrames()
    {
        if (trampolinoSheet == null) return;
        int total  = trampolinoColumns * trampolinoRows;
        int frameW = trampolinoSheet.width  / trampolinoColumns;
        int frameH = trampolinoSheet.height / trampolinoRows;
        trampolinoFrames = new Sprite[total];
        for (int i = 0; i < total; i++)
        {
            int col = i % trampolinoColumns;
            int row = trampolinoRows - 1 - (i / trampolinoColumns);
            trampolinoFrames[i] = Sprite.Create(
                trampolinoSheet,
                new Rect(col * frameW, row * frameH, frameW, frameH),
                new Vector2(0.5f, 0.5f),
                frameW
            );
        }
    }

    void ApplySprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;
        if (tileAnimator != null)
            tileAnimator.sprites = sprites;
        else if (mainSpriteRenderer != null)
            mainSpriteRenderer.sprite = sprites[0];
    }

    void SetIdleSprite()
    {
        if (trampolinoFrames == null || trampolinoFrames.Length == 0) return;
        if (mainSpriteRenderer != null)
            mainSpriteRenderer.sprite = trampolinoFrames[trampolinoFrames.Length - 1];
    }

    void SetPlacedSprite()
    {
        if (trampolinoFrames == null || trampolinoFrames.Length == 0) return;
        if (mainSpriteRenderer != null)
            mainSpriteRenderer.sprite = trampolinoFrames[0];
    }

    void Update()
    {
        HandleMouseInput();

        if (isDragging) return;

        UpdateScale();
        UpdateAnimation();
        CheckOrbInput();

        // Aggiorna posizione label sopra al player
        if (hintLabel != null && hintLabel.gameObject.activeSelf && player != null)
            hintLabel.transform.position = (Vector2)player.transform.position + hintOffset;
    }

    // ── Input Mouse (manuale, bypassa il raycast di Unity) ───────

    void HandleMouseInput()
    {
        Vector3 mouseWorld = GetMouseWorldPos();
        bool mouseOver = col != null && col.OverlapPoint(mouseWorld);

        // Hover (solo quando sbloccato e non piazzato)
        if (!isDragging)
            isHovered = isUnlocked && !isPlaced && mouseOver;

        // Click sinistro: inizia drag
        if (mouseOver && Input.GetMouseButtonDown(0) && !isDragging)
        {
            if (!isUnlocked)
            {
                SoundManager.Instance?.PlaySFX(SoundID.PlaceableLockedClick);
            }
            else
            {
                if (isPlaced)
                    isPlaced = false;

                isDragging = true;
                transform.rotation = Quaternion.identity;
                dragOffset = transform.position - mouseWorld;
                if (trampolinoSheet != null) SetPlacedSprite();
                SoundManager.Instance?.PlaySFX(SoundID.PlaceableDragStart);

                if (player != null)
                    player.SetActive(false);
            }
        }

        // Mentre trascino
        if (isDragging)
        {
            if (Input.GetMouseButton(0))
            {
                transform.position = GetMouseWorldPos() + dragOffset;
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        // Click destro su orb piazzato: riporta alla posizione originale
        if (mouseOver && Input.GetMouseButtonDown(1) && isPlaced && !Input.GetMouseButtonUp(0))
        {
            isPlaced = false;
            startPosition = originalPosition; // ripristina per l'animazione bob
            transform.position = originalPosition;
            transform.localScale = baseScale;
            transform.rotation = Quaternion.identity;
            currentScaleFactor = 1f;
            if (trampolinoSheet != null) SetIdleSprite();
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableReturn);

            if (player != null)
            {
                player.SetActive(true);
                player.transform.position = Checkpoint.GetActivePosition();
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void EndDrag()
    {
        isDragging = false;

        Physics2D.SyncTransforms();

        if (IsOverlapping())
        {
            transform.position = startPosition;
            if (trampolinoSheet != null) SetIdleSprite();
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableFailedSnap);
        }
        else
        {
            startPosition = transform.position;
            isPlaced = true;
            currentScaleFactor = 1f;
            pulsePhase = 0f;
            if (trampolinoSheet != null) SetPlacedSprite();
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableAnchored);
        }

        if (player != null)
        {
            player.SetActive(true);
            player.transform.position = Checkpoint.GetActivePosition();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    // ── Scale / Hover ─────────────────────────────────────────────

    void UpdateScale()
    {
        if (isPlaced && isUnlocked)
        {
            // Pulse lento quando piazzato
            pulsePhase += Time.deltaTime * pulseFrequency * Mathf.PI * 2f;
            float pulse = 1f + Mathf.Sin(pulsePhase) * pulseAmplitude;
            transform.localScale = baseScale * pulse;
        }
        else
        {
            // Hover scale quando non piazzato
            bool canHover = isUnlocked && !isPlaced;
            float targetScale = (canHover && isHovered) ? hoverScale : 1f;
            currentScaleFactor = Mathf.Lerp(currentScaleFactor, targetScale, Time.deltaTime * hoverScaleSpeed);
            transform.localScale = baseScale * currentScaleFactor;
        }
    }

    // ── Animazione ────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (isUnlocked && !isPlaced)
        {
            // Bob idle (solo quando sbloccato e non piazzato)
            float mult = isHovered ? hoverBobMultiplier : 1f;
            bobPhase += Time.deltaTime * bobVelocita * Mathf.PI * 2f;
            float sine = Mathf.Sin(bobPhase);
            float angle = sine * bobInclinazione * mult;
            transform.position = startPosition + new Vector3(0f, sine * bobAltezza * mult, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (isPlaced)
        {
            // Float quando piazzato
            floatPhase += Time.deltaTime * floatVelocita * Mathf.PI * 2f;
            transform.position = startPosition + new Vector3(0f, Mathf.Sin(floatPhase) * floatAltezza, 0f);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }

    // ── Orb Input ─────────────────────────────────────────────────

    void CheckOrbInput()
    {
        if (!isPlaced || !playerInOrb || orbUsed) return;
        if (playerController == null) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        // L'orb agisce solo se il player è in aria
        if (playerController.IsGrounded) return;

        UseOrb();
    }

    void UseOrb()
    {
        float force = playerController.jumpForce * jumpForceFraction;
        playerController.ForceJump(force);
        orbUsed = true;
        SoundManager.Instance?.PlaySFX(SoundID.OrbActivate);
        StartCoroutine(OrbActivateEffect());
    }

    // ── Trigger ───────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPlaced) return;
        if (!other.CompareTag("Player")) return;
        playerInOrb = true;
        orbUsed = false;

        if (hintLabel != null) hintLabel.gameObject.SetActive(true);

        // Modalità trampolino: anima e suona al tocco
        if (trampolinoSheet != null)
        {
            SoundManager.Instance?.PlaySFX(SoundID.TrampolinoActivate);
            if (trampolinoAnim != null) StopCoroutine(trampolinoAnim);
            trampolinoAnim = StartCoroutine(PlayTrampolinoAnim());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInOrb = false;
        orbUsed = false;
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
    }

    // ── Overlap Check ─────────────────────────────────────────────

    bool IsOverlapping()
    {
        // Usa tutti i layer tranne Player — la mask configurata nel prefab
        // potrebbe non includere tutti i layer solidi della scena.
        int mask = ~(1 << LayerMask.NameToLayer("Player"));

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            (Vector2)transform.position,
            overlapCheckSize * 0.9f,
            0f,
            mask
        );

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            if (hit.GetComponentInChildren<LockBlock>() != null) continue;
            if (hit.transform.IsChildOf(transform) || hit.transform == transform) continue;
            return true;
        }
        return false;
    }

    // ── Unlock ────────────────────────────────────────────────────

    public void SetUnlocked()
    {
        isUnlocked = true;
        if (spriteRenderers == null)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
            sr.color = Color.white;
    }

    void ApplyLockedVisual()
    {
        foreach (var sr in spriteRenderers)
            sr.color = lockedTint;
    }

    // ── Effetti ───────────────────────────────────────────────────

    IEnumerator OrbActivateEffect()
    {
        ApplySprites(saltoActivatedSprites);

        float duration = 0.25f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / duration;
            float burst = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            transform.localScale = baseScale * burst;
            yield return null;
        }

        ApplySprites(saltoIdleSprites);
    }

    IEnumerator PlayTrampolinoAnim()
    {
        if (trampolinoFrames == null || trampolinoFrames.Length == 0) yield break;

        float frameDuration = trampolinoFps > 0f ? 1f / trampolinoFps : 0.083f;
        foreach (var frame in trampolinoFrames)
        {
            if (mainSpriteRenderer != null)
                mainSpriteRenderer.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }

        SetPlacedSprite();
        trampolinoAnim = null;
    }

    // ── Utility ───────────────────────────────────────────────────

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(mousePos);
    }
}
