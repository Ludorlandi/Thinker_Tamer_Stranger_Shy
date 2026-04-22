using System.Collections;
using UnityEngine;

/// <summary>
/// Cutscene d'apertura: la camera parte da CamPos_Fase2 (in cima),
/// aspetta 0.5s e scende a CamPos_Room1 in 1s con curva ease-in.
/// Si attiva solo una volta per sessione Play (R non la riattiva).
/// J per skippare.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("Riferimenti (lascia vuoti: cerca per nome)")]
    public Transform startPosition;   // CamPos_Fase2
    public Transform endPosition;     // CamPos_Room1
    public GameObject player;

    [Header("Timing")]
    public float holdDuration  = 0.5f;
    public float panDuration   = 1.0f;

    private static bool s_hasPlayed = false;

    void Awake()
    {
        // Auto-trova se non assegnati
        if (startPosition == null)
        {
            var go = GameObject.Find("CamPos_Fase2");
            if (go != null) startPosition = go.transform;
        }
        if (endPosition == null)
        {
            var go = GameObject.Find("CamPos_Room1");
            if (go != null) endPosition = go.transform;
        }
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go;
        }
    }

    void Start()
    {
        if (s_hasPlayed || startPosition == null || endPosition == null)
        {
            // Salta: assicura che il player sia attivo nella posizione corretta
            EnablePlayer();
            return;
        }

        player?.SetActive(false);
        StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            SkipCutscene();
    }

    private IEnumerator PlayCutscene()
    {
        s_hasPlayed = true;

        Camera cam = Camera.main;
        if (cam == null) { EnablePlayer(); yield break; }

        // Snap camera in cima
        Vector3 start = startPosition.position;
        Vector3 end   = endPosition.position;
        start.z = cam.transform.position.z;
        end.z   = cam.transform.position.z;
        cam.transform.position = start;

        // Attesa iniziale in cima
        float elapsed = 0f;
        while (elapsed < holdDuration)
        {
            if (Input.GetKeyDown(KeyCode.J)) { SkipCutscene(); yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pan verso il basso — curva ease-in (t^2: parte lenta, accelera)
        elapsed = 0f;
        while (elapsed < panDuration)
        {
            if (Input.GetKeyDown(KeyCode.J)) { SkipCutscene(); yield break; }
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / panDuration);
            float tCurved = t * t;  // ease-in
            cam.transform.position = Vector3.LerpUnclamped(start, end, tCurved);
            yield return null;
        }

        cam.transform.position = end;
        EnablePlayer();
    }

    private void SkipCutscene()
    {
        StopAllCoroutines();
        s_hasPlayed = true;

        Camera cam = Camera.main;
        if (cam != null && endPosition != null)
        {
            Vector3 end = endPosition.position;
            end.z = cam.transform.position.z;
            cam.transform.position = end;
        }

        EnablePlayer();
    }

    private void EnablePlayer()
    {
        if (player == null) return;
        player.transform.position = Checkpoint.GetActivePosition();
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.SetActive(true);
    }
}
