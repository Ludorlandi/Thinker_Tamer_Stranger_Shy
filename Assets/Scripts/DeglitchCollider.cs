using UnityEngine;

public class DeglitchCollider : MonoBehaviour
{
    [Tooltip("GameObject da disattivare (con tutti i suoi figli) quando il player entra nel trigger.")]
    public GameObject targetToDisable;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DeglitchCollider] Trigger su {gameObject.name} — colpito da: {other.gameObject.name} (tag: {other.tag})");

        if (!other.CompareTag("Player")) return;
        if (targetToDisable == null)
        {
            Debug.LogWarning($"[DeglitchCollider] targetToDisable non assegnato su {gameObject.name}!");
            return;
        }

        Debug.Log($"[DeglitchCollider] Disattivo: {targetToDisable.name}");
        targetToDisable.SetActive(false);
    }
}
