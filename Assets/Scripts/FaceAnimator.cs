using UnityEngine;

/// <summary>
/// Riproduce in loop uno spritesheet per la faccia del player.
/// La faccia non deve ruotare: assicurarsi che questo GameObject
/// sia figlio diretto del root del player (NON del corpo).
/// </summary>
public class FaceAnimator : MonoBehaviour
{
    [Header("Spritesheet")]
    [Tooltip("Texture dello spritesheet della faccia (es. FaceBlinking_Sheet).")]
    public Texture2D sheet;

    [Tooltip("Numero di colonne dello spritesheet.")]
    public int columns = 12;

    [Tooltip("Numero di righe dello spritesheet.")]
    public int rows    = 12;

    [Header("Animazione")]
    [Tooltip("Frame al secondo di riproduzione.")]
    public float fps = 12f;

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
            // Le texture Unity hanno l'origine in basso a sinistra,
            // ma gli sheet sono letti dall'alto: invertiamo la riga.
            int row = rows - 1 - (i / columns);

            frames[i] = Sprite.Create(
                sheet,
                new Rect(col * frameW, row * frameH, frameW, frameH),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: frameW   // PPU = larghezza frame → 1 unità Unity
            );
        }

        if (sr != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || fps <= 0f) return;

        timer += Time.deltaTime;
        float frameDuration = 1f / fps;

        if (timer >= frameDuration)
        {
            timer        -= frameDuration;
            currentFrame  = (currentFrame + 1) % frames.Length;
            sr.sprite     = frames[currentFrame];
        }
    }
}
