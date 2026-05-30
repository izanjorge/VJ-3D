using UnityEngine;
using System.Collections;

/// <summary>
/// Adjuntar al prefab Puerta.
/// La puerta se coloca incrustada en la pared del fondo (z = filas - 0.5).
/// NO bloquea físicamente: el jugador no puede llegar hasta allí de todos modos.
/// El tránsito de nivel se activa desde ControladorJugador cuando el jugador
/// pulsa adelante en el último tile con la puerta abierta.
///
/// Estados:
///   CERRADA  → cubo oscuro/opaco incrustado en la pared
///   ABRIENDO → animación de temblor + escala
///   PORTAL   → brilla y pulsa en color cian para indicar que se puede pasar
/// </summary>
public class Puerta : MonoBehaviour
{
    [Header("Colores")]
    public Color colorCerrada = new Color(0.15f, 0.08f, 0.02f);  // marrón muy oscuro
    public Color colorPortal  = new Color(0f, 0.8f, 1f);         // cian brillante

    [Header("Giro de apertura")]
    public float duracionGiro      = 1.0f;    // segundos que tarda en girar
    public float vueltasGiro       = 3.0f;    // vueltas completas durante el giro

    [Header("Portal (pulsación)")]
    public float velocidadPulso    = 2.5f;
    public float intensidadEmision = 4f;

    // ── Estado ──────────────────────────────────────────────────────────────
    public bool EstaAbierta { get; private set; }

    private Renderer rend;
    private Light    luzPortal;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        AplicarColorCerrada();
    }

    void Update()
    {
        if (!EstaAbierta || luzPortal == null) return;

        // Pulsar la intensidad de la luz del portal
        float pulso = (Mathf.Sin(Time.time * velocidadPulso) * 0.5f + 0.5f);
        luzPortal.intensity = Mathf.Lerp(1.5f, 4f, pulso);
    }

    // ── API pública ──────────────────────────────────────────────────────────
    public void Abrir()
    {
        if (EstaAbierta) return;
        EstaAbierta = true;
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxPuerta);
        StartCoroutine(SecuenciaApertura());
    }

    // ── Animación ────────────────────────────────────────────────────────────
    IEnumerator SecuenciaApertura()
    {
        Quaternion rotacionOriginal = transform.rotation;

        // ── Fase 1: giro muy rápido sobre sí misma (1 segundo, ~3 vueltas) ──
        // Mantiene el color original durante todo el giro.
        float t = 0f;
        float gradosPorSegundo = (vueltasGiro * 360f) / duracionGiro;

        while (t < duracionGiro)
        {
            t += Time.deltaTime;
            transform.Rotate(Vector3.up, gradosPorSegundo * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Restablecer rotación exacta al terminar el giro
        transform.rotation = rotacionOriginal;

        // ── Fase 2: convertir en portal (instantáneo) ───────────────────
        AplicarColorPortal();
        CrearLuzPortal();
    }

    // ── Helpers de material ──────────────────────────────────────────────────
    void AplicarColorCerrada()
    {
        if (rend == null) return;
        // Instanciamos el material para no modificar el original
        Material mat = rend.material;
        mat.color = colorCerrada;
        mat.DisableKeyword("_EMISSION");
    }

    void AplicarColorPortal()
    {
        if (rend == null) return;
        Material mat = rend.material;
        mat.color = colorPortal;
        // Activar emisión (funciona con URP/Lit y con Standard)
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colorPortal * intensidadEmision);
    }

    void CrearLuzPortal()
    {
        // Añadir una Point Light dinámica para que el portal ilumine el entorno
        GameObject luzGO = new GameObject("LuzPortal");
        luzGO.transform.SetParent(transform);
        luzGO.transform.localPosition = new Vector3(0f, 0f, -0.5f); // un poco hacia el jugador

        luzPortal            = luzGO.AddComponent<Light>();
        luzPortal.type       = LightType.Point;
        luzPortal.color      = colorPortal;
        luzPortal.intensity  = 2f;
        luzPortal.range      = 4f;
        luzPortal.shadows    = LightShadows.None;
    }
}
