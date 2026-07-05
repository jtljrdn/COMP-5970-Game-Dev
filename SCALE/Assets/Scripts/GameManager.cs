using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;

    private bool isGameOver = false;
    private InputAction restartAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Own the Restart action in code so R works without a PlayerInput on this object.
        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
    }

    private void OnEnable()
    {
        restartAction?.Enable();
    }

    private void OnDisable()
    {
        restartAction?.Disable();
    }

    private void OnDestroy()
    {
        restartAction?.Dispose();
    }

    public void GameOver()
    {
        isGameOver = true;
        gameOverText.gameObject.SetActive(true);
        restartText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Restart()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverText.gameObject.SetActive(false);
        restartText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Update and input polling still run while paused (timeScale 0 only affects
        // physics and Time.deltaTime), so the restart check works during game over.
        if (isGameOver && restartAction.triggered)
        {
            Restart();
        }
    }
}
