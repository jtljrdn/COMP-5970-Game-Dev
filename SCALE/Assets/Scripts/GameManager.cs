using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Headline shown when the run ends, either way.")]
    public TextMeshProUGUI gameOverText;

    public TextMeshProUGUI restartText;

    public TextMeshProUGUI levelText;

    [Tooltip("Shown when the player escapes the chamber.")]
    public string completeMessage = "CHAMBER CLEARED";

    [Tooltip("Shown when the player dies.")]
    public string deathMessage = "SUBJECT TERMINATED";

    [Tooltip("References to the player spawner for each level. Index 0 is the first level in the scene, index 1 is the second, etc.")]
    public Transform[] playerSpawners;

    [Tooltip("The player. Either the capsule itself or a parent of it - the CharacterController underneath is what actually gets moved.")]
    public GameObject player;

    private CharacterController playerController;
    private int currentLevelIndex = 0;

    public bool IsGameOver => isGameOver;

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

        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");

        // Resolved once so the spawn logic doesn't depend on whether the capsule or
        // one of its parents was dragged into the inspector slot.
        if (player != null)
        {
            playerController = player.GetComponentInChildren<CharacterController>();
        }

        if (levelText != null)
        {
            levelText.text = $"CHAMBER {currentLevelIndex + 1}";
        }
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

    public void LevelComplete()
    {
        LoadNextLevel();
    }

    // The player was killed by a hazard.
    public void PlayerDied()
    {
        EndRun(deathMessage);
    }

    private void EndRun(string message)
    {
        // Hazards can fire every physics tick, so the first result wins and the
        // rest are ignored.
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        if (gameOverText != null)
        {
            gameOverText.text = message;
            gameOverText.gameObject.SetActive(true);
        }

        if (restartText != null)
        {
            restartText.gameObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    private void LoadNextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= playerSpawners.Length)
        {
            EndRun("ALL CHAMBERS CLEARED");
            return;
        }

        TeleportPlayer(playerSpawners[currentLevelIndex]);

        if (levelText != null)
        {
            levelText.text = $"CHAMBER {currentLevelIndex + 1}";
        }
    }
    // Moves the player to a spawn point.
    //
    // Two things make this less trivial than it looks:
    //   1. The object dragged into the player slot may be a parent wrapper rather
    //      than the capsule itself. Moving the wrapper leaves the capsule sitting
    //      at its own local offset from the destination, which can be metres away.
    //      So the object that actually owns the CharacterController is what moves.
    //   2. A CharacterController caches its position internally and writes it back
    //      over the transform, silently undoing direct position writes. It has to
    //      be disabled across the move.
    private void TeleportPlayer(Transform destination)
    {
        if (destination == null || player == null)
        {
            Debug.LogWarning("TeleportPlayer: missing destination or player reference.");
            return;
        }

        Transform target = playerController != null ? playerController.transform : player.transform;

        bool wasEnabled = playerController != null && playerController.enabled;
        if (wasEnabled)
        {
            playerController.enabled = false;
        }

        target.SetPositionAndRotation(destination.position, destination.rotation);

        if (wasEnabled)
        {
            playerController.enabled = true;
        }
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
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        if (restartText != null)
        {
            restartText.gameObject.SetActive(false);
        }
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
