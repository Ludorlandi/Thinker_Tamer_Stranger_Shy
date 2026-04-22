using UnityEngine;

/// <summary>
/// Versione del GlitchEffect per i Placeable.
/// Anima lo spritesheet e rimane sempre visibile finché il Placeable padre è sbloccato.
/// Non interagisce con GlitchContainer né con gli eventi di drag.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GlitchEffectPlaceable : MonoBehaviour
{
    [Header("Spritesheet")]
    public Texture2D sheet;
    public int columns = 6;
    public int rows    = 1;

    [Header("Animazione")]
    public float fps = 10f;

    private SpriteRenderer sr;
    private Sprite[] frames;
    private int   currentFrame;
    private float timer;
    private Placeable parentPlaceable;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        BuildFrames();

        parentPlaceable = GetComponentInParent<Placeable>();
        if (parentPlaceable != null)
        {
            parentPlaceable.OnUnlocked += OnParentUnlocked;
            sr.enabled = false; // nascosto finché non sbloccato
        }
    }

    void OnDestroy()
    {
        if (parentPlaceable != null)
            parentPlaceable.OnUnlocked -= OnParentUnlocked;
    }

    void OnParentUnlocked()
    {
        sr.enabled = true;
    }

    void Update()
    {
        if (!sr.enabled) return;
        if (frames == null || frames.Length == 0 || fps <= 0f) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer        -= 1f / fps;
            currentFrame  = (currentFrame + 1) % frames.Length;
            sr.sprite     = frames[currentFrame];
        }
    }

    void BuildFrames()
    {
        if (sheet == null) return;

        int total  = columns * rows;
        int frameW = sheet.width  / columns;
        int frameH = sheet.height / rows;

        frames = new Sprite[total];
        for (int i = 0; i < total; i++)
        {
            int col = i % columns;
            int row = rows - 1 - (i / columns);
            frames[i] = Sprite.Create(
                sheet,
                new Rect(col * frameW, row * frameH, frameW, frameH),
                new Vector2(0.5f, -(frameW / 2f) / frameH),
                frameW
            );
        }

        if (sr != null && frames.Length > 0)
            sr.sprite = frames[0];
    }
}
