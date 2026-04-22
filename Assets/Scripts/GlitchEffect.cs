using UnityEngine;

/// <summary>
/// Anima in loop uno spritesheet per l'effetto glitch laterale del LockBlock.
/// Attacca questo componente a ciascuno dei 4 prefab glitch (Top/Right/Bottom/Left).
/// Lo spritesheet deve avere Read/Write abilitato.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class GlitchEffect : MonoBehaviour
{
    [Header("Spritesheet")]
    [Tooltip("Texture dello spritesheet glitch (Read/Write abilitato).")]
    public Texture2D sheet;

    [Tooltip("Numero di colonne dello spritesheet.")]
    public int columns = 4;

    [Tooltip("Numero di righe dello spritesheet.")]
    public int rows = 1;

    [Header("Animazione")]
    [Tooltip("Frame al secondo.")]
    public float fps = 10f;

    private SpriteRenderer sr;
    private Sprite[] frames;
    private int   currentFrame;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        BuildFrames();
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
                new Vector2(0.5f, -(frameW / 2f) / frameH),  // pivot sotto il bordo: la base dello sprite coincide col bordo esterno del blocco
                frameW                                         // PPU = larghezza frame
            );
        }

        if (sr != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || fps <= 0f) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer        -= 1f / fps;
            currentFrame  = (currentFrame + 1) % frames.Length;
            sr.sprite     = frames[currentFrame];
        }
    }
}
