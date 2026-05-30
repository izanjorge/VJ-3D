using UnityEngine;

public class Casilla : MonoBehaviour
{
    public bool tieneSlime = false;

    [Tooltip("Arrastra aquí el hijo ManchaSlime del propio prefab FloorTile")]
    public GameObject manchaVisual;

    void Start()
    {
        tieneSlime = false;
        manchaVisual?.SetActive(false);
    }

    public void MancharConSlime()
    {
        if (tieneSlime) return;
        tieneSlime = true;
        manchaVisual?.SetActive(true);
    }

    public void LimpiarSlime()
    {
        if (!tieneSlime) return;
        tieneSlime = false;
        manchaVisual?.SetActive(false);
    }
}
