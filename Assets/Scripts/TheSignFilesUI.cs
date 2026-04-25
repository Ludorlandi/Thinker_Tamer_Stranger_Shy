using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestisce il pannello UI "The Sign Files".
///
/// Struttura Canvas:
/// Canvas
///  └─ TheSignFilesPanel  [CanvasGroup]
///      ├─ TheSignFilesImage   [Image – sprite TheSignFiles, stretch full screen]
///      ├─ RedactedTamer       [Image – stesso size/pos]
///      ├─ RedactedThinker     [Image – stesso size/pos]
///      ├─ RedactedStranger    [Image – stesso size/pos]
///      ├─ RedactedShy         [Image – stesso size/pos]
///      └─ TitleText           [TextMeshProUGUI – ancorato in alto]
/// </summary>
public class TheSignFilesUI : MonoBehaviour
{
    public static TheSignFilesUI Instance { get; private set; }

    [Header("Panel")]
    public CanvasGroup canvasGroup;

    [Header("Redacted Overlays")]
    public Image redactedThinker;
    public Image redactedShy;
    public Image redactedStranger;
    public Image redactedTamer;

    [Header("Testo titolo")]
    public TextMeshProUGUI titleText;
    [Tooltip("Font pixel-style (es. VCR OSD Mono TMP SDF). Lascia null per usare il default.")]
    public TMP_FontAsset pixelFont;
    [Tooltip("Colore del testo titolo.")]
    public Color titleColor = Color.white;

    [Header("Hint click")]
    public TextMeshProUGUI hintText;

    [Header("Timing")]
    public float fadeInDuration   = 0.15f;
    public float fadeOutDuration  = 0.25f;
    public float glitchOutDuration = 0.30f;
    public float glitchInDuration  = 0.35f;

    // ── stato interno ────────────────────────────────────────────────────────────

    private Dictionary<CollezionabileType, Image> redactedMap;
    private Coroutine fadeCoroutine;
    private float hintPhase;
    private bool  isVisible;

    // ── Unity ────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!isVisible || hintText == null) return;
        hintPhase += Time.unscaledDeltaTime * 1.6f * Mathf.PI * 2f;
        hintText.alpha = Mathf.Lerp(0.30f, 1f, (Mathf.Sin(hintPhase) + 1f) * 0.5f);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        redactedMap = new Dictionary<CollezionabileType, Image>
        {
            { CollezionabileType.Thinker,  redactedThinker  },
            { CollezionabileType.Shy,      redactedShy      },
            { CollezionabileType.Stranger, redactedStranger },
            { CollezionabileType.Tamer,    redactedTamer    },
        };

        if (pixelFont != null && titleText != null)
            titleText.font = pixelFont;

        if (titleText != null)
            titleText.color = titleColor;

        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // ── API pubblica ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggiorna il testo, ripristina tutti i Redacted visibili e fa fade-in del pannello.
    /// </summary>
    public void UpdateAndShow()
    {
        var mgr = CollezionabileManager.Instance;
        int decrypted = mgr != null ? mgr.DecryptedCount : 0;
        int redacted  = 4 - decrypted;

        if (titleText != null)
        {
            titleText.text  = $"The_sign_files: {redacted} redacted - {decrypted} decrypted";
            titleText.color = titleColor;
        }

        // Tutti i Redacted visibili all'inizio (anche quelli già sbloccati:
        // spariranno poco dopo con l'effetto glitch)
        foreach (var pair in redactedMap)
        {
            Image img = pair.Value;
            if (img == null) continue;
            img.gameObject.SetActive(true);
            img.color = Color.white;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Avvia la sequenza di glitch full-screen (identica al portale) e poi
    /// nasconde i Redacted dei collezionabili già ottenuti.
    /// Ritorna una Coroutine su cui VittoriaTrigger può fare yield return.
    /// </summary>
    public Coroutine StartGlitchReveal()
    {
        return StartCoroutine(GlitchRevealCollected());
    }

    /// <summary>
    /// Nasconde il pannello con un fade-out.
    /// </summary>
    public void Hide()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────────

    IEnumerator GlitchRevealCollected()
    {
        var mgr = CollezionabileManager.Instance;

        // Se nessun collezionabile è stato ottenuto, non fare il glitch
        if (mgr == null || mgr.DecryptedCount == 0)
            yield break;

        // Fase 1 – glitch out (schermo si distorce, identico al portale)
        if (GlitchTransition.Instance != null)
            yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(glitchOutDuration));

        // Fase 2 – al picco del glitch: nascondi i Redacted dei collezionabili ottenuti
        if (mgr != null)
        {
            foreach (CollezionabileType tipo in System.Enum.GetValues(typeof(CollezionabileType)))
            {
                if (!mgr.IsCollected(tipo)) continue;
                if (!redactedMap.TryGetValue(tipo, out Image img)) continue;
                if (img != null) img.gameObject.SetActive(false);
            }
        }

        // Fase 3 – glitch in (schermo torna normale, rivelando le colonne sbloccate)
        if (GlitchTransition.Instance != null)
            yield return StartCoroutine(GlitchTransition.Instance.GlitchIn(glitchInDuration));
    }

    IEnumerator FadeIn()
    {
        isVisible  = true;
        hintPhase  = 0f;
        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        isVisible                  = false;
    }
}
