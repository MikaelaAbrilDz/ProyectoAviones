using UnityEngine;

public class MisilController : MonoBehaviour
{
    [SerializeField] private LayerMask buildingLayer;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;

    private void Start()
    {


        // Destruir el misil después de un tiempo por si no colisiona
        Destroy(gameObject, 10f);
    }
    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * 50;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1, buildingLayer))
        {
            hit.collider.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
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
