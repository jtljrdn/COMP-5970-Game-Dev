using System.Collections;
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

    [Tooltip("Chamber scene names, in play order. Every one must be added to Build Settings or the load silently fails in a build.")]
    public string[] chamberSceneNames;

    [Tooltip("The player. Either the capsule itself or a parent of it - the CharacterController underneath is what actually gets moved.")]
    public GameObject player;

    [Tooltip("Name of the object inside each chamber scene marking where the player appears.")]
    public string spawnPointName = "SpawnPoint";

    [Header("Loading")]
    [Tooltip("Optional. One is built at runtime when this is left empty.")]
    public LoadingScreen loadingScreen;

    [Tooltip("Seconds the opening fade from black takes.")]
    public float startFadeDuration = 1f;

    [Tooltip("Seconds the fade back in takes once a chamber has loaded.")]
    public float chamberFadeDuration = 0.35f;

    private CharacterController playerController;
    private ScaleTool playerScaleTool;
    private int currentLevelIndex = -1;

    // The scene this GameManager lives in. It stays loaded for the whole run and
    // owns the player, UI and cameras; chambers come and go on top of it.
    private string bootstrapSceneName;
    private Scene loadedChamber;

    // Scene loading is async, so guard against a second request arriving mid-swap.
    private bool isSwitching;

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

        bootstrapSceneName = gameObject.scene.name;

        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");

        if (player != null)
        {
            playerController = player.GetComponentInChildren<CharacterController>();
            playerScaleTool = player.GetComponentInChildren<ScaleTool>();
        }

        if (loadingScreen == null)
        {
            loadingScreen = LoadingScreen.Build(transform);
        }

        // Black from the very first frame, so the empty bootstrap scene is never
        // on screen while chamber one loads.
        loadingScreen.Cover(false);
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

    public void PlayerDied()
    {
        EndRun(deathMessage);
    }

    private void EndRun(string message)
    {
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
        if (isSwitching)
        {
            return;
        }

        int next = currentLevelIndex + 1;
        if (chamberSceneNames == null || next >= chamberSceneNames.Length)
        {
            EndRun("ALL CHAMBERS CLEARED");
            return;
        }

        StartCoroutine(SwitchChamber(next));
    }

    private IEnumerator SwitchChamber(int index)
    {
        isSwitching = true;

        // The opening load is already behind a black screen from Awake. A swap
        // mid-run covers the screen and pauses instead, so the few frames with no
        // chamber in the world are never seen or played through.
        bool firstLoad = currentLevelIndex < 0;
        if (!firstLoad)
        {
            loadingScreen.Cover(true);
            Time.timeScale = 0f;
        }

        SetPlayerSuspended(true);

        Scene bootstrap = SceneManager.GetSceneByName(bootstrapSceneName);
        if (bootstrap.IsValid() && bootstrap.isLoaded)
        {
            SceneManager.SetActiveScene(bootstrap);
        }

        if (loadedChamber.IsValid() && loadedChamber.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(loadedChamber);
        }

        string sceneName = chamberSceneNames[index];
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        Scene chamber = SceneManager.GetSceneByName(sceneName);
        if (!chamber.IsValid() || !chamber.isLoaded)
        {
            Debug.LogError($"GameManager: chamber scene '{sceneName}' failed to load. " +
                           "Check the name matches the scene asset and that it is listed in Build Settings.");
            Resume();
            isSwitching = false;
            yield return loadingScreen.Reveal(chamberFadeDuration);
            yield break;
        }

        AdoptChamber(chamber, index);

        yield return null;

        Resume();
        isSwitching = false;

        yield return loadingScreen.Reveal(firstLoad ? startFadeDuration : chamberFadeDuration);
    }

    private void Resume()
    {
        SetPlayerSuspended(false);

        // Game over does its own pausing and must not be undone here.
        if (!isGameOver)
        {
            Time.timeScale = 1f;
        }
    }

    private void AdoptChamber(Scene chamber, int index)
    {
        SceneManager.SetActiveScene(chamber);
        loadedChamber = chamber;
        currentLevelIndex = index;

        TeleportPlayer(FindSpawnPoint(chamber));

        if (levelText != null)
        {
            levelText.text = $"CHAMBER {currentLevelIndex + 1}";
        }
    }

    private void SetPlayerSuspended(bool suspended)
    {
        if (playerController != null)
        {
            playerController.enabled = !suspended;
        }

        // Timescale alone wouldn't stop this one: it works off Update and would
        // let the player grab and scale things through the loading screen.
        if (playerScaleTool != null)
        {
            playerScaleTool.enabled = !suspended;
        }
    }

    private Transform FindSpawnPoint(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == spawnPointName)
            {
                return root.transform;
            }

            Transform found = FindDescendant(root.transform, spawnPointName);
            if (found != null)
            {
                return found;
            }
        }

        Debug.LogWarning($"GameManager: no '{spawnPointName}' found in scene '{scene.name}'.");
        return null;
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = FindDescendant(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

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

        // Reload the bootstrap scene, not the active one. Once a chamber is loaded it
        // *is* the active scene, so reloading that would tear down the player, the UI
        // and this GameManager along with it.
        SceneManager.LoadScene(bootstrapSceneName, LoadSceneMode.Single);
    }


    private void Quit()
    {
        Debug.Log("Quitting game.");
        Application.Quit();
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

        if (chamberSceneNames == null || chamberSceneNames.Length == 0)
        {
            Debug.LogWarning("GameManager: chamberSceneNames is empty - no chambers will load.");
            StartCoroutine(loadingScreen.Reveal(startFadeDuration));
            return;
        }

        // If chamber one is already open, adopt it instead of loading a second copy.
        // That is the case whenever it is open alongside this scene in the editor
        // (multi-scene editing), and it means play mode starts with the chamber
        // already there rather than blank for the frames a load takes.
        Scene preloaded = SceneManager.GetSceneByName(chamberSceneNames[0]);
        if (preloaded.IsValid() && preloaded.isLoaded)
        {
            AdoptChamber(preloaded, 0);
            StartCoroutine(loadingScreen.Reveal(startFadeDuration));
        }
        else
        {
            StartCoroutine(SwitchChamber(0));
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

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Quit();
        }
    }
}
