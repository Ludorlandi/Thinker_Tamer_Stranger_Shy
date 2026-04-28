using UnityEngine;

/// <summary>
/// Rende il GameObject "fantasma": visibile con pulse come la grid, colliders disabilitati.
/// Quando viene chiamato Reveal(), ripristina i colori originali e riabilita i collider.
/// Usato insieme ad AllUnlockedTrigger: invece di SetActive(true) chiama Reveal().
/// </summary>
public class GhostUntilUnlocked : MonoBehaviour
{
    [Header("Ghost Pulse")]
    [Tooltip("Se false, l'oggetto parte completamente invisibile (alpha=0) senza pulse. Usare quando deve essere del tutto nascosto fino allo sblocco.")]
    public bool startVisibleAsGhost = true;

    [Tooltip("Opacità minima del pulse (come minAlpha della grid)")]
    [Range(0f, 1f)] public float pulseMinAlpha = 0.3f;

    [Tooltip("Opacità massima del pulse")]
    [Range(0f, 1f)] public float pulseMaxAlpha = 0.6f;

    [Tooltip("Secondi da min a max (e viceversa) — uguale a GridPlaneController.pulseDuration)")]
    [Min(0.1f)] public float pulseDuration = 3f;

    // ── stato interno ─────────────────────────────────────────────────────────

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders;
    private Color[] originalColors;
    private bool isRevealed = false;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders       = GetComponentsInChildren<Collider2D>(true);

        // Salva i colori originali (con alpha originale, tipicamente 1)
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;

        // Disabilita tutti i collider
        foreach (var col in colliders)
            col.enabled = false;

        // Se non deve essere visibile come ghost, parti completamente invisibile
        if (!startVisibleAsGhost)
            SetAlpha(0f);
    }

    void Update()
    {
        if (isRevealed) return;
        if (!startVisibleAsGhost) return; // resta invisibile, non pulsare

        // Pulse sinusoidale identico a GridPlaneController.ComputePulseAlpha()
        float t    = Time.realtimeSinceStartup;
        float sine = Mathf.Sin(t * Mathf.PI / pulseDuration - Mathf.PI * 0.5f); // -1..1
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (sine + 1f) * 0.5f);

        SetAlpha(alpha);
    }

    // ── API pubblica ──────────────────────────────────────────────────────────

    /// <summary>
    /// Chiamato da AllUnlockedTrigger al posto di SetActive(true).
    /// Ripristina i colori originali e riabilita i collider.
    /// </summary>
    public void Reveal()
    {
        if (isRevealed) return;
        isRevealed = true;

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i];

        foreach (var col in colliders)
            col.enabled = true;
    }

    // ── helper ────────────────────────────────────────────────────────────────

    void SetAlpha(float alpha)
    {
        foreach (var sr in spriteRenderers)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
