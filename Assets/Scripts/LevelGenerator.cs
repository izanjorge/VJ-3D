using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Ajustes de Suelo")]
    public GameObject floorTilePrefab;
    public int width = 7;
    public int length = 11;

    [Header("Materiales por Dificultad")]
    public Material matVerde;    // Para niveles 0, 1, 2
    public Material matAmarillo; // Para niveles 3, 4, 5
    public Material matRojo;     // Para niveles 6, 7, 8
    public Material matMorado;   // Para nivel 9 (Jefe)

    [Header("Configuración del Nivel")]
    [Range(0, 9)] public int nivelActual = 0; 

    void Start()
    {
        GenerarSuelo();
    }

    void GenerarSuelo()
    {
        // 1. Decidir qué color toca
        Material materialAEmitir = SeleccionarMaterial();
        
        int offsetCentrado = width / 2;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector3 pos = new Vector3(x - offsetCentrado, 0, z);
                GameObject tile = Instantiate(floorTilePrefab, pos, Quaternion.identity, transform);
                
                // 2. Pintar la casilla
                if (materialAEmitir != null)
                {
                    tile.GetComponent<Renderer>().material = materialAEmitir;
                }
            }
        }
    }

    Material SeleccionarMaterial()
    {
        if (nivelActual <= 2) return matVerde;
        if (nivelActual <= 5) return matAmarillo;
        if (nivelActual <= 8) return matRojo;
        return matMorado;
    }
}