using UnityEngine;

/// <summary>
/// Gestisce l'attivazione/disattivazione del container dei glitch laterali.
/// In edit mode è sempre visibile per facilitare il level design.
/// In play mode parte disattivato e si attiva solo durante il drag di un Placeable.
/// </summary>
[ExecuteAlways]
public class GlitchContainer : MonoBehaviour
{
    void Awake()
    {
        if (!Application.isPlaying) return; // in edit mode: rimane sempre attivo

        Placeable.OnAnyDragStart += OnDragStart;
        Placeable.OnAnyDragEnd   += OnDragEnd;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Placeable.OnAnyDragStart -= OnDragStart;
        Placeable.OnAnyDragEnd   -= OnDragEnd;
    }

    void OnDragStart() => gameObject.SetActive(true);
    void OnDragEnd()   => gameObject.SetActive(false);
}
