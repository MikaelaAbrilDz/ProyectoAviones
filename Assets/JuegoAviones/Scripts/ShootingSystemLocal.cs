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
        // Solo usar PlayerControllerLocal
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
    }

    public void StartFiring()
    {
        if (!isFiring && playerController != null && playerController.Vidas > 0)
        {
            isFiring = true;
            StartCoroutine(FiringCoroutine());
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

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
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
        }
        else
        {
            endPos = startPos + firePoint.forward * laserRange;
        }

        StartCoroutine(ShowLaserBriefly(startPos, endPos));
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        if (!muzzleFlash.gameObject.activeInHierarchy)
            muzzleFlash.gameObject.SetActive(true);

        muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleFlash.Simulate(0, true, true);
        muzzleFlash.Play(true);
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