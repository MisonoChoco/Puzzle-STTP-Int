using UnityEngine;

public class LobbyButtonHandler : MonoBehaviour
{
    [Header("Button References")]
    public UnityEngine.UI.Button newGameButton;

    public UnityEngine.UI.Button continueButton;

    private void Start()
    {
        SetupButtons();
        UpdateContinueButton();
    }

    private void SetupButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void UpdateContinueButton()
    {
        if (continueButton != null)
        {
            // Enable continue button only if player has made progress
            bool hasProgress = ProgressManager.Instance.CurrentLevel > 1;
            continueButton.interactable = hasProgress;
        }
    }

    public void OnNewGameClicked()
    {
        GameSceneManager.Instance.StartNewGame();
    }

    public void OnContinueClicked()
    {
        GameSceneManager.Instance.ContinueGame();
    }
}