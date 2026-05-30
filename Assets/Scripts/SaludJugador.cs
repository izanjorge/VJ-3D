using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class SaludJugador : MonoBehaviour
{
    [Header("Configuración")]
    public int vidasMax = 3;
    public float duracionInvulnerabilidad = 1.5f;
    public float retardoReinicioEscena = 1.5f;

    [Header("Feedback Visual")]
    public Renderer rendererJugador;
    public float intervaloParpadeo = 0.1f;

    [Header("UI (Opcional)")]
    public TextMeshProUGUI textoVidas;

    public int VidasActuales { get; private set; }
    public bool EsInvulnerable { get; private set; }
    public bool EstaVivo => VidasActuales > 0;

    // Suscribirse para reaccionar a cambios de vida o muerte desde otros sistemas (UI, audio, etc.)
    public System.Action<int, int> OnVidaCambiada; // (vidasActuales, vidasMax)
    public System.Action OnMuerte;

    // ── Singleton persistente entre escenas ─────────────────────────────────
    public static SaludJugador Instancia { get; private set; }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            // Ya existe un jugador persistente: actualizar referencia en LevelGenerator
            // (esto ocurre en Awake, antes de que LevelGenerator.Start() se ejecute).
            LevelGenerator gen = FindFirstObjectByType<LevelGenerator>();
            if (gen != null) gen.player = Instancia.gameObject;
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);
        VidasActuales = vidasMax;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnEscenaCargada;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnEscenaCargada;
    }

    // Se llama automáticamente cada vez que termina de cargar una escena.
    void OnEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        StartCoroutine(ReconectarEscena());
    }

    IEnumerator ReconectarEscena()
    {
        // Esperar un frame para que todos los Start() de la escena hayan corrido.
        yield return null;

        // Reactivar controles por si quedaron desactivados (muerte interrumpida).
        ControladorJugador controlador = GetComponent<ControladorJugador>();
        if (controlador != null) controlador.enabled = true;

        // Renotificar la UI con las vidas actuales para que los corazones
        // se muestren correctamente en la nueva escena.
        OnVidaCambiada?.Invoke(VidasActuales, vidasMax);
        ActualizarUI();
    }

    void Start()
    {
        ActualizarUI();
    }

    public void RecibirDanio(int cantidad)
    {
        if (EsInvulnerable || !EstaVivo) return;

        VidasActuales = Mathf.Max(0, VidasActuales - cantidad);
        OnVidaCambiada?.Invoke(VidasActuales, vidasMax);
        ActualizarUI();

        if (VidasActuales <= 0)
        {
            StartCoroutine(SecuenciaMuerte());
        }
        else
        {
            StartCoroutine(SecuenciaInvulnerabilidad());
        }
    }

    IEnumerator SecuenciaInvulnerabilidad()
    {
        EsInvulnerable = true;

        float tiempoRestante = duracionInvulnerabilidad;
        while (tiempoRestante > 0f)
        {
            if (rendererJugador != null)
                rendererJugador.enabled = !rendererJugador.enabled;
            yield return new WaitForSeconds(intervaloParpadeo);
            tiempoRestante -= intervaloParpadeo;
        }

        if (rendererJugador != null)
            rendererJugador.enabled = true;

        EsInvulnerable = false;
    }

    IEnumerator SecuenciaMuerte()
    {
        OnMuerte?.Invoke();

        // Detenemos los controles inmediatamente; al deshabilitar el componente
        // Unity cancela todas sus corrutinas activas (movimiento, ataque, etc.)
        ControladorJugador controlador = GetComponent<ControladorJugador>();
        if (controlador != null) controlador.enabled = false;

        // Parpadeo acelerado como feedback de muerte
        float tiempoRestante = retardoReinicioEscena;
        float intervaloRapido = intervaloParpadeo * 0.5f;
        while (tiempoRestante > 0f)
        {
            if (rendererJugador != null)
                rendererJugador.enabled = !rendererJugador.enabled;
            yield return new WaitForSeconds(intervaloRapido);
            tiempoRestante -= intervaloRapido;
        }

        // Resetear vidas y monedas antes de volver al MainMenu
        VidasActuales = vidasMax;
        OnVidaCambiada?.Invoke(VidasActuales, vidasMax);

        ControladorJugador ctrl = GetComponent<ControladorJugador>();
        if (ctrl != null)
        {
            ctrl.numMonedas = 0;
            ctrl.OnMonedasCambiadas?.Invoke(0);
        }

        SceneManager.LoadScene(0); // MainMenu
    }

    void ActualizarUI()
    {
        if (textoVidas != null)
            textoVidas.text = "Vidas: " + VidasActuales;
    }
}
