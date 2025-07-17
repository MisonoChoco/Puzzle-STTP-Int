using UnityEngine;

// ===============================
// COMPLETE ENHANCED GAME MANAGER - Dog Must Hold Stick on Goal
// ===============================
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private DogController dog;
    private StickController stick;

    [Header("UI References")]
    public GameObject winUI;

    public WinUIHandler winUIHandler;

    [Header("Win Detection Settings")]
    public bool debugWinCheck = true;

    public float stickHoldDistance = 1.0f; // Distance threshold for "holding" stick

    [Header("Audio/Effects (Optional)")]
    public AudioSource winSound;

    public GameObject stickBumpEffect;
    public GameObject winEffect;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (winUIHandler == null && winUI != null)
            winUIHandler = winUI.GetComponent<WinUIHandler>();

        if (winUI != null && winUI.activeInHierarchy)
        {
            Debug.LogWarning("Win UI was active at start - hiding it now");
            winUI.SetActive(false);
        }
    }

    public void Initialize(DogController d, StickController s)
    {
        dog = d;
        stick = s;

        if (winUI != null)
            winUI.SetActive(false);

        Debug.Log($"[GameManager] Initialized with Dog: {dog?.name}, Stick: {stick?.name}");
    }

    public static void PlayEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null) return;

        GameObject instance = GameObject.Instantiate(effectPrefab, position, Quaternion.identity);

        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            GameObject.Destroy(instance, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // fallback destroy if it's not a particle system
            GameObject.Destroy(instance, 2f);
        }
    }

    public void CheckForWin()
    {
        if (dog == null)
        {
            if (debugWinCheck) Debug.Log("[GameManager] Win Check: Dog is null");
            return;
        }

        if (stick == null)
        {
            if (debugWinCheck) Debug.Log("[GameManager] Win Check: Stick is null");
            return;
        }

        //Dog must be holding stick AND on goal tile
        bool dogHasStick = IsDogHoldingStick();
        bool dogOnGoal = IsDogOnGoal();

        if (debugWinCheck)
        {
            Debug.Log($"[GameManager] Win Check - Dog has stick: {dogHasStick}, Dog on goal: {dogOnGoal}");
        }

        // WIN CONDITION: Both must be true
        if (dogHasStick && dogOnGoal)
        {
            if (debugWinCheck) Debug.Log("[GameManager] WIN: Dog is holding stick AND on goal tile!");
            OnLevelComplete();
        }
        else
        {
            if (debugWinCheck)
            {
            }
        }
    }

    private bool IsDogHoldingStick()
    {
        if (stick.holder == dog)
        {
            if (debugWinCheck) Debug.Log("[GameManager]dog holding stick");
            return true;
        }

        return false;
    }

    private bool IsDogOnGoal()
    {
        // Get dog's grid position
        Vector2Int dogGridPos = dog.gridPos; // Use dog's grid position directly

        // Check if position is within bounds
        if (!GridValidator.Instance.IsWithinBounds(dogGridPos))
        {
            if (debugWinCheck) Debug.Log($"[GameManager] Win Check: Dog position {dogGridPos} is out of bounds");
            return false;
        }

        // Check if the tile is a goal tile
        TileType tileType = GridValidator.Instance.mapData[dogGridPos.x, dogGridPos.y];
        bool isGoal = tileType == TileType.Goal;

        return isGoal;
    }

    private void OnLevelComplete()
    {
        Debug.Log("[GameManager] LEVEL COMPLETE! Dog brought stick to goal!");

        // Play win effects
        if (winSound != null)
            winSound.Play();

        // Show win UI after a small delay
        Invoke("ShowWinUI", 0.5f);

        // Save progress (if you have level management system)
        if (LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevel();
    }

    private void ShowWinUI()
    {
        if (winUI != null)
        {
            winUI.SetActive(true);
        }
    }

    // Button methods for win UI
    public void RestartLevel()
    {
        Debug.Log("[GameManager] Restart Level requested");
        if (LevelManager.Instance != null)
            LevelManager.Instance.RestartLevel();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        Debug.Log("[GameManager] Next Level requested");
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadNextLevel();
        else
            Debug.LogWarning("[GameManager] No LevelManager found for next level");
    }

    public void ReturnToLevelSelect()
    {
        Debug.Log("[GameManager] Return to Level Select requested");
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.GoToLevelSelect();
        else
            Debug.LogWarning("[GameManager] No GameSceneManager found");
    }

    public void ReturnToLobby()
    {
        Debug.Log("[GameManager] Return to Lobby requested");
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.ReturnToLobby();
        else
            Debug.LogWarning("[GameManager] No GameSceneManager found");
    }

    // Additional utility methods
    public bool IsGameWon()
    {
        return winUI != null && winUI.activeInHierarchy;
    }

    public void ForceWinCheck()
    {
        Debug.Log("[GameManager] Force win check triggered");
        CheckForWin();
    }

    // Method to manually set references if needed
    public void SetReferences(DogController dogRef, StickController stickRef)
    {
        dog = dogRef;
        stick = stickRef;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Debug method to check current state
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== GAME MANAGER DEBUG STATE ===");
        Debug.Log($"Dog: {(dog != null ? dog.name + " at " + dog.gridPos : "NULL")}");
        Debug.Log($"Stick: {(stick != null ? stick.name + " holder=" + (stick.holder != null ? stick.holder.name : "NONE") : "NULL")}");
        Debug.Log($"Win UI Active: {(winUI != null ? winUI.activeInHierarchy.ToString() : "NULL")}");

        if (dog != null && stick != null)
        {
            bool hasStick = IsDogHoldingStick();
            bool onGoal = IsDogOnGoal();
            Debug.Log($"Dog holding stick: {hasStick}");
            Debug.Log($"Dog on goal: {onGoal}");
            Debug.Log($"Win condition met: {hasStick && onGoal}");
        }
        Debug.Log("================================");
    }
}