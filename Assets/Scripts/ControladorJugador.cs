using UnityEngine;
using System.Collections;

public class ControladorJugador : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;

    private bool estaMoviendose = false;

    void Update()
    {
        // Solo permitimos un nuevo movimiento si el personaje ha terminado el anterior
        if (!estaMoviendose)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                StartCoroutine(MoverJugador(Vector3.forward));

            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                StartCoroutine(MoverJugador(Vector3.back));

            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                StartCoroutine(MoverJugador(Vector3.left));

            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                StartCoroutine(MoverJugador(Vector3.right));
        }
    }

    IEnumerator MoverJugador(Vector3 direccion)
    {
        estaMoviendose = true;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        LimpiarCasilla(posicionInicial);

        // Rotar el personaje hacia la dirección del movimiento
        if (direccion != Vector3.zero)
        {
            transform.forward = direccion;
        }

        float tiempoTranscurrido = 0;
        float duracion = 1f / velocidad;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = tiempoTranscurrido / duracion;

            // Movimiento horizontal suave
            Vector3 posicionActual = Vector3.Lerp(posicionInicial, posicionDestino, porcentaje);

            // Efecto de salto (Arco en el eje Y)
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * alturaSalto;

            transform.position = posicionActual;

            yield return null;
        }

        // Aseguramos que termine en la posición exacta
        transform.position = posicionDestino;
        LimpiarCasilla(posicionDestino);
        estaMoviendose = false;
    }

    private void LimpiarCasilla(Vector3 posicion)
    {
        // Creamos una "burbuja" de detección de 0.5 metros alrededor de los pies del jugador.
        // Esto es mucho más robusto que un rayo porque abarca toda la casilla.
        Collider[] objetosPisados = Physics.OverlapSphere(posicion, 0.1f);

        foreach (Collider obj in objetosPisados)
        {
            // Buscamos si alguno de los objetos que hemos pisado es una Casilla
            Casilla casilla = obj.GetComponentInParent<Casilla>();

            if (casilla != null)
            {
                casilla.LimpiarSlime();
                // No ponemos "break;" aquí por si el jugador pisa entre dos casillas, 
                // así limpiará ambas por seguridad.
            }
        }
    }
}