using UnityEngine;
using System.Collections;

public class TrampaPinchos : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetoPinchos; // Arrastraremos aquí el hijo "Pinchos"

    [Header("Configuración de Tiempos")]
    public float tiempoEsperaBajado = 2f;
    public float tiempoEsperaSubido = 1f;
    public float velocidadMovimiento = 10f;

    [Header("Posiciones")]
    public float alturaEscondido = -0.5f; // Altura cuando no se ven
    public float alturaFuera = 0.5f;      // Altura cuando atacan

    void Start()
    {
        // Iniciamos el ciclo infinito de la trampa
        StartCoroutine(CicloTrampa());
    }

    IEnumerator CicloTrampa()
    {
        while (true)
        {
            // 1. Esperar con los pinchos bajados
            yield return new WaitForSeconds(tiempoEsperaBajado);

            // 2. Subir pinchos rápidamente
            yield return MoverPinchos(alturaFuera);

            // 3. Esperar con los pinchos fuera (peligro)
            yield return new WaitForSeconds(tiempoEsperaSubido);

            // 4. Bajar pinchos
            yield return MoverPinchos(alturaEscondido);
        }
    }

    IEnumerator MoverPinchos(float alturaObjetivo)
    {
        Vector3 posicionObjetivo = new Vector3(objetoPinchos.localPosition.x, alturaObjetivo, objetoPinchos.localPosition.z);
        
        // Mientras no hayamos llegado a la altura deseada...
        while (Vector3.Distance(objetoPinchos.localPosition, posicionObjetivo) > 0.01f)
        {
            objetoPinchos.localPosition = Vector3.MoveTowards(
                objetoPinchos.localPosition, 
                posicionObjetivo, 
                velocidadMovimiento * Time.deltaTime
            );
            yield return null;
        }
        
        objetoPinchos.localPosition = posicionObjetivo;
    }
}