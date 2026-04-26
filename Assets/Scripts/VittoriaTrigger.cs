using System.Collections;
using UnityEngine;

/// <summary>
/// Attacca questo script al GameObject "Vittoria" in scena.
/// Quando il player lo tocca, mostra il pannello TheSignFilesUI.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VittoriaTrigger : MonoBehaviour
{
    [Header("Musica Vittoria")]
    [Tooltip("Traccia da avviare (in crossfade, stesso timestamp) quando il player tocca la vittoria")]
    public MusicID musicaVittoria = MusicID.Vittoria;

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

        // 3. Suono glitch + effetto full-screen (identico al portale):
        //    schermo si distorce → i Redacted sbloccati spariscono al picco → schermo torna normale
        //    Il player rimane bloccato per tutta la durata del glitch
        SoundManager.Instance?.PlaySFX(SoundID.GlitchReveal);
        yield return ui.StartGlitchReveal();

        // 4. Aspetta un attimo e poi aspetta il click per chiudere il pannello
        yield return new WaitForSecondsRealtime(0.5f);
        yield return new WaitUntil(() =>
            Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1));

        ui.Hide();

        // 5. Ri-abilita il player dopo la chiusura
        pc.SetEditMode(false);
        isRunning = false;
    }
}
