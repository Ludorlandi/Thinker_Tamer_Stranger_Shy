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

        // Notifica tutte le istanze di quel tipo nella scena (incluse quelle inattive).
        // Salta i Placeable che sono figli di un altro Placeable (es. il JumpPad child
        // di Placeable_Jump2 che ha un Placeable separato per l'accesso a IsAnchored).
        foreach (var p in FindObjectsByType<Placeable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (p.placeableType != type) continue;
            bool isChildOfPlaceable = p.transform.parent != null &&
                                      p.transform.parent.GetComponentInParent<Placeable>() != null;
            if (isChildOfPlaceable) continue;
            p.SetUnlocked();
        }
        foreach (var p in FindObjectsByType<PlaceableJumpA>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
