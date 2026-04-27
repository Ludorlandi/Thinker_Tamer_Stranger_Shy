using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioClip synthClip;
    [SerializeField] AudioClip menuMusicClip;
    [SerializeField] AudioClip placeableSnapClip;
    [SerializeField] AudioClip checkpointClip;
    [SerializeField] AudioClip placeableSelectClip;
    [Range(0f, 1f)]
    [SerializeField] float musicVolume = 0.6f;

    [Header("UI Elements")]
    [SerializeField] CanvasGroup[] wordGroups;  // Thinker, Tamer, Stranger, Shy (in order)
    [SerializeField] CanvasGroup   buttonGroup;
    [SerializeField] Image         flashOverlay; // white fullscreen panel

    [Header("Timing")]
    [SerializeField] float introDuration    = 0.65f;
    [SerializeField] float wordRevealDur    = 0.30f;
    [SerializeField] float wordGap          = 0.55f;
    [Tooltip("Secondi dall'inizio del Synth prima che compaiano le parole")]
    [SerializeField] float wordStartDelay   = 1.00f;
    [SerializeField] float preFlashPause    = 0.30f;

    [Header("Button Animation")]
    [SerializeField] float bobAmplitude = 8f;
    [SerializeField] float bobFrequency = 1.2f;
    [SerializeField] float pulseMin     = 0.55f;
    [SerializeField] float pulseMax     = 1.0f;
    [SerializeField] float pulsePeriod  = 2.5f;

    AudioSource   sfxSource;
    AudioSource   musicSource;
    bool          buttonActive;
    RectTransform buttonRT;
    Vector2       buttonOrigin;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        sfxSource          = gameObject.AddComponent<AudioSource>();
        musicSource        = gameObject.AddComponent<AudioSource>();
        musicSource.loop   = true;
        musicSource.volume = musicVolume;

        foreach (var g in wordGroups)
        {
            g.alpha = 0f;
            g.gameObject.SetActive(false);
        }
        if (buttonGroup)  { buttonGroup.alpha  = 0f; buttonGroup.gameObject.SetActive(false); }
        if (flashOverlay)   flashOverlay.gameObject.SetActive(false);
    }

    void Start()
    {
        if (buttonGroup)
        {
            buttonRT     = buttonGroup.GetComponent<RectTransform>();
            buttonOrigin = buttonRT.anchoredPosition;
        }

        // Imposta subito intensità 1 (schermo coperto dal glitch dal primo frame)
        if (GlitchTransition.Instance != null)
            GlitchTransition.Instance.SetIntensity(1f);

        // Synth parte come prima cosa in assoluto
        if (synthClip) sfxSource.PlayOneShot(synthClip);

        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        if (!buttonActive || buttonRT == null) return;

        float t   = Time.realtimeSinceStartup;
        float bob = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        buttonRT.anchoredPosition = buttonOrigin + new Vector2(0f, bob);

        float sine        = Mathf.Sin(t * Mathf.PI / pulsePeriod - Mathf.PI * 0.5f);
        buttonGroup.alpha = Mathf.Lerp(pulseMin, pulseMax, (sine + 1f) * 0.5f);
    }

    // ── Intro Sequence ───────────────────────────────────────────

    IEnumerator IntroSequence()
    {
        // GlitchIn e attesa musica girano in parallelo
        if (GlitchTransition.Instance != null)
            StartCoroutine(GlitchTransition.Instance.GlitchIn(introDuration));

        // La musica parte alla fine del Synth (concorrente)
        StartCoroutine(StartMusicAfterSynth());

        // Aspetta wordStartDelay secondi dall'inizio del Synth, poi via con le parole
        yield return new WaitForSeconds(wordStartDelay);

        // Fai apparire le parole una alla volta
        for (int i = 0; i < wordGroups.Length; i++)
        {
            yield return StartCoroutine(RevealWord(wordGroups[i]));
            if (i < wordGroups.Length - 1)
                yield return new WaitForSeconds(wordGap);
        }

        yield return new WaitForSeconds(preFlashPause);

        // Flash bianco + suono Checkpoint
        if (checkpointClip) sfxSource.PlayOneShot(checkpointClip);
        StartCoroutine(WhiteFlash(0.2f));

        // Il tasto appare mentre lo schermo è bianco
        if (buttonGroup)
        {
            buttonGroup.gameObject.SetActive(true);
            buttonGroup.alpha = 1f;
            buttonActive      = true;
        }
    }

    IEnumerator StartMusicAfterSynth()
    {
        float wait = synthClip != null ? synthClip.length : 0f;
        if (wait > 0f) yield return new WaitForSeconds(wait);
        if (menuMusicClip) { musicSource.clip = menuMusicClip; musicSource.Play(); }
    }

    IEnumerator RevealWord(CanvasGroup g)
    {
        if (placeableSnapClip) sfxSource.PlayOneShot(placeableSnapClip);
        g.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < wordRevealDur)
        {
            elapsed += Time.deltaTime;
            float p       = elapsed / wordRevealDur;
            float freq    = Mathf.Lerp(35f, 5f, p);
            float flicker = Mathf.Sin(elapsed * freq * Mathf.PI);
            g.alpha = p > 0.65f ? 1f : (flicker > 0f ? 1f : 0f);
            yield return null;
        }
        g.alpha = 1f;
    }

    IEnumerator WhiteFlash(float duration)
    {
        if (!flashOverlay) yield break;
        flashOverlay.color = Color.white;
        flashOverlay.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        flashOverlay.gameObject.SetActive(false);
    }

    // ── Bottone ──────────────────────────────────────────────────

    public void OnHackClick()
    {
        if (!buttonActive) return;
        buttonActive = false;
        StartCoroutine(GoToGame());
    }

    IEnumerator GoToGame()
    {
        if (placeableSelectClip) sfxSource.PlayOneShot(placeableSelectClip);

        if (GlitchTransition.Instance != null)
        {
            yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(0.30f));
            // Persiste il GlitchTransition nella prossima scena e lancia GlitchIn
            GlitchTransition.Instance.PersistForNextScene(0.35f);
        }

        SceneManager.LoadScene("MainScene");
    }

    // Chiamato da MenuButtonHover
    public void ButtonHoverEnter()
    {
        if (buttonGroup) buttonGroup.transform.localScale = Vector3.one * 1.07f;
    }

    public void ButtonHoverExit()
    {
        if (buttonGroup) buttonGroup.transform.localScale = Vector3.one;
    }
}
