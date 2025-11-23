using System.Globalization;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControllerOnline : NetworkBehaviour
{
    [SerializeField] CinemachineCamera speedCam;
    [SerializeField] LayerMask buildingLayerMask;
    [SerializeField] float raycastDistance = 10f;
    [SerializeField] Transform[] raycastOrigins;
    [SerializeField] Transform camFollowed;
    [SerializeField] GameObject cameraPrefab;
    [SerializeField] GameObject cross;
    GameObject cameraObj;
    Transform pointer;
    Transform otherPlayer;

    public GameObject explosionEffect;

    [Header("Vidas")]
    [SerializeField] private int vidas = 3;
    public int Vidas => vidas;

    [Header("Screen Shake - Disparo")]
    [SerializeField] float screenShakeAmmount = 0.5f;
    [SerializeField] float screenShakeFrequency = 6f;

    [Header("Configuración Partículas del Motor")]
    [SerializeField] private ParticleSystem engineParticleSystem;
    [SerializeField] private Transform enginePosition;

    [Header("Configuración Partículas - Normal")]
    public float normalEmissionRate = 15f;
    public float normalStartSpeed = 8f;
    public float normalStartSize = 0.3f;
    public float normalStartLifetime = 0.3f;

    [Header("Configuración Partículas - Turbo")]
    public float turboEmissionRate = 30f;
    public float turboStartSpeed = 20f;
    public float turboStartSize = 0.6f;
    public float turboStartLifetime = 0.4f;

    [Header("Configuración Fast Turn")]
    [SerializeField] private float fastTurnSpeedMultiplier = 0.3f;
    [SerializeField] private float fastTurnRotationMultiplier = 3f;

    ShootingSystemOnline shootingSystem;

    Vector2 rotation;
    float inclination = 0;
    float speed = 10f;
    int maxInclination = 50;
    float inclinationSpeed = 100f;
    bool isDead = false;
    bool isFiring = false;
    bool isTurboActive = false;
    bool isFastTurnActive = false;

    private ParticleSystem engineParticleInstance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        shootingSystem = GetComponent<ShootingSystemOnline>();

        if (raycastOrigins == null || raycastOrigins.Length == 0)
        {
            raycastOrigins = new Transform[] { transform };
        }

        InitializeEngineParticles();

        if (!IsOwner)
        {
            GetComponent<PlayerInput>().enabled = false;
            enabled = false;
            return;
        }

        // Solo el owner inicializa la cámara y busca otros jugadores
        InitializeCamera();
        FindOtherPlayers();
    }

    private void InitializeCamera()
    {
        if (cameraPrefab != null && camFollowed != null)
        {
            cameraObj = Instantiate(cameraPrefab);
            foreach (var camera in cameraObj.GetComponentsInChildren<CinemachineCamera>())
            {
                camera.Target.TrackingTarget = camFollowed;
                camera.Target.LookAtTarget = camFollowed;
                if (camera.name == "PlayerCamSpeed") speedCam = camera;
            }

            // Configurar cámara para el jugador
            Camera mainCam = cameraObj.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                GetComponent<PlayerInput>().camera = mainCam;
            }

            // Buscar el pointer si existe
            PointerManager pointerManager = cameraObj.GetComponent<PointerManager>();
            if (pointerManager != null)
            {
                pointer = pointerManager.pointerTrnsfm;
            }
        }
    }

    private void InitializeEngineParticles()
    {
        if (enginePosition == null || engineParticleSystem == null) return;

        engineParticleInstance = Instantiate(engineParticleSystem, enginePosition.position, enginePosition.rotation);
        engineParticleInstance.transform.SetParent(enginePosition);
        engineParticleInstance.transform.localPosition = Vector3.zero;
        engineParticleInstance.transform.localRotation = Quaternion.identity;

        ConfigureEngineParticles();
        engineParticleInstance.Play();
    }

    private void ConfigureEngineParticles()
    {
        if (engineParticleInstance == null) return;

        var main = engineParticleInstance.main;
        var emission = engineParticleInstance.emission;
        var shape = engineParticleInstance.shape;

        main.loop = true;
        main.startLifetime = normalStartLifetime;
        main.startSpeed = normalStartSpeed;
        main.startSize = normalStartSize;
        main.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 30;

        emission.rateOverTime = normalEmissionRate;

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.03f;

        engineParticleInstance.transform.localRotation = Quaternion.Euler(0, 180f, 0);
    }

    private void FindOtherPlayers()
    {
        if (otherPlayer == null)
        {
            foreach (var player in FindObjectsByType<PlayerControllerOnline>(FindObjectsSortMode.None))
            {
                if (player != this && player.IsSpawned)
                {
                    otherPlayer = player.transform;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!IsOwner || isDead) return;

        Movement();
        CheckForBuildings();

        if (pointer != null && otherPlayer != null)
            pointer.rotation = Quaternion.LookRotation(otherPlayer.transform.position - transform.position);
    }

    public void AtJoining(OutputChannels channel, LayerMask layerMain, LayerMask layerUI, int playerID)
    {
        if (!IsOwner) return;

        // Esta función parece ser para configuración específica de splitscreen
        // La mantenemos por compatibilidad pero la funcionalidad principal está en InitializeCamera()
        if (cameraObj != null)
        {
            foreach (var camera in cameraObj.GetComponentsInChildren<CinemachineCamera>())
            {
                camera.OutputChannel = channel;
            }

            Camera mainCam = cameraObj.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                mainCam.cullingMask = layerMain;
                foreach (var cam in mainCam.GetComponentsInChildren<Camera>())
                {
                    if (cam.name == "PointerCam") cam.cullingMask = layerUI;
                }
            }

            var brain = cameraObj.GetComponentInChildren<CinemachineBrain>();
            if (brain != null)
            {
                brain.ChannelMask = channel;
            }

            // Configuración de layers para crosshair y pointer
            if (cross != null)
            {
                foreach (var crossTransform in cross.GetComponentsInChildren<Transform>())
                {
                    crossTransform.gameObject.layer = LayerMask.NameToLayer(playerID == 0 ? "Cross_P0" : "Cross_P1");
                }
            }

            if (pointer != null)
            {
                foreach (var pointerTransform in pointer.GetComponentsInChildren<Transform>())
                {
                    pointerTransform.gameObject.layer = LayerMask.NameToLayer(playerID == 0 ? "3DUI_P0" : "3DUI_P1");
                }
            }
        }
    }

    private void Movement()
    {
        float currentSpeed = speed;
        float rotationMultiplier = 50f;

        if (isFastTurnActive)
        {
            currentSpeed *= fastTurnSpeedMultiplier;
            rotationMultiplier *= fastTurnRotationMultiplier;
        }

        transform.position += transform.forward * Time.deltaTime * currentSpeed;
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x + rotation.y * Time.deltaTime * rotationMultiplier,
            transform.eulerAngles.y + rotation.x * Time.deltaTime * rotationMultiplier,
            inclination
        );
        inclination = Mathf.MoveTowards(inclination, -rotation.x * maxInclination, Time.deltaTime * inclinationSpeed);
    }

    private void OnMove(InputValue movementValue)
    {
        if (!IsOwner || isDead) return;
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }

    private void OnFastTurn(InputValue fastTurnValue)
    {
        if (!IsOwner || isDead) return;

        if (fastTurnValue.isPressed && !isFastTurnActive)
        {
            isFastTurnActive = true;
        }
        else if (!fastTurnValue.isPressed && isFastTurnActive)
        {
            isFastTurnActive = false;
        }
    }

    private void OnTurbo(InputValue turbo)
    {
        if (!IsOwner || isDead) return;

        if (turbo.isPressed && !isTurboActive && !isFastTurnActive)
        {
            speed = 25f;
            isTurboActive = true;
            if (speedCam != null) speedCam.Priority = 1;
            ApplyTurboParticleEffects();
        }
        else if (!turbo.isPressed && isTurboActive)
        {
            speed = 10f;
            isTurboActive = false;
            if (speedCam != null) speedCam.Priority = -1;
            ApplyNormalParticleEffects();
        }
    }

    private void ApplyTurboParticleEffects()
    {
        if (engineParticleInstance != null)
        {
            var main = engineParticleInstance.main;
            var emission = engineParticleInstance.emission;

            main.startSpeed = turboStartSpeed;
            main.startSize = turboStartSize;
            main.startLifetime = turboStartLifetime;
            emission.rateOverTime = turboEmissionRate;
            main.startColor = new Color(1f, 0.6f, 0.2f, 1f);
        }
    }

    private void ApplyNormalParticleEffects()
    {
        if (engineParticleInstance != null)
        {
            var main = engineParticleInstance.main;
            var emission = engineParticleInstance.emission;

            main.startSpeed = normalStartSpeed;
            main.startSize = normalStartSize;
            main.startLifetime = normalStartLifetime;
            emission.rateOverTime = normalEmissionRate;
            main.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        }
    }

    private void OnAttack_0(InputValue attack)
    {
        if (!IsOwner || isDead) return;

        if (attack.isPressed && !isFiring)
        {
            isFiring = true;
            if (shootingSystem != null)
                shootingSystem.StartFiring();
            speed = 3f;
            if (cameraObj != null)
            {
                foreach (CinemachineBasicMultiChannelPerlin shake in cameraObj.GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>())
                {
                    shake.AmplitudeGain = screenShakeAmmount;
                    shake.FrequencyGain = screenShakeFrequency;
                }
            }
        }
        else if (!attack.isPressed && isFiring)
        {
            isFiring = false;
            if (shootingSystem != null)
                shootingSystem.StopFiring();
            speed = 10f;
            if (cameraObj != null)
            {
                foreach (CinemachineBasicMultiChannelPerlin shake in cameraObj.GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>())
                {
                    shake.AmplitudeGain = 0;
                    shake.FrequencyGain = 0;
                }
            }
        }
    }

    private void OnAttack_1(InputValue attack1)
    {
        if (!IsOwner || isDead) return;

        if (attack1.isPressed)
        {
            if (shootingSystem != null)
                shootingSystem.Misil();
        }
    }

    private void CheckForBuildings()
    {
        foreach (Transform origin in raycastOrigins)
        {
            if (origin == null) continue;

            Ray ray = new Ray(origin.position, origin.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, buildingLayerMask))
            {
                Debug.DrawRay(origin.position, origin.forward * raycastDistance, Color.red);
                DestroyAirplane();
                return;
            }
            else
            {
                Debug.DrawRay(origin.position, origin.forward * raycastDistance, Color.green);
            }
        }
    }

    public void DestroyAirplane()
    {
        if (isDead) return;

        isDead = true;

        if (engineParticleInstance != null)
        {
            engineParticleInstance.Stop();
        }

        if (isFiring && shootingSystem != null)
        {
            shootingSystem.StopFiring();
            isFiring = false;
        }

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        foreach (var child in GetComponentsInChildren<MeshRenderer>())
        {
            child.gameObject.SetActive(false);
        }

        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // En online, solo el servidor maneja el respawn
        if (IsServer)
        {
            Invoke("RespawnPlayer", 3f);
        }
    }

    private void RespawnPlayer()
    {
        if (IsServer)
        {
            // Reactivar el avión
            isDead = false;
            vidas = 3;

            foreach (var child in GetComponentsInChildren<MeshRenderer>())
            {
                child.gameObject.SetActive(true);
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }

            if (engineParticleInstance != null)
            {
                engineParticleInstance.Play();
            }

            // Buscar spawn point disponible
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawn");
            foreach (var spawn in spawns)
            {
                if (spawn.activeSelf)
                {
                    transform.position = spawn.transform.position;
                    spawn.SetActive(false);
                    break;
                }
            }
        }
    }

    public void DañoAla()
    {
        if (isDead || !IsOwner) return;

        vidas--;
        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }

    public void DañoCabina()
    {
        if (isDead || !IsOwner) return;

        vidas -= 2;
        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (cameraObj != null)
        {
            Destroy(cameraObj);
        }
    }
}