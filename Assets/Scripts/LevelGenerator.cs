using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Suelo")]
    public GameObject floorTilePrefab;

    [Header("Paredes")]
    public GameObject pared1Prefab;
    public GameObject pared2Prefab;
    public GameObject pared3Prefab;

    [Header("Trampas")]
    public GameObject trampaPinchosPrefab;
    public GameObject trampaFlechasPrefab;
    public GameObject trampaLapidaPrefab;

    [Header("Obstáculos y Decoración")]
    public GameObject barrilPrefab;
    public GameObject jarronPrefab;
    public GameObject cajaPrefab;
    public GameObject rocaPrefab;
    public GameObject vallaPrefab;

    [Header("Recogibles")]
    public GameObject monedaPrefab;

    [Header("Jugador")]
    public GameObject player;

    [Header("Materiales por Dificultad")]
    public Material matVerde;
    public Material matAmarillo;
    public Material matRojo;
    public Material matMorado;

    [Header("Configuración")]
    [Range(0, 9)] public int nivelActual = 0;

    [Header("Sistema de Caída del Suelo")]
    public GestorCaidaSuelo gestorCaida;

    const int COLUMNAS    = 7;
    const int OFFSET_COL  = 3; // COLUMNAS / 2
    const float ALTURA_MUROS = 1.0f;

    // Filas variables por nivel (tamaños distintos = niveles más creativos)
    static readonly int[] FILAS_POR_NIVEL = { 8, 10, 10, 12, 14, 12, 14, 16, 14, 12 };

    // ──────────────────────────────────────────────────────────────────────
    //  DISEÑO DE LOS 10 NIVELES
    //  Fila 0 = inicio jugador  |  Fila (FILAS-1) = puerta del fondo
    //  Columnas 0-6  →  X de -3 a +3
    //
    //  Leyenda:
    //   . suelo vacío    P Pinchos     F Flechas    L Lápida
    //   B Barril         J Jarrón      C Caja
    //   R Roca           V Valla       $ Moneda
    // ──────────────────────────────────────────────────────────────────────
    static readonly string[][] NIVELES = new string[][]
    {
        // ── NIVEL 0 (8 filas) · "Sala de Bienvenida" ──────── Verde
        // Tutorial. Sin trampas. Aprende a moverte y recoger monedas.
        new string[] {
            ".......",  // f=0  jugador en c=3
            "...$...",  // f=1
            ".V...V.",  // f=2
            ".......",  // f=3
            ".R...R.",  // f=4
            "...$...",  // f=5
            ".B.J.B.",  // f=6
            ".......",  // f=7
        },

        // ── NIVEL 1 (10 filas) · "Cementerio de Barriles" ─── Verde
        // Los barriles forman una barrera con huecos. Primer contacto con pinchos.
        new string[] {
            ".......",  // f=0
            "B.B.B.B",  // f=1  barrera con huecos en c=1,3,5
            "...$...",  // f=2
            ".......",  // f=3
            ".R...R.",  // f=4
            "..J.J..",  // f=5
            ".......",  // f=6
            "..PPP..",  // f=7  primeros pinchos
            "...$...",  // f=8
            ".......",  // f=9
        },

        // ── NIVEL 2 (10 filas) · "El Slalom" ─────────────── Verde
        // Pinchos forman bloques alternos: debes zigzaguear de lado a lado.
        new string[] {
            ".......",  // f=0
            "V.....V",  // f=1
            ".......",  // f=2
            "..PPPPP",  // f=3  hueco izquierdo (c=0,1)
            "...$...",  // f=4
            "PPPPP..",  // f=5  hueco derecho (c=5,6)
            "...$...",  // f=6
            "..PPPPP",  // f=7  hueco izquierdo otra vez
            ".B.J.B.",  // f=8
            ".......",  // f=9
        },

        // ── NIVEL 3 (12 filas) · "Catacumbas Inestables" ─── Amarillo  ★SUELO CAE★
        // Aparecen las lápidas. El suelo empieza a caer por filas desde detrás.
        new string[] {
            ".......",  // f=0
            "V..$..V",  // f=1
            "..PPP..",  // f=2
            "R.....R",  // f=3
            ".J.$.J.",  // f=4
            "..L.L..",  // f=5
            "P.....P",  // f=6
            "...$...",  // f=7
            ".L...L.",  // f=8
            "R.PPP.R",  // f=9
            "...$...",  // f=10
            ".......",  // f=11
        },

        // ── NIVEL 4 (14 filas) · "Laberinto de los Obstáculos" ─ Amarillo  ★SUELO CAE★
        // Todas las trampas del juego por primera vez juntas en un laberinto largo.
        new string[] {
            ".......",  // f=0
            "B.B.B.B",  // f=1  barrera de barriles
            "...$...",  // f=2
            "P.P.P.P",  // f=3  pinchos alternos (huecos en c=1,3,5)
            ".......",  // f=4
            "R.L.L.R",  // f=5
            "...$...",  // f=6
            "JPP.PPJ",  // f=7  combinación obstáculo + pinchos
            ".......",  // f=8
            "..L.L..",  // f=9
            ".V.$.V.",  // f=10
            "P.....P",  // f=11
            "...$...",  // f=12
            ".......",  // f=13
        },

        // ── NIVEL 5 (12 filas) · "Corredor de Flechas" ─────── Amarillo  ★SUELO CAE★
        // Dispensadores de flechas en ambos laterales. Cruza en el momento justo.
        new string[] {
            ".......",  // f=0
            "F.....F",  // f=1  flechas laterales
            "..P.P..",  // f=2
            ".......",  // f=3
            "F.L.L.F",  // f=4  flechas + lápidas
            "...$...",  // f=5
            "J.P.P.J",  // f=6
            ".......",  // f=7
            "F.....F",  // f=8
            "..L.L..",  // f=9
            "R..$..R",  // f=10
            ".......",  // f=11
        },

        // ── NIVEL 6 (14 filas) · "Catacumbas de la Perdición" ── Rojo  ★SUELO CAE★
        // Intensidad alta. Las tres trampas combinadas a ritmo rápido.
        new string[] {
            ".......",  // f=0
            "..P.P..",  // f=1
            "L.....L",  // f=2
            "...$...",  // f=3
            "F.PPP.F",  // f=4  flechas + pinchos centrales
            ".......",  // f=5
            "P.L.L.P",  // f=6
            "...$...",  // f=7
            "F.....F",  // f=8
            "LPP.PPL",  // f=9  lápidas flanqueando pinchos (hueco en c=3)
            ".......",  // f=10
            "F.P.P.F",  // f=11
            ".L.$.L.",  // f=12
            ".......",  // f=13
        },

        // ── NIVEL 7 (16 filas) · "El Gran Laberinto" ────────── Rojo  ★SUELO CAE★
        // La sala más larga. Cada sección tiene un patrón diferente.
        new string[] {
            ".......",  // f=0
            ".J...J.",  // f=1
            "L.P.P.L",  // f=2
            ".......",  // f=3
            "P.P.P.P",  // f=4  pinchos en toda la fila (huecos en c=1,3,5)
            "...$...",  // f=5
            "F.L.L.F",  // f=6
            "PP...PP",  // f=7  pinchos laterales (hueco central amplio)
            ".......",  // f=8
            "R.P.P.R",  // f=9
            "...$...",  // f=10
            "F.P.P.F",  // f=11
            "L.L.L.L",  // f=12  fila completa de lápidas
            ".......",  // f=13
            "B.P.P.B",  // f=14
            ".......",  // f=15
        },

        // ── NIVEL 8 (14 filas) · "Mazmorra de la Muerte" ─────── Rojo  ★SUELO CAE★
        // Máxima densidad. Hay una sola ruta segura en cada fila.
        new string[] {
            ".......",  // f=0
            "LL...LL",  // f=1
            "F..$..F",  // f=2
            "PP.P.PP",  // f=3  huecos mínimos en c=2 y c=4
            "..L.L..",  // f=4
            "F.P.P.F",  // f=5
            "...$...",  // f=6
            "LPP.PPL",  // f=7  hueco en c=3
            "F.....F",  // f=8
            "P.L.L.P",  // f=9
            "...$...",  // f=10
            "F.PPP.F",  // f=11  huecos en c=1 y c=5
            "L.L.L.L",  // f=12
            ".......",  // f=13
        },

        // ── NIVEL 9 (12 filas) · "Arena del Jefe Final" ──────── Morado
        // Sin caída de suelo. Arena despejada para el Boss (f=5, c=3).
        new string[] {
            ".......",  // f=0
            ".J...J.",  // f=1
            "..P.P..",  // f=2
            ".......",  // f=3
            "P.....P",  // f=4
            ".......",  // f=5   ← Boss se instanciará aquí (implementación futura)
            ".......",  // f=6
            ".......",  // f=7
            "..P.P..",  // f=8
            ".......",  // f=9
            ".B...B.",  // f=10
            ".......",  // f=11
        },
    };

    // Objetos registrados por fila (para la caída del suelo)
    readonly Dictionary<int, List<GameObject>> objetosPorFila = new Dictionary<int, List<GameObject>>();

    void Start()
    {
        objetosPorFila.Clear();
        int filas = FILAS_POR_NIVEL[nivelActual];

        GenerarMapa(filas);
        GenerarParedesL(filas);

        // Activar caída del suelo en niveles 3-8 (no en tutorial ni en boss)
        if (nivelActual >= 3 && nivelActual <= 8 && gestorCaida != null)
            gestorCaida.Iniciar(objetosPorFila, filas, player);
    }

    // ── GENERACIÓN DEL MAPA ─────────────────────────────────────────────
    void GenerarMapa(int filas)
    {
        Material mat = SeleccionarMaterialPorNivel();
        string[] mapa = NIVELES[nivelActual];

        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < COLUMNAS; c++)
            {
                Vector3 pos = new Vector3(c - OFFSET_COL, 0, f);
                char tile = mapa[f][c];

                // SueloTrampa (P) ya lleva su propia base; todo lo demás necesita FloorTile
                if (tile != 'P')
                {
                    GameObject suelo = SpawnRegistrado(floorTilePrefab, pos, Quaternion.identity, f);
                    if (mat != null && suelo != null && suelo.TryGetComponent(out Renderer r))
                        r.material = mat;
                }

                // Posición inicial del jugador (fila 0, columna central)
                if (f == 0 && c == 3 && player != null)
                    player.transform.position = pos + Vector3.up * 0.5f;

                ColocarElemento(tile, pos, c, f);
            }
        }
    }

    void ColocarElemento(char tile, Vector3 pos, int columna, int fila)
    {
        switch (tile)
        {
            // ── TRAMPAS ────────────────────────────────────────────────
            case 'P':
                SpawnRegistrado(trampaPinchosPrefab, pos, Quaternion.identity, fila);
                break;

            case 'F':
                // Lateral izquierdo (c=0) apunta a la derecha, lateral derecho (c=6) a la izquierda
                Quaternion rotF = columna == 0 ? Quaternion.Euler(0, -90, 0) :
                                  columna == 6 ? Quaternion.Euler(0,  90, 0) :
                                                 Quaternion.identity;
                SpawnRegistrado(trampaFlechasPrefab, pos, rotF, fila);
                break;

            case 'L':
                SpawnRegistrado(trampaLapidaPrefab, pos + Vector3.up, Quaternion.identity, fila);
                break;

            // ── OBSTÁCULOS / DECORACIÓN ────────────────────────────────
            case 'B': SpawnRegistrado(barrilPrefab, pos + Vector3.up, Quaternion.identity, fila); break;
            case 'J': SpawnRegistrado(jarronPrefab, pos + Vector3.up, Quaternion.identity, fila); break;
            case 'C': SpawnRegistrado(cajaPrefab,   pos + Vector3.up, Quaternion.identity, fila); break;
            case 'R': SpawnRegistrado(rocaPrefab,   pos + Vector3.up, Quaternion.identity, fila); break;
            case 'V': SpawnRegistrado(vallaPrefab,  pos + Vector3.up, Quaternion.identity, fila); break;

            // ── RECOGIBLES ─────────────────────────────────────────────
            case '$':
                if (monedaPrefab != null)
                {
                    GameObject moneda = Instantiate(monedaPrefab, pos, monedaPrefab.transform.rotation, transform);
                    RegistrarEnFila(fila, moneda);
                }
                break;
        }
    }

    // ── GENERACIÓN DE PAREDES EN L ──────────────────────────────────────
    void GenerarParedesL(int filas)
    {
        float xIzquierda = -OFFSET_COL - 0.5f;
        float zFondo     = filas - 0.5f;

        // ── Pared del FONDO (siempre 5 piezas para COLUMNAS=7) ──────────
        float[] posXFondo = { -2.5f, -1f, 0f, 1f, 2.5f };
        foreach (float x in posXFondo)
        {
            GameObject prefab = x == 0f            ? pared3Prefab :
                                (x == -1f || x == 1f) ? pared2Prefab :
                                                        pared1Prefab;
            Spawn(prefab, new Vector3(x, ALTURA_MUROS, zFondo), Quaternion.Euler(0, 90, 0));
        }

        // ── Pared LATERAL IZQUIERDA (dinámica según número de filas) ────
        int numPiezasLateral = Mathf.CeilToInt(filas / 2f);
        for (int i = 0; i < numPiezasLateral; i++)
        {
            float zPos = (i * 2) + 0.5f;
            if (zPos >= zFondo) continue; // evitar solapamiento con pared del fondo
            GameObject prefab = (i % 2 == 1) ? pared2Prefab : pared1Prefab;
            Spawn(prefab, new Vector3(xIzquierda, ALTURA_MUROS, zPos), Quaternion.identity);
        }
    }

    // ── UTILIDADES ──────────────────────────────────────────────────────
    GameObject SpawnRegistrado(GameObject prefab, Vector3 pos, Quaternion rot, int fila)
    {
        if (prefab == null) return null;
        GameObject obj = Instantiate(prefab, pos, rot, transform);
        RegistrarEnFila(fila, obj);
        return obj;
    }

    void Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab != null) Instantiate(prefab, pos, rot, transform);
    }

    void RegistrarEnFila(int fila, GameObject obj)
    {
        if (obj == null) return;
        if (!objetosPorFila.ContainsKey(fila))
            objetosPorFila[fila] = new List<GameObject>();
        objetosPorFila[fila].Add(obj);
    }

    Material SeleccionarMaterialPorNivel()
    {
        if (nivelActual <= 2) return matVerde;
        if (nivelActual <= 5) return matAmarillo;
        if (nivelActual <= 8) return matRojo;
        return matMorado;
    }
}
