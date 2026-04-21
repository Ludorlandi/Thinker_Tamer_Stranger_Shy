using UnityEngine;

/// <summary>
/// Strumento di debug. Rimuovere prima del build finale.
/// P → sblocca tutti i Placeable nella scena
/// </summary>
public class DebugTools : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            UnlockAllPlaceables();
    }

    void UnlockAllPlaceables()
    {
        if (PlaceableUnlockManager.Instance == null) return;

        foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
        {
            if (p.placeableType != null)
                PlaceableUnlockManager.Instance.Unlock(p.placeableType);
        }

        foreach (var p in FindObjectsByType<PlaceableJumpA>(FindObjectsSortMode.None))
        {
            if (p.placeableType != null)
                PlaceableUnlockManager.Instance.Unlock(p.placeableType);
        }

        Debug.Log("[DebugTools] Tutti i Placeable sbloccati.");
    }
}
