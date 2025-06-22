using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Level Settings")]
    public int levelIndex = 1; // Set this in inspector for each button

    [Header("UI References")]
    public UnityEngine.UI.Button button;

    public TMPro.TextMeshProUGUI levelText;
    public UnityEngine.UI.Image lockIcon; // Optional lock icon

    private void Start()
    {
        if (button == null)
            button = GetComponent<UnityEngine.UI.Button>();

        SetupButton();
        UpdateButtonState();
    }

    private void SetupButton()
    {
        if (button != null)
            button.onClick.AddListener(OnLevelButtonClicked);
    }

    private void UpdateButtonState()
    {
        bool isUnlocked = ProgressManager.Instance.IsLevelUnlocked(levelIndex);

        if (button != null)
            button.interactable = isUnlocked;

        if (levelText != null)
        {
            levelText.text = isUnlocked ? levelIndex.ToString() : "?";
            levelText.color = isUnlocked ? Color.white : Color.gray;
        }

        if (lockIcon != null)
            lockIcon.gameObject.SetActive(!isUnlocked);
    }

    public void OnLevelButtonClicked()
    {
        if (ProgressManager.Instance.IsLevelUnlocked(levelIndex))
        {
            GameSceneManager.Instance.LoadGameplayLevel(levelIndex);
        }
    }
}