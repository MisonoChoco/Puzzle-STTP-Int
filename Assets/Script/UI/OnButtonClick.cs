using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Method 1: Using Unity's Button Component (Recommended)
public class CanvasButtonLoader : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene Loading")]
    public string sceneToLoad;

    [Header("Optional Button Reference")]
    public Button buttonComponent;

    private void Start()
    {
        // Method 1a: If you have a Button component, use it directly
        if (buttonComponent != null)
        {
            buttonComponent.onClick.AddListener(LoadScene);
        }

        // Method 1b: Try to get Button component from this GameObject
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(LoadScene);
        }
    }

    // Method 1c: Implement IPointerClickHandler for custom click detection
    public void OnPointerClick(PointerEventData eventData)
    {
        LoadScene();
    }

    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}