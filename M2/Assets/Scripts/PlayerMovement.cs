using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{

    float moveSpeed = 5f;
    float jumpForce = 7f;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;

    int maxJumps = 2;
    int jumpsRemaining;

    GameManager gameManager;
    bool isDead = false;
    bool hasWon = false;

    public GameObject[] youDiedMessages;
    public GameObject[] youWinMessages;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        jumpsRemaining = maxJumps;
        gameManager = FindAnyObjectByType<GameManager>();

        youDiedMessages = GameObject.FindGameObjectsWithTag("You Died");
        foreach (GameObject message in youDiedMessages)
        {
            message.SetActive(false);
        }

        youWinMessages = GameObject.FindGameObjectsWithTag("You Win");
        foreach (GameObject message in youWinMessages)
        {
            message.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float moveInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput = -1f; // Move left
        }
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput = 1f; // Move right
        }
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if ((Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }

        if (moveInput < 0)
        {
            spriteRenderer.flipX = true; // Flip sprite to face left
        }
        else if (moveInput > 0)
        {
            spriteRenderer.flipX = false; // Flip sprite to face right
        }

        if (isDead && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
        if (hasWon && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpsRemaining = maxJumps; // Reset jumps when touching the ground
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hazard"))
        {
            HandleDeath();
        }
        if (other.CompareTag("Apple"))
        {
            if (gameManager != null)
            {
                gameManager.HandleCollection(other.gameObject);
            }
        }
        if (other.CompareTag("Goal") && gameManager.canFinish)
        {
            Debug.Log("Level completed! You win!");
            // Show win message
            foreach (GameObject message in youWinMessages)
            {
                message.SetActive(true);
            }
            Time.timeScale = 0f; // Pause the game
        }
    }

    void HandleDeath()
    {
        Debug.Log("You Died!");
        // Show death message
        foreach (GameObject message in youDiedMessages)
        {
            message.SetActive(true);
        }
        isDead = true;
        Time.timeScale = 0f; // Pause the game
    }

    void RestartLevel()
    {
        Time.timeScale = 1f; // Resume the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
