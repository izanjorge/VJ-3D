using UnityEngine;
using System.Collections;

public class ControladorSlime : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;

    [Header("Animación de Gelatina")]
    public float estiramientoMaximo = 1.3f; // Cuánto se estira hacia arriba al saltar
    public float aplastamientoMaximo = 0.7f; // Cuánto se aplasta al tocar el suelo

    private bool estaMoviendose = false;
    private Vector3 escalaOriginal;

    void Start()
    {
        // Guardamos el tamaño original del slime para no deformarlo para siempre
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        // Movimiento con WASD o Flechas
        if (!estaMoviendose)
        {
            if (Input.GetKeyDown(KeyCode.I))
                StartCoroutine(MoverSlime(Vector3.forward));

            else if (Input.GetKeyDown(KeyCode.K))
                StartCoroutine(MoverSlime(Vector3.back));

            else if (Input.GetKeyDown(KeyCode.J))
                StartCoroutine(MoverSlime(Vector3.left));

            else if (Input.GetKeyDown(KeyCode.L))
                StartCoroutine(MoverSlime(Vector3.right));
        }
    }

    IEnumerator MoverSlime(Vector3 direccion)
    {
        estaMoviendose = true;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        MancharCasilla(posicionInicial); // <--- NUEVO: Mancha la casilla justo antes de saltar

        if (direccion != Vector3.zero)
        {
            transform.forward = direccion;
        }

        float tiempoTranscurrido = 0;
        float duracion = 1f / velocidad;

        while (tiempoTranscurrido < duracion)
        {
            // ... (Mantén aquí todo tu código de animación que ya tenías)
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = tiempoTranscurrido / duracion;

            Vector3 posicionActual = Vector3.Lerp(posicionInicial, posicionDestino, porcentaje);
            float curvaSalto = Mathf.Sin(porcentaje * Mathf.PI);
            posicionActual.y = posicionInicial.y + (curvaSalto * alturaSalto);
            transform.position = posicionActual;

            float deformacionY = Mathf.Lerp(aplastamientoMaximo, estiramientoMaximo, curvaSalto);
            float deformacionXZ = Mathf.Lerp(1.3f, 0.8f, curvaSalto);

            transform.localScale = new Vector3(
                escalaOriginal.x * deformacionXZ,
                escalaOriginal.y * deformacionY,
                escalaOriginal.z * deformacionXZ
            );

            yield return null;
        }

        transform.position = posicionDestino;

        MancharCasilla(posicionDestino); // <--- NUEVO: Mancha la casilla al aterrizar

        yield return StartCoroutine(ReboteAterrizaje());

        estaMoviendose = false;
    }

    IEnumerator ReboteAterrizaje()
    {
        // Un pequeño temblor extra al caer al suelo para darle mucho "jugo"
        float tiempo = 0;
        float duracionRebote = 0.15f;

        while (tiempo < duracionRebote)
        {
            tiempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(
                new Vector3(escalaOriginal.x * 1.3f, escalaOriginal.y * 0.7f, escalaOriginal.z * 1.3f), // Muy aplastado
                escalaOriginal, // Vuelve a la normalidad
                tiempo / duracionRebote
            );
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    private void MancharCasilla(Vector3 posicion)
    {
        RaycastHit[] impactos = Physics.RaycastAll(posicion + Vector3.up * 1f, Vector3.down, 3f);

        foreach (RaycastHit impacto in impactos)
        {
            Casilla casilla = impacto.collider.GetComponentInParent<Casilla>();
            if (casilla != null)
            {
                casilla.MancharConSlime();
                break;
            }
        }
    }
}