using UnityEngine;
using UnityEngine.UI;

public class WinUIHandler : MonoBehaviour
{
    [Header("Win UI Buttons")]
    public Button nextLevelButton;

    public Button restartButton;
    public Button goHomeButton; // You already have this one

    [Header("Optional UI Elements")]
    public Text levelCompleteText;

    public Text nextLevelText; // Shows "Next Level" or "All Levels Complete"

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        // Called when win UI is activated
        UpdateButtonStates();
    }

    private void SetupButtons()
    {
        // Setup Next Level button
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        // Setup Restart button
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        // Setup Go Home button (if you haven't already)
        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(OnGoHomeClicked);
    }

    private void UpdateButtonStates()
    {
        // Check if there's a next level available
        bool hasNextLevel = HasNextLevel();

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = hasNextLevel;
        }

        // Update button text based on availability
        if (nextLevelText != null)
        {
            nextLevelText.text = hasNextLevel ? "Next Level" : "All Complete!";
        }

        // Update level complete text
        if (levelCompleteText != null)
        {
            int currentLevel = LevelManager.Instance.CurrentLevelIndex + 1; // Convert to 1-based
            levelCompleteText.text = $"Level {currentLevel} Complete!";
        }
    }

    private bool HasNextLevel()
    {
        if (LevelManager.Instance == null)
            return false;

        int currentIndex = LevelManager.Instance.CurrentLevelIndex;
        int totalLevels = LevelManager.Instance.levelFiles.Length;

        return currentIndex + 1 < totalLevels;
    }

    public void OnNextLevelClicked()
    {
        if (HasNextLevel())
        {
            // Hide win UI
            gameObject.SetActive(false);

            // Load next level
            GameManager.Instance.LoadNextLevel();
        }
        else
        {
            // No more levels, go to level select or lobby
            OnGoHomeClicked();
        }
    }

    public void OnRestartClicked()
    {
        // Hide win UI
        gameObject.SetActive(false);

        // Restart current level
        GameManager.Instance.RestartLevel();
    }

    public void OnGoHomeClicked()
    {
        // Hide win UI
        gameObject.SetActive(false);

        // Return to level select or lobby
        GameManager.Instance.ReturnToLevelSelect();
    }
}