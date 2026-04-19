using UnityEngine;

/// <summary>
/// Applica la texture griglia al Plane di sfondo e la allinea alla griglia del Level Grid Editor.
/// Scambia la texture quando un Placeable viene trascinato (Grid Edit) e la ripristina al rilascio.
///
/// Come allineare la griglia:
///   1. Rimani in EDIT MODE (non play).
///   2. Regola il campo "Manual Offset" nel Inspector: vedrai i cambiamenti live.
///   3. Una volta allineato, il valore si salva con la scena e funziona identico in play.
///   NON spostare il Plane transform per allineare: cambia il position e invalida il calcolo.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class GridPlaneController : MonoBehaviour
{
    [Header("Textures")]
    public Texture2D gridStandard;
    public Texture2D gridEdit;

    [Header("Grid Settings")]
    [Tooltip("Dimensione di una cella in world units (deve corrispondere al Grid Size del Level Grid Editor)")]
    public float gridSize = 1f;
    [Tooltip("Quante celle della griglia rappresenta una singola tile della texture.")]
    [Min(1)] public int cellsPerTextureTile = 1;

    [Header("Fine-tuning (regola in Edit Mode — live preview)")]
    [Tooltip("Offset UV aggiuntivo per allineare manualmente la griglia al livello. " +
             "Regola X e Y finché le linee coincidono con i bordi dei tile. Range 0-1.")]
    public Vector2 manualOffset;

    // ── Internals ─────────────────────────────────────────────────────────────
    private MeshRenderer _mr;
    private Material _mat;
    private bool _isDragging;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _mr = GetComponent<MeshRenderer>();
        EnsureMaterial();
        ApplyTexture(gridStandard);

        if (Application.isPlaying)
        {
            Placeable.OnAnyDragStart += OnDragStart;
            Placeable.OnAnyDragEnd   += OnDragEnd;
        }
    }

    void OnDisable()
    {
        Placeable.OnAnyDragStart -= OnDragStart;
        Placeable.OnAnyDragEnd   -= OnDragEnd;
    }

    // Chiamato ogni volta che cambi un valore nell'Inspector (funziona in Edit Mode)
    void OnValidate()
    {
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        EnsureMaterial();
        ApplyTexture(_isDragging ? gridEdit : gridStandard);
    }

    // ── Drag callbacks ────────────────────────────────────────────────────────

    void OnDragStart() { _isDragging = true;  ApplyTexture(gridEdit);     }
    void OnDragEnd()   { _isDragging = false; ApplyTexture(gridStandard); }

    // ── Core ──────────────────────────────────────────────────────────────────

    void EnsureMaterial()
    {
        if (_mat != null) return;
        _mat = new Material(Shader.Find("Unlit/Texture")) { name = "GridPlane_Instance" };
        if (_mr != null) _mr.sharedMaterial = _mat;
    }

    void ApplyTexture(Texture2D tex)
    {
        if (_mat == null || tex == null) return;

        // Il Plane di Unity è 10×10 unità locali.
        // Con rotazione (-90°, 0, 0): local X → world X, local Z → world Y.
        float worldWidth  = 10f * transform.lossyScale.x;
        float worldHeight = 10f * transform.lossyScale.z;
        float cellWorld   = gridSize * cellsPerTextureTile;

        float tilingX = worldWidth  / cellWorld;
        float tilingY = worldHeight / cellWorld;

        // Auto-alignment: calcola l'offset base per mettere le linee agli interi world.
        // manualOffset permette di correggere eventuali scostamenti residui.
        float leftEdge   = transform.position.x - worldWidth  * 0.5f;
        float bottomEdge = transform.position.y - worldHeight * 0.5f;

        float autoX = -Mathf.Repeat(-leftEdge   / cellWorld, 1f);
        float autoY = -Mathf.Repeat(-bottomEdge / cellWorld, 1f);

        _mat.mainTexture       = tex;
        _mat.mainTextureScale  = new Vector2(tilingX, tilingY);
        _mat.mainTextureOffset = new Vector2(autoX + manualOffset.x,
                                             autoY + manualOffset.y);
    }
}
