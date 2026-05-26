using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

// Adjuntar al mismo GameObject que LevelGenerator.
// LevelGenerator lo llama automáticamente en niveles 3-8.
public class GestorCaidaSuelo : MonoBehaviour
{
    [Header("Tiempos base (se sobreescriben por nivel)")]
    public float retardoInicial      = 4f;    // segundos de calma al entrar al nivel
    public float intervaloEntreFilas = 1.0f;  // se ajusta en Iniciar() según nivelActual
    public float duracionAdvertencia = 0.5f;  // temblor previo a la caída
    public float duracionCaida       = 0.6f;  // duración de la caída en sí

    [Header("Sacudida de Cámara")]
    // Añadir CinemachineImpulseSource a este GameObject.
    // Añadir CinemachineImpulseListener al prefab CinemachineCamera.
    //   → Impulse Duration: 0.15 s  |  Amplitude: 1  |  Channel: 1
    public CinemachineImpulseSource impulseSource;
    [Tooltip("Amplitud máxima durante la advertencia (temblor completo).")]
    public float fuerzaSacudidaMax = 0.4f;
    [Tooltip("Segundos entre impulsos consecutivos. Valor menor = temblor más continuo.")]
    public float intervalImpulso   = 0.07f;

    // Velocidad de caída por nivel (índice 0 = Nivel3, ..., índice 5 = Nivel8)
    static readonly float[] INTERVALOS_POR_NIVEL = { 7f, 6f, 5f, 5f, 2f, 4f };

    private Dictionary<int, List<GameObject>> porFila;
    private int totalFilas;
    private Transform jugador;
    private SaludJugador saludJugador;
    private LayerMask capaSuelo;

    // ── API pública ─────────────────────────────────────────────────────────
    public void Iniciar(Dictionary<int, List<GameObject>> datos, int filas,
                        GameObject jugadorObj, int nivelActual)
    {
        porFila      = datos;
        totalFilas   = filas;
        jugador      = jugadorObj.transform;
        saludJugador = jugadorObj.GetComponent<SaludJugador>();

        ControladorJugador ctrl = jugadorObj.GetComponent<ControladorJugador>();
        if (ctrl != null) capaSuelo = ctrl.capaSuelo;

        int idx = Mathf.Clamp(nivelActual - 3, 0, INTERVALOS_POR_NIVEL.Length - 1);
        intervaloEntreFilas = INTERVALOS_POR_NIVEL[idx];

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        StartCoroutine(SecuenciaCaida());
    }

    // ── Reasignación de fila (llamado por CajaEmpujable al moverse) ─────────
    /// <summary>
    /// Mueve un objeto de una fila a otra en el diccionario objetosPorFila.
    /// Así la caja cae con su fila actual, no con la fila donde fue spawneada.
    /// </summary>
    public void MoverObjetoEntreFilas(GameObject obj, int filaAnterior, int filaDestino)
    {
        if (porFila == null) return;

        if (porFila.ContainsKey(filaAnterior))
            porFila[filaAnterior].Remove(obj);

        if (!porFila.ContainsKey(filaDestino))
            porFila[filaDestino] = new List<GameObject>();

        if (!porFila[filaDestino].Contains(obj))
            porFila[filaDestino].Add(obj);
    }

    // ── Secuencia principal ─────────────────────────────────────────────────
    // Estructura: [calma] → [advertencia+caída] → [calma] → [advertencia+caída] → ...
    // La CAÍDA de cada fila ocurre exactamente a (intervaloEntreFilas) segundos de la anterior.
    // La ADVERTENCIA empieza (duracionAdvertencia) antes de la caída.
    IEnumerator SecuenciaCaida()
    {
        yield return new WaitForSeconds(retardoInicial);

        for (int f = 0; f < totalFilas - 1; f++)
        {
            // Período de calma: (intervalo - advertencia) segundos sin temblor
            float esperaQuieta = intervaloEntreFilas - duracionAdvertencia;
            if (esperaQuieta > 0f)
                yield return new WaitForSeconds(esperaQuieta);

            // Derrumbe bloqueante: el bucle espera a que termine antes de la siguiente fila
            if (porFila.ContainsKey(f) && porFila[f].Count > 0)
                yield return StartCoroutine(DerrumbarFila(f, porFila[f]));
            else
                yield return new WaitForSeconds(duracionAdvertencia + duracionCaida);
        }
    }

