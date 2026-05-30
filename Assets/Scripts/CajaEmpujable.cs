using UnityEngine;
using System.Collections;

/// <summary>
/// Añadir al prefab Caja. Permite que el jugador empuje la caja un paso
/// si la casilla de destino tiene suelo y está libre de obstáculos.
/// Cuando se empuja, notifica a GestorCaidaSuelo para que la caja
/// caiga con su nueva fila (no con la fila donde fue spawneada).
/// </summary>
public class CajaEmpujable : MonoBehaviour
{
    [Header("Capas (se auto-rellenan desde ControladorJugador)")]
    public LayerMask capaSuelo;
    public LayerMask capaObstaculos;

    private bool estaMoviendose = false;

    // Seguimiento de fila para el sistema de caída del suelo
    private int filaActual;
    private GestorCaidaSuelo gestorCaida;

    void Start()
    {
        // Heredar las capas del jugador para no tener que asignarlas manualmente
        ControladorJugador jugador = FindFirstObjectByType<ControladorJugador>();
        if (jugador != null)
        {
            capaSuelo      = jugador.capaSuelo;
            capaObstaculos = jugador.capaObstaculos;
        }
        else
        {
            Debug.LogWarning("CajaEmpujable: No se encontró ControladorJugador. " +
                             "Asigna capaSuelo y capaObstaculos manualmente en el Inspector.");
        }

        // Registrar fila de spawn y buscar el gestor (solo existe en niveles 3-8)
        filaActual  = Mathf.RoundToInt(transform.position.z);
        gestorCaida = FindFirstObjectByType<GestorCaidaSuelo>();
    }

    /// <summary>
    /// Llamado por ControladorJugador cuando el jugador intenta entrar en la casilla de la caja.
    /// Devuelve true  → la caja se ha movido, el jugador puede avanzar.
    /// Devuelve false → la caja está bloqueada, el jugador NO puede avanzar.
    /// </summary>
    public bool IntentarEmpujar(Vector3 direccion, float duracion)
    {
        if (estaMoviendose) return false;

        Vector3 destino = transform.position + direccion;

        // La caja no puede caer al vacío
        if (!HaySueloEn(destino)) return false;

        // La caja no puede incrustarse en una pared ni en otra caja
        if (HayObstaculoEnDestino(destino)) return false;

        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxCajaEmpujar);
        StartCoroutine(DeslizarHacia(destino, duracion));
        return true;
    }

    // ── Animación ───────────────────────────────────────────────────────────

    IEnumerator DeslizarHacia(Vector3 destino, float duracion)
    {
        estaMoviendose = true;
        Vector3 origen = transform.position;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            // Ease-out cuadrático: arranca rápido y frena suavemente al llegar
            float tSmooth = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(origen, destino, tSmooth);
            yield return null;
        }

        // Snap exacto a la casilla de destino
        transform.position = destino;
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxCajaGolpe);
        estaMoviendose = false;

        // ── Actualizar fila en el sistema de caída del suelo ────────────────
        // Esto evita que la caja se destruya con su fila de origen si fue empujada
        // a otra fila, y garantiza que caiga correctamente con su nueva fila.
        int nuevaFila = Mathf.RoundToInt(destino.z);
        if (gestorCaida != null && nuevaFila != filaActual)
        {
            gestorCaida.MoverObjetoEntreFilas(gameObject, filaActual, nuevaFila);
            filaActual = nuevaFila;
        }
    }

    // ── Helpers de colisión (igual que en ControladorJugador) ────────────────

    bool HaySueloEn(Vector3 pos)
    {
        return Physics.Raycast(
            pos + Vector3.up,
            Vector3.down,
            2f,
            capaSuelo,
            QueryTriggerInteraction.Ignore
        );
    }

    bool HayObstaculoEnDestino(Vector3 pos)
    {
        Vector3 centro = pos + Vector3.up;
        Vector3 mitad  = new Vector3(0.4f, 1f, 0.4f);
        return Physics.CheckBox(
            centro, mitad,
            Quaternion.identity,
            capaObstaculos,
            QueryTriggerInteraction.Ignore
        );
    }
}
