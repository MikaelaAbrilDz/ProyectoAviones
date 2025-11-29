using UnityEngine;
using Unity.Netcode;

public class MisilController : MonoBehaviour
{
    [SerializeField] private LayerMask buildingLayer, playerLayer;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float misilSpeed = 50f;

    private PlayerControllerLocal playerControllerLocal;
    private PlayerControllerOnline playerControllerOnline;
    private bool isOnlineMode = false;
    private NetworkObject networkObject;

    private void Start()
    {
        // Detectar si estamos en modo online o local
        playerControllerLocal = GetComponent<PlayerControllerLocal>();
        playerControllerOnline = GetComponent<PlayerControllerOnline>();
        networkObject = GetComponent<NetworkObject>();

        isOnlineMode = playerControllerOnline != null && networkObject != null;

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
        if (IsServerOrLocal())
        {
            Destroy(buildingCollider.gameObject);
        }

        // Efecto de explosión
        SpawnExplosionEffect(hitPoint);

        // Destruir el misil
        DestroyMissile();
    }

    private void HandlePlayerCollision(Collider playerCollider, Vector3 hitPoint)
    {
        if (playerCollider != null && !IsSamePlayer(playerCollider.gameObject))
        {
            if (playerCollider.CompareTag("Alas") || playerCollider.CompareTag("Cabina"))
            {
                Debug.Log("Misil impactó en jugador");

                // Aplicar daño según el modo
                if (isOnlineMode)
                {
                    PlayerControllerOnline targetPlayer = playerCollider.GetComponentInParent<PlayerControllerOnline>();
                    if (targetPlayer != null)
                    {
                        targetPlayer.TakeDamage(999);
                    }
                }
                else
                {
                    PlayerControllerLocal targetPlayer = playerCollider.GetComponentInParent<PlayerControllerLocal>();
                    if (targetPlayer != null)
                    {
                        targetPlayer.DestroyAirplane();
                    }
                }

                // Efecto de explosión
                SpawnExplosionEffect(hitPoint);

                // Destruir el misil
                DestroyMissile();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si colisionó con un edificio
        if (((1 << collision.gameObject.layer) & buildingLayer) != 0)
        {
            HandleBuildingCollision(collision.collider, collision.contacts[0].point);
        }
        // Verificar si colisionó con un jugador
        else if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            if (!IsSamePlayer(collision.gameObject))
            {
                HandlePlayerCollision(collision.collider, collision.contacts[0].point);
            }
        }
    }

    private bool IsSamePlayer(GameObject hitObject)
    {
        if (isOnlineMode)
        {
            PlayerControllerOnline hitPlayer = hitObject.GetComponentInParent<PlayerControllerOnline>();
            return hitPlayer != null && hitPlayer == playerControllerOnline;
        }
        else
        {
            PlayerControllerLocal hitPlayer = hitObject.GetComponentInParent<PlayerControllerLocal>();
            return hitPlayer != null && hitPlayer == playerControllerLocal;
        }
    }

    private bool IsServerOrLocal()
    {
        if (isOnlineMode)
        {
            return networkObject != null && networkObject.IsSpawned && (networkObject.IsOwnedByServer || !NetworkManager.Singleton.IsClient);
        }
        return true; // En local siempre es true
    }

    private bool IsServer()
    {
        if (isOnlineMode)
        {
            return networkObject != null && networkObject.IsSpawned && networkObject.IsOwnedByServer;
        }
        return true; // En local simular ser servidor
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
        if (isOnlineMode && networkObject != null && networkObject.IsSpawned)
        {
            if (networkObject.IsOwnedByServer)
            {
                networkObject.Despawn();
            }
        }
        else
        {
            Destroy(gameObject);
        }
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