using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControladorPanda : MonoBehaviour
{
    // ── Registro estático ────────────────────────────────────────────────────
    public static readonly List<ControladorPanda> pandasActivos = new List<ControladorPanda>();

    [Header("Movimiento (muy lento)")]
    public float velocidad              = 1.5f;
    public float distanciaPaso          = 1f;
    public float alturaSalto            = 0.4f;
    public float tiempoEntreMovimientos = 2.5f;

    [Header("Cuchillos en cruz")]
    public GameObject proyectilPrefab;   // Arrastra Cuchillo.prefab aquí
    public float      velocidadCuchillo  = 6f;

    [Header("Detección")]
    public LayerMask capaSuelo;
    public LayerMask capaObstaculos;

    // ── Estado ───────────────────────────────────────────────────────────────
    public bool EstaMuerto { get; private set; } = false;

    private bool estaMoviendose = false;
    private Vector3 escalaOriginal;
    private ControladorJugador jugadorCache;

    static readonly Vector3[] DIRS =
        { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void OnEnable()  { if (!pandasActivos.Contains(this)) pandasActivos.Add(this); }
    void OnDisable() { pandasActivos.Remove(this); }
    void OnDestroy() { pandasActivos.Remove(this); }

    void Start()
    {
        escalaOriginal = transform.localScale;
        jugadorCache   = SaludJugador.Instancia != null
            ? SaludJugador.Instancia.GetComponent<ControladorJugador>()
            : FindFirstObjectByType<ControladorJugador>();

        StartCoroutine(BucleIA(Random.Range(0f, tiempoEntreMovimientos)));
    }

    // ── IA: persigue al jugador igual que el Esqueleto ────────────────────────
    IEnumerator BucleIA(float offsetInicial = 0f)
    {
        yield return new WaitForSeconds(offsetInicial);

        while (!EstaMuerto)
        {
            if (!estaMoviendose)
            {
                if (jugadorCache != null && JugadorEsAdyacente())
                {
                    // Adyacente: lanzar cuchillos en cruz + animación de ataque
                    LanzarCuchillosEnCruz();
                    yield return StartCoroutine(AnimacionAtaque());
                }
                else
                {
                    // No adyacente: perseguir al jugador
                    Vector3 dir = CalcularDireccion();
                    if (dir != Vector3.zero)
                        yield return StartCoroutine(Mover(dir));
                }
            }
            yield return new WaitForSeconds(tiempoEntreMovimientos);
        }
    }

    bool JugadorEsAdyacente()
    {
        if (jugadorCache == null) return false;
        Vector3 diff = jugadorCache.transform.position - transform.position;
        diff.y = 0;
        return diff.magnitude < 1.3f;
    }

    // Prioriza la dirección más cercana al jugador; igual que Esqueleto
    Vector3 CalcularDireccion()
    {
        if (jugadorCache == null) return Vector3.zero;

        Vector3 diff = jugadorCache.transform.position - transform.position;
        diff.y = 0;
        if (diff.sqrMagnitude < 0.01f) return Vector3.zero;

        Vector3 primaria   = Mathf.Abs(diff.x) >= Mathf.Abs(diff.z)
            ? (diff.x > 0 ? Vector3.right   : Vector3.left)
            : (diff.z > 0 ? Vector3.forward : Vector3.back);
        Vector3 secundaria = Mathf.Abs(diff.x) < Mathf.Abs(diff.z)
            ? (diff.x > 0 ? Vector3.right   : Vector3.left)
            : (diff.z > 0 ? Vector3.forward : Vector3.back);

        Vector3[] prioridad = { primaria, secundaria, -secundaria, -primaria };
        foreach (Vector3 dir in prioridad)
        {
            Vector3 dest = transform.position + dir * distanciaPaso;
            if (HaySueloEn(dest) && !HayObstaculoEn(dest))
                return dir;
        }
        return Vector3.zero;
    }

    bool HaySueloEn(Vector3 pos)
        => Physics.Raycast(pos + Vector3.up, Vector3.down, 2f, capaSuelo,
                           QueryTriggerInteraction.Ignore);

    bool HayObstaculoEn(Vector3 pos)
        => Physics.CheckBox(pos + Vector3.up, new Vector3(0.4f, 1f, 0.4f),
                            Quaternion.identity, capaObstaculos,
                            QueryTriggerInteraction.Ignore);

    // ── Movimiento: misma animación de salto que el Esqueleto ───────────────
    IEnumerator Mover(Vector3 dir)
    {
        estaMoviendose    = true;
        transform.forward = dir;

        Vector3 posIni  = transform.position;
        Vector3 posDest = posIni + dir * distanciaPaso;

        float t = 0f, dur = 1f / velocidad;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p   = Mathf.Clamp01(t / dur);
            Vector3 pos = Vector3.Lerp(posIni, posDest, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * alturaSalto;
            transform.position = pos;

            // Inclinación suave hacia adelante (igual que Esqueleto, pero más leve)
            float inclinacion = Mathf.Sin(p * Mathf.PI) * 15f;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(inclinacion, 0, 0);
            yield return null;
        }

        transform.position = posDest;
        transform.rotation = Quaternion.LookRotation(dir);
        estaMoviendose     = false;

        // Lanza cuchillos en cruz al aterrizar en cada paso
        LanzarCuchillosEnCruz();
        yield return StartCoroutine(AnimacionAtaque());

        ComprobarContactoJugador();
    }

    // ── Cuchillos en cruz ─────────────────────────────────────────────────────
    void LanzarCuchillosEnCruz()
    {
        if (proyectilPrefab == null) return;
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxCuchillo);

        Vector3 origen = transform.position + Vector3.up * 1f;
        foreach (Vector3 dir in DIRS)
        {
            GameObject cuchillo = Instantiate(proyectilPrefab, origen, Quaternion.LookRotation(dir));
            cuchillo.transform.localScale *= 0.8f;

            Proyectil p = cuchillo.GetComponent<Proyectil>();
            if (p != null)
            {
                p.velocidad = velocidadCuchillo;
                p.Iniciar(dir);
            }
        }
    }

    // Animación de "lanzamiento" — squash & stretch lateral
    IEnumerator AnimacionAtaque()
    {
        float t = 0f, dur = 0.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Sin(t / dur * Mathf.PI);
            transform.localScale = new Vector3(
                escalaOriginal.x * (1f + p * 0.3f),
                escalaOriginal.y * (1f - p * 0.2f),
                escalaOriginal.z * (1f + p * 0.3f));
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    // ── Daño ─────────────────────────────────────────────────────────────────
    void ComprobarContactoJugador()
    {
        if (jugadorCache == null) return;
        Vector3 miXZ  = new Vector3(transform.position.x,        0f, transform.position.z);
        Vector3 jugXZ = new Vector3(jugadorCache.transform.position.x, 0f, jugadorCache.transform.position.z);
        if (Vector3.Distance(miXZ, jugXZ) < 0.7f)
            SaludJugador.Instancia?.RecibirDanio(1);
    }

    // ── Muerte: explota en pedazos (igual que el Esqueleto) ──────────────────
    public void RecibirGolpe()
    {
        if (EstaMuerto) return;
        EstaMuerto = true;
        GestorNivel.Instancia?.NotificarMuerteEnemigo();
        StopAllCoroutines();
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxPandaMuerte);

        Vector3 escalaBase = transform.localScale;

        // Flash de expansión
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaBase, escalaBase * 1.4f, t / 0.1f);
            yield return null;
        }

        // Lanza pedazos y colapsa a cero
        LanzarPedazos();

        t = 0f;
        Vector3 escalaGrande = transform.localScale;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaGrande, Vector3.zero, t / 0.2f);
            yield return null;
        }

        Destroy(gameObject);
    }

    void LanzarPedazos()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        Material mat  = rend != null ? rend.sharedMaterial : null;

        for (int i = 0; i < 10; i++)
        {
            // Alterna entre esferas (trozos de cuerpo) y cubos (bambú/ropa)
            PrimitiveType tipo = (i % 3 == 0) ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject trozo  = GameObject.CreatePrimitive(tipo);
            trozo.transform.position = transform.position + Vector3.up * 0.8f;

            float s = Random.Range(0.06f, 0.18f);
            trozo.transform.localScale = new Vector3(s, s * Random.Range(1f, 2f), s);
            if (mat != null) trozo.GetComponent<Renderer>().sharedMaterial = mat;

            Rigidbody rb = trozo.AddComponent<Rigidbody>();
            Vector3 dir  = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 2f),
                Random.Range(-1f, 1f)).normalized;
            rb.AddForce(dir * Random.Range(4f, 9f), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.VelocityChange);

            Collider col = trozo.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(trozo, 1.5f);
        }
    }
}
