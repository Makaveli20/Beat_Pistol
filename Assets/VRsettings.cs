using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
public class VRsettings : MonoBehaviour
{
    public Slider volumeSlider;
    public AudioMixer audioMixer;
    public Button returnButton;

    void Start()
    {
        // Load the saved volume level from PlayerPrefs and set the slider value
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.75f);
        SetVolume(volumeSlider.value);

        // Add listener to slider to call SetVolume when value changes
        volumeSlider.onValueChanged.AddListener(SetVolume);

        // Add listener to return button to call ReturnToMainMenu when clicked
        returnButton.onClick.AddListener(ReturnToMainMenu);
    }

    public void SetVolume(float volume)
    {
        // Set the volume in the audio mixer
        audioMixer.SetFloat("Volume", Mathf.Log10(volume) * 20);

        // Save the volume level in PlayerPrefs
        PlayerPrefs.SetFloat("Volume", volume);
    }

    void ReturnToMainMenu()
    {
        // Load the main menu scene
        SceneManager.LoadScene("MainMenuScene"); // replace with your main menu scene name
    }
}