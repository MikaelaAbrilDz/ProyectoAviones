using UnityEngine;
using Unity.Netcode;

public class MisilControllerOnline : NetworkBehaviour
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
        Invoke(nameof(DestroyMissile), 10f);
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
        if (playerCollider != null && playerCollider.gameObject.GetComponentInParent<PlayerControllerOnline>().gameObject != shooter)
        {
            if (playerCollider.CompareTag("Alas") || playerCollider.CompareTag("Cabina"))
            {
                Debug.Log("Misil impactó en jugador");


                    PlayerControllerOnline targetPlayer = playerCollider.GetComponentInParent<PlayerControllerOnline>();
                    if (targetPlayer != null)
                    {
                        targetPlayer.TakeDamage(999);
                        Debug.Log("Jugador recibió daño de misil. Vida restante: " + targetPlayer.life);
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
            if (shooter.GetComponent<PlayerControllerOnline>().IsServer)
            {
                SpawnExplosionEffectClientRpc(position);
                return;
            }
            SpawnExplosionEffectServerRpc(position);
        }
    }
    [ClientRpc(RequireOwnership = false)]
    private void SpawnExplosionEffectClientRpc(Vector3 position)
    {
        Instantiate(explosionEffect, position, Quaternion.identity);
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnExplosionEffectServerRpc(Vector3 position)
    {
        SpawnExplosionEffectClientRpc(position);
    }

    private void DestroyMissile()
    {
        if (shooter.GetComponent<PlayerControllerOnline>().IsServer)
        {
            DestroyMissileClientRpc();
            return;
        }
        DestroyMissileServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void DestroyMissileServerRpc()
    {
        DestroyMissileClientRpc();
    }
    [ClientRpc(RequireOwnership = false)]
    private void DestroyMissileClientRpc()
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