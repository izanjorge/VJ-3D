using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Jefe final del juego.
/// - Aparece cayendo desde el cielo al inicio del nivel.
/// - IA: Bloquea la posición del jugador → espera 2 s → salta → lanza cuchillos en cruz → repite.
/// - Barra de vida 3D (verde→rojo) encima de la cabeza.
/// - Necesita varios golpes del jugador para morir.
/// - Al morir: animación épica → carga la escena de Créditos (índice 1).
/// </summary>
public class ControladorMetagross : MonoBehaviour
{
    // ── Singleton (un solo jefe por nivel) ───────────────────────────────────
    public static ControladorMetagross Instancia { get; private set; }

    [Header("Vida")]
    public int vidasMax = 5;

    [Header("IA — Salto")]
    public float velocidadSalto          = 3.5f;
    public float alturaSalto             = 5f;
    public float duracionPreparacion     = 2f;
    public float pausaEntreAtaques       = 1.5f;

    [Header("Proyectiles")]
    public GameObject proyectilPrefab;   // Arrastra Cuchillo.prefab aquí
    public float velocidadCuchillo       = 7f;

    // ── Estado ───────────────────────────────────────────────────────────────
    public bool EstaMuerto              { get; private set; } = false;
    public int  VidasActuales           { get; private set; }

    private Vector3 escalaOriginal;
    private Vector3 posicionObjetivo;
    private ControladorJugador jugadorCache;

    // Y fija de aterrizaje (se guarda en Start para ser consistente con el jugador)
    private float groundY;

    // Posición real en suelo (usada por FeedbackGolpe para no acumular deriva)
    private Vector3 posicionSuelo;

    // Impide que FeedbackGolpe interfiera durante el salto
    private bool estaSaltando = false;

    // ── Barra de vida ─────────────────────────────────────────────────────────
    private GameObject barraVidaParent;
    private GameObject barraRelleno;
    private Material   matRelleno;

    static readonly Color COLOR_VERDE = new Color(0.1f, 0.85f, 0.1f);
    static readonly Color COLOR_ROJO  = new Color(0.9f, 0.1f, 0.1f);
    const float ANCHO_BARRA           = 1.6f;

    static readonly Vector3[] DIRS =
        { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        VidasActuales  = vidasMax;
        escalaOriginal = transform.localScale;

        // Guardar la Y de spawn como referencia permanente de "nivel del suelo".
        // Así todos los aterrizajes posteriores quedan exactamente a la misma altura
        // que cuando el diseñador colocó a Metagross en el nivel.
        groundY        = transform.position.y;
        posicionSuelo  = transform.position;

        jugadorCache = SaludJugador.Instancia != null
            ? SaludJugador.Instancia.GetComponent<ControladorJugador>()
            : FindFirstObjectByType<ControladorJugador>();

        CrearBarraVida();
        StartCoroutine(SecuenciaCompleta());
    }

