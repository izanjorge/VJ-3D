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

    [Header("Referencias de Paredes")]
    public GameObject pared1Prefab; 
    public GameObject pared2Prefab; 
    public GameObject pared3Prefab; 

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
        GenerarParedesL(); 
    }

    void GenerarMapa()
    {
        Material materialSeleccionado = SeleccionarMaterialPorNivel();
        int offsetColumnas = columnas / 2;

        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < columnas; c++)
            {
                Vector3 posicionCelda = new Vector3(c - offsetColumnas, 0, f);
                
                bool esTrampa = (nivelActual == 1 && f == 5 && (c == 2 || c == 3 || c == 4));

                if (esTrampa)
                {
                    Instantiate(trampaPinchosPrefab, posicionCelda, Quaternion.identity, transform);
                }
                else
                {
                    GameObject suelo = Instantiate(floorTilePrefab, posicionCelda, Quaternion.identity, transform);
                    if (materialSeleccionado != null)
                    {
                        suelo.GetComponent<Renderer>().material = materialSeleccionado;
                    }
                }

                if (f == 0 && c == 3 && player != null)
                {
                    player.transform.position = posicionCelda + Vector3.up * 0.5f;
                }

                if (!esTrampa)
                {
                    DecidirObjeto(f, c, posicionCelda);
                }
            }
        }
    }

    void GenerarParedesL()
    {
        int offsetColumnas = columnas / 2; 
        
        float xIzquierda = -offsetColumnas - 0.5f; 
        float zFondo = filas - 0.5f; 

        // SOLUCIÓN ALTURA: Subimos los muros para que queden a ras de suelo y tapen al jugador
        float alturaMuros = 1.0f; 

        // 1. PARED DEL FONDO (Cierre exacto sin sobresalir)
        // Posiciones solapadas inteligentemente para que los extremos queden clavados en los bordes
        float[] posicionesXFondo = { -2.5f, -1f, 0f, 1f, 2.5f };
        
        foreach (float x in posicionesXFondo)
        {
            Vector3 posicionCelda = new Vector3(x, alturaMuros, zFondo);
            GameObject paredAInstanciar = pared1Prefab;

            if (x == 0f) 
            {
                paredAInstanciar = pared3Prefab; // Puerta central
            }
            else if (x == -1f || x == 1f)
            {
                paredAInstanciar = pared2Prefab; // Antorchas
            }

            if (paredAInstanciar != null)
            {
                Instantiate(paredAInstanciar, posicionCelda, Quaternion.Euler(0, 90, 0), transform);
            }
        }

        // 2. PARED LATERAL IZQUIERDA (Cierre Hermético exacto)
        // 5 Muros que cubren exactamente todo el lateral sin pasarse
        for (int i = 0; i < 5; i++)
        {
            float zPos = (i * 2) + 0.5f; 
            Vector3 posicionCelda = new Vector3(xIzquierda, alturaMuros, zPos);
            GameObject paredAInstanciar = pared1Prefab;

            // Antorchas en posiciones que no estorben
            if (i == 1 || i == 3)
            {
                paredAInstanciar = pared2Prefab;
            }

            if (paredAInstanciar != null)
            {
                Instantiate(paredAInstanciar, posicionCelda, Quaternion.identity, transform);
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
        if (nivelActual == 0)
        {
            if ((f == 3 && (c == 4 || c == 5 || c == 6)) || 
                (f == 5 && (c == 0 || c == 1 || c == 2)))
            {
                Instantiate(barrilPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            if ((f == 6 && c == 1) || (f == 4 && c == 5))
            {
                Instantiate(monedaPrefab, pos, monedaPrefab.transform.rotation, transform);
            }
        }
        else if (nivelActual == 1) 
        {
            if (f == 0 && c == 0)
            {
                Instantiate(jarronPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            if ((f == 2 && (c == 1 || c == 5)) || 
                (f == 3 && (c == 2 || c == 4)) || 
                (f == 5 && (c == 0 || c == 6)) || 
                (f == 7 && (c == 2 || c == 3 || c == 4)))
            {
                Instantiate(barrilPrefab, pos + Vector3.up * 1f, Quaternion.identity, transform);
            }

            if ((f == 4 && c == 3) || (f == 6 && c == 1) || (f == 6 && c == 5))
            {
                Instantiate(monedaPrefab, pos, monedaPrefab.transform.rotation, transform);
            }
        }
    }
}