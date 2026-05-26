using UnityEngine;
using TMPro;

// Colocar este script en el mismo GameObject que tiene el TextMeshProUGUI
// del contador de monedas (dentro del Canvas de la escena).
// Funciona igual que UIVidas: se suscribe al evento del jugador persistente
// (DontDestroyOnLoad) cada vez que se carga la escena.
public class UIMonedas : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoMonedas;

    private ControladorJugador jugador;

    void Start()
    {
        // Usar el singleton de SaludJugador (establecido en Awake, antes que Start)
        // garantiza que obtenemos el jugador PERSISTENTE y no el duplicado
        // que Awake acaba de marcar para destruir (con numMonedas = 0).
        if (SaludJugador.Instancia != null)
            jugador = SaludJugador.Instancia.GetComponent<ControladorJugador>();

        // Fallback por si se entra al nivel sin pasar por el menú
        if (jugador == null)
            jugador = FindFirstObjectByType<ControladorJugador>();

        if (jugador != null)
        {
            jugador.OnMonedasCambiadas += ActualizarTexto;
            // Sincronizamos el valor acumulado (monedas de escenas anteriores)
            ActualizarTexto(jugador.numMonedas);
        }
        else
        {
            Debug.LogWarning("UIMonedas: No se encontró ControladorJugador en la escena.");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse para evitar llamadas sobre objetos destruidos al cambiar escena
        if (jugador != null)
            jugador.OnMonedasCambiadas -= ActualizarTexto;
    }

    void ActualizarTexto(int monedas)
    {
        if (textoMonedas != null)
            textoMonedas.text = "x" + monedas;
    }
}
