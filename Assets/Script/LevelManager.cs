using Newtonsoft.Json;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Files")]
    public TextAsset[] levelFiles; // Assign levels in order here

    [Header("Quick Test (Optional)")]
    public TextAsset levelToTest; // Drag a file to test one level only

    public int CurrentLevelIndex { get; private set; } = 0;

    private bool hasLoadedLevel = false; // Prevent double loading

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Only load level once, and only if we haven't loaded yet
        if (!hasLoadedLevel)
        {
            if (levelToTest != null)
            {
                Debug.Log("Loading test level via quick test...");
                LoadLevel(levelToTest);
            }
            else
            {
                // Load the level specified by scene transition, or default to level 0
                int levelToLoad = PlayerPrefs.GetInt("LevelToLoad", 1);
                LoadLevel(levelToLoad - 1); // Convert to 0-based index
            }
        }
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levelFiles.Length)
        {
            Debug.LogError($"Invalid level index {index}");
            return;
        }

        CurrentLevelIndex = index;
        hasLoadedLevel = true;
        LoadLevel(levelFiles[index]);
    }

    public void LoadLevel(TextAsset json)
    {
        if (json == null)
        {
            Debug.LogError("Level JSON is null.");
            return;
        }

        hasLoadedLevel = true;
        LevelJsonData data = JsonConvert.DeserializeObject<LevelJsonData>(json.text);

        Debug.Log($"Loading level file: {json.name}");
        if (data == null)
        {
            Debug.LogError("Failed to parse level JSON data.");
            return;
        }

        // Clear any existing state first
        if (GridValidator.Instance != null)
            GridValidator.Instance.Initialize(data.width, data.height);
        else
            Debug.LogError("GridValidator.Instance is null!");

        if (GridManager.Instance != null)
        {
            GridManager.Instance.GenerateLevel(data, out DogController dog, out StickController stick);

            // Init gameplay only if we have valid components
            if (GameManager.Instance != null)
                GameManager.Instance.Initialize(dog, stick);
            else if (GameManager.Instance != null)
                GameManager.Instance.Initialize(dog, stick);

            if (UndoManager.Instance != null)
                UndoManager.Instance.Initialize(dog, stick);

            // Setup camera
            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null && dog != null)
                cam.SetFollowTarget(dog.transform);
        }
        else
        {
            Debug.LogError("GridManager.Instance is null!");
        }
    }

    public void CompleteLevel()
    {
        // Save progress when level is completed
        ProgressManager.Instance.SaveProgress(CurrentLevelIndex + 1); // Convert to 1-based

        // Optional: Auto-load next level or return to level select
        LoadNextLevelOrReturn();
    }

    private void LoadNextLevelOrReturn()
    {
        int nextLevel = CurrentLevelIndex + 1;
        if (nextLevel < levelFiles.Length)
        {
            // Load next level after a delay
            Invoke("LoadNextLevel", 2f);
        }
        else
        {
            // All levels completed, return to lobby
            Invoke("ReturnToLobby", 2f);
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = CurrentLevelIndex + 1;
        if (nextIndex < levelFiles.Length)
        {
            GameSceneManager.Instance.LoadGameplayLevel(nextIndex + 1);
        }
    }

    public void ReturnToLobby()
    {
        GameSceneManager.Instance.ReturnToLobby();
    }

    public void RestartLevel()
    {
        LoadLevel(CurrentLevelIndex);
    }
}