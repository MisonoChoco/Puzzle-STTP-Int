using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";

    public string levelSelectSceneName = "Level";
    public string gameplaySceneName = "Gameplay";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewGame()
    {
        ProgressManager.Instance.ResetProgress();
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void ContinueGame()
    {
        // Load the current level directly
        int currentLevel = ProgressManager.Instance.CurrentLevel;
        LoadGameplayLevel(currentLevel);
    }

    public void GoToLevelSelect()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void LoadGameplayLevel(int levelIndex)
    {
        // Store which level to load
        PlayerPrefs.SetInt("LevelToLoad", levelIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ReturnToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
    }
}