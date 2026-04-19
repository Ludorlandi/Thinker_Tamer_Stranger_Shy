using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FloorTileAnimator : MonoBehaviour
{
    public Sprite[] sprites;
    [Range(0.1f, 10f)] public float frameDuration = 2f;

    private SpriteRenderer _sr;
    private float _lastRealtime;
    private float _accumulated;
    private int _index;

    void OnEnable()
    {
        _sr = GetComponent<SpriteRenderer>();
        _lastRealtime = Time.realtimeSinceStartup;
        _accumulated = 0f;
        _index = 0;
        if (_sr != null && sprites != null && sprites.Length > 0)
            _sr.sprite = sprites[0];
    }

    void Update()
    {
        if (_sr == null || sprites == null || sprites.Length == 0) return;

        float now = Time.realtimeSinceStartup;
        _accumulated += now - _lastRealtime;
        _lastRealtime = now;

        if (_accumulated >= frameDuration)
        {
            _accumulated -= frameDuration;
            _index = (_index + 1) % sprites.Length;
            _sr.sprite = sprites[_index];
        }
    }
}
