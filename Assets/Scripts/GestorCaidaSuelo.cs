using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Adjuntar al mismo GameObject que LevelGenerator.
// LevelGenerator lo llama automáticamente en niveles 3-8.
public class GestorCaidaSuelo : MonoBehaviour
{
    [Header("Tiempos")]
    public float retardoInicial     = 4f;    // segundos antes de que empiece a caer
    public float intervaloEntreFilas = 1.0f; // segundos entre cada fila que cae
    public float duracionAdvertencia = 0.35f; // temblor previo a la caída
    public float duracionCaida       = 0.55f; // duración de la caída en sí

    private Dictionary<int, List<GameObject>> porFila;
    private int totalFilas;
    private Transform jugador;
    private SaludJugador saludJugador;

    // Llamado por LevelGenerator tras generar el mapa
    public void Iniciar(Dictionary<int, List<GameObject>> datos, int filas, GameObject jugadorObj)
    {
        porFila       = datos;
        totalFilas    = filas;
        jugador       = jugadorObj.transform;
        saludJugador  = jugadorObj.GetComponent<SaludJugador>();
        StartCoroutine(SecuenciaCaida());
    }

    IEnumerator SecuenciaCaida()
    {
        yield return new WaitForSeconds(retardoInicial);

        // Las filas caen desde detrás del jugador (f=0) hacia adelante,
        // obligando al jugador a avanzar sin posibilidad de retroceder.
        for (int f = 0; f < totalFilas - 1; f++)
        {
            if (porFila.ContainsKey(f) && porFila[f].Count > 0)
                StartCoroutine(DerrumbarFila(f, porFila[f]));

            yield return new WaitForSeconds(intervaloEntreFilas);
        }
    }

    IEnumerator DerrumbarFila(int indiceFila, List<GameObject> objetos)
    {
        // Capturar posiciones originales
        Vector3[] posOrig = new Vector3[objetos.Count];
        for (int i = 0; i < objetos.Count; i++)
            posOrig[i] = objetos[i] != null ? objetos[i].transform.position : Vector3.zero;

        // ── FASE 1: Temblor de advertencia ──────────────────────────
        float t = 0f;
        while (t < duracionAdvertencia)
        {
            t += Time.deltaTime;
            float temblor = Mathf.Sin(t * 40f) * 0.06f;
            for (int i = 0; i < objetos.Count; i++)
                if (objetos[i] != null)
                    objetos[i].transform.position = posOrig[i] + Vector3.up * temblor;
            yield return null;
        }

        // Daño si el jugador está sobre esta fila cuando empieza a caer
        VerificarJugadorEnFila(indiceFila);

        // ── FASE 2: Caída con aceleración (simulación de gravedad) ───
        t = 0f;
        while (t < duracionCaida)
        {
            t += Time.deltaTime;
            float yOffset = -12f * t * t; // y = -½·g·t² con g≈24
            for (int i = 0; i < objetos.Count; i++)
                if (objetos[i] != null)
                    objetos[i].transform.position = posOrig[i] + Vector3.up * yOffset;
            yield return null;
        }

        // Comprobación final antes de destruir
        VerificarJugadorEnFila(indiceFila);

        foreach (var obj in objetos)
            if (obj != null) Destroy(obj);
    }

    void VerificarJugadorEnFila(int indiceFila)
    {
        if (jugador == null || saludJugador == null) return;
        if (Mathf.RoundToInt(jugador.position.z) == indiceFila)
            saludJugador.RecibirDanio(1);
    }
}
