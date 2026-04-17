using UnityEngine;

public class PlaceableUnlockItem : MonoBehaviour
{
    [Tooltip("Il tipo di Placeable che questo oggetto sblocca.")]
    public PlaceableTypeSO typeToUnlock;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlaceableUnlockManager.Instance == null) return;

        PlaceableUnlockManager.Instance.Unlock(typeToUnlock);
        SoundManager.Instance?.PlaySFX(SoundID.PlaceableUnlocked);
        Destroy(gameObject);
    }
}
