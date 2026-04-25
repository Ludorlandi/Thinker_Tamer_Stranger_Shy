using System.Collections.Generic;
using UnityEngine;

public class CollezionabileManager : MonoBehaviour
{
    public static CollezionabileManager Instance { get; private set; }

    private int totalCount;
    private int collectedCount;
    private HashSet<CollezionabileType> collectedTypes = new HashSet<CollezionabileType>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        totalCount     = FindObjectsByType<Collezionabile>(FindObjectsSortMode.None).Length;
        collectedCount = 0;
    }

    // Chiamato da Collezionabile.cs al momento della raccolta
    public void OnCollected(CollezionabileType tipo)
    {
        collectedTypes.Add(tipo);
        collectedCount++;
        CollezionabileUnlockScreen.Instance?.Show(tipo);
    }

    public bool IsCollected(CollezionabileType tipo) => collectedTypes.Contains(tipo);
    public int  DecryptedCount => collectedTypes.Count;
    public int  RedactedCount  => 4 - collectedTypes.Count;
    public int  Collected      => collectedCount;
    public int  Total          => totalCount;
}
