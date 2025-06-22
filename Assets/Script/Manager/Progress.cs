using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    [Header("Progress Settings")]
    public int totalLevels = 10; // Set this to your total number of levels

    private const string PROGRESS_KEY = "GameProgress";
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";

    public int CurrentLevel { get; private set; } = 1;
    public int HighestUnlockedLevel { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveProgress(int levelCompleted)
    {
        CurrentLevel = levelCompleted + 1; // Next level to play
        HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel, CurrentLevel);

        // Clamp to available levels
        CurrentLevel = Mathf.Clamp(CurrentLevel, 1, totalLevels);
        HighestUnlockedLevel = Mathf.Clamp(HighestUnlockedLevel, 1, totalLevels);

        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, CurrentLevel);
        PlayerPrefs.SetInt(PROGRESS_KEY, HighestUnlockedLevel);
        PlayerPrefs.Save();

        Debug.Log($"Progress saved - Current: {CurrentLevel}, Highest: {HighestUnlockedLevel}");
    }

    public void LoadProgress()
    {
        CurrentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);
        HighestUnlockedLevel = PlayerPrefs.GetInt(PROGRESS_KEY, 1);

        Debug.Log($"Progress loaded - Current: {CurrentLevel}, Highest: {HighestUnlockedLevel}");
    }

    public void ResetProgress()
    {
        CurrentLevel = 1;
        HighestUnlockedLevel = 1;

        PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
        PlayerPrefs.DeleteKey(PROGRESS_KEY);
        PlayerPrefs.Save();

        Debug.Log("Progress reset to level 1");
    }

    public bool IsLevelUnlocked(int level)
    {
        return level <= HighestUnlockedLevel;
    }
}