    void Update()
    {
        // La barra siempre mira a la cámara (billboard)
        if (barraVidaParent != null && Camera.main != null)
            barraVidaParent.transform.rotation = Camera.main.transform.rotation;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BARRA DE VIDA 3D
    // ═══════════════════════════════════════════════════════════════════════════

    void CrearBarraVida()
    {
        barraVidaParent = new GameObject("BarraVidaParent");
        barraVidaParent.transform.SetParent(transform);
        barraVidaParent.transform.localPosition = new Vector3(0f, 2.2f, 0f);

        // Fondo oscuro
        GameObject fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "BarraFondo";
        fondo.transform.SetParent(barraVidaParent.transform, false);
        fondo.transform.localScale    = new Vector3(ANCHO_BARRA + 0.1f, 0.28f, 1f);
        fondo.transform.localPosition = Vector3.zero;
        fondo.GetComponent<MeshRenderer>().sharedMaterial = CrearMaterial(new Color(0.15f, 0.15f, 0.15f));
        Destroy(fondo.GetComponent<Collider>());

        // Relleno (cambia de color según la vida)
        barraRelleno = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barraRelleno.name = "BarraRelleno";
        barraRelleno.transform.SetParent(barraVidaParent.transform, false);
        barraRelleno.transform.localScale    = new Vector3(ANCHO_BARRA, 0.2f, 1f);
        barraRelleno.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        matRelleno = CrearMaterial(COLOR_VERDE);
        barraRelleno.GetComponent<MeshRenderer>().sharedMaterial = matRelleno;
        Destroy(barraRelleno.GetComponent<Collider>());

        ActualizarBarraVida();
    }

    static Material CrearMaterial(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color")
                 ?? Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.color = color;
        return mat;
    }

    void ActualizarBarraVida()
    {
        if (barraRelleno == null || matRelleno == null) return;

        float pct = (float)VidasActuales / vidasMax;

        // Escalar en X proporcional a la vida
        Vector3 escala = barraRelleno.transform.localScale;
        escala.x = ANCHO_BARRA * pct;
        barraRelleno.transform.localScale = escala;

        // Alinear a la izquierda: desplazar el pivote central
        Vector3 pos = barraRelleno.transform.localPosition;
        pos.x = (ANCHO_BARRA * pct - ANCHO_BARRA) * 0.5f;
        barraRelleno.transform.localPosition = pos;

        // Color: verde (lleno) → rojo (vacío)
        matRelleno.color = Color.Lerp(COLOR_ROJO, COLOR_VERDE, pct);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SECUENCIA DE COMBATE
    // ═══════════════════════════════════════════════════════════════════════════

    IEnumerator SecuenciaCompleta()
    {
        // Fase 0: cae desde el cielo
        yield return StartCoroutine(EntradaDesdeCielo());

        // Bucle de combate
        while (!EstaMuerto)
        {
            // Fase 1: preparación (bloquea objetivo al INICIO del período de espera)
            yield return StartCoroutine(FasePreparacion());

            // Fase 2: salto al objetivo bloqueado
            yield return StartCoroutine(FaseSalto());

            // Fase 3: lanzar cuchillos + comprobar contacto + pausa
            LanzarCuchillosEnCruz();
            ComprobarContactoJugador();
            yield return new WaitForSeconds(pausaEntreAtaques);
        }
    }

    // ── Fase 0: caída desde el cielo ─────────────────────────────────────────
    IEnumerator EntradaDesdeCielo()
    {
        estaSaltando = true;

        const float ALTURA_INICIO = 14f;
        Vector3 posFinal  = new Vector3(transform.position.x, groundY, transform.position.z);
        Vector3 posInicio = posFinal + Vector3.up * ALTURA_INICIO;
        transform.position = posInicio;

        float t = 0f, dur = 1.8f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.position = Vector3.Lerp(posInicio, posFinal, p * p * p); // Aceleración cúbica
            yield return null;
        }

        // Snap garantizado al groundY
        transform.position = posFinal;
        posicionSuelo      = posFinal;
        estaSaltando       = false;

        yield return StartCoroutine(AnimacionAterrizaje());
        LanzarCuchillosEnCruz(); // Primer saludo
    }

    // ── Fase 1: preparación (2 s) — bloquea objetivo desde el PRIMER FOTOGRAMA ─
    IEnumerator FasePreparacion()
    {
        // Bloquear la posición del jugador AL INICIO, usando siempre groundY como Y de aterrizaje.
        // Esto garantiza que Metagross siempre aterriza a la misma altura que el jugador.
        if (jugadorCache != null)
        {
            posicionObjetivo = new Vector3(
                Mathf.Round(jugadorCache.transform.position.x),
                groundY,
                Mathf.Round(jugadorCache.transform.position.z));
        }
        else
        {
            posicionObjetivo = posicionSuelo; // Fallback: saltar al mismo sitio
        }

        Vector3 posBase = posicionSuelo; // Usar posicionSuelo (no transform.position, que puede haber derivado)
        float t = 0f;

        while (t < duracionPreparacion)
        {
            t += Time.deltaTime;
            float progreso = t / duracionPreparacion;

            // Girar suavemente hacia el objetivo bloqueado
            Vector3 dir = posicionObjetivo - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.forward = Vector3.Slerp(transform.forward, dir.normalized, Time.deltaTime * 4f);

            // Vibración que se intensifica
            float intensidad = progreso * progreso;
            float agitacion  = Mathf.Sin(t * 18f) * 0.07f * intensidad;
            transform.position = posBase + new Vector3(agitacion, 0f, agitacion * 0.6f);

            // Pulsación de escala
            float pulso = 1f + Mathf.Sin(t * 12f) * 0.05f * intensidad;
            transform.localScale = escalaOriginal * pulso;

            yield return null;
        }

        // Restaurar exactamente a la posición de suelo
        transform.position   = posBase;
        transform.localScale = escalaOriginal;
    }

    // ── Fase 2: salto al objetivo ─────────────────────────────────────────────
    IEnumerator FaseSalto()
    {
        estaSaltando = true;

        Vector3 posInicio = posicionSuelo;                 // Siempre parte del suelo real
        Vector3 posFin    = posicionObjetivo;              // Siempre aterriza en groundY

        Vector3 dir = posFin - posInicio;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f) transform.forward = dir.normalized;

        float distanciaXZ = Vector3.Distance(
            new Vector3(posInicio.x, 0, posInicio.z),
            new Vector3(posFin.x,    0, posFin.z));
        float duracion = Mathf.Max(0.5f, distanciaXZ / velocidadSalto);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float p     = Mathf.Clamp01(t / duracion);
            Vector3 pos = Vector3.Lerp(posInicio, posFin, p);
            // Arco: Y base siempre entre groundY (inicio) y groundY (fin), más el arco parabólico
            pos.y = Mathf.Lerp(posInicio.y, posFin.y, p) + Mathf.Sin(p * Mathf.PI) * alturaSalto;
            transform.position = pos;
            yield return null;
        }

