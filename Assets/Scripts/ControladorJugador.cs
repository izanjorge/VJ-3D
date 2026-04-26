using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro; // Necesario para controlar el texto de la UI

public class ControladorJugador : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;
    public LayerMask capaSuelo;

    [Header("Economía del Juego")]
    public int numMonedas = 0;
    public TextMeshProUGUI marcadorTexto; // Arrastra aquí el objeto TextoMonedas

    private bool estaMoviendose = false;

    void Start()
    {
        ActualizarMarcador();
    }

    void Update()
    {
        ManejarCambioEscenas();

        if (!estaMoviendose)
        {
            Vector3 direccion = Vector3.zero;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) direccion = Vector3.forward;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) direccion = Vector3.back;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) direccion = Vector3.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) direccion = Vector3.right;

            if (direccion != Vector3.zero)
            {
                Vector3 destino = transform.position + (direccion * distanciaPaso);
                
                if (HaySueloEn(destino))
                {
                    StartCoroutine(MoverJugador(direccion));
                }
            }
        }
    }

    // Detector de colisión con la moneda
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Moneda"))
        {
            numMonedas++;
            ActualizarMarcador();
            Destroy(other.gameObject); // La moneda desaparece
        }
    }

    void ActualizarMarcador()
    {
        if (marcadorTexto != null)
        {
            marcadorTexto.text = "Monedas: " + numMonedas;
        }
    }

    bool HaySueloEn(Vector3 posicionDestino)
    {
        return Physics.Raycast(posicionDestino + Vector3.up, Vector3.down, 2f, capaSuelo);
    }

    IEnumerator MoverJugador(Vector3 direccion)
    {
        estaMoviendose = true;
        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        transform.forward = direccion;

        float tiempoTranscurrido = 0;
        float duracion = 1f / velocidad;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = tiempoTranscurrido / duracion;
            Vector3 posicionActual = Vector3.Lerp(posicionInicial, posicionDestino, porcentaje);
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * alturaSalto;
            transform.position = posicionActual;
            yield return null;
        }

        transform.position = posicionDestino;
        estaMoviendose = false;
    }

    void ManejarCambioEscenas()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                if (i < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(i);
                }
            }
        }
    }
}