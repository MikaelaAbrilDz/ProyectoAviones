using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ShootingSystemLocal : MonoBehaviour
{
    [Header("Configuración Láser Metralleta")]
    [SerializeField] private Transform firePoint; // Punto de origen del láser
    [SerializeField] private float fireRate = 10f; // Disparos por segundo
    [SerializeField] private float laserRange = 100f;
    [SerializeField] private float laserDuration = 0.1f;
    [SerializeField] private float damage = 10f;
    
    [Header("Efectos Visuales")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private ParticleSystem muzzleFlash;
    //[SerializeField] private GameObject hitEffect;
    
    //[Header("Audio")]
    //[SerializeField] private AudioClip laserSound;
    //[SerializeField] private AudioClip hitSound;
    
    [Header("Configuración de Layers")]
    [SerializeField] private LayerMask hitLayers = ~0; // Todos los layers por defecto
    
    private AudioSource audioSource;
    private bool isFiring = false;
    private float fireDelay;
    private PlayerControllerOnline playerController;

    private void Start()
    {
        // Obtener referencia al PlayerController
        playerController = GetComponent<PlayerControllerOnline>();
        
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Calcular delay entre disparos
        fireDelay = 1f / fireRate;
        
        // Configurar LineRenderer si existe
        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }
        
        // Verificar que tenemos el firePoint asignado
        if (firePoint == null)
        {
            Debug.LogError("FirePoint no asignado en MachineGunLaser!");
        }
    }

    private void Update()
    {
        // Solo permitir disparar si somos el dueño del objeto
        //if (!IsOwner) return;
        
        // Detectar input de disparo
        if (Input.GetMouseButtonDown(0))
        {
            StartFiring();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopFiring();
        }
    }

    public void StartFiring()
    {
        if (!isFiring)
        {
            isFiring = true;
            StartCoroutine(FiringCoroutine());
            Debug.Log("disparando");
        }
    }

    public void StopFiring()
    {
        isFiring = false;
        StopAllCoroutines();
        
        // Ocultar láser inmediatamente al dejar de disparar
        if (laserLine != null)
        {
            laserLine.enabled = false;
        }
    }

    private IEnumerator FiringCoroutine()
    {
        while (isFiring)
        {
            ShootLaser();
            yield return new WaitForSeconds(fireDelay);
        }
    }

    private void ShootLaser()
    {
        // Solo el cliente que es dueño puede disparar
        //if (IsOwner)
        {
            ShootLaserServerRpc();
        }
    }

    [ServerRpc]
    private void ShootLaserServerRpc()
    {
        // Realizar raycast desde el servidor
        RaycastHit hit;
        Vector3 rayDirection = firePoint.forward;
        bool hasHit = Physics.Raycast(firePoint.position, rayDirection, out hit, laserRange, hitLayers);

        // Procesar el impacto
        if (hasHit)
        {
            ProcessHit(hit);
        }

        // Mostrar efectos en todos los clientes
        ShowLaserEffectsClientRpc(firePoint.position, hasHit ? hit.point : firePoint.position + rayDirection * laserRange, hasHit);
    }

    [ClientRpc]
    private void ShowLaserEffectsClientRpc(Vector3 startPos, Vector3 endPos, bool hasHit)
    {
        // Mostrar efecto de láser
        StartCoroutine(ShowLaserBriefly(startPos, endPos));

        // Efecto de muzzle flash
        if (muzzleFlash != null)
        {
            
            muzzleFlash.Play();
        }

        // Sonido de disparo
        /*if (laserSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(laserSound);
        }*/

        // Efecto de impacto si hubo hit
        /*if (hasHit && hitEffect != null)
        {
            //Instantiate(hitEffect, endPos, Quaternion.LookRotation(hit.point - firePoint.position));
            
            // Sonido de impacto
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }*/
    }

    private IEnumerator ShowLaserBriefly(Vector3 startPos, Vector3 endPos)
    {
        // Mostrar el láser por un breve momento
        if (laserLine != null)
        {
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, endPos);
            laserLine.enabled = true;
            
            yield return new WaitForSeconds(laserDuration);
            
            laserLine.enabled = false;
        }
    }

    private void ProcessHit(RaycastHit hit)
    {
        // Aquí puedes agregar lógica de daño
        Debug.Log($"Láser impactó: {hit.collider.name}");

        // Ejemplo: aplicar daño a objetos con salud
        /* 
        HealthSystem health = hit.collider.GetComponent<HealthSystem>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
        */

       

        // Si es otro avión
        if (hit.collider.CompareTag("Player"))
        {
            // Lógica para dañar otros jugadores
            Debug.Log("¡Avión enemigo impactado!");
        }
    }

    // Método para cambiar la configuración del láser en tiempo de ejecución
    public void SetLaserConfig(float newFireRate, float newRange, float newDamage)
    {
        fireRate = newFireRate;
        fireDelay = 1f / fireRate;
        laserRange = newRange;
        damage = newDamage;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Debug visual en el editor
    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * laserRange);
        }
    }
}
