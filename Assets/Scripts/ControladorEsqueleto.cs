using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControladorEsqueleto : MonoBehaviour
{
    // ── Registro estático ────────────────────────────────────────────────────
    public static readonly List<ControladorEsqueleto> esqueletosActivos = new List<ControladorEsqueleto>();

    [Header("Movimiento")]
    public float velocidad             = 4f;
    public float distanciaPaso         = 1f;
    public float alturaSalto           = 0.8f;
    public float tiempoEntreMovimientos = 0.7f;

    [Header("Detección")]
    public LayerMask capaSuelo;
    public LayerMask capaObstaculos;

    // ── Estado ───────────────────────────────────────────────────────────────
    public bool EstaMuerto { get; private set; } = false;

    private bool estaMoviendose = false;
    private bool estaAtacando   = false;
    private ControladorJugador jugadorCache;

    static readonly Vector3[] DIRS =
        { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void OnEnable()  { if (!esqueletosActivos.Contains(this)) esqueletosActivos.Add(this); }
    void OnDisable() { esqueletosActivos.Remove(this); }
    void OnDestroy() { esqueletosActivos.Remove(this); }

    void Start()
    {
        jugadorCache = SaludJugador.Instancia != null
            ? SaludJugador.Instancia.GetComponent<ControladorJugador>()
            : FindFirstObjectByType<ControladorJugador>();

        StartCoroutine(BucleIA());
    }

    // ── IA: persigue al jugador, ataca si está adyacente ─────────────────────
    IEnumerator BucleIA()
    {
        yield return new WaitForSeconds(Random.Range(0f, tiempoEntreMovimientos));

        while (!EstaMuerto)
        {
            if (!estaMoviendose && !estaAtacando)
            {
                if (jugadorCache != null && JugadorEsAdyacente())
                    yield return StartCoroutine(AtaqueAgresivo());
                else
                {
                    Vector3 dir = CalcularDireccion();
                    if (dir != Vector3.zero)
                        yield return StartCoroutine(MoverEsqueleto(dir));
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

    // Prioriza la dirección más cercana al jugador; si está bloqueada, prueba las demás
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

    // ── Movimiento ───────────────────────────────────────────────────────────
    IEnumerator MoverEsqueleto(Vector3 dir)
    {
        estaMoviendose = true;
        Vector3 posIni  = transform.position;
        Vector3 posDest = posIni + dir * distanciaPaso;

        transform.forward = dir;

        float t = 0f, dur = 1f / velocidad;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p  = Mathf.Clamp01(t / dur);
            Vector3 pos = Vector3.Lerp(posIni, posDest, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * alturaSalto;
            transform.position = pos;

            // Inclinación agresiva hacia adelante
            float inclinacion = Mathf.Sin(p * Mathf.PI) * 25f;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(inclinacion, 0, 0);
            yield return null;
        }

        transform.position      = posDest;
        transform.rotation      = Quaternion.LookRotation(dir);
        estaMoviendose          = false;

        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxEsqueletoPaso);
        ComprobarContactoJugador();
    }

    // ── Ataque cuerpo a cuerpo ────────────────────────────────────────────────
    IEnumerator AtaqueAgresivo()
    {
        estaAtacando = true;
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxEsqueletoAtaque);

        // Girar hacia el jugador
        if (jugadorCache != null)
        {
            Vector3 d = jugadorCache.transform.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.forward = d.normalized;
        }

        Vector3 posOrigen    = transform.position;
        Vector3 escalaOrig   = transform.localScale;
        Quaternion rotOrig   = transform.rotation;
        Vector3 posEmbestida = posOrigen + transform.forward * 1f;

        // FASE 1 — Anticipación (echarse atrás)
        float t = 0f;
        Vector3 posAtras = posOrigen - transform.forward * 0.2f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float p = t / 0.15f;
            transform.position = Vector3.Lerp(posOrigen, posAtras, p);
            transform.rotation = rotOrig * Quaternion.Euler(-20f * p, 0, 0);
            yield return null;
        }

        // FASE 2 — Embestida
        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float p = t / 0.15f;
            Vector3 pos = Vector3.Lerp(posAtras, posEmbestida, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 1.5f;
            transform.position = pos;
            transform.rotation = rotOrig * Quaternion.Euler(Mathf.Lerp(-20f, 45f, p), 0, 0);
            yield return null;
        }

        // IMPACTO
        transform.position   = posEmbestida;
        transform.localScale = new Vector3(escalaOrig.x * 1.2f, escalaOrig.y * 0.5f, escalaOrig.z * 1.2f);
        transform.rotation   = rotOrig * Quaternion.Euler(45f, 0, 0);

        ComprobarDanioAlJugador(); // ← Daño en el pico de la embestida

        yield return new WaitForSeconds(0.15f);

        // FASE 3 — Recuperación
        t = 0f;
        Vector3 escalaActual = transform.localScale;
        Vector3 posActual    = transform.position;
        Quaternion rotActual = transform.rotation;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float p = t / 0.25f;
            transform.position   = Vector3.Lerp(posActual,    posOrigen,  p);
            transform.localScale = Vector3.Lerp(escalaActual, escalaOrig, p);
            transform.rotation   = Quaternion.Lerp(rotActual, rotOrig,    p);
            yield return null;
        }

        transform.position   = posOrigen;
        transform.localScale = escalaOrig;
        transform.rotation   = rotOrig;
        estaAtacando         = false;
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

    void ComprobarDanioAlJugador()
    {
        if (jugadorCache == null) return;
        Vector3 miXZ  = new Vector3(transform.position.x,        0f, transform.position.z);
        Vector3 jugXZ = new Vector3(jugadorCache.transform.position.x, 0f, jugadorCache.transform.position.z);
        if (Vector3.Distance(miXZ, jugXZ) < 1.6f)
            SaludJugador.Instancia?.RecibirDanio(1);
    }

    // ── Muerte ───────────────────────────────────────────────────────────────
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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxEsqueletoMuerte);

        // Flash de expansión
        Vector3 escalaBase = transform.localScale;
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaBase, escalaBase * 1.4f, t / 0.1f);
            yield return null;
        }

        LanzarHuesos();

        // Colapso a cero
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

    void LanzarHuesos()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        Material mat  = rend != null ? rend.sharedMaterial : null;

        for (int i = 0; i < 10; i++)
        {
            // Mezcla de cápsulas y cubos para simular huesos
            PrimitiveType tipo = (i % 3 == 0) ? PrimitiveType.Cube : PrimitiveType.Capsule;
            GameObject hueso  = GameObject.CreatePrimitive(tipo);
            hueso.transform.position = transform.position + Vector3.up * 0.8f;

            float s = Random.Range(0.08f, 0.2f);
            hueso.transform.localScale = new Vector3(s, s * Random.Range(1f, 2.5f), s);
            if (mat != null) hueso.GetComponent<Renderer>().sharedMaterial = mat;

            Rigidbody rb = hueso.AddComponent<Rigidbody>();
            Vector3 dir  = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 2f), Random.Range(-1f, 1f)).normalized;
            rb.AddForce(dir * Random.Range(4f, 9f), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.VelocityChange);

            Collider col = hueso.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(hueso, 1.5f);
        }
    }
}
