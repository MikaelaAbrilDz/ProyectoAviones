using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class MisilControllerOnline : NetworkBehaviour
{
    private ulong missileOwnerId;
    private NetworkObject ownerNetObject;
    [SerializeField] private LayerMask buildingLayer, playerLayer;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject explosionEffect;
    private float misilSpeed = 150f;
    public GameObject shooter;

    private void Start()
    {
      

       

        shooter = Physics.OverlapSphere(transform.position, 2, playerLayer)[0].transform.parent.gameObject;
        //ownerNetObject = shooter.GetComponent<NetworkObject>();
        missileOwnerId = shooter.GetComponent<NetworkObject>().OwnerClientId;
        // Destruir el misil despuйs de un tiempo por si no colisiona
        Invoke(nameof(DestroyMissile), 10f);
    }

    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * misilSpeed;

        // Detecciуn de colisiones por raycast (mбs preciso)
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
        Debug.Log($"Misil impactу con edificio: {buildingCollider.name}");

        // Destruir el edificio
        buildingCollider.GetComponent<BuildingManagerOnline>().Collapse();

        // Efecto de explosiуn
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
                Debug.Log("Misil impactу en jugador");

                Debug.Log("HOLAT2");
                PlayerControllerOnline targetPlayer = playerCollider.GetComponentInParent<PlayerControllerOnline>();
                Debug.Log("HOLAT3");
                if (targetPlayer != null)
                    {
                    Debug.Log("HOLAT4");
                    targetPlayer.TakeDamage(999, missileOwnerId);
                        Debug.Log(missileOwnerId);
                    
                }
                    else
                {
                    Debug.Log("HOLATT");
                }


                // Efecto de explosiуn
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

    // Mйtodo para configurar el misil desde el sistema de disparo
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