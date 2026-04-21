using UnityEngine;

public class Placeable : MonoBehaviour
{
    public static event System.Action OnAnyDragStart;
    public static event System.Action OnAnyDragEnd;

    [Header("References")]
    public GameObject player;

    public Transform keyTransform;

    [Tooltip("Seconda chiave (opzionale). Se assegnata, ENTRAMBE le chiavi devono trovare un LockBlock libero per ancorarsi.")]
    public Transform keyTransform2;

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
    private Transform currentLock2 = null;
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

    [Header("Hover Effect")]
    [Tooltip("Scala raggiunta quando il mouse è sopra. Es: 1.15 = 15% più grande")]
    [Range(1f, 1.5f)]
    public float hoverScale = 1.15f;

    [Tooltip("Velocità con cui la scala cresce/decresce durante l'hover")]
    [Range(1f, 20f)]
    public float hoverScaleSpeed = 10f;

    [Tooltip("Quanto aumentano ampiezza e inclinazione del bob durante l'hover")]
    [Range(1f, 4f)]
    public float hoverBobMultiplier = 2f;

    public bool IsAnchored => isAnchored;

    private bool isUnlocked = false;
    private SpriteRenderer[] spriteRenderers;
    private float bobPhase = 0f;
    private Vector3 visualCenterOffset;
    private bool isHovered = false;
    private float currentScaleFactor = 1f;

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
            bool dualKey = keyTransform2 != null;

