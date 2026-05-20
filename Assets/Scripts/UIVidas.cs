using UnityEngine;
using UnityEngine.UI;

// Colocar este script en un GameObject vacío dentro del Canvas.
// Requiere un array de Image[] con tantos elementos como vidasMax.
// Asignar un sprite de corazón lleno y uno vacío en el Inspector.
public class UIVidas : MonoBehaviour
{
    [Header("Referencias")]
    public SaludJugador saludJugador;
    public Image[] iconosVida;

    [Header("Sprites")]
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    void Start()
    {
        if (saludJugador == null)
            saludJugador = FindFirstObjectByType<SaludJugador>();

        saludJugador.OnVidaCambiada += ActualizarIconos;

        ActualizarIconos(saludJugador.VidasActuales, iconosVida.Length);
    }

    void OnDestroy()
    {
        if (saludJugador != null)
            saludJugador.OnVidaCambiada -= ActualizarIconos;
    }

    void ActualizarIconos(int vidasActuales, int vidasMax)
    {
        for (int i = 0; i < iconosVida.Length; i++)
        {
            if (iconosVida[i] == null) continue;
            iconosVida[i].sprite = i < vidasActuales ? corazonLleno : corazonVacio;
        }
    }
}
