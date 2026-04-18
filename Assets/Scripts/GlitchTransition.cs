using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GlitchTransition : MonoBehaviour
{
    public static GlitchTransition Instance { get; private set; }

    private Material glitchMat;
    private Image    glitchImage;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        glitchImage = GetComponentInChildren<Image>();
        // Instanzia il materiale per non modificare l'asset condiviso
        glitchMat = Instantiate(glitchImage.material);
        glitchImage.material = glitchMat;
        SetIntensity(0f);
    }

    void SetIntensity(float v)
    {
        glitchMat.SetFloat("_Intensity",   Mathf.Clamp01(v));
        glitchMat.SetFloat("_GlitchTime",  Time.unscaledTime);
    }

    // Glitch crescente fino a schermo pieno (~0.3s consigliato)
    public IEnumerator GlitchOut(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float baseVal  = Mathf.Pow(progress, 0.55f);
            float spike    = (Random.value > 0.72f) ? Random.Range(0f, 0.28f) : 0f;
            SetIntensity(baseVal + spike);
            yield return null;
        }
        SetIntensity(1f);
    }

    // Glitch decrescente fino a 0 (~0.35s consigliato)
    public IEnumerator GlitchIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / duration;
            float baseVal  = 1f - Mathf.Pow(progress, 0.55f);
            float spike    = (Random.value > 0.80f) ? Random.Range(0f, 0.18f) : 0f;
            SetIntensity(baseVal + spike);
            yield return null;
        }
        SetIntensity(0f);
    }
}
