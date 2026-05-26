using UnityEngine;
using System.Collections;

public class ControladorEsqueleto : MonoBehaviour
{
    [Header("Configuración de Movimiento Agresivo")]
    public float velocidad = 6f; // Un poco más rápido que el jugador normal
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.8f; // Un salto más alto y exagerado

    private bool estaMoviendose = false;
    private bool estaAtacando = false;

    void Update()
    {
        // Solo puede moverse o atacar si no está ya haciendo otra cosa
        if (!estaMoviendose && !estaAtacando)
        {
            // Movimiento con T, G, F, H
            if (Input.GetKeyDown(KeyCode.T))
                StartCoroutine(MoverEsqueleto(Vector3.forward));

            else if (Input.GetKeyDown(KeyCode.G))
                StartCoroutine(MoverEsqueleto(Vector3.back));

            else if (Input.GetKeyDown(KeyCode.F))
                StartCoroutine(MoverEsqueleto(Vector3.left));

            else if (Input.GetKeyDown(KeyCode.H))
                StartCoroutine(MoverEsqueleto(Vector3.right));

            // Ataque con la Y
            else if (Input.GetKeyDown(KeyCode.Y))
                StartCoroutine(AtaqueAgresivo());
        }
    }

    IEnumerator MoverEsqueleto(Vector3 direccion)
    {
        estaMoviendose = true;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        // Rotar inmediatamente hacia donde va a saltar
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

            // 1. Movimiento base hacia adelante y hacia arriba
            Vector3 posicionActual = Vector3.Lerp(posicionInicial, posicionDestino, porcentaje);
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * alturaSalto;
            transform.position = posicionActual;

            // 2. EL TOQUE AGRESIVO: Hacemos que se incline hacia adelante mientras salta
            // Usamos el seno para que se incline al subir y se enderece al caer
            float inclinacion = Mathf.Sin(porcentaje * Mathf.PI) * 25f; // Se inclina 25 grados
            transform.rotation = Quaternion.LookRotation(direccion) * Quaternion.Euler(inclinacion, 0, 0);

            yield return null;
        }

        // Aterrizaje: aseguramos la posición y rotación exactas
        transform.position = posicionDestino;
        transform.rotation = Quaternion.LookRotation(direccion);

        estaMoviendose = false;
    }

    IEnumerator AtaqueAgresivo()
    {
        estaAtacando = true;

        Vector3 posicionOriginal = transform.position;
        Vector3 escalaOriginal = transform.localScale;
        Quaternion rotacionOriginal = transform.rotation;

        // El esqueleto atacará justo a la casilla de enfrente (1 metro por delante)
        Vector3 posicionEmbestida = posicionOriginal + (transform.forward * 1f);

        // FASE 1: ANTICIPACIÓN (Toma impulso echándose hacia atrás)
        float tiempo = 0;
        float duracionAnticipacion = 0.15f; // Breve momento de tensión
        Vector3 posicionAtras = posicionOriginal - (transform.forward * 0.2f);

        while (tiempo < duracionAnticipacion)
        {
            tiempo += Time.deltaTime;
            float porcentaje = tiempo / duracionAnticipacion;

            transform.position = Vector3.Lerp(posicionOriginal, posicionAtras, porcentaje);
            // Se inclina hacia atrás (-20 grados) para coger fuerza
            transform.rotation = rotacionOriginal * Quaternion.Euler(-20f * porcentaje, 0, 0);
            yield return null;
        }

        // FASE 2: SALTO Y EMBESTIDA (¡El Golpe!)
        tiempo = 0;
        float duracionGolpe = 0.15f; // Rapidísimo

        while (tiempo < duracionGolpe)
        {
            tiempo += Time.deltaTime;
            float porcentaje = tiempo / duracionGolpe;

            Vector3 posicionActual = Vector3.Lerp(posicionAtras, posicionEmbestida, porcentaje);
            // Arco de salto pronunciado (Salta más alto de lo normal para caer con fuerza)
            posicionActual.y += Mathf.Sin(porcentaje * Mathf.PI) * 1.5f;
            transform.position = posicionActual;

            // Pasa de estar inclinado hacia atrás a dar un cabezazo violento hacia adelante (45 grados)
            transform.rotation = rotacionOriginal * Quaternion.Euler(Mathf.Lerp(-20f, 45f, porcentaje), 0, 0);

            yield return null;
        }

        // IMPACTO: Fijamos la posición y aplastamos el modelo por la fuerza del golpe
        transform.position = posicionEmbestida;
        transform.localScale = new Vector3(escalaOriginal.x * 1.2f, escalaOriginal.y * 0.5f, escalaOriginal.z * 1.2f);
        transform.rotation = rotacionOriginal * Quaternion.Euler(45f, 0, 0);

        // --- AQUÍ IRÍA EL CÓDIGO PARA QUITARLE VIDA AL JUGADOR ---

        // Pausa dramática en el suelo (le da mucha contundencia al impacto)
        yield return new WaitForSeconds(0.15f);

        // FASE 3: RECUPERACIÓN (Vuelve a su sitio recomponiéndose)
        tiempo = 0;
        float duracionRecuperacion = 0.25f;

        while (tiempo < duracionRecuperacion)
        {
            tiempo += Time.deltaTime;
            float porcentaje = tiempo / duracionRecuperacion;

            // Volvemos a la posición, rotación y escala originales suavemente
            transform.position = Vector3.Lerp(posicionEmbestida, posicionOriginal, porcentaje);
            transform.localScale = Vector3.Lerp(transform.localScale, escalaOriginal, porcentaje);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotacionOriginal, porcentaje);

            yield return null;
        }

        // Aseguramos que quede perfecto al terminar por si ha habido micro-desajustes
        transform.position = posicionOriginal;
        transform.localScale = escalaOriginal;
        transform.rotation = rotacionOriginal;

        estaAtacando = false;
    }
}