            if (currentLock == null || keyTransform == null || (dualKey && currentLock2 == null))
            {
                // Lock perso durante lo snap: annulla tutto
                currentLock?.GetComponent<LockBlock>()?.SetFree();
                currentLock2?.GetComponent<LockBlock>()?.SetFree();
                currentLock = null;
                currentLock2 = null;
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

                if (IsOverlappingOtherBlocks(currentLock, currentLock2))
                {
                    // Compenetrazione: annulla ancoraggio e torna all'inizio
                    currentLock.GetComponent<LockBlock>().SetFree();
                    currentLock2?.GetComponent<LockBlock>()?.SetFree();
                    currentLock = null;
                    currentLock2 = null;
                    isSnapping = false;
                    transform.position = startPosition;
                    SoundManager.Instance?.PlaySFX(SoundID.PlaceableFailedSnap);
                }
                else
                {
                    isSnapping = false;
                    isAnchored = true;
                }
            }
        }

        // Hover scale: lerp verso hoverScale se hovering, altrimenti torna a 1
        bool canHover = isUnlocked && !isAnchored && !isDragging && !isSnapping;
        float targetScaleFactor = (canHover && isHovered) ? hoverScale : 1f;
        currentScaleFactor = Mathf.Lerp(currentScaleFactor, targetScaleFactor, Time.deltaTime * hoverScaleSpeed);
        transform.localScale = Vector3.one * currentScaleFactor;

        // Oscillazione idle: attiva solo se sbloccato, fermo, non ancorato
        if (isUnlocked && !isDragging && !isSnapping && !isAnchored)
        {
            float mult = (isHovered) ? hoverBobMultiplier : 1f;
            bobPhase += Time.deltaTime * bobVelocita * Mathf.PI * 2f;
            float sine = Mathf.Sin(bobPhase);
            float angle = sine * bobInclinazione * mult;

            Vector3 worldCenter = startPosition + visualCenterOffset + new Vector3(0f, sine * bobAltezza * mult, 0f);
            transform.position = worldCenter - (Quaternion.Euler(0f, 0f, angle) * visualCenterOffset);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (!isDragging && !isSnapping)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    void OnMouseDown()
    {
        if (!isUnlocked)
        {
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableLockedClick);
            return;
        }

        // Se è ancorato, liberalo e permettine il riprelievo
        if (isAnchored)
        {
            currentLock?.GetComponent<LockBlock>()?.SetFree();
            currentLock2?.GetComponent<LockBlock>()?.SetFree();
            currentLock = null;
            currentLock2 = null;
            isAnchored = false;
        }

        isDragging = true;
        isSnapping = false;
        transform.rotation = Quaternion.identity;
        offset = transform.position - GetMouseWorldPos();
        SoundManager.Instance?.PlaySFX(SoundID.PlaceableDragStart);
        OnAnyDragStart?.Invoke();

        if (player != null)
            player.SetActive(false);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        transform.position = GetMouseWorldPos() + offset;
    }

    void OnMouseEnter()
    {
        if (isUnlocked && !isAnchored)
            isHovered = true;
    }

    void OnMouseExit()
    {
        isHovered = false;
    }

    void OnMouseOver()
    {
        // Tasto destro su un Placeable ancorato → torna alla posizione di partenza
        if (isAnchored && Input.GetMouseButtonDown(1))
        {
            currentLock?.GetComponent<LockBlock>()?.SetFree();
            currentLock2?.GetComponent<LockBlock>()?.SetFree();
            currentLock = null;
            currentLock2 = null;
            isAnchored = false;
            transform.position = startPosition;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            currentScaleFactor = 1f;
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableReturn);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        OnAnyDragEnd?.Invoke();

        // Se non c'è un lock rilevato dal trigger, cerca il più vicino nel raggio
        if (currentLock == null && keyTransform != null)
            currentLock = FindNearestFreeLock(keyTransform);

        bool dualKey = keyTransform2 != null;
        if (dualKey && currentLock2 == null && keyTransform2 != null)
            currentLock2 = FindNearestFreeLock(keyTransform2);

        bool canSnap = currentLock != null && (!dualKey || currentLock2 != null);

        if (canSnap)
        {
            isSnapping = true;
            currentLock.GetComponent<LockBlock>().SetOccupied(this);
            if (dualKey) currentLock2.GetComponent<LockBlock>().SetOccupied(this);
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableAnchored);
        }
        else
        {
            currentLock?.GetComponent<LockBlock>()?.SetFree();
            currentLock2?.GetComponent<LockBlock>()?.SetFree();
            currentLock = null;
            currentLock2 = null;
            transform.position = startPosition;
            SoundManager.Instance?.PlaySFX(SoundID.PlaceableReturn);
        }

        if (player != null)
        {
            player.SetActive(true);
            player.transform.position = Checkpoint.GetActivePosition();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public void OnKeyEnterLock(Transform lockTransform, Key sourceKey)
    {
        if (lockTransform.GetComponent<LockBlock>().IsOccupied()) return;

        if (keyTransform2 == null || sourceKey.transform == keyTransform)
            currentLock = lockTransform;
        else
            currentLock2 = lockTransform;
    }

    public void OnKeyExitLock(Key sourceKey)
    {
        // Non azzerare currentLock se stiamo già snappando verso di esso
        if (isAnchored || isSnapping) return;

        if (keyTransform2 == null || sourceKey.transform == keyTransform)
            currentLock = null;
        else
            currentLock2 = null;
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

    Transform FindNearestFreeLock(Transform fromKey)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(fromKey.position, proximitySnapRadius);
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

    bool IsOverlappingOtherBlocks(Transform lockTransform = null, Transform lockTransform2 = null)
    {
        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>();

        // Raccoglie tutti i GameObject nelle gerarchie dei LockBlock da ignorare
        var excluded = new System.Collections.Generic.HashSet<GameObject>();
        void ExcludeHierarchy(Transform t) { while (t != null) { excluded.Add(t.gameObject); t = t.parent; } }
        if (lockTransform != null) ExcludeHierarchy(lockTransform);
        if (lockTransform2 != null) ExcludeHierarchy(lockTransform2);

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
                // Un oggetto che contiene un LockBlock è uno slot di piazzamento, non un ostacolo
                if (hit.GetComponentInChildren<LockBlock>() != null) continue;
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