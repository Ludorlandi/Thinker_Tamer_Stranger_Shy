using System.Collections;
using UnityEngine;

public class PlaceableUnlockItem : MonoBehaviour
{
    [Tooltip("Il tipo di Placeable che questo oggetto sblocca.")]
    public PlaceableTypeSO typeToUnlock;

    [Header("Oscillazione")]
    public float oscillateAmplitude = 0.18f;
    [Range(0.5f, 4f)]
    public float oscillateSpeed     = 1.8f;

    [Header("Rotazione")]
    public float rotateAngle    = 90f;
    public float rotateInterval = 2f;
    public float rotateDuration = 0.4f;

    private Vector3 originPos;
    private float   phase;

    void Start()
    {
        originPos = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
        StartCoroutine(RotateLoop());
    }

    void Update()
    {
        phase += Time.deltaTime * oscillateSpeed * Mathf.PI * 2f;
        transform.position = originPos + new Vector3(0f, Mathf.Sin(phase) * oscillateAmplitude, 0f);
    }

    IEnumerator RotateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(rotateInterval);

            float elapsed   = 0f;
            float startZ    = transform.eulerAngles.z;
            float targetZ   = startZ + rotateAngle;

            while (elapsed < rotateDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / rotateDuration);
                float z = Mathf.LerpAngle(startZ, targetZ, t);
                transform.rotation = Quaternion.Euler(0f, 0f, z);
                yield return null;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, targetZ);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[UnlockItem] Trigger su '{gameObject.name}' — colpito da: '{other.gameObject.name}' tag: '{other.tag}'");
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[UnlockItem] Player rilevato. Manager: {PlaceableUnlockManager.Instance != null}, typeToUnlock: {typeToUnlock}");
        if (PlaceableUnlockManager.Instance == null) return;

        PlaceableUnlockManager.Instance.Unlock(typeToUnlock);
        SoundManager.Instance?.PlaySFX(SoundID.PlaceableUnlocked);
        Destroy(gameObject);
    }
}
