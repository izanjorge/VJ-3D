using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControladorSlime : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Registro estático: todos los slimes vivos se apuntan aquí para que
    // ControladorJugador pueda encontrarlos sin depender de capas.
    // ------------------------------------------------------------------
    public static readonly List<ControladorSlime> slimesActivos = new List<ControladorSlime>();

    [Header("Configuración de Movimiento")]
    public float velocidad = 3f;
    public float distanciaPaso = 1f;
    public float alturaSalto = 0.5f;
    public float tiempoEntreMovimientos = 0.5f;

    [Header("Animación de Gelatina")]
    public float estiramientoMaximo = 1.3f;
    public float aplastamientoMaximo = 0.7f;

    [Header("Detección de Nivel")]
    public LayerMask capaSuelo;
    public LayerMask capaObstaculos;

    // ------------------------------------------------------------------
    // Estado
    // ------------------------------------------------------------------
    public bool EstaMuerto { get; private set; } = false;

    private bool estaMoviendose = false;
    private Vector3 escalaOriginal;

    static readonly Vector3[] DIRECCIONES =
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right
    };

    // ------------------------------------------------------------------
    // Ciclo de vida
    // ------------------------------------------------------------------

    void OnEnable()  { if (!slimesActivos.Contains(this)) slimesActivos.Add(this); }
    void OnDisable() { slimesActivos.Remove(this); }
    void OnDestroy() { slimesActivos.Remove(this); }

    void Start()
    {
        escalaOriginal = transform.localScale;
        // Pequeño offset aleatorio para que varios slimes no se muevan sincronizados
        float offsetInicial = Random.Range(0f, tiempoEntreMovimientos);
        StartCoroutine(BucleMovimiento(offsetInicial));
    }

    // ------------------------------------------------------------------
    // Bucle de IA: elige dirección → mueve → espera → repite
    // ------------------------------------------------------------------

    IEnumerator BucleMovimiento(float offsetInicial = 0f)
    {
        yield return new WaitForSeconds(offsetInicial);

        while (!EstaMuerto)
        {
            if (!estaMoviendose)
            {
                Vector3 direccion = ElegirDireccion();
                if (direccion != Vector3.zero)
                    yield return StartCoroutine(MoverSlime(direccion));
                else
                    yield return new WaitForSeconds(0.1f); // Sin movimiento válido: reintenta pronto
            }

            yield return new WaitForSeconds(tiempoEntreMovimientos);
        }
    }

    /// <summary>
    /// Baraja las 4 direcciones y devuelve la primera con suelo y sin obstáculo.
    /// Devuelve Vector3.zero si ninguna es válida.
    /// </summary>
    Vector3 ElegirDireccion()
    {
        // Copia y baraje (Fisher-Yates)
        Vector3[] dirs = (Vector3[])DIRECCIONES.Clone();
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 tmp = dirs[i]; dirs[i] = dirs[j]; dirs[j] = tmp;
        }

        foreach (Vector3 dir in dirs)
        {
            Vector3 destino = transform.position + dir * distanciaPaso;
            if (HaySueloEn(destino) && !HayObstaculoEn(destino))
                return dir;
        }
        return Vector3.zero;
    }

    // ------------------------------------------------------------------
    // Detección de suelo y obstáculos (igual que ControladorJugador)
    // ------------------------------------------------------------------

    bool HaySueloEn(Vector3 pos)
    {
        return Physics.Raycast(pos + Vector3.up, Vector3.down, 2f,
                               capaSuelo, QueryTriggerInteraction.Ignore);
    }

    bool HayObstaculoEn(Vector3 pos)
    {
        Vector3 centro = pos + Vector3.up;
        Vector3 mitad  = new Vector3(0.4f, 1f, 0.4f);
        return Physics.CheckBox(centro, mitad, Quaternion.identity,
                                capaObstaculos, QueryTriggerInteraction.Ignore);
    }

    // ------------------------------------------------------------------
    // Corrutina de movimiento con animación squash & stretch
    // ------------------------------------------------------------------

    IEnumerator MoverSlime(Vector3 direccion)
    {
        estaMoviendose = true;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial + (direccion * distanciaPaso);

        MancharCasilla(posicionInicial); // Deja rastro en la casilla de salida

        if (direccion != Vector3.zero)
            transform.forward = direccion;

        float tiempoTranscurrido = 0f;
        float duracion = 1f / velocidad;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / duracion);

            Vector3 pos = Vector3.Lerp(posicionInicial, posicionDestino, t);
            float curvaSalto = Mathf.Sin(t * Mathf.PI);
            pos.y = posicionInicial.y + curvaSalto * alturaSalto;
            transform.position = pos;

            float defY  = Mathf.Lerp(aplastamientoMaximo, estiramientoMaximo, curvaSalto);
            float defXZ = Mathf.Lerp(1.3f, 0.8f, curvaSalto);
            transform.localScale = new Vector3(
                escalaOriginal.x * defXZ,
                escalaOriginal.y * defY,
                escalaOriginal.z * defXZ);

            yield return null;
        }

        transform.position = posicionDestino;
        MancharCasilla(posicionDestino); // Deja rastro en la casilla de llegada

        yield return StartCoroutine(ReboteAterrizaje());

        ComprobarColisionJugador();

        estaMoviendose = false;
    }

    IEnumerator ReboteAterrizaje()
    {
        float tiempo = 0f;
        float duracion = 0.15f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(
                new Vector3(escalaOriginal.x * 1.3f, escalaOriginal.y * 0.7f, escalaOriginal.z * 1.3f),
                escalaOriginal,
                tiempo / duracion);
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    // ------------------------------------------------------------------
    // Rastro de slime
    // ------------------------------------------------------------------

    private void MancharCasilla(Vector3 posicion)
    {
        // Buscamos el tile directamente en sus coordenadas de grid (Y=0),
        // independientemente de la altura a la que esté el slime.
        Vector3 centroCasilla = new Vector3(Mathf.Round(posicion.x), 0f, Mathf.Round(posicion.z));
        Collider[] cols = Physics.OverlapSphere(centroCasilla, 0.4f);
        foreach (Collider col in cols)
        {
            Casilla casilla = col.GetComponentInParent<Casilla>();
            if (casilla != null)
            {
                casilla.MancharConSlime();
                return;
            }
        }
    }

    // ------------------------------------------------------------------
    // Colisión con el jugador (comprobada al aterrizar)
    // ------------------------------------------------------------------

    void ComprobarColisionJugador()
    {
        ControladorJugador jugador = FindFirstObjectByType<ControladorJugador>();
        if (jugador == null) return;

        Vector3 miPos     = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 jugadorPos = new Vector3(jugador.transform.position.x, 0f, jugador.transform.position.z);

        if (Vector3.Distance(miPos, jugadorPos) < 0.7f)
        {
            SaludJugador saludJugador = jugador.GetComponent<SaludJugador>();
            saludJugador?.RecibirDanio(1);
        }
    }

    // ------------------------------------------------------------------
    // Muerte del slime (llamada desde ControladorJugador al atacar)
    // ------------------------------------------------------------------

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
        // Paso 1: pequeño flash de expansión
        float tiempoExpansion = 0.12f;
        float tiempo = 0f;
        Vector3 escalaGrande = escalaOriginal * 1.5f;

        while (tiempo < tiempoExpansion)
        {
            tiempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, tiempo / tiempoExpansion);
            yield return null;
        }

        // Paso 2: lanzar trozos y contraerse hasta desaparecer
        LanzarTrozos();

        float tiempoContraer = 0.25f;
        tiempo = 0f;
        while (tiempo < tiempoContraer)
        {
            tiempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaGrande, Vector3.zero, tiempo / tiempoContraer);
            yield return null;
        }

        Destroy(gameObject);
    }

    void LanzarTrozos()
    {
        // Obtener un material para los trozos (intenta usar el renderer del propio slime)
        Material matTrozo = null;
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
            matTrozo = rend.sharedMaterial;

        int numTrozos = 8;
        for (int i = 0; i < numTrozos; i++)
        {
            // Crea una esfera pequeña como "trozo"
            GameObject trozo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trozo.transform.position = transform.position + Vector3.up * 0.5f;
            float escalaMin = escalaOriginal.x * 0.15f;
            float escalaMax = escalaOriginal.x * 0.35f;
            float s = Random.Range(escalaMin, escalaMax);
            trozo.transform.localScale = Vector3.one * s;

            // Aplica el material del slime
            if (matTrozo != null)
                trozo.GetComponent<Renderer>().sharedMaterial = matTrozo;

            // Añade física
            Rigidbody rb = trozo.AddComponent<Rigidbody>();
            Vector3 dirExplosion = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1.5f),
                Random.Range(-1f, 1f)).normalized;
            float fuerza = Random.Range(3f, 7f);
            rb.AddForce(dirExplosion * fuerza, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

            // El colisionador del trozo no debe bloquear al jugador ni a la IA
            Destroy(trozo.GetComponent<Collider>());

            Destroy(trozo, 1.2f);
        }
    }
}
