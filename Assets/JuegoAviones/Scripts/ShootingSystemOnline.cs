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
    public int maxMisil = 7;
    private int misilAmmount = 7;

    [Header("Layers")]
    [SerializeField] private LayerMask hitLayers;

    private PlayerControllerOnline playerController;
    private bool isFiring;
    private float fireDelay;


    private void Start()
    {
        playerController = GetComponent<PlayerControllerOnline>();
        fireDelay = 1f / fireRate;

        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }
    }

    public int _misilAmmount
    {
        get => misilAmmount;
        set => misilAmmount = value;
    }

    public void StartFiring()
    {
        if (!IsOwner || isFiring || playerController.life <= 0) return;
        isFiring = true;
        StartCoroutine(FiringCoroutine());
    }

    public void StopFiring()
    {
        isFiring = false;
        StopAllCoroutines();
        if (laserLine != null) laserLine.enabled = false;
    }

    private IEnumerator FiringCoroutine()
    {
        while (isFiring && playerController.life > 0)
        {
            ShootRequestServerRpc();
            yield return new WaitForSeconds(fireDelay);
        }
    }

    // =========================
    // CLIENTE -> SERVIDOR
    // =========================
    [ServerRpc]
    private void ShootRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        Vector3 startPos = firePoint.position;
        Vector3 direction = firePoint.forward;

        if (Physics.Raycast(startPos, direction, out RaycastHit hit, laserRange, hitLayers))
        {
            ApplyDamage(hit);
            ShowImpactClientRpc(hit.point, hit.normal);
            ShowLaserClientRpc(startPos, hit.point);

            ShowBulletTrailClientRpc(startPos, hit.point, direction);
        }
        else
        {
            Vector3 endPos = startPos + direction * laserRange;

            ShowLaserClientRpc(startPos, endPos);

            ShowBulletTrailClientRpc(startPos, endPos, direction);
        }

        ShowMuzzleClientRpc();
    }


    // =========================
    // DAÑO (SOLO SERVIDOR)
    // =========================
    private void ApplyDamage(RaycastHit hit)
    {
        PlayerControllerOnline target =
            hit.collider.GetComponentInParent<PlayerControllerOnline>();

        if (target == null) return;

        if (hit.collider.CompareTag("Alas"))
            target.TakeDamage(2, PlayerControllerOnline.DeathCause.shoot);
        else if (hit.collider.CompareTag("Cabina"))
            target.TakeDamage(4, PlayerControllerOnline.DeathCause.shoot);
    }

    // =========================
    // FX PARA TODOS
    // =========================
    [ClientRpc]
    private void ShowImpactClientRpc(Vector3 point, Vector3 normal)
    {
        if (impactParticle == null) return;

        ParticleSystem p =
            Instantiate(impactParticle, point, Quaternion.LookRotation(normal));

        p.Play();
        Destroy(p.gameObject, p.main.duration + 1f);
    }

    [ClientRpc]
    private void ShowLaserClientRpc(Vector3 start, Vector3 end)
    {
        if (laserLine == null) return;

        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
        laserLine.enabled = true;
        StartCoroutine(HideLaser());
    }

    private IEnumerator HideLaser()
    {
        yield return new WaitForSeconds(laserDuration);
        if (laserLine != null) laserLine.enabled = false;
    }

    [ClientRpc]
    private void ShowMuzzleClientRpc()
    {
        if (muzzleFlash == null || firePoint == null) return;

        ParticleSystem p =
            Instantiate(muzzleFlash, firePoint.position, firePoint.rotation, firePoint);

        p.Play();
        Destroy(p.gameObject, p.main.duration + 0.2f);
    }

    // =========================
    // MISIL (YA ESTABA BIEN)
    // =========================
    public void ShootMisil()
    {
        if (_misilAmmount <= 0 || playerController.life <= 0) return;
        _misilAmmount--;
        ShootMisilServerRpc(GetComponent<PlayerControllerOnline>().shooterId);
    }

    [ServerRpc]
    private void ShootMisilServerRpc(int player)
    {
         
        GameObject misilInst = Instantiate(misil, misilPoint.position, misilPoint.rotation);
        misilInst.GetComponent<MisilControllerOnline>().SetShooter(player);
        misilInst.GetComponent<NetworkObject>().Spawn();

    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // =========================
    // BULLET TRAIL (FX ONLINE)
    // =========================

    [ClientRpc]
    private void ShowBulletTrailClientRpc(
    Vector3 startPos,
    Vector3 endPos,
    Vector3 direction)
    {
        if (bulletTrailParticle == null) return;

        float distance = Vector3.Distance(startPos, endPos);

        ParticleSystem trailInstance =
            Instantiate(bulletTrailParticle, startPos, Quaternion.LookRotation(direction));

        var mainModule = trailInstance.main;

        float speed = mainModule.startSpeed.constant;
        if (speed <= 0.01f)
            speed = 50f;

        mainModule.startLifetime = distance / speed;

        trailInstance.Play();
        Destroy(trailInstance.gameObject, mainModule.startLifetime.constant + 1f);
    }



}
