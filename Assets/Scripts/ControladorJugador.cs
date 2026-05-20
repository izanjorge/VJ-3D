using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class ControladorJugador : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;

    [Header("Capas de Detección (Layers)")]
    public LayerMask capaSuelo;
    public LayerMask capaObstaculos;

    [Header("Economía del Juego")]
    public int numMonedas = 0;
    public TextMeshProUGUI marcadorTexto;

    // --- NUEVO: Máquina de estados básica ---
    private bool estaMoviendose = false;
    private bool estaAtacando = false; 

    void Start()
    {
        ActualizarMarcador();
    }

    void Update()
    {
        ManejarCambioEscenas();

        // Solo podemos actuar si no nos estamos moviendo ni atacando
        if (!estaMoviendose && !estaAtacando)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(AnimacionAtaque());
            }
            else
            {
                Vector3 direccion = Vector3.zero;

                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) direccion = Vector3.forward;
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) direccion = Vector3.back;
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) direccion = Vector3.left;
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) direccion = Vector3.right;

                if (direccion != Vector3.zero)
                {
                    Vector3 destino = transform.position + (direccion * distanciaPaso);

                    if (HaySueloEn(destino) && !HayObstaculoEnDestino(destino))
                    {
                        StartCoroutine(MoverJugador(direccion));
                    }
                    else
                    {
                        transform.forward = direccion;
                    }
                }
            }
        }
    }

    IEnumerator AnimacionAtaque()
    {
        estaMoviendose = true; 
        estaAtacando = true; // Entramos en estado "Ataque", por lo que seremos sordos a las monedas
        
        Vector3 posicionInicial = transform.position;
        Vector3 posicionPico = posicionInicial + (transform.forward * (distanciaPaso * 0.6f)); 
        
        float duracionFase = (1f / velocidad) * 0.4f; 
        float tiempoTranscurrido = 0;

        // FASE 1: Embestida
        while (tiempoTranscurrido < duracionFase)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = tiempoTranscurrido / duracionFase;
            Vector3 posicionActual = Vector3.Lerp(posicionInicial, posicionPico, porcentaje);
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * alturaSalto;
            transform.position = posicionActual;
            yield return null;
        }

        // Ejecutamos el impacto (aquí aparece la moneda)
        EjecutarGolpe();

        // FASE 2: Retroceso
        tiempoTranscurrido = 0;
        while (tiempoTranscurrido < duracionFase)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = tiempoTranscurrido / duracionFase;
            Vector3 posicionActual = Vector3.Lerp(posicionPico, posicionInicial, porcentaje);
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * alturaSalto; 
            transform.position = posicionActual;
            yield return null;
        }

        transform.position = posicionInicial;
        estaMoviendose = false;
        
        // ¡IMPORTANTE! Hemos vuelto a nuestra casilla, ya podemos volver a recoger monedas
        estaAtacando = false; 
    }

    void EjecutarGolpe()
    {
        Vector3 posicionAtaque = transform.position + (transform.forward * (distanciaPaso * 0.4f)) + (Vector3.up * 1f);
        Vector3 dimensionesMitad = new Vector3(0.4f, 1f, 0.4f);

        Collider[] objetosGolpeados = Physics.OverlapBox(posicionAtaque, dimensionesMitad, Quaternion.identity, capaObstaculos);

        foreach (Collider col in objetosGolpeados)
        {
            JarronDestruible jarron = col.GetComponentInParent<JarronDestruible>();
            if (jarron == null) jarron = col.GetComponentInChildren<JarronDestruible>();

            if (jarron != null)
            {
                jarron.Romper();
                break; 
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // CONDICIÓN SALVAVIDAS: Solo recojo la moneda si NO estoy atacando
        if (other.CompareTag("Moneda") && !estaAtacando)
        {
            // NUEVO: Apagamos el colisionador de la moneda inmediatamente para evitar doble conteo
            other.enabled = false; 
            
            numMonedas++;
            ActualizarMarcador();
            Destroy(other.gameObject);
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
        return Physics.Raycast(posicionDestino + Vector3.up, Vector3.down, 2f, capaSuelo, QueryTriggerInteraction.Ignore);
    }

    bool HayObstaculoEnDestino(Vector3 posicionDestino)
    {
        Vector3 centroCaja = posicionDestino + (Vector3.up * 1f);
        Vector3 dimensionesMitad = new Vector3(0.4f, 1f, 0.4f);
        // Ignoramos Triggers explícitamente: las trampas son Triggers y no deben bloquear el movimiento
        return Physics.CheckBox(centroCaja, dimensionesMitad, Quaternion.identity, capaObstaculos, QueryTriggerInteraction.Ignore);
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