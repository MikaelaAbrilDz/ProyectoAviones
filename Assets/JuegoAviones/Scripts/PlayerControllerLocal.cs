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
    public int Vidas => vidas; // Propiedad pública para leer las vidas

    // Referencia al shooting system
    ShootingSystemLocal shootingSystem;

    Vector2 rotation;
    float inclination = 0;
    float speed = 10f;
    int maxInclination = 50;
    float inclinationSpeed = 100f;
    bool isDead = false;
    bool isFiring = false;

    private void Start()
    {
        // OBTENER LA REFERENCIA AL SHOOTING SYSTEM
        shootingSystem = GetComponent<ShootingSystemLocal>();
        
        if (shootingSystem == null)
        {
            Debug.LogError("No se encontró ShootingSystemLocal en el objeto");
        }

        // Si no hay raycastOrigins asignados, usar el transform actual
        if (raycastOrigins == null || raycastOrigins.Length == 0)
        {
            raycastOrigins = new Transform[] { transform };
            Debug.LogWarning("No hay raycastOrigins asignados, usando transform por defecto");
        }

        GetOtherPlayer();
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
            if(cam.name == "PointerCam") cam.cullingMask = layerUI;
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
        transform.position += transform.forward * Time.deltaTime * speed;
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x + rotation.y * Time.deltaTime * 50, 
            transform.eulerAngles.y + rotation.x * Time.deltaTime * 50,
            inclination
        );
        inclination = Mathf.MoveTowards(inclination, -rotation.x * maxInclination, Time.deltaTime * inclinationSpeed);
    }

    private void OnMove(InputValue movementValue)
    {
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }

    private void OnTurbo(InputValue turbo)
    {
        if (turbo.isPressed)
        {
            speed = 25f;
            if (speedCam != null) speedCam.Priority = 1;
        }
        else
        {
            speed = 10f;
            if (speedCam != null) speedCam.Priority = -1;
        }
    }

    private void OnAttack_0(InputValue attack)
    {
        if (attack.isPressed && !isFiring && !isDead)
        {
            isFiring = true;
            if (shootingSystem != null)
                shootingSystem.StartFiring();
            Debug.Log("Pium pium...");
            speed = 3f;
        }
        else if (!attack.isPressed && isFiring)
        {
            isFiring = false;
            if (shootingSystem != null)
                shootingSystem.StopFiring();
            speed = 10f;
            Debug.Log("No Pium pium...");
        }
    }

    private void OnAttack_1(InputValue attack1)
    {
        if (attack1.isPressed && !isDead)
        {
            if (shootingSystem != null)
                shootingSystem.Misil();
            Debug.Log("Misilazo");
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
        
        if (isFiring && shootingSystem != null)
        {
            shootingSystem.StopFiring();
            isFiring = false;
        }
        
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        foreach(var child in GetComponentsInChildren<MeshRenderer>())
        {
            child.gameObject.SetActive(false);
        }

        // Desactivar colliders para evitar más colisiones
        foreach(var collider in GetComponentsInChildren<Collider>())
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
        Debug.Log($"Daño en ala. Vidas restantes: {vidas}");

        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }

    public void DañoCabina()
    {
        if (isDead) return;

        vidas -= 2;
        Debug.Log($"Daño en cabina. Vidas restantes: {vidas}");

        if (vidas <= 0)
        {
            DestroyAirplane();
        }
    }
}