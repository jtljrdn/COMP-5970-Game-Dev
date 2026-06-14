using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController : MonoBehaviour
{
    public float acceleration = 5f;
    public float deceleration = 5f;
    public float maxSpeed = 6f;

    float turnSpeed = 200f;

    Rigidbody2D rb;

    Vector2 moveInput;

    float currentSpeed = 0f;

    public bool hasPackage = false;

    int score = 0;
    public TextMeshProUGUI scoreText;

    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;

    bool isGameOver = false;

    public SoundController soundController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnRestart()
    {
        if (!isGameOver)
        {
            return;
        }
        TimerController timerController = FindAnyObjectByType<TimerController>();
        timerController.StartTimer();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    void FixedUpdate()
    {

        float moveAmount = moveInput.y;
        if (moveAmount != 0)
        {
            currentSpeed += moveAmount * acceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
        }

        float turnAmount = -moveInput.x * turnSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation + turnAmount);

        rb.linearVelocity = transform.up * currentSpeed;
    }

    public void PickUpPackage()
    {
        hasPackage = true;
        soundController.PlayPickupSound();
        Debug.Log("Package picked up!");
    }

    public void DeliverPackage()
    {
        hasPackage = false;
        score++;
        scoreText.text = "Score: " + score;
        soundController.PlayDropoffSound();
        Debug.Log("Package delivered! Score: " + score);
    }

    public void GameOver()
    {
        Debug.Log("Game Over! Final Score: " + score);
        isGameOver = true;
        gameOverText.gameObject.SetActive(true);
        restartText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}
