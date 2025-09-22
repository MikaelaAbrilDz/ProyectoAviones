using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerControler : MonoBehaviour
{
    Vector2 rotation;
    float speed = 15f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * Time.deltaTime * speed;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x + rotation.y * Time.deltaTime * 50, transform.eulerAngles.y + rotation.x * Time.deltaTime * 50, transform.eulerAngles.z);
    }
    private void OnMove(InputValue movementValue)
    {
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }
    private void OnTurbo()
    {
        ButtonControl turbo;

        if (true) speed = 30f;
        else speed = 15f;
    }
}
