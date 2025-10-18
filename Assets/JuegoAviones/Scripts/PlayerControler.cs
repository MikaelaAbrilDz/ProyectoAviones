using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Netcode;

public class PlayerControler : NetworkBehaviour
{
    [SerializeField] CinemachineCamera speedCam;
    [SerializeField] LayerMask buildingLayerMask; // Capa para los edificios
    [SerializeField] float raycastDistance = 10f; // Distancia del raycast
    [SerializeField] Transform[] raycastOrigins; // Puntos de origen para los raycasts //NO LOS HEMOS PUESTO, DE MOMENTO LO COGE DESDE EL CENTRO DEL AVION
    [SerializeField] Transform camFollowed;
    [SerializeField] GameObject cameraPrefab;
    GameObject cameraObj;


    Vector2 rotation;
    float inclination = 0;
    float speed = 10f;
    int maxInclination = 50;
    float inclinationSpeed = 50f;

    private void Start()
    {
        // Si no se asignan puntos de origen, usar la posición del avión --> (ESTO ES LO QUE HACE)
        if (raycastOrigins == null || raycastOrigins.Length == 0)
        {
            raycastOrigins = new Transform[] { transform };
        }

        // Generar cámara
        cameraObj = Instantiate(cameraPrefab);
        foreach (var camera in cameraObj.GetComponentsInChildren<CinemachineCamera>())
        {
            camera.Target.TrackingTarget = camFollowed;
            camera.Target.LookAtTarget = camFollowed;
            if (camera.name == "PlayerCamSpeed") speedCam = camera;
        }
        cameraObj.SetActive(IsOwner);

        if (!IsOwner)
        {
            GetComponent<PlayerInput>().enabled = false;
            enabled = false;
        }
    }
    void Update()
    {
        Movement();        
        CheckForBuildings();
    }

private void Movement()
    {        
        //APLICAR EL MOVIMIENTO AL TRANSFORM
        transform.position += transform.forward * Time.deltaTime * speed;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x + rotation.y * Time.deltaTime * 50, transform.eulerAngles.y + rotation.x * Time.deltaTime * 50,
            inclination);

        //CÁLCULOS PARA INCLINACIÓN SMOOTH
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
            speedCam.Priority = 1;
        }
        else
        {
            speed = 10f;
            speedCam.Priority = -1;
            
        }
    }

    private void CheckForBuildings()
    {
        foreach (Transform origin in raycastOrigins)
        {
            // Lanzar raycast hacia adelante desde cada punto de origen
            Ray ray = new Ray(origin.position, origin.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, buildingLayerMask))
            {
                Debug.DrawRay(origin.position, origin.forward * raycastDistance, Color.red);

                // Si golpea un edificio, destruir el avión
                DestroyAirplane();
                return; 
            }
            else
            {
                Debug.DrawRay(origin.position, origin.forward * raycastDistance, Color.green);
            }
        }


    }

    private void DestroyAirplane()
    {
        Debug.Log("YETS destruido :(");

        //Para otro dia metemos efectos
        // Instantiate(explosionEffect, transform.position, transform.rotation);

        // Destruye el avión
        Destroy(gameObject);  
    }
}
