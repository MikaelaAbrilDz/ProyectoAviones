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
        
        // Detener el muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
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
    PlayMuzzleFlash();
    
    // Resto del código del láser...
    RaycastHit hit;
    Vector3 startPos = firePoint.position;
    Vector3 endPos;
    
    bool hasHit = Physics.Raycast(startPos, firePoint.forward, out hit, laserRange, hitLayers);

    if (hasHit)
    {
        endPos = hit.point;
        ProcessHit(hit);
    }
    else
    {
        endPos = startPos + firePoint.forward * laserRange;
    }
    
    StartCoroutine(ShowLaserBriefly(startPos, endPos));
}
        
        // Aquí puedes agregar sonido
        // if (laserSound != null && audioSource != null)
        // {
        //     audioSource.PlayOneShot(laserSound);
        // }
private void PlayMuzzleFlash()
{
    if (muzzleFlash == null)
    {
        Debug.LogError("MuzzleFlash no asignado!");
        return;
    }

    // Asegurar que el GameObject esté activo
    if (!muzzleFlash.gameObject.activeInHierarchy)
        muzzleFlash.gameObject.SetActive(true);

    // SOLUCIÓN: Usar Simulate y Play juntos
    muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    muzzleFlash.Simulate(0, true, true); // Resetear al inicio
    muzzleFlash.Play(true); // Forzar play incluyendo hijos
    
    Debug.Log($"MuzzleFlash playing: {muzzleFlash.isPlaying}");
    Debug.Log($"Particle count: {muzzleFlash.particleCount}");
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
            hit.collider.gameObject.GetComponent<PlayerControllerLocal>().DestroyAirplane();
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
