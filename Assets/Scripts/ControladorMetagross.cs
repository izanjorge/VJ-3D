using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Jefe final del juego.
/// - Aparece cayendo desde el cielo al inicio del nivel.
/// - IA: Bloquea la posición del jugador → espera 2 s → salta → lanza cuchillos en cruz → repite.
/// - Barra de vida UI en la parte superior de la pantalla (verde→rojo).
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

    // ── Barra de vida UI ─────────────────────────────────────────────────────
    private GameObject    barraVidaParent;   // Canvas raíz
    private Image         imgRelleno;        // Image del relleno
    private RectTransform rtRelleno;         // RectTransform del relleno (para escalar el ancho)

    static readonly Color COLOR_VERDE = new Color(0.1f, 0.85f, 0.1f);
    static readonly Color COLOR_ROJO  = new Color(0.9f, 0.1f, 0.1f);

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
        // Destruir el canvas si el objeto se destruye
        if (barraVidaParent != null) Destroy(barraVidaParent);
    }

    void Start()
    {
        VidasActuales  = vidasMax;
        escalaOriginal = transform.localScale;

        groundY       = transform.position.y;
        posicionSuelo = transform.position;

        jugadorCache = SaludJugador.Instancia != null
            ? SaludJugador.Instancia.GetComponent<ControladorJugador>()
            : FindFirstObjectByType<ControladorJugador>();

        CrearBarraVida();
        StartCoroutine(SecuenciaCompleta());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BARRA DE VIDA UI (ScreenSpace Overlay — parte superior de la pantalla)
    // ═══════════════════════════════════════════════════════════════════════════

    void CrearBarraVida()
    {
        // ── Canvas ScreenSpace-Overlay ────────────────────────────────────────
        barraVidaParent = new GameObject("CanvasVidaMetagross");
        Canvas canvas = barraVidaParent.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = barraVidaParent.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        barraVidaParent.AddComponent<GraphicRaycaster>();

        // ── Fondo (franja oscura semitransparente) ────────────────────────────
        GameObject fondoGO = new GameObject("BarraFondo");
        fondoGO.transform.SetParent(barraVidaParent.transform, false);
        RectTransform fondoRect = fondoGO.AddComponent<RectTransform>();
        // Barra centrada, ocupa el 40% central (30%→70%), franja muy fina en la parte superior (96-99%)
        fondoRect.anchorMin = new Vector2(0.30f, 0.955f);
        fondoRect.anchorMax = new Vector2(0.70f, 0.995f);
        fondoRect.offsetMin = Vector2.zero;
        fondoRect.offsetMax = Vector2.zero;
        Image fondoImg  = fondoGO.AddComponent<Image>();
        fondoImg.color  = new Color(0.08f, 0.08f, 0.08f, 0.88f);

        // ── Relleno (crece/decrece desde la izquierda) ───────────────────────
        // Se ancla desde el borde izquierdo del fondo; anchorMax.x controla el ancho.
        GameObject rellenoGO = new GameObject("BarraRelleno");
        rellenoGO.transform.SetParent(fondoGO.transform, false);
        rtRelleno             = rellenoGO.AddComponent<RectTransform>();
        rtRelleno.anchorMin   = new Vector2(0f, 0f);
        rtRelleno.anchorMax   = new Vector2(1f, 1f);   // empieza llena (100 %)
        rtRelleno.offsetMin   = new Vector2(4f, 4f);   // padding interior izquierdo/inferior
        rtRelleno.offsetMax   = new Vector2(-4f, -4f); // padding interior derecho/superior

        imgRelleno       = rellenoGO.AddComponent<Image>();
        imgRelleno.color = COLOR_VERDE;
        // Simple: el ancho lo controlamos nosotros moviendo anchorMax.x

        ActualizarBarraVida();
    }

    void ActualizarBarraVida()
    {
        if (imgRelleno == null || rtRelleno == null) return;

        float pct = (float)VidasActuales / vidasMax;

        // Encoger la barra desde la derecha: anchorMax.x va de 1 (llena) a 0 (vacía).
        // El offsetMax.x se mantiene en -4 para preservar el padding derecho cuando está llena;
        // en cualquier otro caso se pone a 0 para que la barra no tenga hueco extra a la derecha.
        rtRelleno.anchorMax = new Vector2(pct, 1f);
        rtRelleno.offsetMax = new Vector2(pct >= 1f ? -4f : 0f, -4f);

        // Color: verde → rojo según la vida restante
        imgRelleno.color = Color.Lerp(COLOR_ROJO, COLOR_VERDE, pct);
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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMetaEntrada);

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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMetaCargando);

        if (jugadorCache != null)
        {
            posicionObjetivo = new Vector3(
                Mathf.Round(jugadorCache.transform.position.x),
                groundY,
                Mathf.Round(jugadorCache.transform.position.z));
        }
        else
        {
            posicionObjetivo = posicionSuelo;
        }

        Vector3 posBase = posicionSuelo;
        float t = 0f;

        while (t < duracionPreparacion)
        {
            t += Time.deltaTime;
            float progreso = t / duracionPreparacion;

            Vector3 dir = posicionObjetivo - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.forward = Vector3.Slerp(transform.forward, dir.normalized, Time.deltaTime * 4f);

            float intensidad = progreso * progreso;
            float agitacion  = Mathf.Sin(t * 18f) * 0.07f * intensidad;
            transform.position = posBase + new Vector3(agitacion, 0f, agitacion * 0.6f);

            float pulso = 1f + Mathf.Sin(t * 12f) * 0.05f * intensidad;
            transform.localScale = escalaOriginal * pulso;

            yield return null;
        }

        transform.position   = posBase;
        transform.localScale = escalaOriginal;
    }

    // ── Fase 2: salto al objetivo ─────────────────────────────────────────────
    IEnumerator FaseSalto()
    {
        estaSaltando = true;

        Vector3 posInicio = posicionSuelo;
        Vector3 posFin    = posicionObjetivo;

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
            pos.y = Mathf.Lerp(posInicio.y, posFin.y, p) + Mathf.Sin(p * Mathf.PI) * alturaSalto;
            transform.position = pos;
            yield return null;
        }

        transform.position = posFin;
        posicionSuelo      = posFin;
        estaSaltando       = false;

        yield return StartCoroutine(AnimacionAterrizaje());
    }

    // ── Animación de aterrizaje (squash & stretch) ────────────────────────────
    IEnumerator AnimacionAterrizaje()
    {
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMetaAterrizaje);

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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxCuchillo);
        // Y = 1.5 para que la flecha (collider fino: centro 0.1, semialtura 0.1)
        // quede en el rango Y 1.4–1.6 y solape con el collider del jugador (Y 1.15–2.75).
        Vector3 origen = transform.position + Vector3.up * 1.5f;
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
            StartCoroutine(FeedbackGolpe());
        }
    }

    IEnumerator FeedbackGolpe()
    {
        if (estaSaltando)
        {
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

        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMetaDanio);
        Vector3 refBase = posicionSuelo;
        float tt = 0f;
        while (tt < 0.3f)
        {
            tt += Time.deltaTime;
            float agit = Mathf.Sin(tt * 35f) * 0.08f * (1f - tt / 0.3f);
            transform.position = refBase + new Vector3(agit, 0f, -agit * 0.5f);
            yield return null;
        }
        transform.position = refBase;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ANIMACIÓN DE MUERTE ÉPICA → CRÉDITOS
    // ═══════════════════════════════════════════════════════════════════════════

    IEnumerator AnimacionMuerteEpica()
    {
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxMetaMuerteExplosion);

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
