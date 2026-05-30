using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Adjuntar al LevelManager (junto a LevelGenerator y GestorCaidaSuelo).
/// Gestiona:
///   - Conteo de enemigos vivos → abre la puerta cuando llega a 0
///   - Detección de salida → carga el siguiente nivel cuando el jugador
///     intenta avanzar más allá del último tile
///   - Transición al nivel siguiente o a Créditos al completar Nivel9
///
/// LevelGenerator llama a Iniciar() al final de su Start().
/// </summary>
public class GestorNivel : MonoBehaviour
{
    // ── Singleton (solo para la escena actual, NO DontDestroyOnLoad) ────────
    public static GestorNivel Instancia { get; private set; }

    // Propiedades consultadas por ControladorJugador
    public bool PuedeAvanzar  { get; private set; }
    public int  TotalFilas    { get; private set; }

    private int       enemigosVivos;
    private bool      avanzando = false;   // evita llamadas múltiples
    private Transform jugador;
    private Puerta    puerta;

    // Build Settings: 0=MainMenu, 1=Creditos, 2=Nivel0 … 11=Nivel9
    const int INDICE_CREDITOS  = 1;
    const int INDICE_NIVEL0    = 2;
    const int INDICE_NIVEL9    = 11;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    /// <summary>
    /// Llamado por LevelGenerator.Start() una vez generado el mapa.
    /// </summary>
    public void Iniciar(int filas, GameObject jugadorObj)
    {
        TotalFilas = filas;
        jugador    = jugadorObj.transform;
        puerta     = FindFirstObjectByType<Puerta>();

        // Contar todos los Enemigo de la escena
        enemigosVivos = FindObjectsByType<Enemigo>(FindObjectsSortMode.None).Length;

        // Si no hay enemigos, la puerta se abre de inmediato
        if (enemigosVivos == 0)
            AbrirPuerta();
    }

    // ── Update: comprueba si el jugador ha llegado a la salida ──────────────
    void Update()
    {
        if (!PuedeAvanzar || jugador == null || avanzando) return;

        // La puerta bloquea z=filas-1 mientras está cerrada.
        // En cuanto se abre y el jugador pisa ese tile, avanzamos al siguiente nivel.
        if (Mathf.RoundToInt(jugador.position.z) >= TotalFilas - 1)
            SiguienteNivel();
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// <summary>Llamado por Enemigo.Morir().</summary>
    public void NotificarMuerteEnemigo()
    {
        enemigosVivos = Mathf.Max(0, enemigosVivos - 1);
        if (enemigosVivos <= 0)
            AbrirPuerta();
    }

    /// <summary>
    /// Llamado por ControladorJugador cuando el jugador intenta avanzar
    /// más allá del último tile (y PuedeAvanzar == true).
    /// </summary>
    public void SiguienteNivel()
    {
        if (avanzando) return;
        avanzando = true;
        StartCoroutine(CargarSiguienteNivel());
    }

    // ── Privados ─────────────────────────────────────────────────────────────
    void AbrirPuerta()
    {
        PuedeAvanzar = true;
        puerta?.Abrir();

        // Si el jugador ya está en la última fila cuando se abre la puerta,
        // avanzamos automáticamente (para que no quede "atascado" esperando).
        if (jugador != null && Mathf.RoundToInt(jugador.position.z) >= TotalFilas - 1)
            SiguienteNivel();
    }

    IEnumerator CargarSiguienteNivel()
    {
        // Pequeña pausa para que la animación de apertura se aprecie
        yield return new WaitForSeconds(0.6f);

        int indiceActual = SceneManager.GetActiveScene().buildIndex;

        if (indiceActual >= INDICE_NIVEL9)
            SceneManager.LoadScene(INDICE_CREDITOS);   // ¡Juego completado!
        else
            SceneManager.LoadScene(indiceActual + 1);  // Siguiente nivel
    }
}
