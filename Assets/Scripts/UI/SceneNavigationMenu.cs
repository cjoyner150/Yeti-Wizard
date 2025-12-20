using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationMenu : MonoBehaviour
{
    public void GoToScene(int sceneNum)
    {
        SceneManager.LoadScene(sceneNum);
    }

    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
