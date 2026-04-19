using UnityEngine;

/// <summary>
/// Applica la texture griglia al Plane di sfondo e la allinea alla griglia del Level Grid Editor.
/// Quando un Placeable viene trascinato, il pulse si ferma e la griglia va a maxAlpha.
/// La griglia pulsa tra minAlpha e maxAlpha con periodo pulseDuration * 2.
///
/// Come allineare la griglia:
///   1. Rimani in EDIT MODE (non play).
///   2. Regola "Manual Offset" nell'Inspector: vedrai i cambiamenti live.
///   3. Una volta allineato, salva la scena — il valore persiste identico in Play Mode.
///   NON spostare il Plane transform per allineare.
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
    [Tooltip("Offset UV aggiuntivo per allineare manualmente la griglia. Range 0–1.")]
    public Vector2 manualOffset;

    [Header("Pulse Effect")]
    [Tooltip("Attiva/disattiva il pulse di opacità.")]
    public bool enablePulse = true;
    [Tooltip("Opacità minima della griglia (0 = trasparente, 1 = opaco).")]
    [Range(0f, 1f)] public float minAlpha = 0.5f;
    [Tooltip("Opacità massima della griglia.")]
    [Range(0f, 1f)] public float maxAlpha = 1f;
    [Tooltip("Secondi per passare dal minimo al massimo (e viceversa).")]
    [Min(0.1f)] public float pulseDuration = 3f;

    // ── Internals ─────────────────────────────────────────────────────────────
    private MeshRenderer _mr;
    private Material     _mat;
    private bool         _isDragging;

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

    void Update()
    {
        if (_mat == null) return;
        // Durante il drag: niente pulse, opacità massima
        float alpha = (_isDragging || !enablePulse) ? maxAlpha : ComputePulseAlpha();
        _mat.color = new Color(1f, 1f, 1f, alpha);
    }

    // Chiamato quando cambi un valore nell'Inspector (Edit Mode live preview)
    void OnValidate()
    {
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        EnsureMaterial();
        ApplyTexture(_isDragging ? gridEdit : gridStandard);
    }

    // ── Drag callbacks ────────────────────────────────────────────────────────

    void OnDragStart() { _isDragging = true;  ApplyTexture(gridEdit);     }
    void OnDragEnd()   { _isDragging = false; ApplyTexture(gridStandard); }

    // ── Helpers ───────────────────────────────────────────────────────────────

    float ComputePulseAlpha()
    {
        // Onda sinusoidale: parte a minAlpha, sale a maxAlpha in pulseDuration secondi,
        // poi torna a minAlpha, e così via.
        // sin va da -1 a 1 con periodo 2π; vogliamo periodo = pulseDuration * 2.
        float t = Time.realtimeSinceStartup;
        float sine = Mathf.Sin(t * Mathf.PI / pulseDuration - Mathf.PI * 0.5f); // -1..1, inizia a -1
        return Mathf.Lerp(minAlpha, maxAlpha, (sine + 1f) * 0.5f);
    }

    void EnsureMaterial()
    {
        if (_mat != null) return;
        // Custom/UnlitTransparentColor: campiona la texture e moltiplica per _Color (incluso alpha).
        _mat = new Material(Shader.Find("Custom/UnlitTransparentColor")) { name = "GridPlane_Instance" };
        if (_mr != null) _mr.sharedMaterial = _mat;
    }

    void ApplyTexture(Texture2D tex)
    {
        if (_mat == null || tex == null) return;

        // Plane Unity = 10×10 unità locali.
        // Rotazione (-90°, 0, 0): local X → world X, local Z → world Y.
        float worldWidth  = 10f * transform.lossyScale.x;
        float worldHeight = 10f * transform.lossyScale.z;
        float cellWorld   = gridSize * cellsPerTextureTile;

        float tilingX = worldWidth  / cellWorld;
        float tilingY = worldHeight / cellWorld;

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
