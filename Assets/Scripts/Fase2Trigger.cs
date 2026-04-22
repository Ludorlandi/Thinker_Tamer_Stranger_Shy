using UnityEngine;

public class Fase2Trigger : MonoBehaviour
{
    [Tooltip("Posizione dove spostare la camera quando il player entra nel trigger.")]
    public Transform cameraPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (cameraPosition == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 target = cameraPosition.position;
        target.z = mainCam.transform.position.z;
        mainCam.transform.position = target;
    }
}
