using UnityEngine;

public class DomeBoundary : MonoBehaviour
{
    [Header("Dome Settings")]
    public Transform domeCenter;    // Empty central
    public float domeRadius = 200f; // Radio de la cúpula

    [Header("Return Settings")]
    public float turnSpeed = 2f;    // Velocidad de giro

    void Update()
    {
        if (domeCenter == null) return;

        float dist = Vector3.Distance(transform.position, domeCenter.position);

        // Si el avión sale del radio...
        if (dist > domeRadius)
        {
            // Apuntar al centro
            Vector3 dir = (domeCenter.position - transform.position).normalized;

            // Crear rotación objetivo
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

            // Girar suavemente hacia el centro
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                turnSpeed * Time.deltaTime
            );
        }
    }
}

