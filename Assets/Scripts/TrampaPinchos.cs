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

        if (colisionadorDanio != null)
        {
            // El BoxCollider del prefab cubre y=-0.5..0.5 (suelo).
            // El jugador tiene su collider centrado en y=1.9 (cubre y=1.1..2.7).
            // Ajustamos el trigger para que cubra la altura del cuerpo del jugador.
            BoxCollider box = colisionadorDanio as BoxCollider;
            if (box != null)
            {
                box.center = new Vector3(0f, 1.9f, 0f);
                box.size   = new Vector3(1f, 1.8f, 1f);  // cubre y=1.0..2.8
            }
            // Convertir a trigger: detecta al jugador sin bloquearlo físicamente
            colisionadorDanio.isTrigger = true;
            // Estado inicial: pinchos escondidos, sin daño
            colisionadorDanio.enabled = false;
        }

        SetY(alturaEscondido);
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
        AudioManager.Instancia?.PlaySFX(AudioManager.Instancia.sfxPinchos);

        // Animamos la subida SIN activar daño (los pinchos aún no son visibles)
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
        // Activamos el daño solo cuando los pinchos están COMPLETAMENTE ARRIBA
        if (colisionadorDanio != null) colisionadorDanio.enabled = true;
    }

    IEnumerator Bajar()
    {
        // Desactivamos el daño ANTES de empezar a bajar: ya no son peligrosos
        if (colisionadorDanio != null) colisionadorDanio.enabled = false;

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
    }

    void SetY(float y)
    {
        Vector3 pos = objetoPinchos.localPosition;
        pos.y = y;
        objetoPinchos.localPosition = pos;
    }
}
