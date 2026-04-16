using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    private Image fadeImage;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        fadeImage = GetComponentInChildren<Image>();
        SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    public IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(t / duration));
            yield return null;
        }
        SetAlpha(1f);
    }

    public IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(1f - t / duration));
            yield return null;
        }
        SetAlpha(0f);
    }
}
