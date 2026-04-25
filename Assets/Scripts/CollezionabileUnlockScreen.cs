using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Schermata di sblocco per i collezionabili.
/// Stessa logica e animazione di UnlockScreen (Placeables):
/// fade-in + scale pop + bob + blocca player + click per chiudere.
/// </summary>
public class CollezionabileUnlockScreen : MonoBehaviour
{
    public static CollezionabileUnlockScreen Instance { get; private set; }

    [Header("Riferimenti UI")]
    public CanvasGroup      canvasGroup;
    public Image            unlockImage;
    public TextMeshProUGUI  hintText;

    [Header("Sprite per tipo")]
    public Sprite thinkerSprite;
    public Sprite shySprite;
    public Sprite strangerSprite;
    public Sprite tamerSprite;

    [Header("Timing")]
    public float fadeInDuration  = 0.45f;
    public float fadeOutDuration = 0.30f;
    [Tooltip("Pausa dopo il fade-in prima di accettare il click di chiusura")]
    public float dismissDelay    = 0.50f;

    [Header("Bob immagine")]
    [Range(0f, 60f)]  public float bobHeight = 18f;
    [Range(0.5f, 4f)] public float bobSpeed  = 1.6f;
    [Range(0f, 10f)]  public float bobTilt   = 3.5f;

    private bool          isVisible;
    private bool          canDismiss;
    private RectTransform imageRect;
    private Vector2       imageBase;
    private float         bobPhase;
    private float         hintPhase;

    private Dictionary<CollezionabileType, Sprite> spriteMap;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        imageRect = unlockImage.rectTransform;
        imageBase = imageRect.anchoredPosition;

        spriteMap = new Dictionary<CollezionabileType, Sprite>
        {
            { CollezionabileType.Thinker,  thinkerSprite  },
            { CollezionabileType.Shy,      shySprite      },
            { CollezionabileType.Stranger, strangerSprite },
            { CollezionabileType.Tamer,    tamerSprite    },
        };

        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (!isVisible) return;

        bobPhase  += Time.unscaledDeltaTime * bobSpeed * Mathf.PI * 2f;
        hintPhase += Time.unscaledDeltaTime * 1.6f    * Mathf.PI * 2f;

        float sine = Mathf.Sin(bobPhase);
        imageRect.anchoredPosition = imageBase + new Vector2(0f, sine * bobHeight);
        imageRect.localRotation    = Quaternion.Euler(0f, 0f, sine * bobTilt);

        if (hintText != null)
            hintText.alpha = Mathf.Lerp(0.30f, 1f, (Mathf.Sin(hintPhase) + 1f) * 0.5f);

        if (canDismiss && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            StartCoroutine(HideRoutine());
    }

    // ── API pubblica ──────────────────────────────────────────────────────────

    public void Show(CollezionabileType tipo)
    {
        if (isVisible) return;
        spriteMap.TryGetValue(tipo, out Sprite sprite);
        unlockImage.sprite = sprite;
        StartCoroutine(ShowRoutine());
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    IEnumerator ShowRoutine()
    {
        isVisible  = true;
        canDismiss = false;
        bobPhase   = 0f;
        hintPhase  = 0f;
        canvasGroup.blocksRaycasts = true;

        imageRect.anchoredPosition = imageBase;
        imageRect.localRotation    = Quaternion.identity;
        imageRect.localScale       = Vector3.one * 0.82f;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.SetEditMode(true);

        // Fade in + scale pop
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeInDuration));
            canvasGroup.alpha    = smooth;
            imageRect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, smooth);
            yield return null;
        }
        canvasGroup.alpha    = 1f;
        imageRect.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(dismissDelay);
        canDismiss = true;
    }

    IEnumerator HideRoutine()
    {
        canDismiss = false;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        isVisible                  = false;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.SetEditMode(false);
    }
}
