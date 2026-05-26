using UnityEngine;

// Adjuntar al prefab Flecha.
// La dirección de vuelo NO depende de la rotación del modelo FBX:
// TrampaDisparador llama a Iniciar(dir) justo después de instanciar y le
// pasa explícitamente el vector hacia donde debe volar la flecha.
// El daño al jugador también se gestiona aquí.
[RequireComponent(typeof(Collider))]
public class Proyectil : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad       = 8f;
    public float distanciaMaxima = 12f;

    [Header("Daño")]
    public int danio = 1;

    // ── Dirección de vuelo (se asigna externamente vía Iniciar) ───────────
    private Vector3 origenDisparo;
    private Vector3 direccionVuelo;   // dirección en espacio mundo, normalizada
    private bool    listo = false;    // true cuando Iniciar() ya fue llamado

    // Llamado por TrampaDisparador justo tras Instantiate, antes de Start().
    public void Iniciar(Vector3 dir)
    {
        origenDisparo  = transform.position;
        direccionVuelo = dir.normalized;
        listo = true;
    }

    void Start()
    {
        // Fallback: si nadie llamó a Iniciar (p.ej. flecha colocada a mano
        // en la escena para depuración), usamos transform.forward.
        if (!listo)
        {
            origenDisparo  = transform.position;
            direccionVuelo = transform.forward;
            listo = true;
        }
    }

    void Update()
    {
        if (!listo) return;

        transform.position += direccionVuelo * velocidad * Time.deltaTime;

        if (Vector3.Distance(origenDisparo, transform.position) >= distanciaMaxima)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Dañar al jugador y destruirse
        if (other.CompareTag("Jugador"))
        {
            SaludJugador salud = other.GetComponentInParent<SaludJugador>();
            salud?.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // Destruirse al golpear cualquier sólido EXCEPTO las Vallas,
        // que son transparentes a los proyectiles por diseño de juego.
        if (!other.isTrigger && !other.CompareTag("Valla"))
            Destroy(gameObject);
    }
}
