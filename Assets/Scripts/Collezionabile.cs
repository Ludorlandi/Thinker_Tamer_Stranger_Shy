using UnityEngine;

public enum CollezionabileType { Thinker, Shy, Stranger, Tamer }

public class Collezionabile : MonoBehaviour
{
    [Header("Tipo")]
    public CollezionabileType tipo;

    [Header("Oscillazione")]
    public float oscillateAmplitude = 0.18f;
    [Range(0.5f, 4f)]
    public float oscillateSpeed     = 1.8f;

    private Vector3 originPos;
    private float   phase;

    void Start()
    {
        originPos = transform.position;
        // Fase casuale per non avere tutti i collezionabili sincronizzati
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        phase += Time.deltaTime * oscillateSpeed * Mathf.PI * 2f;
        transform.position = originPos + new Vector3(0f, Mathf.Sin(phase) * oscillateAmplitude, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    void Collect()
    {
        SoundManager.Instance?.PlaySFX(SoundID.CollezionabilePickup);
        CollezionabileManager.Instance?.OnCollected(tipo);
        Destroy(gameObject);
    }
}
