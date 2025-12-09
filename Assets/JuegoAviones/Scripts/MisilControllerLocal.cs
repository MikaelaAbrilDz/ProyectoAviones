using UnityEngine;
using Unity.Netcode;

public class MisilControllerLocal : MonoBehaviour
{
    [SerializeField] private LayerMask buildingLayer, playerLayer;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float misilSpeed = 50f;
    public GameObject shooter;

    private void Start()
    {
        // Destruir el misil después de un tiempo por si no colisiona
        Destroy(gameObject, 10f);
    }

    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * misilSpeed;

        // Detección de colisiones por raycast (más preciso)
        RaycastHit hitBuilding;
        if (Physics.Raycast(transform.position, transform.forward, out hitBuilding, 1, buildingLayer))
        {
            HandleBuildingCollision(hitBuilding.collider, hitBuilding.point);
        }

        RaycastHit targetPlayer;
        if (Physics.Raycast(transform.position, transform.forward, out targetPlayer, 1, playerLayer))
        {
            HandlePlayerCollision(targetPlayer.collider, targetPlayer.point);
        }
    }

    private void HandleBuildingCollision(Collider buildingCollider, Vector3 hitPoint)
    {
        Debug.Log($"Misil impactó con edificio: {buildingCollider.name}");

        // Destruir el edificio
        buildingCollider.gameObject.SetActive(false);

        // Efecto de explosión
        SpawnExplosionEffect(hitPoint);

        // Destruir el misil
        DestroyMissile();
    }

    private void HandlePlayerCollision(Collider playerCollider, Vector3 hitPoint)
    {
        if (playerCollider != null)
        {
            if (playerCollider.CompareTag("Alas") || playerCollider.CompareTag("Cabina"))
            {
                Debug.Log("Misil impactó en jugador");


                    PlayerControllerLocal targetPlayer = playerCollider.GetComponentInParent<PlayerControllerLocal>();
                    if (targetPlayer != null)
                    {
                        targetPlayer.DestroyAirplane();
                    }

                // Efecto de explosión
                SpawnExplosionEffect(hitPoint);

                // Destruir el misil
                DestroyMissile();
            }
        }
    }


    private void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, position, Quaternion.identity);
        }
    }

    private void DestroyMissile()
    {
        Destroy(gameObject);
    }

    // Método para configurar el misil desde el sistema de disparo
    public void SetMissileParameters(float speed, LayerMask buildingMask, LayerMask playerMask)
    {
        misilSpeed = speed;
        buildingLayer = buildingMask;
        playerLayer = playerMask;
    }

    // Para debug visual
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
    }
}
