using UnityEngine;

public class MisilController : MonoBehaviour
{
    [SerializeField] private string buildingLayerName = "Buildings";
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;

    private int buildingLayer;

    private void Start()
    {
        // Obtener el índice de la layer Building
        buildingLayer = LayerMask.NameToLayer(buildingLayerName);


        // Si no se encuentra la layer, usar por defecto
        if (buildingLayer == -1)
        {
            Debug.LogWarning($"Layer '{buildingLayerName}' no encontrada. Asegúrate de que existe en los layers del proyecto.");
        }


        // Destruir el misil después de un tiempo por si no colisiona
        Destroy(gameObject, 10f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si colisionó con un edificio
        if (collision.gameObject.layer == buildingLayer)
        {
            Debug.Log($"Misil impactó con edificio: {collision.gameObject.name}");

            // Destruir el edificio
            Destroy(collision.gameObject);

            //Efecto de explosión
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, transform.rotation);
            }

            // Destruir el misil
            Destroy(gameObject);
        }
        
        
        else if (collision.gameObject.CompareTag("Player"))
        {
            
            Debug.Log("Misil impactó con jugador");

            // Destruir el misil
            Destroy(gameObject);
        }
        
    }
}
