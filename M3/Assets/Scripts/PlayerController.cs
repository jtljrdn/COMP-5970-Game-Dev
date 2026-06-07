using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{

    float moveSpeed = 5f;
    Vector2 moveInput;

    float minX = -2.5f;
    float maxX = 2.5f;
    float minY = -4.5f;
    float maxY = 4.5f;

    public GameObject bulletPrefab;
    public Transform firePoint;

    float fireRate = 0.25f;
    float nextFire = 0f;

    public AudioSource audioSource;
    public AudioClip shootClip;
    public AudioClip explosionClip;

    int playerLives = 3;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;
    int score = 0;
    public TextMeshProUGUI scoreText;


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack()
    {
        if (Time.time >= nextFire)
        {
            audioSource.PlayOneShot(shootClip);
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            nextFire = Time.time + fireRate;
        }
    }

    public void OnRestart()
    {
        if (gameOverText.enabled)
        {
            Time.timeScale = 1f;
            SetScore(0);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameOverText.enabled = false;
        restartText.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += moveSpeed * Time.deltaTime * movement;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    public void DecreaseLives()
    {
        playerLives--;
        Debug.Log("Player lives: " + playerLives);
        HealthUI healthUI = FindAnyObjectByType<HealthUI>();
        if (healthUI != null)
        {
            healthUI.UpdateHealth(playerLives);
        }
        if (playerLives <= 0)
        {

            GameOver();
        }
    }

    public void GameOver()
    {
        audioSource.PlayOneShot(explosionClip);
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
        gameOverText.enabled = true;
        restartText.enabled = true;
    }

    public void IncreaseScore(int amount)
    {
        score += amount;
        scoreText.text = "SCORE: " + score;
    }

    public void SetScore(int points)
    {
        score = points;
        scoreText.text = "SCORE: " + score;
    }
}
