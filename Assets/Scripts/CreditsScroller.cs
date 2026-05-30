using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScroller : MonoBehaviour
{
    [SerializeField] private RectTransform contenedor;
    [SerializeField] private float velocidad = 65f;
    [SerializeField] private float finScroll = 1800f;
    [SerializeField] private float inicioY = -700f;

    void Update()
    {
        contenedor.anchoredPosition += Vector2.up * velocidad * Time.deltaTime;

        if (contenedor.anchoredPosition.y > finScroll)
            contenedor.anchoredPosition = new Vector2(0f, inicioY);

        if (Input.GetKeyDown(KeyCode.Escape))
            VolverAlMenu();
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene(0);
    }
}
