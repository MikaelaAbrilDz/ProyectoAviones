using UnityEngine;

public class MovimientoNiebla : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    [Tooltip("Qué tan rápido se mueve (frecuencia)")]
    public float velocidad = 0.5f;

    [Tooltip("Qué tanto sube y baja (amplitud en metros)")]
    public float alturaMovimiento = 2.0f;

    [Tooltip("Si quieres que también se mueva un poco hacia los lados")]
    public float derivaLateral = 1.0f;

    private Vector3 posicionInicial;

    void Start()
    {
        // Guardamos donde pusiste el cubo originalmente para que no se pierda
        posicionInicial = transform.position;
    }

    void Update()
    {
        // Usamos la función Seno (Sin) para crear ondas suaves repetitivas

        // Calcular nuevo Y (Arriba/Abajo)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidad) * alturaMovimiento;

        // Calcular nuevo X (Vaivén lateral suave, desfasado con Coseno)
        float nuevoX = posicionInicial.x + Mathf.Cos(Time.time * (velocidad * 0.5f)) * derivaLateral;

        // Aplicar la nueva posición
        transform.position = new Vector3(nuevoX, nuevoY, posicionInicial.z);
    }
}