using UnityEngine;
using System.Collections;

/// <summary>
/// Adjuntar al prefab Puerta.
/// La puerta se coloca en el ÚLTIMO TILE del nivel (z = filas-1) y físicamente
/// bloquea el paso mientras haya enemigos vivos.
/// Cuando GestorNivel llama a Abrir():
///   1. Desactiva el BoxCollider (deja de bloquear al jugador).
///   2. Anima la puerta hundiéndose en el suelo.
///
/// IMPORTANTE — en Unity: asigna el prefab Puerta a la layer "Obstaculos"
/// para que HayObstaculoEnDestino del ControladorJugador la detecte.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Puerta : MonoBehaviour
{
    [Header("Animación de apertura")]
    public float duracionApertura = 0.45f;

    private Collider bloqueador;
    private bool abierta = false;
    public bool EstaAbierta => abierta;

    void Awake()
    {
        bloqueador = GetComponent<Collider>();
    }

    // ── Llamado por GestorNivel cuando no quedan enemigos ───────────────────
    public void Abrir()
    {
        if (abierta) return;
        abierta = true;

        // 1. Quitar el bloqueo físico inmediatamente
        if (bloqueador != null) bloqueador.enabled = false;

        // 2. Animación visual
        StartCoroutine(AnimacionApertura());
    }

    IEnumerator AnimacionApertura()
    {
        Vector3 escalaInicial = transform.localScale;
        float t = 0f;

        // La puerta se "hunde" en el suelo (Y → 0)
        while (t < duracionApertura)
        {
            t += Time.deltaTime;
            float suave = Mathf.Clamp01(t / duracionApertura);
            suave = suave * suave; // ease-in cuadrático
            transform.localScale = new Vector3(
                escalaInicial.x,
                Mathf.Lerp(escalaInicial.y, 0f, suave),
                escalaInicial.z
            );
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
