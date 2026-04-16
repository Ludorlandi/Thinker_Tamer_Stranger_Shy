using UnityEngine;

public class Placeable : MonoBehaviour
{
    [Header("References")]
    public GameObject player;

    public Transform keyTransform;

    [Header("Snap Settings")]
    public float snapSpeed = 15f;

    [Tooltip("Layer da controllare per la compenetrazione (di default solo 'Placeable'). NON includere Ground/muri.")]
    public LayerMask overlapCheckMask = 0;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPosition;

    private Transform currentLock = null;
    private bool isAnchored = false;
    private bool isSnapping = false;

    void Start()
    {
        cam = Camera.main;
        startPosition = transform.position;
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

                if (IsOverlappingOtherBlocks())
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
    }

    void OnMouseDown()
    {
        // Se è ancorato, liberalo e permettine il riprelievo
        if (isAnchored)
        {
            currentLock.GetComponent<LockBlock>().SetFree();
            currentLock = null;
            isAnchored = false;
        }

        isDragging = true;
        isSnapping = false;
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

    bool IsOverlappingOtherBlocks()
    {
        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>();
        var results = new System.Collections.Generic.List<Collider2D>();

        var filter = new ContactFilter2D();
        filter.useTriggers = false;

        // Usa la maschera configurata; se non impostata, default = solo layer "Placeable"
        int mask = overlapCheckMask.value != 0
            ? overlapCheckMask.value
            : LayerMask.GetMask("Placeable");
        filter.useLayerMask = true;
        filter.layerMask = mask;

        foreach (var col in ownColliders)
        {
            if (col.isTrigger) continue;

            results.Clear();
            col.Overlap(filter, results);

            foreach (var hit in results)
            {
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