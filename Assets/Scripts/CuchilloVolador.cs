using UnityEngine;

public class CuchilloVolador : MonoBehaviour
{
    [Header("Ajustes del Proyectil")]
    public float velocidad = 8f;
    public float tiempoDeVida = 3f;

    void Start()
    {
        // 1. Desaparecer por tiempo: Destruye este objeto tras X segundos
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        // Mueve el cuchillo hacia adelante constantemente
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    // 2. Desaparecer al salir del mapa/cámara: 
    // Este evento de Unity detecta cuando el objeto ya no es visto por ninguna cámara
    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}