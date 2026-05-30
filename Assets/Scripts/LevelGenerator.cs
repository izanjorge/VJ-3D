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
    public GameObject sueloLapidaPrefab;   // FloorTile + lápida + trigger de daño

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

    [Header("Puerta de Salida")]
    public GameObject puertaPrefab;   // Prefab Puerta (cubo visual, script Puerta)

    const int COLUMNAS    = 7;
    const int OFFSET_COL  = 3; // COLUMNAS / 2
    const float ALTURA_MUROS = 1.0f;

    // Filas variables por nivel (tamaños distintos = niveles más creativos)
    static readonly int[] FILAS_POR_NIVEL = { 8, 10, 10, 12, 14, 12, 14, 24, 14, 18 };

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
        // Tutorial puro. Introduce cajas (empujables) y vallas (bloquean paso).
        new string[] {
            ".......",  // f=0  jugador en c=3
            "...$...",  // f=1  moneda fácil al centro
            "..C.C..",  // f=2  cajas c=2,c=4 (empujables, no en esquinas)
            ".......",  // f=3
            "V..$..V",  // f=4  vallas c=0,c=6 + moneda: aprende que las vallas bloquean
            "..C.C..",  // f=5  más cajas
            "B.....B",  // f=6  barriles decorativos
            ".......",  // f=7
        },

        // ── NIVEL 1 (10 filas) · "Taller de Barriles" ────── Verde
        // Pasillos de barriles, cajas fuera de esquinas, primeros pinchos asimétricos.
        new string[] {
            ".......",  // f=0
            "B.B.B..",  // f=1  barrera de barriles, hueco en c=5,c=6
            "...$...",  // f=2
            ".C...C.",  // f=3  cajas c=1,c=5 (nunca en esquinas c=0/c=6)
            ".......",  // f=4
            ".J.$.J.",  // f=5  jarrones + moneda central
            "P.....P",  // f=6  pinchos en los flancos
            ".PP.PP.",  // f=7  pinchos asimétrico: c=1,2,4,5
            ".$.....",  // f=8  moneda izquierda (asimétrico)
            ".......",  // f=9
        },

        // ── NIVEL 2 (10 filas) · "El Slalom" ─────────────── Verde
        // Slalom con pinchos y vallas asimétricas que limitan rutas.
        new string[] {
            ".......",  // f=0
            "V.V....",  // f=1  vallas c=0,c=2 (asimétricas, limitan movimiento)
            ".......",  // f=2
            "..PPPPP",  // f=3  hueco izquierdo c=0,c=1
            ".$.C...",  // f=4  moneda + caja c=3
            "PPPP...",  // f=5  hueco derecho c=4,c=5,c=6 (asimétrico)
            "V.....V",  // f=6  vallas flancos
            "...C.$.",  // f=7  caja c=3 + moneda derecha
            "..PPPP.",  // f=8  pinchos c=2-5, huecos c=0,c=1,c=6
            ".......",  // f=9
        },

        // ── NIVEL 3 (12 filas) · "Catacumbas" ────────────── Amarillo  ★SUELO CAE★
        // Pinchos asimétricos, cajas fuera de esquinas, primeras vallas como obstáculo real.
        new string[] {
            ".......",  // f=0
            "V.....V",  // f=1  vallas flancos (fuerzan pasar por el centro)
            "PPP....",  // f=2  pinchos izquierda asimétrico c=0,1,2
            ".C...C.",  // f=3  cajas c=1,c=5
            ".J.$.J.",  // f=4  jarrones + moneda
            "PP....P",  // f=5  pinchos asimétrico c=0,1,6
            "R..C..R",  // f=6  rocas flancos + caja central c=3
            "...$...",  // f=7
            "B.PPP.B",  // f=8  barriles + pinchos centrales
            ".C.....",  // f=9  caja c=1 (sola, asimétrica)
            ".$...$.",  // f=10 monedas difíciles en los lados
            ".......",  // f=11
        },

        // ── NIVEL 4 (14 filas) · "Flechas por Primera Vez" ── Amarillo  ★SUELO CAE★
        // Cajas en filas de flechas: empújalas a c=1 ó c=5 para bloquear el proyectil.
        // Vallas en la trayectoria reducen posiciones seguras de cruce.
        new string[] {
            ".......",  // f=0
            "B.B.B..",  // f=1  barrera barriles, hueco c=5,c=6
            "...$...",  // f=2
            "P.PP..P",  // f=3  pinchos asimétrico c=0,2,3,6
            ".......",  // f=4
            "F.V.C.F",  // f=5  FLECHAS + valla c=2 + caja c=4 (empuja a c=5 para bloquear flecha dcha)
            ".......",  // f=6
            "J.P.P.J",  // f=7  jarrones + pinchos
            ".......",  // f=8
            "F.C.V.F",  // f=9  FLECHAS + caja c=2 + valla c=4 (empuja a c=1 para bloquear flecha izq)
            ".$...R.",  // f=10 moneda izquierda + roca
            "PPP.PP.",  // f=11 pinchos densos, hueco c=3
            "...$...",  // f=12
            ".......",  // f=13
        },

        // ── NIVEL 5 (12 filas) · "Corredor de Flechas" ─────── Amarillo  ★SUELO CAE★
        // Vallas DENTRO de filas con flechas: solo 2 posiciones seguras de cruce.
        new string[] {
            ".......",  // f=0
            "F.C.V.F",  // f=1  flechas + caja c=2 + valla c=4
            ".VP.P..",  // f=2  valla c=1 en trayectoria + pinchos asimétrico
            "..P.P..",  // f=3
            ".C.$...",  // f=4  caja c=1 + moneda c=3
            "F.P.P.F",  // f=5  flechas + pinchos
            "V.....V",  // f=6  vallas flancos (reducen anchura de juego)
            "PPP.PP.",  // f=7  pinchos densos, hueco c=3
            ".......",  // f=8
            "F.V.C.F",  // f=9  flechas + valla c=2 + caja c=4
            "R..$..R",  // f=10 rocas + moneda
            ".......",  // f=11
        },

        // ── NIVEL 6 (14 filas) · "Catacumbas de la Perdición" ── Rojo  ★SUELO CAE★
        // Máxima combinación: vallas + pinchos en fila de flechas → solo c=1 y c=5 libres.
        new string[] {
            ".......",  // f=0
            "..P.PP.",  // f=1  pinchos asimétrico c=2,4,5
            ".C.....",  // f=2  caja c=1
            "...$...",  // f=3
            "F.VPV.F",  // f=4  FLECHAS + vallas c=2,c=4 + pinchos c=3 → solo c=1,c=5 libres
            ".......",  // f=5
            "P.C.C.P",  // f=6  pinchos flancos + cajas c=2,c=4
            ".$...$.",  // f=7  monedas difíciles en los lados
            "F.V...F",  // f=8  flechas + valla c=2 (solo c=1,c=3,c=4,c=5 libres)
            "PPP.PP.",  // f=9  pinchos densos, hueco c=3
            "..L....",  // f=10 1ª lápida c=2 (asimétrica)
            "F.P.V.F",  // f=11 flechas + pinchos c=2 + valla c=4
            "B..L..B",  // f=12 barriles + 2ª lápida c=3
            ".......",  // f=13
        },

        // ── NIVEL 7 (24 filas) · "La S Letal × 2" ──────────── Rojo  ★SUELO CAE★  ★HABILIDAD★
        // DOS bucles en S completos sobre el vacío. El jugador hace la S hacia la izquierda,
        // vuelve al centro, la repite hacia la derecha, y culmina con puerta de flechas + muro frontal.
        //
        //  Bucle 1 (f=4-10):  c=3 → izquierda (gauntlet) → c=6
        //  Bucle 2 (f=14-20): c=3 → derecha (gauntlet)   → c=0
        new string[] {
            "___.___",  // f=0   spawn corredor c=3
            "___.___",  // f=1
            "___.___",  // f=2
            "F.....F",  // f=3   PUERTA 1: flechas c=0,6; suelo c=1-5
            "___.___",  // f=4
            ".PP.___",  // f=5   CURVA IZQ 1: suelo c=0,c=3; pinchos c=1,2; vacío c=4-6
            ".______",  // f=6   BORDE IZQ: solo c=0 seguro
            ".PPPPP.",  // f=7   GAUNTLET 1: suelo c=0 y c=6; pinchos c=1-5
            "______.",  // f=8   BORDE DER: solo c=6 seguro
            "______.",  // f=9   BORDE DER (respiro)
            "___.PP.",  // f=10  CURVA DER 1: suelo c=3,c=6; pinchos c=4,5; vacío c=0-2
            "___.___",  // f=11  corredor c=3
            "F.....F",  // f=12  PUERTA 2
            "___.___",  // f=13
            "___.PP.",  // f=14  CURVA DER 2: suelo c=3,c=6; pinchos c=4,5 (ahora va hacia c=6)
            "______.",  // f=15  BORDE DER: solo c=6 seguro
            ".PPPPP.",  // f=16  GAUNTLET 2
            ".______",  // f=17  BORDE IZQ: solo c=0 seguro
            ".______",  // f=18  BORDE IZQ (respiro)
            ".PP.___",  // f=19  CURVA IZQ 2: suelo c=0,c=3; pinchos c=1,2 (vuelve a c=3)
            "___.___",  // f=20  corredor c=3
            "F.....F",  // f=21  PUERTA 3
            "_GG.GG_",  // f=22  MURO FRONTAL: c=1,2,4,5 con ola escalonada
            ".......",  // f=23  SALIDA - suelo completo
        },

        // ── NIVEL 8 (14 filas) · "Mazmorra de la Muerte" ─────── Rojo  ★SUELO CAE★
        // Flechas con vallas dentro de la trayectoria: el cruce más difícil del juego.
        new string[] {
            ".......",  // f=0
            ".C.B.C.",  // f=1  cajas c=1,c=5 + barril c=3 (no en esquinas)
            "F..$..F",  // f=2  flechas + moneda
            "PP.P.PP",  // f=3  pinchos, huecos c=2,c=4
            "..C.C..",  // f=4  cajas c=2,c=4
            "F.VP.VF",  // f=5  FLECHAS + vallas c=2,c=5 + pinchos c=3 → solo c=1,c=4 libres
            "...$...",  // f=6
            "BPP.PPB",  // f=7  barriles + pinchos, hueco c=3
            "F.V...F",  // f=8  flechas + valla c=2
            "P...L.P",  // f=9  pinchos c=0,c=6 + lápida c=4
            ".$...$.",  // f=10 monedas difíciles
            "F.PPP.F",  // f=11 flechas + pinchos centrales
            "B..L..B",  // f=12 barriles + 2ª lápida c=3
            ".......",  // f=13
        },

        // ── NIVEL 9 (18 filas) · "Arena del Jefe Final" ──────── Morado
        // Arena completamente despejada para la batalla contra Metagross.
        // Sin obstáculos: Metagross puede saltar y aterrizar libremente en cualquier casilla.
        // Monedas en los extremos incentivan al jugador a moverse por todo el ancho.
        new string[] {
            ".......",  // f=0   jugador (c=3)
            ".......",  // f=1
            ".$...$.",  // f=2   monedas en extremos
            ".......",  // f=3
            ".......",  // f=4
            ".......",  // f=5
            ".......",  // f=6
            ".......",  // f=7   ZONA BOSS: arena completamente abierta
            ".......",  // f=8
            ".......",  // f=9
            ".......",  // f=10
            ".......",  // f=11
            ".......",  // f=12
            ".......",  // f=13
            ".......",  // f=14
            ".$...$.",  // f=15  monedas en extremos
            ".......",  // f=16
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
        SpawnearPuerta(filas);

        // Activar caída del suelo en niveles 3-8 (no en tutorial ni en boss)
        if (nivelActual >= 3 && nivelActual <= 8 && gestorCaida != null)
            gestorCaida.Iniciar(objetosPorFila, filas, player, nivelActual);

        // Inicializar gestor de nivel (enemigos + salida) en TODOS los niveles
        GestorNivel gestor = GetComponent<GestorNivel>();
        if (gestor != null) gestor.Iniciar(filas, player);
    }

    // ── PUERTA DE SALIDA ────────────────────────────────────────────────────
    void SpawnearPuerta(int filas)
    {
        if (puertaPrefab == null) return;
        // La puerta se incrusta en la pared del fondo (z = filas - 0.5, igual que GenerarParedesL).
        // Y = 1 → base a ras del suelo para un cubo de altura 2.
        // No necesita BoxCollider: el jugador no puede caminar hasta allí de todos modos.
        // El paso al siguiente nivel se activa desde ControladorJugador al pulsar adelante.
        Vector3 pos = new Vector3(0f, 1.5f, filas - 0.5f);
        Instantiate(puertaPrefab, pos, puertaPrefab.transform.rotation, transform);
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

                // P y L llevan su propia base (SueloTrampa / SueloLapida); _ no genera suelo
                if (tile != 'P' && tile != '_' && tile != 'L')
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
                // SueloLapida ya incluye el tile de suelo; se spawna en pos (Y=0).
                // El trigger de daño y DetectorGolpe están configurados en el prefab.
                SpawnRegistrado(sueloLapidaPrefab, pos, Quaternion.identity, fila);
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
        // Se registra en la última fila (puerta), que NO cae en SecuenciaCaida.
        float[] posXFondo = { -2.5f, -1f, 0f, 1f, 2.5f };
        foreach (float x in posXFondo)
        {
            GameObject prefab = x == 0f            ? pared3Prefab :
                                (x == -1f || x == 1f) ? pared2Prefab :
                                                        pared1Prefab;
            SpawnRegistrado(prefab, new Vector3(x, ALTURA_MUROS, zFondo), Quaternion.Euler(0, 90, 0), filas - 1);
        }

        // ── Pared LATERAL IZQUIERDA (dinámica según número de filas) ────
        // Cada pieza se registra en su fila correspondiente para caer con el suelo.
        int numPiezasLateral = Mathf.CeilToInt(filas / 2f);
        for (int i = 0; i < numPiezasLateral; i++)
        {
            float zPos = (i * 2) + 0.5f;
            if (zPos >= zFondo) continue; // evitar solapamiento con pared del fondo
            GameObject prefab = (i % 2 == 1) ? pared2Prefab : pared1Prefab;
            SpawnRegistrado(prefab, new Vector3(xIzquierda, ALTURA_MUROS, zPos), Quaternion.identity, i * 2);
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
