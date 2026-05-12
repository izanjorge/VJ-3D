using UnityEngine;
using System.Collections;

public class ControladorPanda : MonoBehaviour
{
    [Header("Movimiento (Saltitos)")]
    public float velocidad = 5f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;
    private bool estaMoviendose = false;
    private Vector3 escalaOriginal;

    [Header("Ataque ninja")]
    public GameObject cuchilloPrefab;
    public Transform puntoDeDisparo;
    public float cooldownAtaque = 0.5f;
    public float escalaCuchillo = 0.6f; // El multiplicador de tamaño
    private bool puedeAtacar = true;

    void Start()
    {
        // Guardamos la escala original para las animaciones
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        if (!estaMoviendose && puedeAtacar)
        {
            // Movimiento (Teclado numérico según tu código)
            if (Input.GetKeyDown(KeyCode.Keypad5)) StartCoroutine(Mover(Vector3.forward));
            else if (Input.GetKeyDown(KeyCode.Keypad2)) StartCoroutine(Mover(Vector3.back));
            else if (Input.GetKeyDown(KeyCode.Keypad1)) StartCoroutine(Mover(Vector3.left));
            else if (Input.GetKeyDown(KeyCode.Keypad3)) StartCoroutine(Mover(Vector3.right));

            // Ataque (Barra espaciadora)
            else if (Input.GetKeyDown(KeyCode.Space))
                StartCoroutine(LanzarCuchillo());
        }
    }

    IEnumerator Mover(Vector3 direccion)
    {
        estaMoviendose = true;
        transform.forward = direccion;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

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

    IEnumerator LanzarCuchillo()
    {
        puedeAtacar = false;

        if (cuchilloPrefab != null && puntoDeDisparo != null)
        {
            // 1. CREAR Y ESCALAR EL CUCHILLO
            GameObject nuevoCuchillo = Instantiate(cuchilloPrefab, puntoDeDisparo.position, transform.rotation);
            // Multiplicamos su escala actual por 0.6
            nuevoCuchillo.transform.localScale *= escalaCuchillo;

            // 2. ANIMACIÓN AGRESIVA (Squash & Stretch de ataque)
            float tiempoAnim = 0;
            float duracionAnim = 0.15f; // Muy rápida para que se sienta un "latigazo"

            while (tiempoAnim < duracionAnim)
            {
                tiempoAnim += Time.deltaTime;
                float t = tiempoAnim / duracionAnim;

                // El panda se estira hacia adelante (Z) y se encoge un poco de los lados
                transform.localScale = new Vector3(
                    escalaOriginal.x * 0.8f,
                    escalaOriginal.y * 0.8f,
                    escalaOriginal.z * 1.4f  // Estiramiento agresivo hacia adelante
                );
                yield return null;
            }

            transform.localScale = escalaOriginal;
        }

        yield return new WaitForSeconds(cooldownAtaque);
        puedeAtacar = true;
    }
}