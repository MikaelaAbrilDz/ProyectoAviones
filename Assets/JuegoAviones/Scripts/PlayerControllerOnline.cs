using System.Globalization;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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
    public NetworkVariable<int> networkLifes = new NetworkVariable<int>(15);

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

    [Header("SISTEMA DE HUMO - Configuración")]
    [SerializeField] private GameObject smokePrefab; // Cambiado a GameObject para más flexibilidad
    [SerializeField] private Transform[] smokePositions; // Donde quieres que salga el humo

    // Variables internas del humo
    private GameObject[] smokeInstances;

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

    public int life
    {
        get
        {
            return networkLifes.Value;
        }
        set
        {
            if (networkLifes.Value > 0 && value <= 0)
            {
                DestroyAirplaneClientRpc();
            }
            networkLifes.Value = value;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        shootingSystem = GetComponent<ShootingSystemOnline>();

        if (raycastOrigins == null || raycastOrigins.Length == 0)
        {
            raycastOrigins = new Transform[] { transform };
        }

        InitializeEngineParticles();
        InitializeSmokeSystem();

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

    private void InitializeSmokeSystem()
    {
        if (smokePrefab == null)
        {
            Debug.LogError("¡SmokePrefab no está asignado en el Inspector!");
            return;
        }

        if (smokePositions == null || smokePositions.Length == 0)
        {
            Debug.LogError("¡No hay SmokePositions asignadas en el Inspector!");
            return;
        }

        // Crear array para almacenar las instancias de humo
        smokeInstances = new GameObject[smokePositions.Length];

        // Instanciar todos los humos pero desactivarlos inicialmente
        for (int i = 0; i < smokePositions.Length; i++)
        {
            if (smokePositions[i] != null)
            {
                smokeInstances[i] = Instantiate(smokePrefab, smokePositions[i].position, smokePositions[i].rotation);
                smokeInstances[i].transform.SetParent(smokePositions[i]);
                smokeInstances[i].transform.localPosition = Vector3.zero;
                smokeInstances[i].transform.localRotation = Quaternion.identity;

                // Desactivar inicialmente
                smokeInstances[i].SetActive(false);

                Debug.Log($"Humo {i} instanciado en posición: {smokePositions[i].name}");
            }
            else
            {
                Debug.LogError($"SmokePosition[{i}] no está asignado!");
            }
        }
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
                TakeDamage(999);
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
        isDead = true;
        if (IsServer)
        {
            DestroyAirplaneClientRpc();
            return;
        }

        if (engineParticleInstance != null)
        {
            engineParticleInstance.Stop();
        }

        // Detener todos los humos al morir
        StopAllSmoke();

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
    }

    [ClientRpc(RequireOwnership = false)]
    void DestroyAirplaneClientRpc()
    {
        isDead = true;

        if (engineParticleInstance != null)
        {
            engineParticleInstance.Stop();
        }

        // Detener todos los humos al morir
        StopAllSmoke();

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
    }

    private void RespawnPlayer()
    {
        if (IsServer)
        {
            // Reactivar el avión
            isDead = false;
            networkLifes.Value = 3;

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

            // Resetear humo
            UpdateSmokeBasedOnHealth();

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
        Invoke("RestartGame", 1f);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene("OnlineMultiScene");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (!IsServer)
        {
            TakeDamageServerRpc(damage);
            return;
        }

        life -= damage;

        // Actualizar humo cuando se recibe daño (solo en el servidor)
        UpdateSmokeBasedOnHealth();
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(int damage)
    {
        if (isDead) return;

        life -= damage;

        // Actualizar humo cuando se recibe daño
        UpdateSmokeBasedOnHealthClientRpc();
    }

    [ClientRpc]
    void UpdateSmokeBasedOnHealthClientRpc()
    {
        UpdateSmokeBasedOnHealth();
    }

    // Método para actualizar el humo según las vidas
    private void UpdateSmokeBasedOnHealth()
    {
        if (isDead || smokeInstances == null) return;

        // Calcular cuántas vidas faltan (asumiendo 3 vidas máximas)
        int vidasFaltantes = 3 - networkLifes.Value;

        Debug.Log($"Actualizando humo online. Vidas: {networkLifes.Value}, Faltantes: {vidasFaltantes}");

        // Activar/desactivar humos según vidas faltantes
        for (int i = 0; i < smokeInstances.Length; i++)
        {
            if (smokeInstances[i] != null)
            {
                if (i < vidasFaltantes)
                {
                    // Activar este humo si le faltan suficientes vidas
                    if (!smokeInstances[i].activeSelf)
                    {
                        smokeInstances[i].SetActive(true);
                        Debug.Log($"Activando humo online {i}");
                    }
                }
                else
                {
                    // Desactivar este humo si ya no le faltan tantas vidas
                    if (smokeInstances[i].activeSelf)
                    {
                        smokeInstances[i].SetActive(false);
                        Debug.Log($"Desactivando humo online {i}");
                    }
                }
            }
        }
    }

    // Método para detener todos los humos
    private void StopAllSmoke()
    {
        if (smokeInstances == null) return;

        for (int i = 0; i < smokeInstances.Length; i++)
        {
            if (smokeInstances[i] != null && smokeInstances[i].activeSelf)
            {
                smokeInstances[i].SetActive(false);
            }
        }
    }

    public void DañoAla()
    {
        if (isDead) return;

        if (!IsServer)
        {
            DañoAlaServerRpc();
            return;
        }

        life--;
        Debug.Log($"Daño al ala! Vidas restantes: {life}");

        // Actualizar humo
        UpdateSmokeBasedOnHealth();
    }

    [ServerRpc(RequireOwnership = false)]
    void DañoAlaServerRpc()
    {
        life--;
        UpdateSmokeBasedOnHealthClientRpc();
    }

    public void DañoCabina()
    {
        if (isDead) return;

        if (!IsServer)
        {
            DañoCabinaServerRpc();
            return;
        }

        life -= 2;
        Debug.Log($"Daño a la cabina! Vidas restantes: {life}");

        // Actualizar humo
        UpdateSmokeBasedOnHealth();
    }

    [ServerRpc(RequireOwnership = false)]
    void DañoCabinaServerRpc()
    {
        life -= 2;
        UpdateSmokeBasedOnHealthClientRpc();
    }

    // MÉTODOS DE DEBUG - Puedes llamarlos desde el Inspector
    [ContextMenu("Probar Humo Nivel 1")]
    public void TestSmokeLevel1()
    {
        if (IsServer)
        {
            networkLifes.Value = 2;
            UpdateSmokeBasedOnHealthClientRpc();
        }
    }

    [ContextMenu("Probar Humo Nivel 2")]
    public void TestSmokeLevel2()
    {
        if (IsServer)
        {
            networkLifes.Value = 1;
            UpdateSmokeBasedOnHealthClientRpc();
        }
    }

    [ContextMenu("Probar Humo Nivel 3")]
    public void TestSmokeLevel3()
    {
        if (IsServer)
        {
            networkLifes.Value = 0;
            UpdateSmokeBasedOnHealthClientRpc();
        }
    }

    [ContextMenu("Resetear Humo")]
    public void ResetSmoke()
    {
        if (IsServer)
        {
            networkLifes.Value = 3;
            UpdateSmokeBasedOnHealthClientRpc();
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