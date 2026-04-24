using System.Collections;
using UnityEngine;

/// <summary>
/// Attacca questo script al GameObject "Vittoria" in scena.
/// Quando il player lo tocca, mostra il pannello TheSignFilesUI.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VittoriaTrigger : MonoBehaviour
{
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

        // 1. Blocca il player e mostra il pannello con TUTTI i Redacted visibili
        pc.SetEditMode(true);
        ui.UpdateAndShow();

        // 2. Aspetta 0.5 secondi: tutti i Redacted visibili, player fermo
        yield return new WaitForSecondsRealtime(0.5f);

        // 3. Effetto glitch full-screen (identico al portale):
        //    schermo si distorce → i Redacted sbloccati spariscono al picco → schermo torna normale
        //    Il player rimane bloccato per tutta la durata del glitch
        yield return ui.StartGlitchReveal();

        // 4. Ri-abilita il movimento del player
        pc.SetEditMode(false);

        // 5. Aspetta che il player si muova per chiudere il pannello
        yield return new WaitUntil(() =>
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Input.GetKeyDown(KeyCode.Space));

        ui.Hide();
        isRunning = false;
    }
}
