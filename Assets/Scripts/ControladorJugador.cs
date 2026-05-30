using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

    // Igual que SaludJugador.OnVidaCambiada: cualquier UI de la escena
    // activa se suscribe a este evento para mostrar el contador.
    public System.Action<int> OnMonedasCambiadas;

    // --- Máquina de estados básica ---
    private bool estaMoviendose = false;
    private bool estaAtacando = false;

    // --- Ralentización por rastro de slime ---
    private float velocidadNormal;
    private Coroutine corutinRalentizacion;

    void Start()
    {
        velocidadNormal = velocidad;
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
                        // Casilla libre: mover normalmente
                        StartCoroutine(MoverJugador(direccion));
                    }
                    else if (HaySueloEn(destino))
                    {
                        // Hay un obstáculo — ¿es una caja empujable?
                        CajaEmpujable caja = ObtenerCajaEn(destino);
                        if (caja != null && caja.IntentarEmpujar(direccion, 1f / velocidad))
                        {
                            // La caja cedió: el jugador también avanza (animaciones simultáneas)
                            StartCoroutine(MoverJugador(direccion));
                        }
                        else
                        {
                            // Obstáculo fijo o caja bloqueada: solo giramos hacia él
                            transform.forward = direccion;
                        }
                    }
                    else
                    {
                        // Sin suelo al frente: solo giramos (evita caer al vacío)
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

        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorAtaque);

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
                AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorImpacto);
                jarron.Romper();
                break;
            }
        }

        Vector3 dirAtaque = transform.forward;

        // ── Slimes ─────────────────────────────────────────────────────────
        foreach (ControladorSlime slime in ControladorSlime.slimesActivos)
        {
            if (slime == null || slime.EstaMuerto) continue;
            Vector3 h = VectorXZ(slime.transform.position - transform.position);
            if (h.magnitude < 1.5f && Vector3.Dot(h.normalized, dirAtaque) > 0.5f)
            { AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorImpacto); slime.RecibirGolpe(); return; }
        }

        // ── Esqueletos ─────────────────────────────────────────────────────
        foreach (ControladorEsqueleto esq in ControladorEsqueleto.esqueletosActivos)
        {
            if (esq == null || esq.EstaMuerto) continue;
            Vector3 h = VectorXZ(esq.transform.position - transform.position);
            if (h.magnitude < 1.5f && Vector3.Dot(h.normalized, dirAtaque) > 0.5f)
            { AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorImpacto); esq.RecibirGolpe(); return; }
        }

        // ── Pandas ─────────────────────────────────────────────────────────
        foreach (ControladorPanda panda in ControladorPanda.pandasActivos)
        {
            if (panda == null || panda.EstaMuerto) continue;
            Vector3 h = VectorXZ(panda.transform.position - transform.position);
            if (h.magnitude < 1.5f && Vector3.Dot(h.normalized, dirAtaque) > 0.5f)
            { AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorImpacto); panda.RecibirGolpe(); return; }
        }

        // ── Metagross (jefe — rango mayor, ángulo más amplio) ──────────────
        if (ControladorMetagross.Instancia != null && !ControladorMetagross.Instancia.EstaMuerto)
        {
            Vector3 h = VectorXZ(ControladorMetagross.Instancia.transform.position - transform.position);
            if (h.magnitude < 2.5f && Vector3.Dot(h.normalized, dirAtaque) > 0.3f)
            { AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorImpacto); ControladorMetagross.Instancia.RecibirGolpe(); }
        }
    }

    // Aplana un vector a XZ (ignora la diferencia de altura)
    static Vector3 VectorXZ(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void OnTriggerEnter(Collider other)
    {
        // CONDICIÓN SALVAVIDAS: Solo recojo la moneda si NO estoy atacando
        if (other.CompareTag("Moneda") && !estaAtacando)
        {
            // NUEVO: Apagamos el colisionador de la moneda inmediatamente para evitar doble conteo
            other.enabled = false;

            AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMoneda, AudioManager.Instancia.volumenMoneda);
            numMonedas++;
            ActualizarMarcador();
            Destroy(other.gameObject);
        }
    }

    void ActualizarMarcador()
    {
        OnMonedasCambiadas?.Invoke(numMonedas);
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

    /// <summary>
    /// Busca un componente CajaEmpujable en la posición dada.
    /// Devuelve null si el obstáculo no es una caja.
    /// </summary>
    CajaEmpujable ObtenerCajaEn(Vector3 posicionDestino)
    {
        Vector3 centro = posicionDestino + Vector3.up;
        Vector3 mitad  = new Vector3(0.4f, 1f, 0.4f);
        Collider[] cols = Physics.OverlapBox(centro, mitad, Quaternion.identity, capaObstaculos, QueryTriggerInteraction.Ignore);
        foreach (Collider col in cols)
        {
            CajaEmpujable caja = col.GetComponentInParent<CajaEmpujable>()
                              ?? col.GetComponent<CajaEmpujable>();
            if (caja != null) return caja;
        }
        return null;
    }

    IEnumerator MoverJugador(Vector3 direccion)
    {
        estaMoviendose = true;
        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxSalto);

        LimpiarCasilla(posicionInicial);
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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorPaso);
        ComprobarRastroSlime(posicionDestino); // Antes de limpiar para detectar el rastro
        LimpiarCasilla(posicionDestino);
        ComprobarContactoEnemigos();
        estaMoviendose = false;
    }

    // ------------------------------------------------------------------
    // Ralentización por rastro de slime
    // ------------------------------------------------------------------

    void ComprobarRastroSlime(Vector3 posicion)
    {
        Vector3 centroCasilla = new Vector3(Mathf.Round(posicion.x), 0f, Mathf.Round(posicion.z));
        Collider[] cols = Physics.OverlapSphere(centroCasilla, 0.4f);
        foreach (Collider col in cols)
        {
            Casilla casilla = col.GetComponentInParent<Casilla>();
            if (casilla != null && casilla.tieneSlime)
            {
                AplicarRalentizacion(2f);
                break;
            }
        }
    }

    void AplicarRalentizacion(float duracion)
    {
        if (corutinRalentizacion != null) StopCoroutine(corutinRalentizacion);
        corutinRalentizacion = StartCoroutine(CorutinRalentizacion(duracion));
    }

    IEnumerator CorutinRalentizacion(float duracion)
    {
        velocidad = velocidadNormal * 0.5f;
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxJugadorRalentizado);
        yield return new WaitForSeconds(duracion);
        velocidad = velocidadNormal;
        corutinRalentizacion = null;
    }

    // ------------------------------------------------------------------
    // Contacto con cualquier enemigo al aterrizar en una casilla
    // ------------------------------------------------------------------

    void ComprobarContactoEnemigos()
    {
        Vector3 miPos = new Vector3(transform.position.x, 0f, transform.position.z);

        foreach (ControladorSlime s in ControladorSlime.slimesActivos)
        {
            if (s == null || s.EstaMuerto) continue;
            if (Vector3.Distance(miPos, VectorXZ(s.transform.position)) < 0.7f)
            { SaludJugador.Instancia?.RecibirDanio(1); return; }
        }
        foreach (ControladorEsqueleto e in ControladorEsqueleto.esqueletosActivos)
        {
            if (e == null || e.EstaMuerto) continue;
            if (Vector3.Distance(miPos, VectorXZ(e.transform.position)) < 0.7f)
            { SaludJugador.Instancia?.RecibirDanio(1); return; }
        }
        foreach (ControladorPanda p in ControladorPanda.pandasActivos)
        {
            if (p == null || p.EstaMuerto) continue;
            if (Vector3.Distance(miPos, VectorXZ(p.transform.position)) < 0.7f)
            { SaludJugador.Instancia?.RecibirDanio(1); return; }
        }
        if (ControladorMetagross.Instancia != null && !ControladorMetagross.Instancia.EstaMuerto)
        {
            if (Vector3.Distance(miPos, VectorXZ(ControladorMetagross.Instancia.transform.position)) < 1.5f)
            { SaludJugador.Instancia?.RecibirDanio(1); return; }
        }
    }

    // Elimina el rastro de slime de la casilla pisada
    private void LimpiarCasilla(Vector3 posicion)
    {
        Vector3 centroCasilla = new Vector3(Mathf.Round(posicion.x), 0f, Mathf.Round(posicion.z));
        Collider[] objetosPisados = Physics.OverlapSphere(centroCasilla, 0.4f);
        foreach (Collider obj in objetosPisados)
        {
            Casilla casilla = obj.GetComponentInParent<Casilla>();
            if (casilla != null)
                casilla.LimpiarSlime();
        }
    }

    void ManejarCambioEscenas()
    {
        // Build Settings: 0=MainMenu, 1=Creditos, 2=Nivel0 ... 11=Nivel9
        // Tecla 0 -> Nivel0 (indice 2), Tecla 9 -> Nivel9 (indice 11)
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                int indiceEscena = i + 2;
                if (indiceEscena < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(indiceEscena);
                }
            }
        }
    }
}