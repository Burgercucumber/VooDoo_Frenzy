using UnityEngine;
using UnityEngine.SceneManagement;

public class Escenas : MonoBehaviour
{
    public void MainMenu()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }

    public void Menu()
    {
        SceneManager.LoadSceneAsync("Menu");
    }

    public void Tienda()
    {
        SceneManager.LoadSceneAsync("Tienda");
    }

    public void Personalizacion()
    {
        SceneManager.LoadSceneAsync("Personalizacion");
    }

    public void Batalla()
    {
        SceneManager.LoadSceneAsync("CampoDeBatalla");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
