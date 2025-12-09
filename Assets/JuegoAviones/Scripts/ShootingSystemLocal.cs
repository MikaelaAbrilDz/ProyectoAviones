using UnityEngine;
using System.Collections;

public class ShootingSystemLocal : MonoBehaviour
{
    [Header("Configuración Láser Metralleta")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private float laserRange = 100f;
    [SerializeField] private float laserDuration = 0.1f;
    [SerializeField] private float damage = 10f;

    [Header("Efectos Visuales")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem bulletTrailParticle;
    [SerializeField] private ParticleSystem impactParticle;

    [Header("Misil")]
    [SerializeField] private GameObject misil;
    [SerializeField] private Transform misilPoint;
    [SerializeField] private float misilSpeed = 20f;
    [SerializeField] private int misilAmmount = 3;

    [Header("Configuración de Layers")]
    [SerializeField] private LayerMask hitLayers = ~0;

    private AudioSource audioSource;
    private bool isFiring = false;
    private float fireDelay;
    private PlayerControllerLocal playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerControllerLocal>();

        if (playerController == null)
        {
            Debug.LogError("No se encontró PlayerControllerLocal en el objeto");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        fireDelay = 1f / fireRate;

        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }

        if (firePoint == null)
        {
            Debug.LogError("FirePoint no asignado en ShootingSystemLocal!");
        }

        // DEBUG: Verificar si el muzzle flash está asignado
        if (muzzleFlash == null)
        {
            Debug.LogError("MUZZLE FLASH NO ASIGNADO en el Inspector!");
        }
        else
        {
            Debug.Log($"Muzzle Flash asignado: {muzzleFlash.name}, Estado: {muzzleFlash.gameObject.activeInHierarchy}");
        }
    }

    public void StartFiring()
    {
        if (!isFiring && playerController != null && playerController.Vidas > 0)
        {
            isFiring = true;
            StartCoroutine(FiringCoroutine());

            // DEBUG
            Debug.Log("StartFiring llamado");

            Debug.Log("Disparando");
        }
    }

    public void StopFiring()
    {
        isFiring = false;
        StopAllCoroutines();

        if (laserLine != null)
        {
            laserLine.enabled = false;
        }

        // Detener partículas
        if (muzzleFlash != null)
        {
            Debug.Log("Deteniendo muzzle flash");
        }
    }

    private IEnumerator FiringCoroutine()
    {
        while (isFiring && playerController != null && playerController.Vidas > 0)
        {
            ShootLaser();
            yield return new WaitForSeconds(fireDelay);
        }
    }

    private void ShootLaser()
    {
        if (firePoint == null) return;

        PlayMuzzleFlash();

        RaycastHit hit;
        Vector3 startPos = firePoint.position;
        Vector3 endPos;

        bool hasHit = Physics.Raycast(startPos, firePoint.forward, out hit, laserRange, hitLayers);

        if (hasHit)
        {
            endPos = hit.point;
            ProcessHit(hit);

            // Mostrar partícula de impacto
            ShowImpactParticle(hit.point, hit.normal);
        }
        else
        {
            endPos = startPos + firePoint.forward * laserRange;
        }

        // Mostrar partícula de trayecto
        ShowBulletTrail(startPos, endPos);

        StartCoroutine(ShowLaserBriefly(startPos, endPos));
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null)
        {
            Debug.LogError("Muzzle Flash es NULL en PlayMuzzleFlash!");
            return;
        }

        Debug.Log($"Ejecutando PlayMuzzleFlash. MuzzleFlash activo: {muzzleFlash.gameObject.activeInHierarchy}");

        // SOLUCIÓN OPCIÓN 1: Instanciar el prefab
        ParticleSystem muzzleInstance = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
        muzzleInstance.transform.parent = firePoint; // Hacerlo hijo del firePoint

        // Configurar y reproducir
        muzzleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleInstance.Clear();
        muzzleInstance.Play(true);

        Debug.Log("Muzzle flash instanciado y reproducido");

        // Destruir después de que termine
        Destroy(muzzleInstance.gameObject, muzzleInstance.main.duration + 0.1f);
    }

