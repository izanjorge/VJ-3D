using UnityEngine;

// Añadir este componente al objeto hijo con IsTrigger (Pinchos, Flecha, etc.)
// El daño se propaga al SaludJugador del objeto que entra en el trigger.
public class DetectorGolpe : MonoBehaviour
{
    [Header("Configuración")]
    public int danio = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Jugador")) return;

        // GetComponentInParent cubre el caso de que el collider sea de un hijo del jugador
        SaludJugador salud = other.GetComponentInParent<SaludJugador>();
        salud?.RecibirDanio(danio);
    }
}
