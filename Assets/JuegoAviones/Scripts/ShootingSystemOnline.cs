using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ShootingSystemOnline : NetworkBehaviour
{
    [Header("Configuración Láser Metralleta")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private float laserRange = 100f;
    [SerializeField] private float laserDuration = 0.1f;

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
    [SerializeField] private LayerMask hitLayers;

    private AudioSource audioSource;
    private bool isFiring = false;
    private float fireDelay;
    private PlayerControllerOnline playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerControllerOnline>();

        if (playerController == null)
        {
            Debug.LogError("No se encontró PlayerControllerOnline en el objeto");
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
            Debug.LogError("FirePoint no asignado en ShootingSystemOnline!");
        }
    }

    public void StartFiring()
    {
        if (!isFiring && playerController != null && playerController.life > 0)
        {
            isFiring = true;
            StartCoroutine(FiringCoroutine());
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
    }

    private IEnumerator FiringCoroutine()
    {
        while (isFiring && playerController != null && playerController.life > 0)
        {
            ShootLaser();
            yield return new WaitForSeconds(fireDelay);
        }
    }

    private void ShootLaser()
    {
        if (firePoint == null) return;

        RaycastHit hit;
        Vector3 startPos = firePoint.position;
        Vector3 endPos;

        bool hasHit = Physics.Raycast(startPos, firePoint.forward, out hit, laserRange, hitLayers);

        if (hasHit)
        {
            endPos = hit.point;
            ProcessHit(hit);
            ShowImpactParticle(hit.point, hit.normal);
        }
        else
        {
            endPos = startPos + firePoint.forward * laserRange;
        }

        ShowBulletTrail(startPos, endPos);
        PlayMuzzleFlash();
        StartCoroutine(ShowLaserBriefly(startPos, endPos));
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        ParticleSystem muzzleInstance = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
        muzzleInstance.transform.parent = firePoint;
        muzzleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzleInstance.Clear();
        muzzleInstance.Play(true);
        Destroy(muzzleInstance.gameObject, muzzleInstance.main.duration + 0.1f);
    }

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
        PlayerControllerOnline targetPlayer = hit.collider.GetComponentInParent<PlayerControllerOnline>();

        if (targetPlayer != null && targetPlayer != this.playerController)
        {
            if (hit.collider.CompareTag("Alas"))
            {
                targetPlayer.TakeDamage(2);
            }
            if (hit.collider.CompareTag("Cabina"))
            {
                targetPlayer.TakeDamage(4);
            }
        }
    }

    public void Misil()
    {
        if (misilAmmount > 0 && playerController != null && playerController.life > 0)
        {
            if (misil == null || misilPoint == null)
            {
                Debug.LogError("Misil o misilPoint no asignado!");
                return;
            }

            // Solo el dueño puede disparar misiles
            if (IsOwner)
            {
                ShootMisilServerRpc();
            }
        }
    }

    [ServerRpc]
    private void ShootMisilServerRpc()
    {
        GameObject misilInstanciado = Instantiate(misil, misilPoint.position, transform.rotation);

        // Configurar el misil
        MisilController misilController = misilInstanciado.GetComponent<MisilController>();
        if (misilController != null)
        {
            // Puedes configurar parámetros específicos aquí si es necesario
        }

        Rigidbody misilRb = misilInstanciado.GetComponent<Rigidbody>();
        if (misilRb != null)
        {
            misilRb.linearVelocity = misilPoint.forward * misilSpeed;
            misilRb.useGravity = false;
        }

        // Replicar el misil en la red
        NetworkObject networkObject = misilInstanciado.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }

        misilAmmount--;

        // Replicar la reducción de munición a los clientes
        UpdateMisilAmmountClientRpc(misilAmmount);
    }

    [ClientRpc]
    private void UpdateMisilAmmountClientRpc(int newAmmount)
    {
        misilAmmount = newAmmount;
    }

    public void SetLaserConfig(float newFireRate, float newRange, float newDamage)
    {
        fireRate = newFireRate;
        fireDelay = 1f / fireRate;
        laserRange = newRange;
    }

    new void OnDestroy()
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
