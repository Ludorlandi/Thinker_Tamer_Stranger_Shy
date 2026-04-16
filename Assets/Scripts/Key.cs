using UnityEngine;

public class Key : MonoBehaviour
{
    public Placeable parentPlaceable;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Lock"))
        {
            parentPlaceable.OnKeyEnterLock(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Lock"))
        {
            parentPlaceable.OnKeyExitLock();
        }
    }
}
