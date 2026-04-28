using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // ── Stato globale ──────────────────────────────────────────────────────────
    private static Checkpoint      activeCheckpoint;
    private static Material        _sharedMat;           // materiale condiviso con saturation

    [Header("Spawn iniziale")]
    public bool isDefault = false;

    [Header("Touch Effect")]
    [Tooltip("Sprite dell'animazione 'touch' (CheckpointTouch-Sheet).")]
    public Sprite[] touchSprites;

    [Tooltip("Scala raggiunta al tocco (es. 1.5 = 50% più grande).")]
    [Range(1f, 3f)] public float touchScale = 1.5f;

    [Tooltip("Durata in secondi dell'effetto touch prima di tornare allo stato idle.")]
    [Range(0.1f, 10f)] public float touchDuration = 2f;

    [Tooltip("Velocità con cui la scala cresce/decresce (lerp speed).")]
    [Range(1f, 20f)] public float scaleSpeed = 8f;

    // ── Internals ──────────────────────────────────────────────────────────────
    private SpriteRenderer  _sr;
    private FloorTileAnimator _anim;
    private Sprite[]        _idleSprites;
    private Vector3         _baseScale;
    private Coroutine       _touchRoutine;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        _sr        = GetComponent<SpriteRenderer>();
        _anim      = GetComponent<FloorTileAnimator>();
        _baseScale = transform.localScale;
        if (_anim != null) _idleSprites = _anim.sprites;

        // Materiale condiviso con shader saturation
        if (_sharedMat == null)
        {
            var shader = Shader.Find("Custom/SpriteWithSaturation");
            if (shader != null) _sharedMat = new Material(shader) { name = "Checkpoint_Shared" };
        }
        if (_sharedMat != null && _sr != null)
            _sr.sharedMaterial = _sharedMat;
    }

    void Start()
    {
        if (isDefault)
        {
            activeCheckpoint = this;
            SetAppearance(true);
        }
        else
        {
            SetAppearance(false);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public static Vector3 GetActivePosition()
    {
        return activeCheckpoint != null ? activeCheckpoint.transform.position : Vector3.zero;
    }

    // ── Trigger ────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Disattiva il precedente
        if (activeCheckpoint != null && activeCheckpoint != this)
            activeCheckpoint.SetAppearance(false);

        activeCheckpoint = this;
        SetAppearance(true);
        PlayTouchEffect();
        SoundManager.Instance?.PlaySFX(SoundID.CheckpointTouch);
    }

    // ── Appearance ─────────────────────────────────────────────────────────────

    void SetAppearance(bool active)
    {
        // Animazione: abilita/disabilita il componente
        if (_anim != null)
        {
            _anim.enabled = active;
            // Torna al primo frame quando disabilitato
            if (!active && _idleSprites != null && _idleSprites.Length > 0 && _sr != null)
                _sr.sprite = _idleSprites[0];
        }

        // Saturazione via MaterialPropertyBlock (nessuna istanza extra di materiale)
        if (_sr != null)
        {
            if (_sharedMat != null)
            {
                var mpb = new MaterialPropertyBlock();
                _sr.GetPropertyBlock(mpb);
                mpb.SetFloat("_Saturation", active ? 1f : 0f);
                _sr.SetPropertyBlock(mpb);
            }
            else
            {
                // Fallback quando lo shader non è disponibile in build:
                // usa il colore del SpriteRenderer per simulare il grigio
                _sr.color = active ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            }
        }
    }

    // ── Touch effect ───────────────────────────────────────────────────────────

    void PlayTouchEffect()
    {
        if (touchSprites == null || touchSprites.Length == 0 || _anim == null) return;
        if (_touchRoutine != null) StopCoroutine(_touchRoutine);
        _touchRoutine = StartCoroutine(TouchRoutine());
    }

    IEnumerator TouchRoutine()
    {
        _anim.sprites = touchSprites;

        Vector3 targetBig = _baseScale * touchScale;
        while (Vector3.Distance(transform.localScale, targetBig) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetBig, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = targetBig;

        yield return new WaitForSeconds(touchDuration);

        _anim.sprites = _idleSprites;

        while (Vector3.Distance(transform.localScale, _baseScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = _baseScale;
        _touchRoutine = null;
    }
}
