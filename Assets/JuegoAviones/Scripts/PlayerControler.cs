using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.InputSystem.Controls;
using System.Collections;

public class PlayerControler : MonoBehaviour
{
    [SerializeField] CinemachineCamera speedCam;

    Vector2 rotation;
    float inclination = 0;
    float speed = 15f;
    int maxInclination = 50;
    float inclinationSpeed = 50f;

    private void Start()
    {

    }
    void Update()
    {
        Movement();
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
            speed = 30f;
            speedCam.Priority = 1;
        }
        else
        {
            speed = 15f;
            speedCam.Priority = -1;
            
        }
    }
}
