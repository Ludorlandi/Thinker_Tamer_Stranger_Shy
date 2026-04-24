using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollezionabileManager : MonoBehaviour
{
    public static CollezionabileManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Riferimento al CanvasGroup del pannello contatore (bottom-left)")]
    public CanvasGroup counterCanvasGroup;
    [Tooltip("Testo TMP che mostra 'x/4'")]
    public TextMeshProUGUI counterText;

    [Header("Timing")]
    public float fadeInDuration  = 0.2f;
    public float displayDuration = 2f;
    public float fadeOutDuration = 0.4f;

    private int   totalCount;
    private int   collectedCount;
    private Coroutine showCoroutine;
    private HashSet<CollezionabileType> collectedTypes = new HashSet<CollezionabileType>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Conta automaticamente tutti i collezionabili presenti in scena
        totalCount     = FindObjectsByType<Collezionabile>(FindObjectsSortMode.None).Length;
        collectedCount = 0;

        if (counterCanvasGroup != null)
        {
            counterCanvasGroup.alpha          = 0f;
            counterCanvasGroup.blocksRaycasts = false;
        }
    }

    // Chiamato da Collezionabile.cs al momento della raccolta
    public void OnCollected(CollezionabileType tipo)
    {
        collectedTypes.Add(tipo);
        collectedCount++;
        ShowCounter();
    }

    public bool IsCollected(CollezionabileType tipo) => collectedTypes.Contains(tipo);
    public int  DecryptedCount => collectedTypes.Count;
    public int  RedactedCount  => 4 - collectedTypes.Count;

    public int Collected => collectedCount;
    public int Total     => totalCount;

    // ── UI ──────────────────────────────────────────────────────────────────────

    void ShowCounter()
    {
        if (counterText != null)
            counterText.text = $"{collectedCount}/{totalCount}";

        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(CounterRoutine());
    }

    IEnumerator CounterRoutine()
    {
        if (counterCanvasGroup == null) yield break;

        counterCanvasGroup.blocksRaycasts = false;

        // Fade in
        yield return Fade(counterCanvasGroup, 0f, 1f, fadeInDuration);

        // Pausa visibile
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return Fade(counterCanvasGroup, 1f, 0f, fadeOutDuration);
        counterCanvasGroup.alpha = 0f;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}
