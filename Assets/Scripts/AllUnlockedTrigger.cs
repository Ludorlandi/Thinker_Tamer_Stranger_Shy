using System.Collections;
using UnityEngine;

/// <summary>
/// Quando tutti i tipi in <see cref="requiredTypes"/> sono sbloccati E il player
/// è in Room Main (camera su <see cref="roomMainCamPos"/>):
/// aspetta 1.5s → GlitchOut → attiva <see cref="objectsToActivate"/> → GlitchIn.
/// </summary>
public class AllUnlockedTrigger : MonoBehaviour
{
    [Tooltip("I PlaceableTypeSO che devono essere tutti sbloccati.")]
    public PlaceableTypeSO[] requiredTypes;

    [Tooltip("GameObject da attivare quando la condizione scatta.")]
    public GameObject[] objectsToActivate;

    [Tooltip("CamPos della Room Main. Se la camera è qui (±soglia) il player è in Room Main.")]
    public Transform roomMainCamPos;

    [Tooltip("Soglia di distanza in unità per considerare la camera 'in Room Main'.")]
    public float camProximityThreshold = 2f;

    [Tooltip("Colore del glitch di attivazione (diverso dal verde dei Gate).")]
    public Color glitchColor = new Color(0.9f, 0.3f, 0.05f, 1f); // arancione di default

    // Mutex statico: impedisce a due istanze di eseguire ActivateRoutine in parallelo.
    // Quando entrambi i trigger (5-type e 9-type) scattano nello stesso frame,
    // il secondo aspetta che il primo abbia finito prima di partire.
    private static bool s_routineRunning = false;

    private bool allUnlocked = false;
    private bool triggerStarted = false;

    void OnEnable()  => PlaceableUnlockManager.OnTypeUnlocked += OnTypeUnlocked;
    void OnDisable() => PlaceableUnlockManager.OnTypeUnlocked -= OnTypeUnlocked;

    void Start()
    {
        s_routineRunning = false;
        CheckAllUnlocked();
    }

    void OnTypeUnlocked(PlaceableTypeSO type) => CheckAllUnlocked();

    void CheckAllUnlocked()
    {
        if (allUnlocked || PlaceableUnlockManager.Instance == null) return;

        foreach (var t in requiredTypes)
            if (!PlaceableUnlockManager.Instance.IsUnlocked(t)) return;

        allUnlocked = true;
        // Non si disabilita qui: continua in Update finché il player non entra in Room Main
    }

    void Update()
    {
        if (!allUnlocked || triggerStarted) return;
        if (!IsInRoomMain()) return;

        triggerStarted = true;
        StartCoroutine(ActivateRoutine());
    }

    bool IsInRoomMain()
    {
        if (roomMainCamPos == null || Camera.main == null) return false;
        Vector2 camXY  = Camera.main.transform.position;
        Vector2 targXY = roomMainCamPos.position;
        return Vector2.Distance(camXY, targXY) < camProximityThreshold;
    }

    IEnumerator ActivateRoutine()
    {
        // Se un'altra istanza sta già girando, aspetta che finisca.
        while (s_routineRunning)
            yield return null;

        s_routineRunning = true;

        if (GlitchTransition.Instance != null)
        {
            GlitchTransition.Instance.SetColor(glitchColor);
            yield return StartCoroutine(GlitchTransition.Instance.GlitchOut(0.30f));
        }

        foreach (var go in objectsToActivate)
        {
            if (go == null) continue;
            var ghost = go.GetComponent<GhostUntilUnlocked>();
            if (ghost != null)
                ghost.Reveal();
            else
                go.SetActive(true);
        }

        if (GlitchTransition.Instance != null)
        {
            yield return StartCoroutine(GlitchTransition.Instance.GlitchIn(0.35f));
            GlitchTransition.Instance.ResetColor();
        }

        s_routineRunning = false;
        enabled = false;
    }
}
