using UnityEngine;
using System.Collections;

/// <summary>
/// Componente genérico de salud para cualquier enemigo.
/// Añádelo al prefab/GameObject de cada enemigo (Slime, Esqueleto, Panda…).
/// Si el enemigo no tiene ningún Collider, se auto-añade un BoxCollider
/// para que el jugador lo detecte con EjecutarGolpe y no lo atraviese.
/// </summary>
public class Enemigo : MonoBehaviour
{
    [Header("Salud")]
    public int vida = 1;

    void Awake()
    {
        // Auto-añadir BoxCollider si no hay ninguno (para que EjecutarGolpe lo detecte
        // y el jugador no lo atraviese).
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.size   = new Vector3(0.8f, 1.6f, 0.8f);
        }
    }

    public void RecibirGolpe(int daño = 1)
    {
        vida -= daño;
        if (vida <= 0)
            Morir();
    }

    void Morir()
    {
        // Notificar ANTES de la animación para que GestorNivel actualice el contador
        GestorNivel.Instancia?.NotificarMuerteEnemigo();

        // Desactivar el collider para que el jugador ya no lo detecte
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        Vector3 escalaBase = transform.localScale;

        // ── Fase 1: rebote (0.15 s) ─────────────────────────────────────
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float prog = t / 0.15f;
            // Seno para hacer el "pop" hacia arriba y volver
            transform.localScale = escalaBase * (1f + 0.4f * Mathf.Sin(prog * Mathf.PI));
            yield return null;
        }

        // ── Fase 2: giro + encogimiento (0.45 s) ─────────────────────────
        t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float prog = Mathf.Clamp01(t / 0.45f);
            // Ease-in cuadrático: lento al principio, rápido al final
            transform.localScale = Vector3.Lerp(escalaBase, Vector3.zero, prog * prog);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }
}
