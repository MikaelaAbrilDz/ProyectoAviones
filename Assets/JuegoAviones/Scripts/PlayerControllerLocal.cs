using System.Globalization;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerLocal : MonoBehaviour
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

    [Header("Sistema de Partículas del Motor")]
    [SerializeField] private ParticleSystem engineParticleSystem;
    [SerializeField] private Transform enginePosition;

    [Header("Configuración Partículas - Normal")]
    public float normalEmissionRate = 15f;
    public float normalStartSpeed = 8f;
    public float normalStartSize = 0.3f;
    public float normalStartLifetime = 0.3f; // REDUCIDO

    [Header("Configuración Partículas - Turbo")]
    public float turboEmissionRate = 30f;
    public float turboStartSpeed = 20f;
    public float turboStartSize = 0.6f;
    public float turboStartLifetime = 0.4f; // REDUCIDO

    [Header("Configuración Fast Turn")]
    [SerializeField] private float fastTurnSpeedMultiplier = 0.3f;
    [SerializeField] private float fastTurnRotationMultiplier = 3f;

    // Referencia al shooting system
    ShootingSystemLocal shootingSystem;

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

    private void Start()
    {
        shootingSystem = GetComponent<ShootingSystemLocal>();

        if (raycastOrigins == null || raycastOrigins.Length == 0)
        {
            raycastOrigins = new Transform[] { transform };
        }

        InitializeEngineParticles();
        GetOtherPlayer();
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
        main.startLifetime = normalStartLifetime; // USANDO VARIABLE PÚBLICA
        main.startSpeed = normalStartSpeed;
        main.startSize = normalStartSize;
        main.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 30; // REDUCIDO

        emission.rateOverTime = normalEmissionRate;

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.03f;

        engineParticleInstance.transform.localRotation = Quaternion.Euler(0, 180f, 0);
    }

    public void GetOtherPlayer()
    {
        if (otherPlayer == null)
        {
            foreach (var players in FindObjectsByType<PlayerControllerLocal>(FindObjectsSortMode.None))
            {
                if (players != this) otherPlayer = players.transform;
            }
            otherPlayer.GetComponent<PlayerControllerLocal>().GetOtherPlayer();
        }
    }

    void Update()
    {
        if (!isDead)
        {
            Movement();
            CheckForBuildings();

            if (pointer != null && otherPlayer != null)
                pointer.rotation = Quaternion.LookRotation(otherPlayer.transform.position - transform.position);
        }
    }

    public void AtJoining(OutputChannels channel, LayerMask layerMain, LayerMask layerUI, int playerID)
    {
        cameraObj = Instantiate(cameraPrefab);
        foreach (var camera in cameraObj.GetComponentsInChildren<CinemachineCamera>())
        {
            camera.Target.TrackingTarget = camFollowed;
            camera.Target.LookAtTarget = camFollowed;
            if (camera.name == "PlayerCamSpeed") speedCam = camera;
            camera.OutputChannel = channel;
        }
        Camera mainCam = cameraObj.GetComponentInChildren<Camera>();
        GetComponent<PlayerInput>().camera = mainCam;
        mainCam.cullingMask = layerMain;
        foreach (var cam in mainCam.GetComponentsInChildren<Camera>())
        {
            if (cam.name == "PointerCam") cam.cullingMask = layerUI;
        }
        cameraObj.GetComponentInChildren<CinemachineBrain>().ChannelMask = channel;
        pointer = cameraObj.GetComponent<PointerManager>().pointerTrnsfm;
        if (playerID == 0)
        {
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

    private void Movement()
    {
        float currentSpeed = speed;
        float rotationMultiplier = 50f;

        // Aplicar modificadores de Fast Turn
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
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }

    private void OnFastTurn(InputValue fastTurnValue)
    {
        if (fastTurnValue.isPressed && !isFastTurnActive && !isDead)
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
            main.startLifetime = turboStartLifetime; // USANDO VARIABLE PÚBLICA
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
            main.startLifetime = normalStartLifetime; // USANDO VARIABLE PÚBLICA
            emission.rateOverTime = normalEmissionRate;
            main.startColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        }
    }

    private void OnAttack_0(InputValue attack)
    {
        if (attack.isPressed && !isFiring && !isDead)
        {
            isFiring = true;
            if (shootingSystem != null)
                shootingSystem.StartFiring();
            speed = 3f;
            foreach (CinemachineBasicMultiChannelPerlin shake in cameraObj.GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>())
            {
                shake.AmplitudeGain = screenShakeAmmount;
                shake.FrequencyGain = screenShakeFrequency;
            }
        }
        else if (!attack.isPressed && isFiring)
        {
            isFiring = false;
            if (shootingSystem != null)
                shootingSystem.StopFiring();
            speed = 10f;
            foreach (CinemachineBasicMultiChannelPerlin shake in cameraObj.GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>())
            {
                shake.AmplitudeGain = 0;
                shake.FrequencyGain = 0;
            }
        }
    }

    private void OnAttack_1(InputValue attack1)
    {
        if (attack1.isPressed && !isDead)
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

        Invoke("RestartGame", 1f);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene("LocalMultiScene");
    }

    public void DañoAla()
    {
        if (isDead) return;

        vidas--;
        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }

    public void DañoCabina()
    {
        if (isDead) return;

        vidas -= 2;
        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }
}