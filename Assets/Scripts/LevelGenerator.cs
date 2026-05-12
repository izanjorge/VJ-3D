using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Referencias de Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject barrilPrefab;
    public GameObject jarronPrefab; 
    public GameObject monedaPrefab;
    public GameObject trampaPinchosPrefab;
    public GameObject player;

    [Header("Materiales por Dificultad")]
    public Material matVerde;    
    public Material matAmarillo; 
    public Material matRojo;     
    public Material matMorado;   

    [Header("Configuración del Nivel")]
    [Range(0, 9)] public int nivelActual = 0; 
    public int filas = 10;
    public int columnas = 7;

    void Start()
    {
        GenerarMapa();
    }

    void GenerarMapa()
    {
        Material materialSeleccionado = SeleccionarMaterialPorNivel();
        int offsetColumnas = columnas / 2;

        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < columnas; c++)
            {
                // Calculamos la posición física de la celda
                Vector3 posicionCelda = new Vector3(c - offsetColumnas, 0, f);
                
                // 1. SIEMPRE GENERAMOS EL SUELO BASE
                GameObject suelo = Instantiate(floorTilePrefab, posicionCelda, Quaternion.identity, transform);
                if (materialSeleccionado != null)
                {
                    suelo.GetComponent<Renderer>().material = materialSeleccionado;
                }

                // 2. Colocamos al Player (Siempre empieza en f=0, c=3)
                if (f == 0 && c == 3 && player != null)
                {
                    player.transform.position = posicionCelda + Vector3.up * 0.5f;
                }

                // 3. Colocamos todos los objetos y trampas ENCIMA del suelo
                DecidirObjeto(f, c, posicionCelda);
            }
        }
    }

    Material SeleccionarMaterialPorNivel()
    {
        if (nivelActual <= 2) return matVerde;
        if (nivelActual <= 5) return matAmarillo;
        if (nivelActual <= 8) return matRojo;
        return matMorado;
    }

    void DecidirObjeto(int f, int c, Vector3 pos)
    {
        // ==========================================
        // NIVEL 1 (Índice 0 en Unity)
        // ==========================================
        if (nivelActual == 0)
        {
            // Lógica de Barriles
            if ((f == 3 && (c == 4 || c == 5 || c == 6)) || 
                (f == 5 && (c == 0 || c == 1 || c == 2)))
            {
                Instantiate(barrilPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            // Lógica de Monedas
            if ((f == 6 && c == 1) || (f == 4 && c == 5))
            {
                Instantiate(monedaPrefab, pos, monedaPrefab.transform.rotation, transform);
            }
        }

        // ==========================================
        // NIVEL 2 (Índice 1 en Unity)
        // ==========================================
        else if (nivelActual == 1)
        {
            // Lógica de la Trampa de Pinchos: [6][2], [6][3], [6][4]
            if (f == 6 && (c == 2 || c == 3 || c == 4))
            {
                // SOLUCIÓN Z-FIGHTING: Elevamos la trampa a 0.51f para que quede ligeramente separada de la textura del suelo
                Instantiate(trampaPinchosPrefab, pos + Vector3.up * 0.51f, Quaternion.identity, transform);
            }

            // Lógica del Jarrón: [0][0]
            if (f == 0 && c == 0)
            {
                Instantiate(jarronPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            // Lógica de Barriles: [3][0],[3][1],[4][1],[5][1],[6][1],[7][1] y [6][5],[7][5],[8][5],[9][5]
            if ((f == 3 && c == 0) || (f == 3 && c == 1) || (f == 4 && c == 1) || 
                (f == 5 && c == 1) || (f == 6 && c == 1) || (f == 7 && c == 1) ||
                (f == 6 && c == 5) || (f == 7 && c == 5) || (f == 8 && c == 5) || (f == 9 && c == 5))
            {
                Instantiate(barrilPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            // Lógica de Monedas: [4][0],[8][6]
            if ((f == 4 && c == 0) || (f == 8 && c == 6))
            {
                Instantiate(monedaPrefab, pos, monedaPrefab.transform.rotation, transform);
            }
        }
    }
}