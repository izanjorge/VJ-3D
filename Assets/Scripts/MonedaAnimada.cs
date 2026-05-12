using UnityEngine;

public class MonedaAnimada : MonoBehaviour
{
    [Header("Ajustes de Animación")]
    public float velocidadRotacion = 100f; // Grados por segundo
    public float amplitudFlotacion = 0.2f; // Cuánto sube y baja desde el centro
    public float frecuenciaFlotacion = 2f;  // Rapidez del movimiento

    private float baseHeight;

    void Start()
    {
        // 1. Solución de Altura: Establecemos una altura base ideal.
        // Como el suelo está en Y=0 y el jugador también, Y=0.5 es el centro visual perfecto.
        baseHeight = 1.5f; 
        transform.position = new Vector3(transform.position.x, baseHeight, transform.position.z);
    }

    void Update()
    {
        // 2. Animación de Rotación (continua)
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.World);

        // 3. Animación de Levitación (arriba y abajo) usando la función Seno (Sin)
        float newY = baseHeight + Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}