    // Método para mostrar partícula de trayecto
    private void ShowBulletTrail(Vector3 startPos, Vector3 endPos)
    {
        if (bulletTrailParticle != null)
        {
            Vector3 direction = (endPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, endPos);

            ParticleSystem trailInstance = Instantiate(bulletTrailParticle, startPos, Quaternion.LookRotation(direction));

            var mainModule = trailInstance.main;
            mainModule.startLifetime = distance / mainModule.startSpeed.constant;

            trailInstance.Play();
            Destroy(trailInstance.gameObject, mainModule.startLifetime.constant + 1f);
        }
    }

    // Método para mostrar partícula de impacto
    private void ShowImpactParticle(Vector3 impactPoint, Vector3 impactNormal)
    {
        if (impactParticle != null)
        {
            ParticleSystem impactInstance = Instantiate(impactParticle, impactPoint, Quaternion.LookRotation(impactNormal));
            impactInstance.Play();
            Destroy(impactInstance.gameObject, impactInstance.main.duration + 1f);
        }
    }

    private IEnumerator ShowLaserBriefly(Vector3 startPos, Vector3 endPos)
    {
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
        Debug.Log($"Láser impactó: {hit.collider.name} - Tag: {hit.collider.tag}");

        PlayerControllerLocal targetPlayer = hit.collider.GetComponent<PlayerControllerLocal>();
        if (targetPlayer == null)
        {
            targetPlayer = hit.collider.GetComponentInParent<PlayerControllerLocal>();
        }

        if (targetPlayer != null && targetPlayer != this.playerController)
        {
            if (hit.collider.CompareTag("Alas"))
            {
                Debug.Log("¡Impacto en ala enemiga -1!");
                targetPlayer.DañoAla();
            }
            else if (hit.collider.CompareTag("Cabina"))
            {
                Debug.Log("¡Impacto en cabina enemiga-2!");
                targetPlayer.DañoCabina();
            }
        }
        else
        {
            Debug.Log($"Objeto impactado no es un jugador enemigo o es el mismo jugador");
        }
    }

    public void Misil()
    {
        if (misilAmmount > 0 && playerController != null && playerController.Vidas > 0)
        {
            if (misil == null || misilPoint == null)
            {
                Debug.LogError("Misil o misilPoint no asignado!");
                return;
            }

            GameObject misilInstanciado = Instantiate(misil, misilPoint.position, transform.rotation);
            Rigidbody misilRb = misilInstanciado.GetComponent<Rigidbody>();

            if (misilRb != null)
            {
                misilRb.linearVelocity = misilPoint.forward * misilSpeed;
                misilRb.useGravity = false;
            }

            misilAmmount--;
            Debug.Log($"Misil disparado. Misiles restantes: {misilAmmount}");
        }
        else
        {
            Debug.Log("No hay misiles disponibles o jugador está muerto");
        }
    }

    public void SetLaserConfig(float newFireRate, float newRange, float newDamage)
    {
        fireRate = newFireRate;
        fireDelay = 1f / fireRate;
        laserRange = newRange;
        damage = newDamage;
    }

    // NUEVO: Método para probar el muzzle flash manualmente
    [ContextMenu("Probar Muzzle Flash")]
    public void ProbarMuzzleFlash()
    {
        Debug.Log("=== PRUEBA MANUAL MUZZLE FLASH ===");

        if (muzzleFlash == null)
        {
            Debug.LogError("Muzzle Flash no asignado!");
            return;
        }

        Debug.Log($"Muzzle Flash: {muzzleFlash.name}");
        Debug.Log($"Transform posición: {muzzleFlash.transform.position}");
        Debug.Log($"Transform padre: {muzzleFlash.transform.parent}");

        PlayMuzzleFlash();
    }

    private void Update()
    {
        // DEBUG: Probar con tecla (opcional)
        if (Input.GetKeyDown(KeyCode.P))
        {
            ProbarMuzzleFlash();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * laserRange);
        }
    }
}