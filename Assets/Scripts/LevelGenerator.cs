using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public GameObject tilePrefab; // Aquí arrastraremos tu FloorTile de Blockbench
    public int width = 7;         // Ancho de la sala (columnas)
    public int height = 10;        // Largo de la sala (filas)

    // Lista para guardar los bloques y poder hacer que caigan después
    private List<GameObject> allTiles = new List<GameObject>();

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Instanciamos el cubo en posiciones enteras (0, 1, 2...)
                Vector3 spawnPos = new Vector3(x, 0, z);
                GameObject newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                
                // Los hacemos hijos de este objeto para no llenar la jerarquía
                newTile.transform.parent = this.transform;
                allTiles.Add(newTile);
            }
        }
    }
}