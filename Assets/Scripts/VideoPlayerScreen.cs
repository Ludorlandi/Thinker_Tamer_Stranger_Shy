using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Riproduce un VideoClip a schermo intero con sfondo nero.
/// Il video viene centrato rispettando il suo aspect ratio (4:3 per CiStannoTracciando).
///
/// Setup in scena:
///   GameObject "VideoPlayerScreen"
///     - Canvas (ScreenSpaceOverlay, sortOrder 500)
///     - CanvasGroup
///     - VideoPlayer
///     - AudioSource (per l'audio del video)
///     - VideoPlayerScreen (questo script)
///     Child "Background"   – Image nera full-stretch
///     Child "VideoContainer" – AspectRatioFitter 4:3
///       Child "VideoImage" – RawImage
///
/// Flusso tipico (da VittoriaTrigger):
///   yield return vps.PrepareAndShow();   // prepara il video, mostra canvas nero
///   vps.StartPlayback();                 // avvia la riproduzione
///   yield return vps.WaitForEnd();       // aspetta fine video, poi nasconde canvas
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerScreen : MonoBehaviour
{
    public static VideoPlayerScreen Instance { get; private set; }

    [Header("Video")]
    public VideoClip videoClip;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public RawImage    videoImage;

    // ── componenti ────────────────────────────────────────────────────────────────

    private VideoPlayer    videoPlayer;
    private RenderTexture  renderTexture;
    private bool           videoFinished;

    // ── Unity ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        videoPlayer              = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake  = false;
        videoPlayer.renderMode   = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        AudioSource audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSrc);

        videoPlayer.loopPointReached += OnVideoFinished;

        // Nascondi all'avvio
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
        ReleaseRenderTexture();
    }

    // ── API pubblica ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Mostra il canvas nero e prepara il video (senza avviare la riproduzione).
    /// Torna quando VideoPlayer.isPrepared == true e la RenderTexture è pronta.
    /// </summary>
    public IEnumerator PrepareAndShow()
    {
        if (videoClip == null || videoImage == null) yield break;

        // Mostra subito il canvas nero (sotto il glitch)
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable   = true;
        }

        // Prepara il video
        videoPlayer.clip = videoClip;
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        // Crea la RenderTexture alla risoluzione nativa del video
        ReleaseRenderTexture();
        renderTexture              = new RenderTexture((int)videoPlayer.width, (int)videoPlayer.height, 0);
        videoPlayer.targetTexture  = renderTexture;
        videoImage.texture         = renderTexture;
    }

    /// <summary>
    /// Avvia la riproduzione del video. Chiamare dopo PrepareAndShow().
    /// </summary>
    public void StartPlayback()
    {
        videoFinished = false;
        videoPlayer.Play();
    }

    /// <summary>
    /// Aspetta che il video finisca, poi nasconde il canvas e libera le risorse.
    /// </summary>
    public IEnumerator WaitForEnd()
    {
        yield return new WaitUntil(() => videoFinished);

        videoPlayer.Stop();
        videoImage.texture = null;
        ReleaseRenderTexture();

        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }
    }

    // ── privati ───────────────────────────────────────────────────────────────────

    void OnVideoFinished(VideoPlayer vp) => videoFinished = true;

    void ReleaseRenderTexture()
    {
        if (renderTexture == null) return;
        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }
}
