using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections;

public class PlayerControler : MonoBehaviour
{
    Vector2 rotation;
    float inclination = 0;
    float speed = 15f;
    float counter = 0;

    private void Start()
    {
        StartCoroutine(AdjustInclination());
    }
    void Update()
    {
        

        transform.position += transform.forward * Time.deltaTime * speed;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x + rotation.y * Time.deltaTime * 50, transform.eulerAngles.y + rotation.x * Time.deltaTime * 50,
            inclination);
    }
    private IEnumerator AdjustInclination()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            counter += Time.deltaTime * 0.01f / (Mathf.Abs(rotation.x) + 5f);
            inclination = Mathf.Lerp(inclination, -rotation.x * 15, counter);

            if (inclination == rotation.x) counter = 0;
        }
    }
    private void OnMove(InputValue movementValue)
    {
        rotation.x = movementValue.Get<Vector2>().x;
        rotation.y = movementValue.Get<Vector2>().y;
    }
    private void OnTurbo()
    {
        if (true) speed = 30f;
        else speed = 15f;
    }
}
