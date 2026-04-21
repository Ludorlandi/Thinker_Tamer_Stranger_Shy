using System.Collections;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public Gate linkedGate;
    public Transform cameraPosition;
    public float teleportCooldown = 1f;

    [Header("Sprites")]
    [Tooltip("Frames animazione normale del gate.")]
    public Sprite[] idleSprites;
    [Tooltip("Frames animazione all'attivazione del gate.")]
    public Sprite[] activatedSprites;

    private bool onCooldown = false;
    private FloorTileAnimator animator;
    private SpriteRenderer sr;

    void Awake()
    {
        animator = GetComponent<FloorTileAnimator>();
        sr = GetComponent<SpriteRenderer>();
        ApplySprites(idleSprites);
    }

    void ApplySprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;
        if (animator != null)
            animator.sprites = sprites;
        else if (sr != null)
            sr.sprite = sprites[0];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (onCooldown) return;
        if (linkedGate == null) return;

        // Attiva cooldown subito per evitare double-trigger
        StartCooldown();
        linkedGate.StartCooldown();

        SoundManager.Instance?.PlaySFX(SoundID.GateTraversal);
        StartCoroutine(TeleportRoutine(other));
    }

    private IEnumerator TeleportRoutine(Collider2D player)
    {
        // Sprite attivazione
        ApplySprites(activatedSprites);

        // Congela il player durante la transizione
        var rb = player.GetComponent<Rigidbody2D>();
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.SetEditMode(true);

        // Glitch out
        if (GlitchTransition.Instance != null)
            yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(0.30f));
        else if (ScreenFade.Instance != null)
            yield return StartCoroutine(ScreenFade.Instance.FadeOut(0.25f));

        // Teletrasporto
        player.transform.position = linkedGate.transform.position + Vector3.up;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Snap camera
        if (cameraPosition != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 newCamPos = cameraPosition.position;
                newCamPos.z = mainCam.transform.position.z;
                mainCam.transform.position = newCamPos;
            }
        }

        // Glitch in
        if (GlitchTransition.Instance != null)
            yield return StartCoroutine(GlitchTransition.Instance.GlitchIn(0.35f));
        else if (ScreenFade.Instance != null)
            yield return StartCoroutine(ScreenFade.Instance.FadeIn(0.25f));

        // Riabilita il player
        if (pc != null) pc.SetEditMode(false);

        // Torna allo sprite idle
        ApplySprites(idleSprites);
    }

    public void StartCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(teleportCooldown);
        onCooldown = false;
    }
}
