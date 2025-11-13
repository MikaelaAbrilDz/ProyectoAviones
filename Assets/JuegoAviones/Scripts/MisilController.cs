using UnityEngine;

public class MisilController : MonoBehaviour
{
    [SerializeField] private LayerMask buildingLayer;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;

    private PlayerControllerLocal playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerControllerLocal>();

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
        
        
       
        
    }

    private void ProcessHit(RaycastHit hit)
    {
          // Verificar si el objeto impactado tiene PlayerControllerLocal
        PlayerControllerLocal targetPlayer = hit.collider.GetComponent<PlayerControllerLocal>();
        if (targetPlayer == null)
        {
            // Si no lo encontramos directamente, buscar en el parent (por si es una parte del avión)
            targetPlayer = hit.collider.GetComponentInParent<PlayerControllerLocal>();
        }

        if (targetPlayer != null && targetPlayer != this.playerController) // Asegurar que no sea el mismo jugador
        {
            if (hit.collider.CompareTag("Alas"))
            {
                Debug.Log("Muerto");
                targetPlayer.DestroyAirplane();
            }
            else if (hit.collider.CompareTag("Cabina"))
            {
                Debug.Log("Muerto");
                targetPlayer.DestroyAirplane();
            }
        }
        else
        {
            Debug.Log($"Objeto impactado no es un jugador enemigo o es el mismo jugador");
        }
    }
}
