using UnityEngine;
using System.Collections;

// Adjuntar al GameObject raíz del prefab TrampaFlechas.
// Dispara flechas en la dirección en que mira la trampa (puntoDisparo.forward),
// pasándole esa dirección directamente al Proyectil para que no dependa de
// ninguna rotación interna del modelo FBX.
public class TrampaDisparador : MonoBehaviour
{
    [Header("Prefab de la Flecha")]
    public GameObject flechaPrefab;

    [Header("Punto de salida")]
    [Tooltip("Transform desde el que sale la flecha. Si queda vacío se usa el raíz.")]
    public Transform puntoDisparo;

    [Header("Cadencia")]
    public float intervaloDisparo = 2.5f;
    public float retardoInicial   = 1.0f;

    void Start()
    {
        if (puntoDisparo == null)
            puntoDisparo = transform;

        StartCoroutine(CicloDisparo());
    }

    IEnumerator CicloDisparo()
    {
        yield return new WaitForSeconds(retardoInicial);
        while (true)
        {
            Disparar();
            yield return new WaitForSeconds(intervaloDisparo);
        }
    }

    void Disparar()
    {
        if (flechaPrefab == null) return;

        // TrampaFlechas tiene la cara de disparo en su eje -X local.
        // LevelGenerator: col0→Y+180, col6→Y+0. Con Y+180, -right = +X mundo (hacia centro).
        Vector3 dirDisparo = -puntoDisparo.right;

        // Flecha tiene la punta en +X local. Y+180 local alinea la punta con dirDisparo.
        Quaternion rotFinal = puntoDisparo.rotation * Quaternion.Euler(0, 180, 0);

        // Spawnear fuera del BoxCollider en la dirección de disparo correcta.
        Vector3 posDisparo = puntoDisparo.position + dirDisparo * 1.0f;

        GameObject flecha = Instantiate(flechaPrefab, posDisparo, rotFinal);

        Proyectil p = flecha.GetComponent<Proyectil>();
        if (p != null) p.Iniciar(dirDisparo);
    }
}
