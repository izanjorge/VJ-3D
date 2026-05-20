using UnityEngine;
using System.Collections;

public class TrampaPinchos : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetoPinchos;

    [Header("Tiempos del Ciclo")]
    public float tiempoEsperaBajado = 2f;
    public float tiempoEsperaSubido = 1f;

    [Header("Animación")]
    public float duracionSubida = 0.9f;
    public float duracionBajada = 1.2f;

    [Header("Posiciones")]
    public float alturaEscondido = -1f;
    public float alturaFuera = 1f;

    // El colisionador de daño está en ESTE objeto (junto a DetectorGolpe)
    private Collider colisionadorDanio;

    void Start()
    {
        colisionadorDanio = GetComponent<Collider>();

        // Estado inicial: pinchos escondidos y sin colisión de daño
        SetY(alturaEscondido);
        if (colisionadorDanio != null)
            colisionadorDanio.enabled = false;

        StartCoroutine(CicloTrampa());
    }

    IEnumerator CicloTrampa()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEsperaBajado);
            yield return Subir();
            yield return new WaitForSeconds(tiempoEsperaSubido);
            yield return Bajar();
        }
    }

    IEnumerator Subir()
    {
        // Activamos el daño en cuanto empiezan a salir
        if (colisionadorDanio != null) colisionadorDanio.enabled = true;

        float tiempo = 0f;
        while (tiempo < duracionSubida)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionSubida);
            // SmoothStep (S-curve): emerge despacio, acelera en el centro, frena al llegar
            float tSmooth = t * t * (3f - 2f * t);
            SetY(Mathf.Lerp(alturaEscondido, alturaFuera, tSmooth));
            yield return null;
        }
        SetY(alturaFuera);
    }

    IEnumerator Bajar()
    {
        float tiempo = 0f;
        while (tiempo < duracionBajada)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionBajada);
            // Ease-out (1-(1-t)²): rápido al principio, frena al llegar abajo
            SetY(Mathf.Lerp(alturaFuera, alturaEscondido, 1f - (1f - t) * (1f - t)));
            yield return null;
        }
        SetY(alturaEscondido);

        // Solo quitamos el daño cuando están completamente escondidos
        if (colisionadorDanio != null) colisionadorDanio.enabled = false;
    }

    void SetY(float y)
    {
        Vector3 pos = objetoPinchos.localPosition;
        pos.y = y;
        objetoPinchos.localPosition = pos;
    }
}
