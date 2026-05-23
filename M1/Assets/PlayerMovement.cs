using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    float horizontalInput = 0f;

    float moveSpeed = 5f;

    float xLimit = 8f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            horizontalInput = -1f;
        }
        else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            horizontalInput = 1f;
        }
        transform.position += Vector3.right * horizontalInput * moveSpeed * Time.deltaTime;
        float campedX = Mathf.Clamp(transform.position.x, -xLimit, xLimit);
        transform.position = new Vector3(campedX, transform.position.y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FindAnyObjectByType<GameManager>().GameOver();
    }
}
