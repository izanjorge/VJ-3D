using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScroller : MonoBehaviour
{
    [SerializeField] private RectTransform contenedor;
    [SerializeField] private float velocidad = 65f;

    // Posición Y a partir de la cual se considera que el scroll ha terminado
    // (el borde inferior del contenido ha superado la mitad superior de la pantalla).
    // Se calcula dinámicamente en Start.
    private float finScroll;
    private float posicionInicio;

    void Start()
    {
        if (contenedor == null) return;

        // Forzar el rebuild del layout para que rect.height sea correcto
        Canvas.ForceUpdateCanvases();

        RectTransform parentRect = contenedor.parent as RectTransform;
        float mitadPantalla  = parentRect != null ? parentRect.rect.height * 0.5f : 540f;
        float mitadContenido = contenedor.rect.height * 0.5f;

        // Empieza con el borde SUPERIOR del contenido justo DEBAJO del borde inferior de la pantalla.
        // Así el primer elemento (nombre de los desarrolladores) entra desde abajo.
        posicionInicio = -(mitadPantalla + mitadContenido);

        // Termina cuando el borde INFERIOR del contenido supera el borde SUPERIOR de la pantalla.
        finScroll = mitadPantalla + mitadContenido;

        contenedor.anchoredPosition = new Vector2(0f, posicionInicio);
    }

    void Update()
    {
        if (contenedor == null) return;

        contenedor.anchoredPosition += Vector2.up * velocidad * Time.deltaTime;

        // Al llegar al final, volver al principio (bucle)
        if (contenedor.anchoredPosition.y > finScroll)
            contenedor.anchoredPosition = new Vector2(0f, posicionInicio);

        if (Input.GetKeyDown(KeyCode.Escape))
            VolverAlMenu();
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene(0);
    }
}
