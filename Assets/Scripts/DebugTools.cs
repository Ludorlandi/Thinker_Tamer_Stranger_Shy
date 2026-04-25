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

        if (Input.GetKeyDown(KeyCode.L))
            UnanchorAllPlaceables();
    }

    void UnlockAllPlaceables()
    {
        if (PlaceableUnlockManager.Instance == null) return;

        foreach (var p in FindObjectsByType<Placeable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.placeableType != null)
                PlaceableUnlockManager.Instance.Unlock(p.placeableType);
        }

        foreach (var p in FindObjectsByType<PlaceableJumpA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.placeableType != null)
                PlaceableUnlockManager.Instance.Unlock(p.placeableType);
        }

        Debug.Log("[DebugTools] Tutti i Placeable sbloccati.");
    }

    void UnanchorAllPlaceables()
    {
        int count = 0;
        foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
        {
            if (p.IsAnchored) { p.ForceUnanchor(); count++; }
        }
        Debug.Log($"[DebugTools] {count} Placeable disancorati.");
    }
}