        // Snap garantizado: Metagross aterriza EXACTAMENTE en el punto objetivo (groundY)
        transform.position = posFin;
        posicionSuelo      = posFin;
        estaSaltando       = false;

        yield return StartCoroutine(AnimacionAterrizaje());
    }

    // ── Animación de aterrizaje (squash & stretch) ────────────────────────────
    IEnumerator AnimacionAterrizaje()
    {
        Vector3 escalaSquash = new Vector3(escalaOriginal.x * 1.5f, escalaOriginal.y * 0.45f, escalaOriginal.z * 1.5f);
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaSquash, t / 0.1f);
            yield return null;
        }
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaSquash, escalaOriginal, t / 0.2f);
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    // ── Cuchillos en cruz ─────────────────────────────────────────────────────
    void LanzarCuchillosEnCruz()
    {
        if (proyectilPrefab == null) return;
        Vector3 origen = transform.position + Vector3.up * 1f;
        foreach (Vector3 dir in DIRS)
        {
            GameObject cuchillo = Instantiate(proyectilPrefab, origen, Quaternion.LookRotation(dir));
            Proyectil p = cuchillo.GetComponent<Proyectil>();
            if (p != null)
            {
                p.velocidad = velocidadCuchillo;
                p.Iniciar(dir);
            }
        }
    }

    // ── Contacto con el jugador ───────────────────────────────────────────────
    void ComprobarContactoJugador()
    {
        if (jugadorCache == null) return;
        Vector3 miXZ  = new Vector3(transform.position.x,        0f, transform.position.z);
        Vector3 jugXZ = new Vector3(jugadorCache.transform.position.x, 0f, jugadorCache.transform.position.z);
        if (Vector3.Distance(miXZ, jugXZ) < 1.2f)
            SaludJugador.Instancia?.RecibirDanio(1);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RECIBIR GOLPE DEL JUGADOR
    // ═══════════════════════════════════════════════════════════════════════════

    public void RecibirGolpe()
    {
        if (EstaMuerto) return;

        VidasActuales = Mathf.Max(0, VidasActuales - 1);
        ActualizarBarraVida();

        if (VidasActuales <= 0)
        {
            EstaMuerto = true;
            StopAllCoroutines();
            StartCoroutine(AnimacionMuerteEpica());
        }
        else
        {
            // Pequeño temblor de feedback sin interrumpir la IA
            StartCoroutine(FeedbackGolpe());
        }
    }

    IEnumerator FeedbackGolpe()
    {
        if (estaSaltando)
        {
            // Estamos en el aire: solo feedback visual de escala, sin tocar la posición
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float p = Mathf.Sin(t * 30f) * (1f - t / 0.2f);
                transform.localScale = escalaOriginal * (1f + p * 0.08f);
                yield return null;
            }
            transform.localScale = escalaOriginal;
            yield break;
        }

        // En tierra: sacudida de posición usando posicionSuelo como referencia fija.
        // Siempre restauramos exactamente a posicionSuelo para no acumular deriva.
        Vector3 refBase = posicionSuelo;
        float tt = 0f;
        while (tt < 0.3f)
        {
            tt += Time.deltaTime;
            float agit = Mathf.Sin(tt * 35f) * 0.08f * (1f - tt / 0.3f);
            transform.position = refBase + new Vector3(agit, 0f, -agit * 0.5f);
            yield return null;
        }
        // Restauración exacta: sin deriva acumulada
        transform.position = refBase;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ANIMACIÓN DE MUERTE ÉPICA → CRÉDITOS
    // ═══════════════════════════════════════════════════════════════════════════

    IEnumerator AnimacionMuerteEpica()
    {
        // Ocultar barra de vida
        if (barraVidaParent != null) barraVidaParent.SetActive(false);

        Vector3 posBase = transform.position;

        // — FASE 1: Gira rápido + crece + sube (1.8 s) —
        float t = 0f, dur1 = 1.8f;
        while (t < dur1)
        {
            t += Time.deltaTime;
            float p = t / dur1;

            transform.Rotate(Vector3.up, 600f * Time.deltaTime, Space.World);

            float pulso = 1f + Mathf.Sin(t * 14f) * 0.12f + p * 0.5f;
            transform.localScale   = escalaOriginal * pulso;
            transform.position = posBase + Vector3.up * (Mathf.Sin(t * 8f) * 0.2f + p * 0.8f);

            yield return null;
        }

        // — FASE 2: Explosión de escombros + cuchillos finales —
        LanzarEscombros();
        LanzarCuchillosEnCruz();

        // — FASE 3: Colapso a cero (0.8 s) —
        t = 0f;
        Vector3 escalaActual = transform.localScale;
        Vector3 posActual    = transform.position;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.8f);
            transform.localScale = Vector3.Lerp(escalaActual, Vector3.zero, p);
            transform.position   = posActual + Vector3.down * (p * 0.4f);
            yield return null;
        }

        // Objeto invisible (no destruido aún para que la corrutina siga)
        transform.localScale = Vector3.zero;

        // — Pausa dramática —
        yield return new WaitForSeconds(2.5f);

        // — Ir a créditos (índice 1 en Build Settings) —
        SceneManager.LoadScene(1);
    }

    void LanzarEscombros()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        Material[] mats  = rends.Length > 0 ? rends[0].sharedMaterials : null;

        for (int i = 0; i < 22; i++)
        {
            PrimitiveType tipo  = (i % 2 == 0) ? PrimitiveType.Sphere : PrimitiveType.Cube;
            GameObject trozo    = GameObject.CreatePrimitive(tipo);
            trozo.transform.position = transform.position
                                     + Random.insideUnitSphere * 0.6f
                                     + Vector3.up * 0.5f;

            float s = Random.Range(0.08f, 0.35f);
            trozo.transform.localScale = Vector3.one * s;

            if (mats != null && mats.Length > 0)
                trozo.GetComponent<Renderer>().sharedMaterial = mats[Random.Range(0, mats.Length)];

            Rigidbody rb = trozo.AddComponent<Rigidbody>();
            Vector3 dir  = (Random.insideUnitSphere + Vector3.up * 0.4f).normalized;
            rb.AddForce(dir * Random.Range(7f, 16f), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.VelocityChange);

            Collider col = trozo.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(trozo, 3.5f);
        }
    }
}
