using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartPromptText;
    public TextMeshProUGUI menuPromptText;
    private float elapsed = 0f;

    private bool isGameOver = false;

    void Start()
    {
        elapsed = 0f;
        UpdateScoreText();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        int score = Mathf.FloorToInt(elapsed);
        scoreText.text = $"SCORE: {score}";
    }

    public void PenalizeScore(float penalty)
    {
        Debug.Log($"Penalty applied: {penalty} seconds");
        elapsed -= Mathf.FloorToInt(penalty);
        UpdateScoreText();
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
        gameOverText.gameObject.SetActive(true);
        restartPromptText.gameObject.SetActive(true);
        menuPromptText.gameObject.SetActive(true);
        isGameOver = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
        isGameOver = false;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
        isGameOver = false;
    }
}