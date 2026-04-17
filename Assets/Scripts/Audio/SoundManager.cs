using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Libreria")]
    public SoundLibrary library;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Header("Musica Adattiva")]
    [Tooltip("Secondi di crossfade tra le due tracce quando si cambia zona")]
    public float fadeDuration = 1.5f;

    [Tooltip("Musica attiva all'avvio del gioco")]
    public MusicID startingMusic = MusicID.OtherAreas;

    // Pool SFX — permette suoni sovrapposti
    private const int SFX_POOL_SIZE = 8;
    private AudioSource[] sfxPool;

    // musicA = OtherAreas, musicB = MainRoom — sempre in sync, mai fermate
    private AudioSource musicA; // OtherAreas
    private AudioSource musicB; // MainRoom

    private MusicID currentMusic = MusicID.None;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildSFXPool();
        BuildMusicSources();
    }

    void Start()
    {
        InitAdaptiveMusic();
    }

    // ── API pubblica ────────────────────────────────────────────

    public void PlaySFX(SoundID id)
    {
        if (library == null) return;
        SoundEntry entry = library.GetEntry(id);
        if (entry == null) return;

        AudioClip clip = entry.GetRandomClip();
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.clip = clip;
        source.volume = entry.volume * sfxVolume;
        source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        source.Play();
    }

    // Cambia zona musicale — modifica solo i volumi, mai stop/play
    public void PlayMusic(MusicID id)
    {
        if (id == currentMusic) return;
        currentMusic = id;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToMusic(id));
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        // Aggiorna il volume della traccia attiva senza toccare quella muta
        if (currentMusic == MusicID.MainRoom)
            musicB.volume = musicVolume;
        else
            musicA.volume = musicVolume;
    }

    // ── Internals ───────────────────────────────────────────────

    void BuildSFXPool()
    {
        sfxPool = new AudioSource[SFX_POOL_SIZE];
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            var go = new GameObject($"SFX_Source_{i}");
            go.transform.SetParent(transform);
            sfxPool[i] = go.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
        }
    }

    void BuildMusicSources()
    {
        var goA = new GameObject("Music_OtherAreas");
        goA.transform.SetParent(transform);
        musicA = goA.AddComponent<AudioSource>();
        musicA.loop = true;
        musicA.playOnAwake = false;
        musicA.volume = 0f;

        var goB = new GameObject("Music_MainRoom");
        goB.transform.SetParent(transform);
        musicB = goB.AddComponent<AudioSource>();
        musicB.loop = true;
        musicB.playOnAwake = false;
        musicB.volume = 0f;
    }

    // Avvia entrambe le tracce in sincronia perfetta tramite DSP scheduling
    void InitAdaptiveMusic()
    {
        if (library == null) return;

        AudioClip clipOther = library.GetMusic(MusicID.OtherAreas);
        AudioClip clipMain  = library.GetMusic(MusicID.MainRoom);

        // Assegna i clip (possono essere null se non ancora assegnati)
        musicA.clip = clipOther;
        musicB.clip = clipMain;

        // Volumi iniziali in base alla zona di partenza
        currentMusic = startingMusic;
        musicA.volume = (startingMusic == MusicID.OtherAreas) ? musicVolume : 0f;
        musicB.volume = (startingMusic == MusicID.MainRoom)   ? musicVolume : 0f;

        // Partenza sincronizzata — PlayScheduled garantisce lo stesso sample di inizio
        double startDSP = AudioSettings.dspTime + 0.1;
        if (clipOther != null) musicA.PlayScheduled(startDSP);
        if (clipMain  != null) musicB.PlayScheduled(startDSP);
    }

    // Fade solo dei volumi — i clip continuano a girare in sync
    IEnumerator FadeToMusic(MusicID target)
    {
        float startA = musicA.volume;
        float startB = musicB.volume;

        float targetA = (target == MusicID.OtherAreas) ? musicVolume : 0f;
        float targetB = (target == MusicID.MainRoom)   ? musicVolume : 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            musicA.volume = Mathf.Lerp(startA, targetA, t);
            musicB.volume = Mathf.Lerp(startB, targetB, t);
            yield return null;
        }

        musicA.volume = targetA;
        musicB.volume = targetB;
    }

    AudioSource GetAvailableSource()
    {
        foreach (var s in sfxPool)
            if (!s.isPlaying) return s;
        return sfxPool[0];
    }
}
