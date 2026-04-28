using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attacca questo script al GameObject "Vittoria" in scena.
/// Quando il player lo tocca, mostra il pannello TheSignFilesUI.
///
/// Se il player ha raccolto tutti e 4 i collezionabili e clicca il tasto sinistro del mouse,
/// parte la sequenza finale: glitch → video CiStannoTracciando → ritorno al Menu.
/// Altrimenti il click chiude il pannello e il player continua ad esplorare.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VittoriaTrigger : MonoBehaviour
{
    [Header("Musica Vittoria")]
    [Tooltip("Traccia da avviare (in crossfade, stesso timestamp) quando il player tocca la vittoria")]
    public MusicID musicaVittoria = MusicID.Vittoria;

    [Header("Glitch sequenza finale")]
    public float glitchOutDuration  = 0.30f;
    public float glitchInDuration   = 0.35f;

    private bool isRunning = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isRunning) return;
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        isRunning = true;
        StartCoroutine(Sequence(pc));
    }

    IEnumerator Sequence(PlayerController pc)
    {
        TheSignFilesUI ui = TheSignFilesUI.Instance;
        if (ui == null)
        {
            isRunning = false;
            yield break;
        }

        // 1. Blocca il player, avvia musica vittoria e mostra il pannello
        pc.SetEditMode(true);
        SoundManager.Instance?.PlayMusic(musicaVittoria);
        SoundManager.Instance?.PlaySFX(SoundID.VittoriaReach);
        ui.UpdateAndShow();

        // 2. Aspetta 0.5 secondi: tutti i Redacted visibili, player fermo
        yield return new WaitForSecondsRealtime(0.5f);

        // 3. Effetto glitch full-screen:
        //    schermo si distorce → i Redacted sbloccati spariscono al picco → schermo torna normale
        yield return ui.StartGlitchReveal();

        // 4. Aspetta un attimo, poi aspetta il click sinistro del mouse
        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

        // 5. Tutti e 4 i collezionabili raccolti → sequenza finale
        bool tuttiRaccolti = CollezionabileManager.Instance != null &&
                             CollezionabileManager.Instance.DecryptedCount >= 4;

        if (tuttiRaccolti)
        {
            yield return StartCoroutine(SequenzaFinale(ui));
        }
        else
        {
            // Meno di 4 collezionabili: chiudi pannello e riabilita il player
            ui.Hide();
            pc.SetEditMode(false);
            isRunning = false;
        }
    }

    IEnumerator SequenzaFinale(TheSignFilesUI ui)
    {
        VideoPlayerScreen vps = VideoPlayerScreen.Instance;

        // Glitch out → schermo coperto
        if (GlitchTransition.Instance != null)
            yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(glitchOutDuration));

        // Schermo coperto: nascondi il pannello e taglia la musica di sottofondo
        ui.Hide();
        SoundManager.Instance?.StopMusicImmediate();

        if (vps != null)
        {
            // Prepara il video e mostra lo sfondo nero (ancora sotto il glitch)
            yield return vps.PrepareAndShow();

            // Glitch in → rivela lo schermo nero con il video pronto
            if (GlitchTransition.Instance != null)
                yield return StartCoroutine(GlitchTransition.Instance.GlitchIn(glitchInDuration));

            // Avvia il video e aspetta che finisca
            vps.StartPlayback();
            yield return vps.WaitForEnd();

            // Glitch out finale prima di tornare al menu
            if (GlitchTransition.Instance != null)
                yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(glitchOutDuration));
        }

        SceneManager.LoadScene("Menu");
    }
}
