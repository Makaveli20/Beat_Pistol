using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public Text scoreText; // Reference to the UI Text element for displaying the score
    private int score = 0; // Player's score

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

        // Find the ScoreText object in the scene and assign it
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText").GetComponent<Text>();

        // Initialize the score display
        UpdateScoreDisplay();
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}