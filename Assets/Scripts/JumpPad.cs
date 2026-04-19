using System.Collections;
using UnityEngine;

/// <summary>
/// Componente root di Placeables_JumpG.
/// Quando il player entra nel trigger del pad, viene lanciato verso l'alto
/// con forza = jumpForce del player * jumpMultiplier.
/// Usa OnTriggerEnter2D (affidabile su tutti i tipi di Rigidbody2D).
/// </summary>
[RequireComponent(typeof(Placeable))]
public class JumpPad : MonoBehaviour
{
    [Tooltip("Moltiplicatore applicato al jumpForce base del PlayerController.")]
    public float jumpMultiplier = 2f;

    [Tooltip("Tempo minimo tra un lancio e il successivo — evita re-trigger immediato.")]
    public float launchCooldown = 0.4f;

    [Tooltip("Transform del figlio visivo su cui applicare l'effetto squash.")]
    public Transform visualTransform;

    private Placeable placeable;
    private float lastLaunchTime = -10f;

    void Start()
    {
        placeable = GetComponent<Placeable>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (placeable != null && !placeable.IsAnchored) return;
        if (Time.time - lastLaunchTime < launchCooldown) return;

        // Non rilanciare se il player sta già volando verso l'alto
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.linearVelocity.y > 1f) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        pc.ForceJump(pc.jumpForce * jumpMultiplier);
        lastLaunchTime = Time.time;
        SoundManager.Instance?.PlaySFX(SoundID.JumpPadBounce);

        if (visualTransform != null)
            StartCoroutine(SquashEffect());
    }

    IEnumerator SquashEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float squashY = 1f - Mathf.Sin(t * Mathf.PI) * 0.35f;
            float squashX = 1f + Mathf.Sin(t * Mathf.PI) * 0.20f;
            visualTransform.localScale = new Vector3(squashX, squashY, 1f);
            yield return null;
        }
        visualTransform.localScale = Vector3.one;
    }
}
