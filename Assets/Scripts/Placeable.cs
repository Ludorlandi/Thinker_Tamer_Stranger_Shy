using UnityEngine;

public class Placeable : MonoBehaviour
{
    [Header("References")]
    public GameObject player;

    public Transform keyTransform;

    [Header("Unlock")]
    [Tooltip("Il tipo di questo Placeable. Deve corrispondere al PlaceableUnlockItem nella scena.")]
    public PlaceableTypeSO placeableType;

    [Tooltip("Tinta applicata quando il Placeable è bloccato (provvisoria).")]
    public Color lockedTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Snap Settings")]
    public float snapSpeed = 15f;

    [Tooltip("Raggio entro cui cercare un LockBlock al rilascio, anche senza sovrapposizione diretta.")]
    public float proximitySnapRadius = 0.5f;

    [Tooltip("Layer da controllare per la compenetrazione. Di default tutto tranne Player. Rimuovi layer che non devono bloccare il piazzamento.")]
    public LayerMask overlapCheckMask = ~0 & ~(1 << 9); // tutto tranne Player[9]

    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPosition;

    private Transform currentLock = null;
    private bool isAnchored = false;
    private bool isSnapping = false;

    [Header("Idle Bob")]
    [Tooltip("Quanto si muove su e giù (in unità Unity). Es: 0.05 = movimento sottile, 0.15 = molto evidente")]
    [Range(0f, 0.3f)]
    public float bobAltezza = 0.08f;

    [Tooltip("Quante oscillazioni al secondo. Es: 1 = lento e morbido, 3 = rapido e nervoso")]
    [Range(0.5f, 5f)]
    public float bobVelocita = 1.8f;

    [Tooltip("Gradi di inclinazione durante l'oscillazione. Es: 2 = appena percettibile, 8 = molto marcato")]
    [Range(0f, 15f)]
    public float bobInclinazione = 3f;

    private bool isUnlocked = false;
    private SpriteRenderer[] spriteRenderers;
    private float bobPhase = 0f;
    private Vector3 visualCenterOffset;

    void Start()
    {
        cam = Camera.main;
        startPosition = transform.position;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // Calcola il centro visivo reale dai collider fisici dei blocchi figli
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            if (col.isTrigger) continue;
            if (first) { bounds = col.bounds; first = false; }
            else bounds.Encapsulate(col.bounds);
        }
        visualCenterOffset = first ? Vector3.zero : bounds.center - transform.position;

