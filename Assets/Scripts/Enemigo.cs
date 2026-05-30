using UnityEngine;

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
        // Si no hay ningún Collider en el root (ni en hijos), añadimos uno.
        // Esto permite atacar al enemigo y que bloquee el movimiento del jugador.
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            // Tamaño genérico que encaja con la mayoría de modelos de enemigo
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
        GestorNivel.Instancia?.NotificarMuerteEnemigo();
        Destroy(gameObject);
    }
}
