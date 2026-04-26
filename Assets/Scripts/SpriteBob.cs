using UnityEngine;

/// <summary>
/// Fa fluttuare il GameObject su e giù con una leggera inclinazione,
/// identico al comportamento idle dei Placeable.
/// Aggiungilo a qualsiasi sprite che vuoi animare.
/// </summary>
public class SpriteBob : MonoBehaviour
{
    [Range(0f, 0.3f)]
    [Tooltip("Quanto si muove su e giù (unità Unity).")]
    public float bobAltezza = 0.08f;

    [Range(0.5f, 5f)]
    [Tooltip("Oscillazioni al secondo.")]
    public float bobVelocita = 1.8f;

    [Range(0f, 15f)]
    [Tooltip("Gradi di inclinazione durante l'oscillazione.")]
    public float bobInclinazione = 3f;

    private Vector3 startPosition;
    private float bobPhase;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        bobPhase += Time.deltaTime * bobVelocita * Mathf.PI * 2f;
        float sine  = Mathf.Sin(bobPhase);
        float angle = sine * bobInclinazione;

        transform.position = startPosition + new Vector3(0f, sine * bobAltezza, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
