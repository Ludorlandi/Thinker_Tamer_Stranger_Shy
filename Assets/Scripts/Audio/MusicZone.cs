using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MusicZone : MonoBehaviour
{
    [Tooltip("Musica da riprodurre quando il player entra in questa zona")]
    public MusicID musicToPlay = MusicID.OtherAreas;

    void Start()
    {
        // Assicura che il collider sia un trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMusic(musicToPlay);
    }
}
