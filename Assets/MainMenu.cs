using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button settingsButton;
    public Button quitButton;

    void Start()
    {
        // Assigning functions to button click events
        startButton.onClick.AddListener(StartGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);
    }

    void StartGame()
    {
        // Load the game scene (replace "GameScene" with the name of your game scene)
        SceneManager.LoadScene("GameScene");
    }

    void OpenSettings()
    {
        // Load the settings scene or open settings panel (implement as needed)
        SceneManager.LoadScene("SettingsScene");
    }

    void QuitGame()
    {
        // Quit the application
        Application.Quit();

        // If running in the Unity editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
