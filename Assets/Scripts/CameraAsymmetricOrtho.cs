using UnityEngine;

/// <summary>
/// Espande il bordo sinistro della camera ortografica senza toccare il bordo destro.
/// Aggiungi questo script alla Main Camera e modifica <see cref="leftExpansion"/>.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraAsymmetricOrtho : MonoBehaviour
{
    [Tooltip("Unità mondo extra visibili a sinistra (oltre al bordo normale)")]
    public float leftExpansion = 2f;

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

        float right  =   size * aspect;
        float left   = -(size * aspect) - leftExpansion;
        float top    =   size;
        float bottom = - size;

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
