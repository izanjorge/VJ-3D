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
    static readonly int[] FILAS_POR_NIVEL = { 8, 10, 10, 12, 14, 12, 14, 18, 14, 18 };

    // ──────────────────────────────────────────────────────────────────────
    //  DISEÑO DE LOS 10 NIVELES
    //  Fila 0 = inicio jugador  |  Fila (FILAS-1) = puerta del fondo
    //  Columnas 0-6  →  X de -3 a +3
    //
    //  Leyenda:
    //   . suelo vacío    P Pinchos     F Flechas    L Lápida (spawner enemigo)
    //   B Barril         J Jarrón      C Caja
    //   R Roca           V Valla       $ Moneda     _ Vacío (caída libre, sin suelo)
    //   G Flecha Frontal (apunta hacia jugador +Z, retraso escalonado por columna; solo c=1-5)
    // ──────────────────────────────────────────────────────────────────────
    static readonly string[][] NIVELES = new string[][]
    {
        // ── NIVEL 0 (8 filas) · "Sala de Bienvenida" ──────── Verde
        // Tutorial puro. Sin trampas. Aprende a moverte y recoger monedas.
        new string[] {
            ".......",  // f=0  jugador en c=3
            "...$...",  // f=1  moneda fácil al centro
            ".C...C.",  // f=2  cajas decorativas
            ".......",  // f=3
            "R..$..R",  // f=4  rocas + moneda ligeramente descentrada
            "..C.C..",  // f=5  cajas
            "B.....B",  // f=6  barriles en las esquinas
            ".......",  // f=7
        },

        // ── NIVEL 1 (10 filas) · "Taller de Barriles" ────── Verde
        // Barriles y cajas forman pasillos. Primeros pinchos al final.
        new string[] {
            ".......",  // f=0
            "B.B.B.B",  // f=1  barrera de barriles con huecos en c=1,3,5
            "...$...",  // f=2
            "C.....C",  // f=3  cajas en las esquinas
            ".......",  // f=4
            ".J.$.J.",  // f=5  jarrones con moneda central
            ".......",  // f=6
            "..PPP..",  // f=7  primeros pinchos
            ".$...J.",  // f=8  moneda difícil izquierda + jarrón
            ".......",  // f=9
        },

        // ── NIVEL 2 (10 filas) · "El Slalom" ─────────────── Verde
        // Pinchos forman bloques alternos: zigzaguea de lado a lado.
        new string[] {
            ".......",  // f=0
            "V.....V",  // f=1
            ".......",  // f=2
            "..PPPPP",  // f=3  hueco izquierdo (c=0,1)
            ".$.C...",  // f=4  moneda izquierda + caja
            "PPPPP..",  // f=5  hueco derecho (c=5,6)
            "...C.$.",  // f=6  caja + moneda difícil derecha
            "..PPPPP",  // f=7  hueco izquierdo otra vez
            "B.....B",  // f=8
            ".......",  // f=9
        },

        // ── NIVEL 3 (12 filas) · "Catacumbas" ────────────── Amarillo  ★SUELO CAE★
        // El suelo empieza a caer. Sin lápidas aún: solo obstáculos y pinchos.
        new string[] {
            ".......",  // f=0
            "V..$..V",  // f=1  vallas + moneda
            "..PPP..",  // f=2
            "C.....C",  // f=3  cajas en los flancos
            ".J.$.J.",  // f=4  jarrones + moneda central
            "P.....P",  // f=5  pinchos en los flancos
            "R..C..R",  // f=6  rocas + caja central
            "...$...",  // f=7
            "B.PPP.B",  // f=8  barriles + pinchos
            "C.....C",  // f=9  cajas
            ".$...$.",  // f=10 monedas difíciles (c=1 y c=5)
            ".......",  // f=11
        },

        // ── NIVEL 4 (14 filas) · "Flechas por Primera Vez" ── Amarillo  ★SUELO CAE★
        // Flechas laterales por primera vez. Aprende el timing de cruce.
        new string[] {
            ".......",  // f=0
            "B.B.B..",  // f=1  barrera de barriles, hueco derecho en c=5,6
            "...$...",  // f=2
            "P.P.P.P",  // f=3  pinchos alternos (huecos en c=1,3,5)
            ".......",  // f=4
            "F.....F",  // f=5  PRIMERAS FLECHAS
            "C..$..C",  // f=6  cajas + moneda descentrada
            "J.P.P.J",  // f=7  jarrones + pinchos
            ".......",  // f=8
            "F.....F",  // f=9  más flechas
            ".$...R.",  // f=10 moneda difícil izquierda + roca
            "R.C.C.R",  // f=11 rocas + cajas
            "...$...",  // f=12
            ".......",  // f=13
        },

        // ── NIVEL 5 (12 filas) · "Corredor de Flechas" ─────── Amarillo  ★SUELO CAE★
        // Flechas frecuentes + primera lápida (sorpresa al final). Timing crucial.
        new string[] {
            ".......",  // f=0
            "F.....F",  // f=1  flechas
            "..P.P..",  // f=2
            ".......",  // f=3
            "C..$..C",  // f=4  cajas + moneda
            "F.P.P.F",  // f=5  flechas + pinchos
            "B.....B",  // f=6
            "..PPP..",  // f=7
            ".......",  // f=8
            "F.....F",  // f=9
            "R..$..R",  // f=10 rocas + moneda
            "...L...",  // f=11 1 lápida central (sorpresa al final)
        },

        // ── NIVEL 6 (14 filas) · "Catacumbas de la Perdición" ── Rojo  ★SUELO CAE★
        // Intensidad alta. Flechas, pinchos y lápidas combinados.
        new string[] {
            ".......",  // f=0
            "..P.P..",  // f=1
            "C.....C",  // f=2
            "...$...",  // f=3
            "F.PPP.F",  // f=4  flechas + pinchos centrales
            ".......",  // f=5
            "P.C.C.P",  // f=6  pinchos flancos + cajas
            ".$...$.",  // f=7  monedas difíciles en los lados
            "F.....F",  // f=8
            "..PPP..",  // f=9
            "..L....",  // f=10 1ª lápida (fuera del centro)
            "F.P.P.F",  // f=11
            "B..L..B",  // f=12 2ª lápida + barriles
            ".......",  // f=13
        },

        // ── NIVEL 7 (18 filas) · "La S Letal" ───────────────── Rojo  ★SUELO CAE★  ★HABILIDAD★
        // Camino en S con casillas seguras (.) dispersas. El resto es vacío (caída libre).
        // Recorrido: c=3 vertical → cruce izquierdo (c=0) → gauntlet → cruce derecho → c=3 vertical
        //
        //  ___.___  corredor central (c=3 seguro, vacío alrededor)
        //  F.....F  puerta de flechas: flechas c=0 y c=6, suelo seguro en el medio
        //  .PP.___  curva izquierda: seguro c=0 y c=3, pinchos c=1-2 (cruzar con cuidado)
        //  .______  borde izquierdo (solo c=0 seguro)
        //  .PPPPP.  gauntlet horizontal: seguro c=0 y c=6, pinchos c=1-5
        //  ______.  borde derecho (solo c=6 seguro)
        //  ___.PP.  curva derecha: seguro c=3 y c=6, pinchos c=4-5 (cruzar con cuidado)
        new string[] {
            "___.___",  // f=0  corredor: solo c=3 seguro (jugador spawn)
            "___.___",  // f=1  corredor
            "___.___",  // f=2  corredor
            "F.....F",  // f=3  PUERTA DE FLECHAS: flechas c=0,6; suelo seguro c=1-5
            "___.___",  // f=4  corredor
            ".PP.___",  // f=5  CURVA IZQ: seguro c=0,3; pinchos c=1,2; vacío c=4-6
            ".______",  // f=6  BORDE IZQ: solo c=0 seguro, vacío resto
            ".PPPPP.",  // f=7  GAUNTLET: seguro c=0 y c=6; pinchos c=1-5
            "______.",  // f=8  BORDE DER: solo c=6 seguro, vacío resto
            "______.",  // f=9  BORDE DER: solo c=6 seguro (momento de respiro)
            "___.PP.",  // f=10 CURVA DER: seguro c=3,6; pinchos c=4,5; vacío c=0-2
            "___.___",  // f=11 corredor
            "___.___",  // f=12 corredor
            "F.....F",  // f=13 PUERTA DE FLECHAS: flechas c=0,6; suelo seguro c=1-5
            "_GG.GG_",  // f=14 MURO DE FLECHAS FRONTALES: c=1,2 y c=4,5 con ola escalonada
            "___.___",  // f=15 corredor seguro
            "___.___",  // f=16 corredor final
            ".......",  // f=17 SALIDA - suelo seguro
        },

        // ── NIVEL 8 (14 filas) · "Mazmorra de la Muerte" ─────── Rojo  ★SUELO CAE★
        // Máxima densidad. Hay una sola ruta segura en cada fila.
        new string[] {
            ".......",  // f=0
            "C..B..C",  // f=1  cajas + barril (decoración)
            "F..$..F",  // f=2  flechas + moneda descentrada
            "PP.P.PP",  // f=3  huecos mínimos en c=2 y c=4
            "..C.C..",  // f=4  cajas
            "F.P.P.F",  // f=5  flechas + pinchos
            "...$...",  // f=6
            "BPP.PPB",  // f=7  barriles flanqueando pinchos, hueco en c=3
            "F.....F",  // f=8
            "P...L.P",  // f=9  pinchos flancos + 1ª lápida
            ".$...$.",  // f=10 monedas difíciles en los lados
            "F.PPP.F",  // f=11 flechas + pinchos centrales
            "B..L..B",  // f=12 2ª lápida + barriles
            ".......",  // f=13
        },

        // ── NIVEL 9 (18 filas) · "Arena del Jefe Final" ──────── Morado
        // Sin caída de suelo. Arena amplia para el Boss (4×4 casillas, aprox. c=2-5, f=6-9).
        new string[] {
            ".......",  // f=0
            ".J...J.",  // f=1  jarrones decorativos
            "..P.P..",  // f=2  pinchos ornamentales
            ".......",  // f=3
            "R.....R",  // f=4  rocas en flancos
            ".......",  // f=5
            ".......",  // f=6  zona boss (4×4 centrado: c=2-5, f=6-9)
            ".......",  // f=7
            ".......",  // f=8
            ".......",  // f=9
            ".......",  // f=10
            "R.....R",  // f=11 rocas simétricas
            ".......",  // f=12
            "V.....V",  // f=13 vallas
            "..P.P..",  // f=14
            ".......",  // f=15
            "C.....C",  // f=16 cajas
            ".......",  // f=17
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

                // SueloTrampa (P) ya lleva su propia base; vacío (_) no genera suelo
                if (tile != 'P' && tile != '_')
                {
                    GameObject suelo = SpawnRegistrado(floorTilePrefab, pos, Quaternion.identity, f);
                    if (mat != null && suelo != null && suelo.TryGetComponent(out Renderer r))
                        r.material = mat;
                }

                // Posición inicial del jugador (fila 0, columna central)
                if (f == 0 && c == 3 && player != null)
                    player.transform.position = pos + Vector3.up * 0.05f;

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
                // c=0 (izquierda) dispara hacia la derecha (+X)
                // c=6 (derecha) dispara hacia la izquierda (-X)
                Quaternion rotF = columna == 0 ? Quaternion.Euler(0,  180, 0) :
                                  columna == 6 ? Quaternion.Euler(0, 0, 0) :
                                                 Quaternion.identity;
                SpawnRegistrado(trampaFlechasPrefab, pos + Vector3.up * 1.7f, rotF, fila);
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

            // ── TRAMPAS FRONTALES (apuntan hacia el jugador, -Z) ───────
            case 'G':
            {
                // Y=-90 → puntoDisparo.right apunta a -Z mundo → -right = +Z (hacia jugador)
                Quaternion rotG = Quaternion.Euler(0, -90, 0);
                GameObject trapG = SpawnRegistrado(trampaFlechasPrefab, pos + Vector3.up * 1.7f, rotG, fila);
                if (trapG != null)
                {
                    TrampaDisparador td = trapG.GetComponent<TrampaDisparador>();
                    // Retraso escalonado: cada columna dispara 0.4 s más tarde que la anterior.
                    // Crea una ola de izquierda a derecha que el jugador puede leer y esquivar.
                    if (td != null) td.retardoInicial = columna * 0.4f;
                }
                break;
            }

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
