using UnityEngine;

public class MisilController : MonoBehaviour
{
    [SerializeField] private LayerMask buildingLayer, playerLayer;
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

        RaycastHit hitBuilding;
        if (Physics.Raycast(transform.position, transform.forward, out hitBuilding, 1, buildingLayer))
        {
            hitBuilding.collider.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
        RaycastHit targetPlayer;
        if (Physics.Raycast(transform.position, transform.forward, out targetPlayer, 1, playerLayer))
        {
            if (targetPlayer.collider != null && targetPlayer.collider.gameObject != this) // Asegurar que no sea el mismo jugador
            {
                if (targetPlayer.collider.CompareTag("Alas"))
                {
                    Debug.Log("Muerto");
                    targetPlayer.collider.gameObject.GetComponentInParent<PlayerControllerLocal>().DestroyAirplane();
                }
                else if (targetPlayer.collider.CompareTag("Cabina"))
                {
                    Debug.Log("Muerto");
                    targetPlayer.collider.gameObject.GetComponentInParent<PlayerControllerLocal>().DestroyAirplane();
                }
            }
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
}
