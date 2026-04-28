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
    public MusicID startingMusic = MusicID.MainRoomBassa;

    // Pool SFX — permette suoni sovrapposti
    private const int SFX_POOL_SIZE = 8;
    private AudioSource[] sfxPool;

    // Sorgente dedicata al movimento — non si sovrappone mai
    private AudioSource movementSource;

    // Una AudioSource per ogni valore di MusicID (indice = (int)MusicID).
    // [0] = None → non usato. [1..N] = tracce vere.
    // Tutte girano in sync sempre; solo i volumi cambiano.
    private static readonly int MUSIC_COUNT = System.Enum.GetValues(typeof(MusicID)).Length;
    private AudioSource[] musicSources;

    private MusicID currentMusic = MusicID.None;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildSFXPool();
        BuildMusicSources();
        BuildMovementSource();
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

    // Riproduce il suono di movimento solo se il precedente è terminato
    public void PlayMovementSFX()
    {
        if (movementSource.isPlaying) return;
        if (library == null) return;
        SoundEntry entry = library.GetEntry(SoundID.PlayerMove);
        if (entry == null) return;
        AudioClip clip = entry.GetRandomClip();
        if (clip == null) return;

        movementSource.clip = clip;
        movementSource.volume = entry.volume * sfxVolume;
        movementSource.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        movementSource.Play();
    }

    // Cambia zona musicale — modifica solo i volumi, mai stop/play
    public void PlayMusic(MusicID id)
    {
        if (id == currentMusic) return;
        currentMusic = id;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToMusic(id));
    }

    /// <summary>
    /// Azzera immediatamente il volume di tutte le tracce musicali.
    /// Usare quando la transizione visiva copre già lo schermo (es. GlitchOut al picco).
    /// </summary>
    public void StopMusicImmediate()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        currentMusic = MusicID.None;
        for (int i = 1; i < MUSIC_COUNT; i++)
            musicSources[i].volume = 0f;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        // Aggiorna solo la traccia attiva, le altre restano a 0
        int activeIdx = (int)currentMusic;
        for (int i = 1; i < MUSIC_COUNT; i++)
            musicSources[i].volume = (i == activeIdx) ? musicVolume : 0f;
    }

    // ── Internals ───────────────────────────────────────────────

    void BuildMovementSource()
    {
        var go = new GameObject("SFX_Movement");
        go.transform.SetParent(transform);
        movementSource = go.AddComponent<AudioSource>();
        movementSource.playOnAwake = false;
    }

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
        musicSources = new AudioSource[MUSIC_COUNT];
        // indice 0 = MusicID.None → AudioSource placeholder inutilizzato
        for (int i = 0; i < MUSIC_COUNT; i++)
        {
            string trackName = i == 0 ? "Music_None" : ((MusicID)i).ToString();
            var go = new GameObject(trackName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            musicSources[i] = src;
        }
    }

    // Avvia tutte le tracce in sincronia perfetta tramite DSP scheduling
    void InitAdaptiveMusic()
    {
        if (library == null) return;

        double startDSP = AudioSettings.dspTime + 0.1;

        for (int i = 1; i < MUSIC_COUNT; i++)
        {
            MusicID id = (MusicID)i;
            AudioClip clip = library.GetMusic(id);
            musicSources[i].clip = clip;
            musicSources[i].volume = (id == startingMusic) ? musicVolume : 0f;
            if (clip != null) musicSources[i].PlayScheduled(startDSP);
        }

        currentMusic = startingMusic;
    }

    // Fade solo dei volumi — i clip continuano a girare in sync
    IEnumerator FadeToMusic(MusicID target)
    {
        int targetIdx = (int)target;
        float[] startVolumes = new float[MUSIC_COUNT];
        for (int i = 1; i < MUSIC_COUNT; i++)
            startVolumes[i] = musicSources[i].volume;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            for (int i = 1; i < MUSIC_COUNT; i++)
            {
                float targetVol = (i == targetIdx) ? musicVolume : 0f;
                musicSources[i].volume = Mathf.Lerp(startVolumes[i], targetVol, t);
            }
            yield return null;
        }

        for (int i = 1; i < MUSIC_COUNT; i++)
            musicSources[i].volume = (i == targetIdx) ? musicVolume : 0f;
    }

    AudioSource GetAvailableSource()
    {
        foreach (var s in sfxPool)
            if (!s.isPlaying) return s;
        return sfxPool[0];
    }
}
