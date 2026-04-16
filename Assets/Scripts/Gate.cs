using System.Collections;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public Gate linkedGate;
    public Transform cameraPosition;
    public float teleportCooldown = 1f;

    private bool onCooldown = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (onCooldown) return;
        if (linkedGate == null) return;

        // Attiva cooldown subito per evitare double-trigger
        StartCooldown();
        linkedGate.StartCooldown();

        StartCoroutine(TeleportRoutine(other));
    }

    private IEnumerator TeleportRoutine(Collider2D player)
    {
        // Congela il player durante la transizione
        var rb = player.GetComponent<Rigidbody2D>();
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.SetEditMode(true);

        // Dissolvenza in nero (0.25s)
        if (ScreenFade.Instance != null)
            yield return StartCoroutine(ScreenFade.Instance.FadeOut(0.25f));

        // Teletrasporto
        player.transform.position = linkedGate.transform.position + Vector3.up;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Snap camera
        if (cameraPosition != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 newCamPos = cameraPosition.position;
                newCamPos.z = mainCam.transform.position.z;
                mainCam.transform.position = newCamPos;
            }
        }

        // Dissolvenza da nero (0.25s)
        if (ScreenFade.Instance != null)
            yield return StartCoroutine(ScreenFade.Instance.FadeIn(0.25f));

        // Riabilita il player
        if (pc != null) pc.SetEditMode(false);
    }

    public void StartCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(teleportCooldown);
        onCooldown = false;
    }
}
