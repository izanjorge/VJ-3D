using UnityEngine;

public class JarronDestruible : MonoBehaviour
{
    [Header("Configuración de Recompensas")]
    public GameObject monedaPrefab;
    [Range(0f, 1f)] public float probabilidadMoneda = 0.5f;

    [Header("Efectos Visuales")]
    public GameObject particulasExplosion; // NUEVO: Hueco para nuestro prefab de partículas

    public void Romper()
    {
        Transform raizPrefab = transform;
        while (raizPrefab.parent != null && raizPrefab.parent.GetComponent<LevelGenerator>() == null)
        {
            raizPrefab = raizPrefab.parent;
        }

        // 1. Instanciamos la moneda si toca
        if (Random.value <= probabilidadMoneda)
        {
            if (monedaPrefab != null)
            {
                Vector3 posicionSuelo = new Vector3(raizPrefab.position.x, 0f, raizPrefab.position.z);
                Instantiate(monedaPrefab, posicionSuelo, monedaPrefab.transform.rotation);
            }
        }

        // 2. NUEVO: Instanciamos las partículas de explosión en el sitio del jarrón
        if (particulasExplosion != null)
        {
            Instantiate(particulasExplosion, raizPrefab.position, Quaternion.identity);
        }

        // 3. Destruimos el jarrón
        Destroy(raizPrefab.gameObject);
    }
}