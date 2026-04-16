using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private static Checkpoint activeCheckpoint;

    [Header("Spawn iniziale")]
    public bool isDefault = false;

    void Start()
    {
        if (isDefault)
            activeCheckpoint = this;
    }

    public static Vector3 GetActivePosition()
    {
        if (activeCheckpoint != null)
            return activeCheckpoint.transform.position;
        return Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            activeCheckpoint = this;
    }
}
