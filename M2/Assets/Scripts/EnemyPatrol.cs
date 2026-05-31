using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveDistance = 3f;

    Vector3 startingPosition;
    int direction = 1; // 1 for right, -1 for left
    SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startingPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += direction * moveSpeed * Time.deltaTime * Vector3.right;
        float distanceFromStart = transform.position.x - startingPosition.x;
        if (Mathf.Abs(distanceFromStart) >= moveDistance)
        {
            direction *= -1; // Reverse direction
        }
        if (direction > 0)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
}
