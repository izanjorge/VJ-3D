using UnityEngine;

public class Casilla : MonoBehaviour
{
    public bool tieneSlime = false;

    [Header("Arrastra aquí tu plano de pintura")]
    public GameObject manchaVisual;

    void Start()
    {
        // Esto es un "seguro de vida": nada más empezar la partida, 
        // obligamos a la mancha a apagarse, independientemente de cómo esté en el Prefab.
        if (manchaVisual != null)
        {
            manchaVisual.SetActive(false);
        }

        tieneSlime = false;
    }

    public void MancharConSlime()
    {
        if (tieneSlime) return;
        tieneSlime = true;

        if (manchaVisual != null)
        {
            manchaVisual.SetActive(true); // Encendemos la mancha
        }
    }

    public void LimpiarSlime()
    {
        if (!tieneSlime) return;
        tieneSlime = false;

        if (manchaVisual != null)
        {
            manchaVisual.SetActive(false); // Apagamos la mancha
        }
    }
}