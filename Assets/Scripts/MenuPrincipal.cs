using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    // Build Settings: 0=MainMenu, 1=Creditos, 2=Nivel0, 3=Nivel1 ... 11=Nivel9

    void Update()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
                SceneManager.LoadScene(i + 2); // Nivel0 esta en indice 2
        }
    }

    public void Jugar()   => SceneManager.LoadScene(2); // Nivel0
    public void Creditos() => SceneManager.LoadScene(1); // Creditos
}