    // ── Derrumbe de una fila ────────────────────────────────────────────────
    IEnumerator DerrumbarFila(int indiceFila, List<GameObject> objetos)
    {
        // Capturar posiciones originales
        Vector3[] posOrig = new Vector3[objetos.Count];
        for (int i = 0; i < objetos.Count; i++)
            posOrig[i] = objetos[i] != null ? objetos[i].transform.position : Vector3.zero;

        // ── FASE 1: Advertencia — bloques tiemblan + cámara sacude sin parar ──
        float t = 0f;
        float timerImpulso = 0f;
        while (t < duracionAdvertencia)
        {
            t            += Time.deltaTime;
            timerImpulso += Time.deltaTime;

            // Temblor de bloques
            float temblor = Mathf.Sin(t * 45f) * 0.07f;
            for (int i = 0; i < objetos.Count; i++)
                if (objetos[i] != null)
                    objetos[i].transform.position = posOrig[i] + Vector3.up * temblor;

            // Sacudida de cámara continua a fuerza máxima
            if (timerImpulso >= intervalImpulso)
            {
                timerImpulso -= intervalImpulso;
                SacudirCamara(fuerzaSacudidaMax);
            }

            yield return null;
        }

        // Restaurar posición exacta antes de iniciar la caída
        for (int i = 0; i < objetos.Count; i++)
            if (objetos[i] != null)
                objetos[i].transform.position = posOrig[i];

        VerificarJugadorEnFila(indiceFila);

        // ── FASE 2: Caída con sacudida decreciente ──────────────────────────
        t = 0f;
        timerImpulso = 0f;
        while (t < duracionCaida)
        {
            t            += Time.deltaTime;
            timerImpulso += Time.deltaTime;

            // y = -½·g·t²  con g≈28
            float yOffset = -14f * t * t;
            for (int i = 0; i < objetos.Count; i++)
                if (objetos[i] != null)
                    objetos[i].transform.position = posOrig[i] + Vector3.up * yOffset;

            // Sacudida decreciente: de fuerza máxima → 0 a lo largo de la caída
            float progreso    = t / duracionCaida;           // 0 → 1
            float fuerzaActual = fuerzaSacudidaMax * (1f - progreso);
            if (timerImpulso >= intervalImpulso && fuerzaActual > 0.01f)
            {
                timerImpulso -= intervalImpulso;
                SacudirCamara(fuerzaActual);
            }

            yield return null;
        }

        VerificarJugadorEnFila(indiceFila);

        foreach (var obj in objetos)
            if (obj != null) Destroy(obj);

        StartCoroutine(VerificarFlotacion());
    }

    // ── Verificaciones de muerte ────────────────────────────────────────────

    void VerificarJugadorEnFila(int indiceFila)
    {
        if (jugador == null || saludJugador == null) return;
        if (Mathf.RoundToInt(jugador.position.z) == indiceFila)
            MuertePorCaida();
    }

    IEnumerator VerificarFlotacion()
    {
        yield return new WaitForFixedUpdate();

        if (jugador == null || saludJugador == null) yield break;

        bool haysuelo = Physics.Raycast(
            jugador.position + Vector3.up,
            Vector3.down,
            2f,
            capaSuelo,
            QueryTriggerInteraction.Ignore
        );

        if (!haysuelo)
            MuertePorCaida();
    }

    void MuertePorCaida()
    {
        if (saludJugador == null) return;
        saludJugador.RecibirDanio(saludJugador.vidasMax);
    }

    // ── Cámara ──────────────────────────────────────────────────────────────
    void SacudirCamara(float fuerza)
    {
        if (impulseSource == null) return;
        // Dirección completamente aleatoria en XY → efecto "cueva que se derrumba"
        Vector3 dir = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f
        );
        impulseSource.GenerateImpulse(dir * fuerza);
    }
}
