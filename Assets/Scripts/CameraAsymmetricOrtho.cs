using UnityEngine;

/// <summary>
/// Modifica asimmetrica della camera ortografica.
/// - <see cref="leftExpansion"/>: espande il bordo sinistro.
/// - <see cref="topReduction"/>: riduce il bordo superiore (valore manuale, positivo = ritira).
/// - <see cref="ceilingTransform"/>: se assegnato, clamppa il bordo superiore al soffitto
///   in modo dinamico (utile quando la camera segue il player).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraAsymmetricOrtho : MonoBehaviour
{
    [Tooltip("Unità mondo extra visibili a sinistra (oltre al bordo normale)")]
    public float leftExpansion = 0f;

    [Tooltip("Unità mondo da sottrarre al bordo superiore (valore positivo = ritira il top verso il basso). Ignorato se ceilingTransform è assegnato.")]
    public float topReduction = 0f;

    [Tooltip("Se assegnato, il bordo superiore viene clampato automaticamente al bordo interno di questo soffitto, qualunque sia la posizione della camera.")]
    public Transform ceilingTransform;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        ApplyProjection();
    }

    void ApplyProjection()
    {
        float size   = cam.orthographicSize;
        float aspect = cam.aspect;

        float right  =  size * aspect;
        float left   = -(size * aspect) - leftExpansion;
        float bottom = -size;
        float top;

        top = size - topReduction;

        if (ceilingTransform != null)
        {
            // Clamp dinamico: il top non supera mai il soffitto (si applica dopo topReduction)
            var col = ceilingTransform.GetComponent<Collider2D>();
            float ceilWorldY = col != null ? col.bounds.min.y : ceilingTransform.position.y;
            float distToCeiling = ceilWorldY - cam.transform.position.y;
            top = Mathf.Min(top, distToCeiling);
        }

        cam.projectionMatrix = Matrix4x4.Ortho(
            left, right, bottom, top,
            cam.nearClipPlane, cam.farClipPlane
        );
    }

    void OnDisable()
    {
        if (cam != null)
            cam.ResetProjectionMatrix();
    }
}
