using UnityEngine;

// Adjuntar al prefab Cuchillo / Flecha.
// La dirección de vuelo NO depende de la rotación del modelo FBX:
// el lanzador llama a Iniciar(dir) justo después de instanciar.
[RequireComponent(typeof(Collider))]
public class Proyectil : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad       = 8f;
    public float distanciaMaxima = 12f;

    [Header("Daño")]
    public int danio = 1;

    // ── Estado interno ────────────────────────────────────────────────────────
    private Vector3    origenDisparo;
    private Vector3    direccionVuelo;
    private bool       listo     = false;
    private bool       yaImpacto = false;  // evita aplicar daño más de una vez
    private Rigidbody  rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Llamado por el lanzador justo tras Instantiate, antes de Start().
    public void Iniciar(Vector3 dir)
    {
        origenDisparo  = transform.position;
        direccionVuelo = dir.normalized;
        listo = true;
    }

    void Start()
    {
        // Fallback: si nadie llamó a Iniciar (flecha colocada a mano en escena).
        if (!listo)
        {
            origenDisparo  = transform.position;
            direccionVuelo = transform.forward;
            listo = true;
        }

        // ── Comprobación de solapamiento inicial con el jugador ───────────────
        // OnTriggerEnter NO se dispara si el proyectil se instancia ya DENTRO
        // del collider del jugador (p. ej. Metagross aterriza encima del jugador
        // y lanza los cuchillos desde esa misma posición).
        //
        // Usamos la posición real del transform del jugador en lugar de
        // Physics.OverlapSphere porque este proyecto tiene autoSyncTransforms = false,
        // lo que puede provocar que la física devuelva posiciones desactualizadas
        // en el mismo frame en que se instancia el proyectil.
        if (SaludJugador.Instancia != null)
        {
            Transform jugTr = SaludJugador.Instancia.transform;
            Vector3 diffXZ  = new Vector3(
                jugTr.position.x - transform.position.x,
                0f,
                jugTr.position.z - transform.position.z);

            // Radio de 0.8 unidades en XZ: cubre el tile exacto y pequeños desfases
            if (diffXZ.sqrMagnitude < 0.64f) // 0.8² = 0.64
            {
                yaImpacto = true;
                SaludJugador.Instancia.RecibirDanio(danio);
                Destroy(gameObject);
            }
        }
    }

    // ── Movimiento en FixedUpdate con MovePosition ───────────────────────────
    // Mover un Rigidbody cinemático mediante rb.MovePosition() en FixedUpdate
    // es la forma correcta: garantiza que el motor de física registre el barrido
    // y dispare OnTriggerEnter aunque autoSyncTransforms esté desactivado.
    void FixedUpdate()
    {
        if (!listo || yaImpacto) return;

        Vector3 nuevaPos = transform.position + direccionVuelo * velocidad * Time.fixedDeltaTime;

        if (Vector3.Distance(origenDisparo, nuevaPos) >= distanciaMaxima)
        {
            Destroy(gameObject);
            return;
        }

        rb.MovePosition(nuevaPos);
    }

    // ── Detección de colisiones ───────────────────────────────────────────────

    // OnTriggerEnter: caso normal (proyectil vuela y entra en el collider).
    private void OnTriggerEnter(Collider other) => ManejarColision(other);

    // OnTriggerStay: respaldo por si el proyectil nació solapado y
    // OnTriggerEnter no se disparó.
    private void OnTriggerStay(Collider other)  => ManejarColision(other);

    private void ManejarColision(Collider other)
    {
        if (yaImpacto) return;

        // Los proyectiles atraviesan al enemigo que los lanzó.
        if (other.GetComponentInParent<ControladorSlime>()     != null ||
            other.GetComponentInParent<ControladorEsqueleto>() != null ||
            other.GetComponentInParent<ControladorPanda>()     != null ||
            other.GetComponentInParent<ControladorMetagross>() != null)
            return;

        // Dañar al jugador.
        if (other.CompareTag("Jugador"))
        {
            yaImpacto = true;
            SaludJugador salud = other.GetComponentInParent<SaludJugador>();
            salud?.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // Destruirse al golpear cualquier sólido excepto las Vallas.
        if (!other.isTrigger && !other.CompareTag("Valla"))
            Destroy(gameObject);
    }
}
