using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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
    private GameObject canvasNivel;        // canvas con el indicador de nivel

    // Build Settings: 0=MainMenu, 1=Creditos, 2=Nivel0 … 11=Nivel9
    const int INDICE_CREDITOS  = 1;
    const int INDICE_NIVEL0    = 2;
    const int INDICE_NIVEL9    = 11;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        CrearIndicadorNivel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            VolverAlMenu();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        if (canvasNivel != null) Destroy(canvasNivel);
    }

    /// <summary>
    /// Llamado por LevelGenerator.Start() una vez generado el mapa.
    /// </summary>
    public void Iniciar(int filas, GameObject jugadorObj)
    {
        TotalFilas = filas;
        jugador    = jugadorObj.transform;
        puerta     = FindFirstObjectByType<Puerta>();

        // Contar enemigos desde las listas estáticas de cada controlador.
        // OnEnable() de cada controlador se ejecuta antes que cualquier Start(),
        // por lo que las listas ya están completas cuando LevelGenerator llama a Iniciar().
        enemigosVivos = ControladorSlime.slimesActivos.Count
                      + ControladorEsqueleto.esqueletosActivos.Count
                      + ControladorPanda.pandasActivos.Count
                      + (ControladorMetagross.Instancia != null ? 1 : 0);

        // Nota: Metagross (Nivel9) carga los Créditos directamente al morir,
        // por lo que su muerte NO pasa por NotificarMuerteEnemigo.
        // En Nivel9, la puerta se abre igualmente cuando los demás enemigos mueran.

        // Si no hay enemigos, la puerta se abre de inmediato
        if (enemigosVivos == 0)
            AbrirPuerta();
    }

    // El avance al siguiente nivel lo inicia ControladorJugador cuando el jugador
    // pulsa adelante desde el tile de la puerta (destino sin suelo + PuedeAvanzar).

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
        // El jugador debe caminar hasta la puerta y pulsar adelante para avanzar.
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

    /// <summary>Vuelve al Menú Principal (índice 0).</summary>
    public void VolverAlMenu()
    {
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Crea un Canvas ScreenSpace-Overlay con un texto "Nivel X" en la
    /// esquina superior derecha, sin interferir con la barra de Metagross
    /// (zona superior central) ni con las vidas (zona superior izquierda).
    /// </summary>
    void CrearIndicadorNivel()
    {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        // Nivel 1-based visible al jugador: Nivel0 (index 2) → "Nivel 1"
        int numNivel = buildIndex - INDICE_NIVEL0 + 1;

        // ── Canvas ──────────────────────────────────────────────────────────
        canvasNivel = new GameObject("CanvasNivel");
        Canvas canvas = canvasNivel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasNivel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasNivel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── Texto ────────────────────────────────────────────────────────────
        GameObject goTexto = new GameObject("TextoNivel");
        goTexto.transform.SetParent(canvasNivel.transform, false);

        TextMeshProUGUI tmp = goTexto.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Nivel " + numNivel;
        tmp.fontSize  = 48f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.BottomLeft;

        // Esquina inferior izquierda
        RectTransform rt = goTexto.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.00f, 0.00f);
        rt.anchorMax        = new Vector2(0.25f, 0.07f);
        rt.offsetMin        = new Vector2(10f, 10f);
        rt.offsetMax        = new Vector2(0f, 0f);
    }
}
