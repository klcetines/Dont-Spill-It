using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
    }

    public void LoadNextScene()
    {
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = (currentSceneIndex + 1) % UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
    }

    public void LoadPreviousScene()
    {
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int previousSceneIndex = (currentSceneIndex - 1 + UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings) % UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        UnityEngine.SceneManagement.SceneManager.LoadScene(previousSceneIndex);
    }
}
