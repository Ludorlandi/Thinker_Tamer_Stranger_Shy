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
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        SetChildrenActive(false); // nasconde i figli ma GlitchContainer resta sempre attivo
    }

    void OnDestroy()
    {
        Placeable.OnAnyDragStart -= OnDragStart;
        Placeable.OnAnyDragEnd   -= OnDragEnd;
    }

    void OnDragStart() => SetChildrenActive(true);
    void OnDragEnd()   => SetChildrenActive(false);

    void SetChildrenActive(bool active)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(active);
    }
}
