using System.Collections.Generic;
using UnityEngine;

public class PlaceableUnlockManager : MonoBehaviour
{
    public static PlaceableUnlockManager Instance { get; private set; }

    public static event System.Action<PlaceableTypeSO> OnTypeUnlocked;

    private readonly HashSet<PlaceableTypeSO> unlockedTypes = new HashSet<PlaceableTypeSO>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Unlock(PlaceableTypeSO type)
    {
        if (type == null || unlockedTypes.Contains(type)) return;

        unlockedTypes.Add(type);

        // Notifica tutte le istanze di quel tipo nella scena
        foreach (var p in FindObjectsByType<Placeable>(FindObjectsSortMode.None))
        {
            if (p.placeableType == type)
                p.SetUnlocked();
        }
        foreach (var p in FindObjectsByType<PlaceableJumpA>(FindObjectsSortMode.None))
        {
            if (p.placeableType == type)
                p.SetUnlocked();
        }

        // Notifica listener esterni
        OnTypeUnlocked?.Invoke(type);

        // Mostra la schermata di sblocco
        UnlockScreen.Instance?.Show(type);
    }

    public bool IsUnlocked(PlaceableTypeSO type)
    {
        return type == null || unlockedTypes.Contains(type);
    }
}
