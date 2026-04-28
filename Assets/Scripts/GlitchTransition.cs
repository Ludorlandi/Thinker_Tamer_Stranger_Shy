using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlitchTransition : MonoBehaviour
{
    public static GlitchTransition Instance { get; private set; }

    private Material glitchMat;
    private Image    glitchImage;
    private static readonly int ColorProp = Shader.PropertyToID("_GlitchColor");
    private Color defaultColor = new Color(0.35f, 0.65f, 0.50f, 1f);
    [Header("Audio")]
    [Tooltip("Suono riprodotto in loop durante ogni transizione GlitchOut / GlitchIn")]
    public AudioClip glitchSound;

    private float       _persistGlitchInDuration;
    private AudioSource _audioSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        glitchImage = GetComponentInChildren<Image>();
        // Instanzia il materiale per non modificare l'asset condiviso
        glitchMat = Instantiate(glitchImage.material);
        glitchImage.material = glitchMat;
        SetIntensity(0f);

        _audioSource            = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop        = false;
    }

    public void SetIntensity(float v)
    {
        glitchMat.SetFloat("_Intensity",   Mathf.Clamp01(v));
        glitchMat.SetFloat("_GlitchTime",  Time.unscaledTime);
    }

    /// <summary>
    /// Rende l'oggetto persistente tra scene e lancia GlitchIn alla scena successiva.
    /// Usato dal MenuController per la transizione Menu → MainScene.
    /// DontDestroyOnLoad richiede un root GameObject: aggiungiamo un Canvas autonomo
    /// e stacchiamo dal Canvas padre prima di chiamarlo.
    /// </summary>
    public void PersistForNextScene(float glitchInDuration = 0.35f)
    {
        _persistGlitchInDuration = glitchInDuration;
        // GlitchTransition deve essere root per DontDestroyOnLoad:
        // nella scena Menu è figlio di GlitchOverlay (Canvas root) → ok.
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(GlitchInWithSound(_persistGlitchInDuration));
    }

    IEnumerator GlitchInWithSound(float duration)
    {
        yield return StartCoroutine(GlitchIn(duration));
    }

    void PlayGlitchSound()
    {
        if (glitchSound == null || _audioSource == null) return;
        _audioSource.clip   = glitchSound;
        _audioSource.loop   = true;
        _audioSource.Play();
    }

    void StopGlitchSound()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    public void SetColor(Color c)   => glitchMat.SetColor(ColorProp, c);
    public void ResetColor()        => glitchMat.SetColor(ColorProp, defaultColor);

    // Glitch crescente fino a schermo pieno (~0.3s consigliato)
    public IEnumerator GlitchOut(float duration)
    {
        PlayGlitchSound();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float baseVal  = Mathf.Pow(progress, 0.55f);
            float spike    = (Random.value > 0.72f) ? Random.Range(0f, 0.28f) : 0f;
            SetIntensity(baseVal + spike);
            yield return null;
        }
        SetIntensity(1f);
        StopGlitchSound();
    }

    // Glitch decrescente fino a 0 (~0.35s consigliato)
    public IEnumerator GlitchIn(float duration)
    {
        PlayGlitchSound();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float baseVal  = 1f - Mathf.Pow(progress, 0.55f);
            float spike    = (Random.value > 0.80f) ? Random.Range(0f, 0.18f) : 0f;
            SetIntensity(baseVal + spike);
            yield return null;
        }
        SetIntensity(0f);
        StopGlitchSound();
    }
}
