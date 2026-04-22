using UnityEngine;

/// <summary>
/// Anima il materiale glitch su uno SpriteRenderer aggiornando
/// _GlitchTime e _Intensity ogni frame tramite MaterialPropertyBlock.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GlitchSpriteAnimator : MonoBehaviour
{
    [Range(0f, 1f)]
    public float intensity = 1f;
    public Color glitchColor = new Color(0.35f, 0.65f, 0.50f, 1f);

    private SpriteRenderer       _sr;
    private MaterialPropertyBlock _mpb;

    private static readonly int IntensityID  = Shader.PropertyToID("_Intensity");
    private static readonly int GlitchTimeID = Shader.PropertyToID("_GlitchTime");
    private static readonly int GlitchColorID = Shader.PropertyToID("_GlitchColor");

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(IntensityID,   intensity);
        _mpb.SetFloat(GlitchTimeID,  Time.time);
        _mpb.SetColor(GlitchColorID, glitchColor);
        _sr.SetPropertyBlock(_mpb);
    }
}
