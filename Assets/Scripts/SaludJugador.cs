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

    void Awake()
    {
        VidasActuales = vidasMax;
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

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ActualizarUI()
    {
        if (textoVidas != null)
            textoVidas.text = "Vidas: " + VidasActuales;
    }
}
