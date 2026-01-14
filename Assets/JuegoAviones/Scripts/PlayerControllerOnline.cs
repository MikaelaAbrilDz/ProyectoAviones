using System;
using System.Globalization;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PlayerControllerOnline : NetworkBehaviour
{
    [SerializeField] CinemachineCamera speedCam;
    [SerializeField] LayerMask buildingLayerMask;
    [SerializeField] float raycastDistance = 10f;
    [SerializeField] Transform[] raycastOrigins;
    [SerializeField] Transform camFollowed;
    [SerializeField] GameObject cameraPrefab;
    [SerializeField] GameObject cross;
    [SerializeField] GameObject visual;
    [SerializeField] GameObject enviro;
    GameObject cameraObj;
    Transform pointer;
    Transform otherPlayer;

    public GameObject explosionEffect;

    [Header("Vidas")]
    public NetworkVariable<int> networkLifes = new NetworkVariable<int>(15);
    public int maxLife = 15;

    [Header("Screen Shake - Disparo")]
    [SerializeField] float screenShakeAmmount = 0f; //original 0.5f
    [SerializeField] float screenShakeFrequency = 0f; //original 6f

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

    // Variables internas del humo
    public GameObject[] smokeParticles;

    ShootingSystemOnline shootingSystem;

    Vector2 rotation;
    float inclination = 0;
    float speed = 10f;
    int maxInclination = 50;
    float inclinationSpeed = 100f;
    [HideInInspector] public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    bool isFiring = false;
    bool isTurboActive = false;
    bool isFastTurnActive = false;

    public int shooterId;

    public NetworkVariable<int> networkId = new NetworkVariable<int>(1);

    private ParticleSystem engineParticleInstance;
    GameObject enviroInstanced;

    [SerializeField] LayerMask mainCamMask_p0;
    [SerializeField] LayerMask mainCamMask_p1;
    [SerializeField] LayerMask uiCamMask_p0;
    [SerializeField] LayerMask uiCamMask_p1;


    public enum DeathCause
    {
        building, misile, shoot
    }
    DeathCause deathCause;
    
    NetworkVariable<float> secondsOfRound = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
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
                DestroyAirplane();
            }

            if (!IsServer) UpdateLifeRpc(value);
            else networkLifes.Value = value;

            UpdateSmokeBasedOnHealthRpc();
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void UpdateLifeRpc(int value)
    {
        networkLifes.Value = value; //ASIGNA EL VALOR
    }

    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            if (IsServer)
            {
                networkId.Value = Accounts.id;
                shooterId = 6;
            }
            else
            {
                SetIdRpc(Accounts.id);
                shooterId = 7;
            }
        }

        if (IsServer && IsOwner && enviroInstanced == null)
        {
            enviroInstanced = Instantiate(enviro, Vector3.zero, Quaternion.identity);
            enviroInstanced.transform.localScale = Vector3.one * 0.1f;
            enviroInstanced.GetComponent<NetworkObject>().Spawn(); 
        } 

        life = maxLife;
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
        FindOtherPlayer();
    }

    [Rpc(SendTo.Server)]
    private void SetIdRpc(int id)
    {
        networkId.Value = id;
    }
    private void InitializeCamera()
    {
            cameraObj = Instantiate(cameraPrefab);
        Camera mainCam = cameraObj.GetComponentInChildren<Camera>();
        if (cameraPrefab != null && camFollowed != null)
        {
            foreach (var camera in cameraObj.GetComponentsInChildren<CinemachineCamera>())
            {
                camera.Target.TrackingTarget = camFollowed;
                camera.Target.LookAtTarget = camFollowed;
                if (camera.name == "PlayerCamSpeed") speedCam = camera;
            }

            // Configurar cámara para el jugador
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
        if (IsServer)
        {
            mainCam.cullingMask = mainCamMask_p0;
            foreach (var cam in mainCam.GetComponentsInChildren<Camera>())
            {
                if (cam.name == "PointerCam") cam.cullingMask = uiCamMask_p0;
            }
            foreach (var cross in cross.GetComponentsInChildren<Transform>())
            {
                cross.gameObject.layer = LayerMask.NameToLayer("Cross_P0");
            }
            foreach (var pointer in pointer.GetComponentsInChildren<Transform>())
            {
                pointer.gameObject.layer = LayerMask.NameToLayer("3DUI_P0");
            }
        }
        else
        {
            mainCam.cullingMask = mainCamMask_p1;
            foreach (var cam in mainCam.GetComponentsInChildren<Camera>())
            {
                if (cam.name == "PointerCam") cam.cullingMask = uiCamMask_p1;
            }
            foreach (var cross in cross.GetComponentsInChildren<Transform>())
            {
                cross.gameObject.layer = LayerMask.NameToLayer("Cross_P1");
            }
            foreach (var pointer in pointer.GetComponentsInChildren<Transform>())
            {
                pointer.gameObject.layer = LayerMask.NameToLayer("3DUI_P1");
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

    public void FindOtherPlayer()
    {
        if (otherPlayer == null)
        {
            foreach (var player in FindObjectsByType<PlayerControllerOnline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (player != this)
                {
                    otherPlayer = player.transform;
                    otherPlayer.GetComponent<PlayerControllerOnline>().FindOtherPlayer();
                    break;
                }
            }
        }
    }

    void Update()
    {
        secondsOfRound.Value += Time.deltaTime;

        if (!IsOwner || isDead.Value) return;
        
       
        
        Movement();
        CheckForBuildings();

        if (pointer != null && otherPlayer != null)
            pointer.rotation = Quaternion.LookRotation(otherPlayer.transform.position - transform.position);
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
        if (!IsOwner || isDead.Value) return;
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }

    private void OnFastTurn(InputValue fastTurnValue)
    {
        if (!IsOwner || isDead.Value) return;

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
        if (!IsOwner || isDead.Value) return;

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
        if (!IsOwner || isDead.Value) return;

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
        if (!IsOwner || isDead.Value) return;

        if (attack1.isPressed)
        {
            if (shootingSystem != null)
                shootingSystem.ShootMisil();
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
                TakeDamage(999, PlayerControllerOnline.DeathCause.building);
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
        AirplaneDieRpc();
        DestroyAirplaneClientRpc();
    }
    IEnumerator FinishRound(int winner, int loser, DeathCause death, Vector3 deathPos, int timer)
    {
        WWWForm form = new WWWForm();
        form.AddField("winner_id", winner);
        form.AddField("loser_id", loser);
        form.AddField("death_cause", death.ToString());
        form.AddField("death_x", deathPos.x.ToString("F3"));
        form.AddField("death_y", deathPos.y.ToString("F3"));
        form.AddField("death_z", deathPos.z.ToString("F3"));
        form.AddField("round_duration", timer);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/unity_api/register_round.php", form))
        {
            yield return www.SendWebRequest();
            //print(www.result.ToString());
        }
    }
    IEnumerator FinishRound(int winner, int loser)
    {
        NetworkManager.Singleton.Shutdown();
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(0);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    void AirplaneDieRpc()
    {
        isDead.Value = true;
    }

    [ClientRpc(RequireOwnership = false)]
    void DestroyAirplaneClientRpc()
    {
 
        if (engineParticleInstance != null)
        {
            engineParticleInstance.Pause();
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

        visual.SetActive(false);

        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        Invoke("AskForRestart", 1f);
    }

    private void AskForRestart()
    {
        if (IsServer) FindAnyObjectByType<RestartGameCounter>()._restartCounter = 0;
        FindAnyObjectByType<OnlineResetUI>(FindObjectsInactive.Include).canvas.SetActive(true);
        FindAnyObjectByType<OnlineResetUI>(FindObjectsInactive.Include).button.SetActive(true);
        FindAnyObjectByType<OnlineResetUI>(FindObjectsInactive.Include).button2.SetActive(true);

        PlayerControllerOnline[] players = FindObjectsByType<PlayerControllerOnline>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int idLoser = 1, idWinner = 1, timer = (int)secondsOfRound.Value; 

        DeathCause death = 0;
        Vector3 deathPos = Vector3.zero;
        if (IsServer) foreach (PlayerControllerOnline p in players)
        {
            if (p.isDead.Value)
            {
                print("Personaje muerto)");
                idLoser = p.networkId.Value;
                death = deathCause;
                deathPos = p.transform.position;
            }
            else
            {
                print("Personaje Vivo)");
                idWinner = p.networkId.Value;
                
            }
            p.speed = 10f;
            p.isTurboActive = false;
            if (p.speedCam != null) p.speedCam.Priority = -1;
            p.ApplyNormalParticleEffects();

        }
        if (IsServer)
        {
            StartCoroutine(FinishRound(idWinner, idLoser, death, deathPos, timer));
           
            print(idWinner + " / " + idLoser);
        }
        Time.timeScale = 0;
    }
    public void RestartGame()
    {
        if (IsServer)
        {
            GetComponent<SpawnManager>().ResetSpawns();
            RestartGameClientRpc();
        }

        AddCounterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCounterServerRpc()
    {
        FindAnyObjectByType<RestartGameCounter>()._restartCounter ++;
    }

    [ClientRpc(RequireOwnership = false)]
    private void RestartGameClientRpc()
    {

        if (IsServer) GetComponent<SpawnManager>().RespawnPlayer();
        GetComponent<ShootingSystemOnline>()._misilAmmount = GetComponent<ShootingSystemOnline>().maxMisil;
        isTurboActive = false;
        if (IsServer)
        {
            life = maxLife;
        }
        if (engineParticleInstance != null) engineParticleInstance.Play();
        isDead.Value = false;
        visual.SetActive(true);
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

        FindOtherPlayer();

        PlayerControllerOnline otherPlayerScript = otherPlayer.GetComponent<PlayerControllerOnline>();
        if (IsServer) otherPlayerScript.GetComponent<SpawnManager>().RespawnPlayer();
        otherPlayerScript.GetComponent<ShootingSystemOnline>()._misilAmmount = otherPlayerScript.GetComponent<ShootingSystemOnline>().maxMisil;
        otherPlayerScript.isTurboActive = false;
        if (IsServer)
        {
            otherPlayerScript.life = otherPlayerScript.maxLife;
        }
        if (otherPlayerScript.engineParticleInstance != null) engineParticleInstance.Play(); otherPlayerScript.engineParticleInstance.Play();
        otherPlayerScript.isDead.Value = false;
        otherPlayerScript.visual.SetActive(true);
        foreach (var collider in otherPlayerScript.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

    }

    public void TakeDamage(int damage, DeathCause cause)
    {
        if (isDead.Value) return;
        deathCause = cause;
        if (!IsServer)
        {
            TakeDamageServerRpc(damage);
            return;
        }

        life -= damage;
    }

    [ServerRpc(RequireOwnership = false)]
    void TakeDamageServerRpc(int damage)
    {
        if (isDead.Value) return;

        life -= damage;
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    void UpdateSmokeBasedOnHealthRpc()
    {
        if (smokeParticles == null) return;

        if (life <= 10) smokeParticles[0].SetActive(true);
        if (life <= 5) smokeParticles[1].SetActive(true);
    }

    // Método para detener todos los humos
    private void StopAllSmoke()
    {
        if (smokeParticles == null) return;

        for (int i = 0; i < smokeParticles.Length; i++)
        {
            if (smokeParticles[i] != null && smokeParticles[i].activeSelf)
            {
                smokeParticles[i].SetActive(false);
            }
        }
    }

    public void DañoAla()
    {
        if (isDead.Value) return;

        if (!IsServer)
        {
            DañoAlaServerRpc();
            return;
        }

        life--;
        Debug.Log($"Daño al ala! Vidas restantes: {life}");

    }

    [ServerRpc(RequireOwnership = false)]
    void DañoAlaServerRpc()
    {
        life--;
        UpdateSmokeBasedOnHealthRpc();
    }

    public void DañoCabina()
    {
        if (isDead.Value) return;

        if (!IsServer)
        {
            DañoCabinaServerRpc();
            return;
        }

        life -= 2;
        Debug.Log($"Daño a la cabina! Vidas restantes: {life}");
    }

    [ServerRpc(RequireOwnership = false)]
    void DañoCabinaServerRpc()
    {
        life -= 2;
        UpdateSmokeBasedOnHealthRpc();
    }

    // MÉTODOS DE DEBUG - Puedes llamarlos desde el Inspector
    [ContextMenu("Probar Humo Nivel 1")]
    public void TestSmokeLevel1()
    {
        if (IsServer)
        {
            networkLifes.Value = 2;
            UpdateSmokeBasedOnHealthRpc();
        }
    }

    [ContextMenu("Probar Humo Nivel 2")]
    public void TestSmokeLevel2()
    {
        if (IsServer)
        {
            networkLifes.Value = 1;
            UpdateSmokeBasedOnHealthRpc();
        }
    }

    [ContextMenu("Probar Humo Nivel 3")]
    public void TestSmokeLevel3()
    {
        if (IsServer)
        {
            networkLifes.Value = 0;
            UpdateSmokeBasedOnHealthRpc();
        }
    }

    [ContextMenu("Resetear Humo")]
    public void ResetSmoke()
    {
        if (IsServer)
        {
            networkLifes.Value = maxLife;
            UpdateSmokeBasedOnHealthRpc();
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