        // Controlla se già sbloccato (es. stanza caricata dopo aver già raccolto l'item)
        if (PlaceableUnlockManager.Instance != null && PlaceableUnlockManager.Instance.IsUnlocked(placeableType))
            SetUnlocked();
        else
            ApplyLockedVisual();
    }

    void Update()
    {
        if (isSnapping && !isDragging)
        {
            if (currentLock == null || keyTransform == null)
            {
                isSnapping = false;
                transform.position = startPosition;
                return;
            }

            // Calcola la posizione target: sposta il piazzabile
            // in modo che la chiave coincida con la serratura
            Vector3 keyOffset = keyTransform.position - transform.position;
            Vector3 targetPos = currentLock.position - keyOffset;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                Time.deltaTime * snapSpeed
            );

            // Quando è abbastanza vicino, controlla compenetrazione poi blocca
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                transform.position = targetPos;
                // Forza Unity ad aggiornare i collider alla nuova posizione prima del check
                Physics2D.SyncTransforms();

                if (IsOverlappingOtherBlocks(currentLock))
                {
                    // Compenetrazione: annulla ancoraggio e torna all'inizio
                    currentLock.GetComponent<LockBlock>().SetFree();
                    currentLock = null;
                    isSnapping = false;
                    transform.position = startPosition;
                }
                else
                {
                    isSnapping = false;
                    isAnchored = true;
                }
            }
        }

        // Oscillazione idle: attiva solo se sbloccato, fermo, non ancorato
        if (isUnlocked && !isDragging && !isSnapping && !isAnchored)
        {
            bobPhase += Time.deltaTime * bobVelocita * Mathf.PI * 2f;
            float sine = Mathf.Sin(bobPhase);
            float angle = sine * bobInclinazione;

            // Il centro visivo sale/scende col bob
            Vector3 worldCenter = startPosition + visualCenterOffset + new Vector3(0f, sine * bobAltezza, 0f);
            // Il root si posiziona in modo che la rotazione avvenga attorno al centro visivo
            transform.position = worldCenter - (Quaternion.Euler(0f, 0f, angle) * visualCenterOffset);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (!isDragging && !isSnapping)
        {
            // Resetta rotazione quando ancorato o bloccato
            transform.rotation = Quaternion.identity;
        }
    }

    void OnMouseDown()
    {
        if (!isUnlocked) return;

        // Se è ancorato, liberalo e permettine il riprelievo
        if (isAnchored)
        {
            currentLock.GetComponent<LockBlock>().SetFree();
            currentLock = null;
            isAnchored = false;
        }

        isDragging = true;
        isSnapping = false;
        transform.rotation = Quaternion.identity;
        offset = transform.position - GetMouseWorldPos();

        if (player != null)
            player.SetActive(false);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        transform.position = GetMouseWorldPos() + offset;
    }

    void OnMouseUp()
    {
        isDragging = false;

        // Se non c'è un lock rilevato dal trigger, cerca il più vicino nel raggio
        if (currentLock == null && keyTransform != null)
        {
            currentLock = FindNearestFreeLock();
        }

        if (currentLock != null)
        {
            isSnapping = true;
            // Occupa la serratura
            currentLock.GetComponent<LockBlock>().SetOccupied(this);
        }
        else
        {
            transform.position = startPosition;
        }

        if (player != null)
        {
            player.SetActive(true);
            player.transform.position = Checkpoint.GetActivePosition();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public void OnKeyEnterLock(Transform lockTransform)
    {
        if (lockTransform.GetComponent<LockBlock>().IsOccupied()) return;
        currentLock = lockTransform;
    }

    public void OnKeyExitLock()
    {
        // Non azzerare currentLock se stiamo già snappando verso di esso
        if (!isAnchored && !isSnapping)
            currentLock = null;
    }

    public void SetUnlocked()
    {
        isUnlocked = true;
        foreach (var sr in spriteRenderers)
            sr.color = Color.white;
    }

    void ApplyLockedVisual()
    {
        foreach (var sr in spriteRenderers)
            sr.color = lockedTint;
    }

    Transform FindNearestFreeLock()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(keyTransform.position, proximitySnapRadius);
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Lock")) continue;
            LockBlock lb = hit.GetComponent<LockBlock>();
            if (lb == null || lb.IsOccupied()) continue;

            float dist = Vector2.Distance(keyTransform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit.transform;
            }
        }
        return nearest;
    }

    bool IsOverlappingOtherBlocks(Transform lockTransform = null)
    {
        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>();

        // Raccoglie tutti i GameObject nella gerarchia del Blocco Lock da ignorare
        var excluded = new System.Collections.Generic.HashSet<GameObject>();
        if (lockTransform != null)
        {
            Transform t = lockTransform;
            while (t != null) { excluded.Add(t.gameObject); t = t.parent; }
        }

        // Se la mask è 0 (non configurata), usa tutto tranne Player
        int mask = overlapCheckMask.value != 0
            ? overlapCheckMask.value
            : ~(1 << LayerMask.NameToLayer("Player"));

        foreach (var col in ownColliders)
        {
            if (col.isTrigger) continue;
            BoxCollider2D box = col as BoxCollider2D;
            if (box == null) continue;

            Vector2 worldCenter = (Vector2)col.transform.TransformPoint(box.offset);
            Vector2 worldSize = new Vector2(
                box.size.x * Mathf.Abs(col.transform.lossyScale.x),
                box.size.y * Mathf.Abs(col.transform.lossyScale.y)
            );
            float angle = col.transform.eulerAngles.z;
            Vector2 checkSize = worldSize * 0.9f;

            Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, checkSize, angle, mask);

            foreach (var hit in hits)
            {
                if (hit.isTrigger) continue;
                if (excluded.Contains(hit.gameObject)) continue;
                bool isOwn = System.Array.Exists(ownColliders, c => c == hit);
                if (!isOwn) return true;
            }
        }
        return false;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(mousePos);
    }
}