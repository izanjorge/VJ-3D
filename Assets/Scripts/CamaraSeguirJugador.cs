using UnityEngine;
using Unity.Cinemachine;

// Adjuntar a la CinemachineCamera (o añadir al prefab CinemachineCamera).
// Reconecta el TrackingTarget al jugador persistente (DontDestroyOnLoad)
// cada vez que se carga una escena nueva.
[RequireComponent(typeof(CinemachineCamera))]
public class CamaraSeguirJugador : MonoBehaviour
{
    void Start()
    {
        Reconectar();
    }

    void Reconectar()
    {
        if (SaludJugador.Instancia == null) return;

        CinemachineCamera cam = GetComponent<CinemachineCamera>();
        cam.Target.TrackingTarget = SaludJugador.Instancia.transform;
    }
}
